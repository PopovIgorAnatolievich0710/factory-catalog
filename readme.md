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

## Demo data

`backend/scripts/demo-data.sql` fills a local database with realistic sample data for every UI screen: components
with stock in factories 110, 125 and 315 (ok, low, out of stock, manual OOS and OOS conflict), processes with capacity,
SKUs with mappings and factory share, sync runs, history entries, quotes and balancer decisions, transfers and
production records over the last 14 days. It is for local development only; never run it against a shared
environment. Apply both migrations first; the script runs in one transaction and can be re-run (it deletes its own
rows before inserting them again, dates are relative to the current time):

```bash
PGPASSWORD=<password> psql -h localhost -p 5432 -U <user> -d factory_catalog -v ON_ERROR_STOP=1 \
  -f backend/scripts/demo-data.sql
```

## Database backup (run on another machine)

`backend/scripts/factory_catalog_backup.sql` is a plain `pg_dump` of the `catalog` and `balancer` schemas from a
working local setup: schema, migration history and the demo data. Restoring it replaces "apply migrations + load demo
data". The `hangfire` and `hangfire_balancer` schemas are not in it; the APIs create them on start.

1. Clone with the UI submodule (needs access to `tenengroup/bo-frontend` over SSH):

   ```bash
   git clone --recurse-submodules https://github.com/PopovIgorAnatolievich0710/factory-catalog.git
   ```

2. Start PostgreSQL 15+ on `localhost:5432` (`docker compose up -d`, or a local install) and restore into an
   empty `factory_catalog` database:

   ```bash
   createdb -h localhost -U postgres factory_catalog
   psql -h localhost -U postgres -d factory_catalog -v ON_ERROR_STOP=1 -f backend/scripts/factory_catalog_backup.sql
   ```

   With `docker compose` the database already exists and the password is `postgres`; skip `createdb`. Without a
   local `psql`, restore through the container:

   ```bash
   docker compose exec -T postgres psql -U postgres -d factory_catalog -v ON_ERROR_STOP=1 < backend/scripts/factory_catalog_backup.sql
   ```

3. If the `postgres` password is not `postgres`, create `appsettings.Local.json` next to `appsettings.json` in both
   `Tenengroup.FactoryCatalog.Api` and `Tenengroup.Balancer.Api` (the file is git-ignored):

   ```json
   { "DATABASE_USERNAME": "postgres", "DATABASE_PASSWORD": "<password>" }
   ```

4. Start the APIs and the UI as in [Run locally](#run-locally-apis-and-ui-from-source), steps 3 and 4. Sign in with a
   Tenengroup Microsoft account; with `permissions.enable_all` off every signed-in user has full access.

Refresh the backup from a local database:

```bash
pg_dump -h localhost -U postgres -d factory_catalog --schema=catalog --schema=balancer --no-owner --no-privileges \
  -f backend/scripts/factory_catalog_backup.sql
sed -i 's/^CREATE SCHEMA \(catalog\|balancer\);/CREATE SCHEMA IF NOT EXISTS \1;/' backend/scripts/factory_catalog_backup.sql
```

The `sed` line keeps the restore working on the `docker compose` database, which creates the schemas on first start.

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
