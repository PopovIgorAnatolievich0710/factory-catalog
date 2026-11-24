# Factory Catalog + Balancer

Two .NET 10 APIs and one React admin UI. Documentation: [`spec/README.md`](./spec/README.md);
deployment: [`spec/devops/overview.md`](./spec/devops/overview.md).

| Deployable | Project | Local URL |
|---|---|---|
| Factory Catalog API | `backend/src/app/Tenengroup.FactoryCatalog.Api` | http://localhost:5010 (Swagger at `/`) |
| Balancer API | `backend/src/app/Tenengroup.Balancer.Api` | http://localhost:5011 (Swagger at `/`) |
| Factory Catalog UI | `frontend/FactoryCatalogUI` | http://localhost:8080 |

## Prerequisites

- .NET 10 SDK, `dotnet tool install --global dotnet-ef`
- Node 20+
- Docker Desktop
- `git submodule update --init` (the UI needs `frontend/FactoryCatalogUI/bo-frontend`)

## Run locally (APIs and UI from source)

1. Start PostgreSQL and LocalStack:

   ```bash
   docker compose up -d
   ```

   PostgreSQL: `localhost:5432`, database `factory_catalog`, login `postgres` / `postgres`, schemas `catalog`,
   `balancer`, `hangfire`, `hangfire_balancer`. LocalStack: `http://localhost:4566` with the bucket
   `factory-catalog-dev-local`, the queues `factory-catalog-local-ocs-order-items` and
   `factory-catalog-local-supplier-events`, the topics `factory-catalog-local-balancer-allocation-decided` and
   `factory-catalog-local-balancer-quote-produced`.

2. Apply the migrations (see below).

3. Start the APIs (each in its own terminal; `appsettings.Development.json` holds the local values):

   ```bash
   dotnet run --project backend/src/app/Tenengroup.FactoryCatalog.Api
   dotnet run --project backend/src/app/Tenengroup.Balancer.Api
   ```

4. Start the UI:

   ```bash
   cd frontend/FactoryCatalogUI
   npm install --legacy-peer-deps
   npm run dev
   ```

## Run everything in Docker

```bash
docker compose --profile apps up -d --build
```

Builds the three images, applies both migrations (`catalog-migrate`, `balancer-migrate`) and starts the APIs and
the UI on the same ports as above. Optional overrides go into a `.env` file next to `docker-compose.yml`:
`POSTGRES_PASSWORD`, `API_SERVICE_USERNAME`, `API_SERVICE_USER_PASSWORD`, `GROUP_PERMISSIONS_URL`,
`AZURE_TENANT_ID`, `AZURE_API_CLIENT_ID`, `AZURE_UI_CLIENT_ID`, `AZURE_BACKEND_SCOPE`, `MASTER_CATALOG_BASE_URL`,
`MASTER_CATALOG_USERNAME`, `MASTER_CATALOG_PASSWORD`, `SHIPMENT_SERVICE_BASE_URL`, `SHIPMENT_SERVICE_USERNAME`,
`SHIPMENT_SERVICE_PASSWORD`.

Stop: `docker compose --profile apps down` (add `-v` to drop the database volume).

## Database migrations

Apply (local database from `docker compose`):

```bash
dotnet ef database update --context FactoryCatalogDbContext \
  --project backend/src/app/Tenengroup.FactoryCatalog.DataAccess \
  --startup-project backend/src/app/Tenengroup.FactoryCatalog.DataAccess

dotnet ef database update --context BalancerDbContext \
  --project backend/src/app/Tenengroup.Balancer.DataAccess \
  --startup-project backend/src/app/Tenengroup.Balancer.Api
```

Add `--connection "Server=<host>;Port=5432;Database=factory_catalog;User Id=<user>;Password=<password>;"` to target
another database. The same migrations are applied by the APIs themselves, with the `DATABASE_*` settings of the
current environment:

```bash
dotnet run --project backend/src/app/Tenengroup.FactoryCatalog.Api -- migrate
dotnet run --project backend/src/app/Tenengroup.Balancer.Api -- migrate
```

Add a migration:

```bash
dotnet ef migrations add "<MigrationName>" --context FactoryCatalogDbContext \
  --project backend/src/app/Tenengroup.FactoryCatalog.DataAccess \
  --startup-project backend/src/app/Tenengroup.FactoryCatalog.DataAccess

dotnet ef migrations add "<MigrationName>" --context BalancerDbContext \
  --project backend/src/app/Tenengroup.Balancer.DataAccess \
  --startup-project backend/src/app/Tenengroup.Balancer.Api
```

## Build and test

```bash
dotnet build backend/src/factory-catalog.sln
dotnet test backend/src/factory-catalog.sln
cd frontend/FactoryCatalogUI && npm run build && npm run typecheck
```

## Deployment

Jenkins multibranch pipeline `devops/Jenkinsfile`: `develop` → dev, `release` → stg, `master` → prod.
Steps to bring up dev and the list of values that still have to be provided:
`spec/devops/overview.md` ("Bringing up the dev environment") and `spec/questions/devops.md`.
