# Backend — Integrations

> **Requirements:** Phase overview v4; Module 1: Components v5; Module 2: Process capacity v5; Module 3: SKUs v4; Module 4: Balancer v4

Contracts are assumptions until the owning team confirms (see [`open-questions.md`](../open-questions.md), section
"Blocking for go-live").
Each integration is implemented and documented by its module; this file lists the shared plumbing.

| Integration | Direction | Module spec |
|---|---|---|
| SAP component master + stock (S3 export) | S3 → catalog | `spec/modules/1-components.md` |
| TG Supplier replacement screen | supplier_api → catalog | `spec/modules/1-components.md` |
| Master catalog SKUs | master → catalog | `spec/modules/3-skus.md` |
| Legacy balancer eligibility + country SKU lists | supplier_api → catalog | `spec/modules/4a-quotes.md` |
| Production quotes, select / produced marking | balancer → catalog | `spec/modules/4a-quotes.md` |
| OCS, shipment service, TG Supplier events, allocation / produced events | balancer | `spec/modules/4b-balancer.md` |

## AWS building blocks (`Tenengroup.Infrastructure.Common.Aws`)

Registered by `services.AddAwsInfrastructure(configuration)` from the `Aws` config section. Clients are created
lazily (first use), so missing AWS settings do not break startup.

| Abstraction | Implementation | Notes |
|---|---|---|
| `IS3FileReader` | `S3FileReader` | `OpenRead(bucket, key)` (buffered stream), `ReadAllText`, `List(bucket, prefix)` (skips empty objects, follows continuation tokens) |
| `IQueueReader` | `QueueReader` | long-polls (20 s), reads `EventType` from the named message attribute or from an `{ EventType, DataJson }` envelope, deletes the message when the handler returns `true`, logs and keeps it otherwise |
| `IMessageReader` | `MessageReader` | turns an SQS `Message` into `EventMessageValue { EventType, DataJson, CorrelationId, MessageId }` |
| `IEventPublisher` | `SnsEventPublisher` | publishes the payload as camelCase JSON with the `EventType` message attribute |

`Aws:ServiceUrl` (LocalStack) switches S3 to path-style addressing; SQS / SNS have their own
`Aws:SqsServiceUrl` / `Aws:SnsServiceUrl`. Without access keys the default AWS credential chain (IRSA) is used.
No consumer is registered by the foundation.

## Configuration placeholders

`SapExport` (`Bucket`, `Prefix`, `CronExpression`) and `MasterCatalog` (`BaseUrl`, `Username`, `Password`,
`CronExpression`) exist in `appsettings.json`; the owning modules read them with `configuration.GetSection(...)`.
