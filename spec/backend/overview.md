# Backend — Architecture Overview

## Project structure

```
backend/src/
├── infrastructure/Tenengroup.Infrastructure.Common       # shared kernel, reusable by both APIs
└── app/
    ├── Tenengroup.FactoryCatalog.Api                     # HTTP entry point
    ├── Tenengroup.FactoryCatalog.Business.Abstractions   # CQRS contracts, DTOs, service interfaces
    ├── Tenengroup.FactoryCatalog.Business                # handlers + services
    ├── Tenengroup.FactoryCatalog.Domains                 # entities + enums
    ├── Tenengroup.FactoryCatalog.DataAccess.Abstractions # IFactoryCatalogDbContext
    ├── Tenengroup.FactoryCatalog.DataAccess              # EF Core + migrations
    └── Tenengroup.FactoryCatalog.CompositionRoot         # DI wiring, Hangfire, recurring jobs
```

References: `Api → CompositionRoot → Business → Business.Abstractions → Common`; `Business → DataAccess.Abstractions → Domains`;
`CompositionRoot → DataAccess`. Business never references `DataAccess` (only the context interface).

## Layers

```
HTTP request
    │
[Api]        controllers, request models                  → IRequestRunner
[Business]   command / query handlers, services           → IFactoryCatalogDbContext
[Domains]    entities, enums
[DataAccess] FactoryCatalogDbContext (PostgreSQL, snake_case)
```

## CQRS

`IRequest`, `IRequest<TResult>`, `IRequestHandler<,>`, `IRequestRunner` (`HandlerBasedRequestRunner`) live in
`Tenengroup.Infrastructure.Common.Mediators`. The runner resolves `IRequestHandler<TRequest, TResult>` from DI.

```csharp
public record GetFactoriesQuery : IRequest<IReadOnlyCollection<FactoryDto>>;

public class GetFactoriesQueryHandler(IFactoryCatalogDbContext db)
    : IRequestHandler<GetFactoriesQuery, IReadOnlyCollection<FactoryDto>> { ... }

services.AddScoped<IRequestHandler<GetFactoriesQuery, IReadOnlyCollection<FactoryDto>>, GetFactoriesQueryHandler>();

requestRunner.Run<GetFactoriesQuery, IReadOnlyCollection<FactoryDto>>(new GetFactoriesQuery(), cancellationToken);
```

## Dependency injection and module extension points

`CompositionRoot/Root.cs` (`RegisterRoot`) registers the mediator, `TimeProvider.System`, the DbContext, AWS,
Hangfire and then calls one extension method per module. Each lives in its own file so that parallel work never
touches the same file:

| File | Method |
|---|---|
| `CompositionRoot/Modules/SharedModule.cs` | `AddSharedModule()` (foundation) |
| `CompositionRoot/Modules/ComponentsModule.cs` | `AddComponentsModule(IConfiguration)` |
| `CompositionRoot/Modules/ProcessesModule.cs` | `AddProcessesModule(IConfiguration)` |
| `CompositionRoot/Modules/SkusModule.cs` | `AddSkusModule(IConfiguration)` |
| `CompositionRoot/Modules/QuotesModule.cs` | `AddQuotesModule(IConfiguration)` |
| `CompositionRoot/Modules/LegacyBalancerModule.cs` | `AddLegacyBalancerModule(IConfiguration)` |

Recurring jobs are registered at startup by `CompositionRoot/Jobs/RecurringJobsHostedService.cs`, which calls
`RegisterComponentsJobs`, `RegisterProcessesJobs`, `RegisterSkusJobs`, `RegisterQuotesJobs`,
`RegisterLegacyBalancerJobs` (`CompositionRoot/Jobs/<Module>Jobs.cs`, signature
`(this IRecurringJobManager recurringJobs, IConfiguration configuration)`).

Everything is `AddScoped` and explicit. There is no attribute scanning.

## Startup pipeline (`Api/Startup.cs`)

ConfigureServices: `RegisterRoot` → `AddTenengroupAuth` → `AddTenengroupCors` → `AddTenengroupControllers` → `AddTenengroupSwagger`.

Middleware order: developer exception page (dev) → Swagger UI at `/` → `/ping` → correlation id → routing → CORS
(`corsapp`) → global exception handler → authentication → authorization → controllers (all require authorization)
and the Hangfire dashboard at `/hangfire`.

`dotnet Tenengroup.FactoryCatalog.Api.dll migrate` applies EF migrations and exits.

## Authentication and authorization

Copied from the shipment service design, implemented once in `Tenengroup.Infrastructure.Common.Auth` /
`.Permissions` so that the Balancer API reuses it (`services.AddTenengroupAuth(configuration)` plus its own
`IPermissionEnforcementToggle` registration).

- One `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` chain: `Basic` scheme
  (`BasicAuthenticationHandler`, credentials `API_SERVICE_USERNAME` / `API_SERVICE_USER_PASSWORD`) then
  `AddMicrosoftIdentityWebApi` (section `AzureAd`). Do not add a second `AddAuthentication` call.
- `[AzureAuthorize]` = any authenticated caller; `[AzureAuthorize(Permission.X)]` = caller needs the permission.
- `AzureAuthorizationHandler`: Basic callers always succeed; unauthenticated fail; when the
  `permissions.enable_all` feature toggle is **off** (no enabled row — the default) every authenticated JWT user
  succeeds; otherwise token `groups` are resolved to roles through `GroupPermissions.json`
  (`GROUP_PERMISSIONS_URL`, cached 20 minutes, **fail closed** when it cannot be loaded or a group is restricted)
  and checked with `PermissionsHelper.HasPermission`.
- `ICurrentUser` (`Business.Abstractions/Shared/Users`) is the accessor for handlers:

```csharp
public interface ICurrentUser
{
    string Email { get; }              // lower-cased; empty for Basic callers and jobs
    string Name { get; }               // "system" outside an HTTP request
    bool IsServiceAccount { get; }     // Basic auth
    Task<IReadOnlyCollection<string>> GetRoles(CancellationToken ct = default);       // AD group role names
    Task<IReadOnlyCollection<string>> GetFactoryIds(CancellationToken ct = default);  // from user_factory
    Task<bool> HasPermission(Permission permission, CancellationToken ct = default);
    Task<bool> CanEditFactory(string factoryId, CancellationToken ct = default);
    Task EnsureCanEditFactory(string factoryId, CancellationToken ct = default);       // throws ForbiddenException → 403
}
```

Own-factory rule (`CanEditFactory`): Basic callers pass; everyone passes while `permissions.enable_all` is off;
`BO_Administrator` passes; otherwise the factory must be in the user's `user_factory` rows. The attribute checks
the permission, the handler checks the factory:

```csharp
[HttpPut("{erpId}/factories/{factoryId}/manual-oos")]
[AzureAuthorize(Permission.ComponentsEditManualOos)]
public Task SetManualOos(...) => requestRunner.Run(new SetManualOosCommand(...), ct);

await currentUser.EnsureCanEditFactory(request.FactoryId, cancellationToken);
```

Details and the matrix: [`permissions.md`](permissions.md).

## Shared building blocks

All in `Tenengroup.Infrastructure.Common` unless stated otherwise.

### Lists: `Paging/`

`ListRequest { Page, PageSize, Search, SortBy, SortDirection, Filters }`, `ExportRequest : ListRequest { Columns }`,
`ListResult<T> { Items, TotalCount }`, `ListFilter { Values, Operator, Value, ValueTo }`.

```csharp
public record SearchComponentsQuery(ListRequest Request) : IRequest<ListResult<ComponentListItemDto>>;

private static readonly Dictionary<string, Expression<Func<Component, object>>> SortableFields = new()
{
    ["erpId"] = c => c.ErpId,
    ["name"] = c => c.Name
};

var query = db.Components.AsNoTracking();
if (!string.IsNullOrWhiteSpace(request.Search))
{
    var search = request.Search.ToLower();
    query = query.Where(c => c.ErpId.ToLower().Contains(search) || c.Name.ToLower().Contains(search));
}

var result = await query
    .ApplyFilter(request, "family", c => c.Family)          // { values: [...] } or { operator: "contains", value }
    .ApplyFilter(request, "status", c => c.Status)          // enum values are parsed case-insensitively
    .ApplyFilter(request, "criticalLevel", c => c.CriticalStockLevel)  // eq / ne / lt / lte / gt / gte / between
    .ApplySorting(request, SortableFields, "erpId")
    .Select(c => new ComponentListItemDto(...))
    .ToListResult(request, cancellationToken);              // CountAsync + Skip/Take + ToListAsync
```

- `ApplyFilter(request, field, selector)` — no filter for the field → query unchanged; invalid value/operator →
  `ValidationException` (422, key `filters.{field}`).
- Derived values that cannot be filtered in SQL: `request.Filter("stockStatus")` returns the `ListFilter`, and
  `ListFilterExpression.Matches("totalStock", filter, value)` evaluates it in memory.
- `ApplyPaging` clamps `page` to ≥ 1 and `pageSize` to 1…500 (default 50). Exports skip `ApplyPaging`.

### Excel export: `Excel/`

```csharp
private static readonly ExcelColumn<ComponentListItemDto>[] Columns =
[
    new("erpId", "ERP ID", c => c.ErpId),
    new("name", "Name", c => c.Name),
    new("totalStock", "Total stock", c => c.TotalStock)
];

ExcelFile file = ExcelExporter.Export("components", rows, Columns, request.Columns, timeProvider.GetUtcNow().UtcDateTime);

[HttpPost("export")]
public async Task<FileResult> Export([FromBody] ExportRequest request, CancellationToken ct) =>
    (await requestRunner.Run<ExportComponentsQuery, ExcelFile>(new ExportComponentsQuery(request), ct)).ToFileResult();
```

The file is named `{page}_{yyyyMMdd_HHmmss}.xlsx`; `request.Columns` picks and orders the columns (empty = all,
unknown keys are ignored). `Content-Disposition` is exposed through CORS.

### History writer: `Business.Abstractions/Shared/History/IHistoryWriter`

```csharp
historyWriter.Record(HistoryEntityTypes.Component, component.ErpId, "criticalStockLevel",
    oldLevel, newLevel, factoryId: request.FactoryId);
historyWriter.Record(HistoryEntityTypes.Component, erpId, "status", old, @new, HistorySource.Sync, changedBy: "SAP sync");
await db.SaveChangesAsync(cancellationToken);
```

`Record` only adds a `history_entry` row to the caller's context (no save) and does nothing when the formatted
old and new values are equal. `changedBy` defaults to the user's email (name when there is no email). Entity
types: `HistoryEntityTypes.Component | Process | Sku | GlobalFactoryShare`; the entity id is the natural key
(`erpId`, `processId`, `skuUuid`). The UI reads it through `GET /v1/history/{entityType}/{entityId}`.

### Sync-run tracking: `Business.Abstractions/Shared/Sync/ISyncRunTracker`

```csharp
var runId = await syncRunTracker.Start(SyncTypes.SapComponents, triggeredBy, ct);   // ConflictException (409) when one is running
try
{
    ...
    await syncRunTracker.Complete(runId, added, updated, failed, ct);
}
catch (Exception e)
{
    await syncRunTracker.Fail(runId, e.Message, ct);   // discards pending tracked changes first
    throw;
}
```

`Start` saves immediately (call it before tracking other changes). A `Running` run older than 6 hours is treated
as abandoned and failed by the next `Start`. `GetStatus(syncType)` backs `GET /v1/sync/{syncType}/status`.
Sync types: `SyncTypes.SapComponents` (`sap-components`), `SyncTypes.MasterCatalogSkus` (`master-catalog-skus`).

### Other

| Block | Where | Use |
|---|---|---|
| Exceptions | `Exceptions/` | `EntityNotFoundException` → 404, `ValidationException` → 422, `ConflictException` → 409, `ForbiddenException` → 403 |
| Feature toggles | `IFeatureToggleService.IsEnabled(name)` | rows of `catalog.feature_toggle`, cached 10 minutes |
| JSON | `Json/JsonExtensions` | `ToJsonString()`, `FromJsonString<T>()` (camelCase, string enums) for `jsonb` columns and events |
| S3 | `Aws/S3/IS3FileReader` | `OpenRead`, `ReadAllText`, `List(bucket, prefix)` |
| SQS | `Aws/Sqs/IQueueReader` | `ConsumeMessageCollection(queueUrl, max, eventTypeAttribute, handler)`; handler returns `true` to delete the message |
| SNS | `Aws/Sns/IEventPublisher` | `Publish(topicArn, eventType, payload)` |
| Web setup | `Web/` | `AddTenengroupControllers`, `AddTenengroupCors`, `AddTenengroupSwagger`, `UseTenengroupSwagger`, `UsePing`, `UseCorrelationId`, `UseGlobalExceptionHandler`, `UseConfiguredSerilog` |
| Hangfire dashboard | `Hangfire/HangfireDashboardAuthorizationFilter` | Basic-auth filter |
| DB connection | `DbConnectionString.Build(configuration)` | from `DATABASE_*` |

## Tests

`tests/unit-tests/Tenengroup.FactoryCatalog.Business.UnitTests` — NUnit, Moq, AutoFixture, FluentAssertions.Json,
`Moq.EntityFrameworkCore`. `TestDb.SetupSet(db, x => x.Components, items...)` returns the backing list (rows added
through `DbSet.Add` appear in it); `TestDb.FixedTime(utcNow)` gives a `TimeProvider`. No database is needed.

## Key dependencies (nuget.org only)

`Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`, `Hangfire.AspNetCore`, `Hangfire.PostgreSql`,
`Microsoft.Identity.Web`, `Serilog.AspNetCore`, `Swashbuckle.AspNetCore`, `ClosedXML`, `AWSSDK.S3`, `AWSSDK.SQS`,
`AWSSDK.SimpleNotificationService`.
