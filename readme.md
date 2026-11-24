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

## Restore the database backup (run on another machine)

The fastest way to get a working copy on a new machine: restore the database from
`backend/scripts/factory_catalog_backup.sql`, then start the two APIs and the UI from source.

### What the backup contains

A plain-SQL `pg_dump` (about 500 KB) of the `catalog` and `balancer` schemas, taken on 2026-11-25 from a working local
setup. It holds the tables, indexes, constraints, the EF migration history (`20261124133504_Initial` for the catalog,
`20261124145246_Initial` for the balancer) and the demo data, so restoring it replaces both "apply the migrations" and
"load the demo data".

| Area | Rows |
|---|---|
| Factories | 3 (110, 125, 315) |
| Components | 32 components, 96 component × factory rows, 7 alternatives |
| Processes | 11 processes, 33 process × factory rows |
| SKUs | 24 SKUs, 11 aliases, 84 component mappings, 118 process mappings, 72 factory rows, global share for 3 factories |
| Quotes | 51 quote sets, 154 quotes |
| Balancer | 50 allocations, 482 cost lines, 3 transfers, 27 production records, 21 cached routes, 10 legacy decisions, 2 capacity reservations, settings |
| Audit | 31 history entries, 3 sync runs |

Not included:

- `hangfire` and `hangfire_balancer`: the APIs create these schemas on start.
- Passwords, keys or other secrets.
- Access rows (`user_factory`) and feature toggles: both tables are empty. With the `permissions.enable_all` toggle
  absent, permissions are not enforced and every signed-in user has full access.

### Prerequisites

| Tool | Version | Used for |
|---|---|---|
| Git | any | clone with the UI submodule |
| PostgreSQL | 15+ (local install or the `postgres` service from `docker compose`) | the database |
| `psql` | 15+ (ships with PostgreSQL; not needed with Docker) | the restore |
| .NET SDK | 10 | the APIs |
| Node.js | 20+ | the UI |
| Docker Desktop | optional | PostgreSQL and LocalStack without local installs |

Access needed:

- SSH access to `github.com/tenengroup/bo-frontend`: the UI submodule `frontend/FactoryCatalogUI/bo-frontend` comes
  from there.
- A Tenengroup Microsoft (Azure AD) account: the UI signs in through the Tenengroup tenant. `http://localhost:8080` is
  the registered redirect address, so keep the UI on port 8080.

Free ports: `5432` (PostgreSQL), `5010` (Factory Catalog API), `5011` (Balancer API), `8080` (UI).

### 1. Clone

```bash
git clone --recurse-submodules https://github.com/PopovIgorAnatolievich0710/factory-catalog.git
cd factory-catalog
```

Already cloned without the submodule: `git submodule update --init`.

### 2. Restore the database

Restore into an empty `factory_catalog` database. Pick one of the options.

**A. Docker** (no local PostgreSQL needed). The `postgres` service creates the `factory_catalog` database and the empty
schemas on first start; login `postgres` / `postgres`:

```bash
docker compose up -d postgres
docker compose cp backend/scripts/factory_catalog_backup.sql postgres:/tmp/factory_catalog_backup.sql
docker compose exec postgres psql -U postgres -d factory_catalog -v ON_ERROR_STOP=1 -f /tmp/factory_catalog_backup.sql
```

The same commands work in bash and PowerShell. Copying the file into the container avoids piping it through the shell,
which in Windows PowerShell 5.1 would damage the non-ASCII characters in the data.

**B. Local PostgreSQL**, bash:

```bash
export PGPASSWORD=<postgres password>
createdb -h localhost -U postgres factory_catalog
psql -h localhost -U postgres -d factory_catalog -v ON_ERROR_STOP=1 -f backend/scripts/factory_catalog_backup.sql
```

Windows PowerShell (adjust the PostgreSQL folder to the installed version):

```powershell
$env:PGPASSWORD = "<postgres password>"
& "C:\Program Files\PostgreSQL\15\bin\createdb.exe" -h localhost -U postgres factory_catalog
& "C:\Program Files\PostgreSQL\15\bin\psql.exe" -h localhost -U postgres -d factory_catalog -v ON_ERROR_STOP=1 -f backend/scripts/factory_catalog_backup.sql
Remove-Item Env:PGPASSWORD
```

The script runs to the end with no `ERROR` lines; the `set_config` and `setval` rows it prints are normal.

### 3. Check the restore

```bash
psql -h localhost -U postgres -d factory_catalog -c "select (select count(*) from catalog.component) as components, (select count(*) from catalog.sku) as skus, (select count(*) from catalog.quote) as quotes, (select count(*) from balancer.allocation) as allocations"
```

With Docker, run the same query through the container: `docker compose exec postgres psql -U postgres -d factory_catalog -c "<query>"`.

Expected: `32 | 24 | 154 | 50`.

### 4. Point the APIs at the database

`appsettings.Development.json` already targets `localhost:5432`, database `factory_catalog`, login `postgres` /
`postgres`. With any other login, create `appsettings.Local.json` next to `appsettings.json` in **both**
`backend/src/app/Tenengroup.FactoryCatalog.Api` and `backend/src/app/Tenengroup.Balancer.Api`:

```json
{ "DATABASE_USERNAME": "postgres", "DATABASE_PASSWORD": "<password>" }
```

The file is git-ignored and overrides `appsettings.json` and `appsettings.Development.json`; `DATABASE_HOST`,
`DATABASE_PORT` and `DATABASE_NAME` can go there too.

### 5. Start the APIs and the UI

Three terminals, from the repository root:

```bash
dotnet run --project backend/src/app/Tenengroup.FactoryCatalog.Api --launch-profile Tenengroup.FactoryCatalog.Api
```

```bash
dotnet run --project backend/src/app/Tenengroup.Balancer.Api --launch-profile Tenengroup.Balancer.Api
```

```bash
cd frontend/FactoryCatalogUI
npm install --legacy-peer-deps
npm run dev
```

LocalStack is optional: the APIs start and every screen works without it; only the AWS queue and file features need it
(`docker compose up -d localstack`).

### 6. Check that it runs

| Check | Expected |
|---|---|
| http://localhost:5010/ping | `200` |
| http://localhost:5011/ping | `200` |
| http://localhost:5010 and http://localhost:5011 | Swagger |
| http://localhost:8080 | Microsoft sign-in, then the Factory Catalog UI |

After sign-in, Configurations → Components lists 32 components with stock statuses, Overview → Process capacity shows the
factory grid, and Balancer → Routing dashboard shows the routing figures.

### Move the demo dates to today

The backup keeps the dates it was taken with (2026-11-25), so "today" figures such as quotes produced today or the
dashboard's last days drift as time passes. `backend/scripts/demo-data.sql` rewrites only the demo rows with dates
relative to the current time, and can be run any number of times on top of the restore:

```bash
psql -h localhost -U postgres -d factory_catalog -v ON_ERROR_STOP=1 -f backend/scripts/demo-data.sql
```

### Restore again from scratch

Drop both schemas, then repeat step 2 (the `createdb` line is not needed):

```bash
psql -h localhost -U postgres -d factory_catalog -c "drop schema if exists catalog cascade; drop schema if exists balancer cascade; drop schema if exists hangfire cascade; drop schema if exists hangfire_balancer cascade"
```

With Docker, `docker compose down -v` followed by `docker compose up -d postgres` gives a fresh database.

### Newer code on an older backup

The APIs apply only missing migrations (`dotnet run --project <api project> -- migrate`), so after pulling commits that
add migrations, run `migrate` for both APIs on top of the restored database.

### Troubleshooting

| Symptom | Cause and fix |
|---|---|
| `password authentication failed for user "postgres"` (API log or `psql`) | Wrong login. Set `PGPASSWORD` for `psql`; put the login into `appsettings.Local.json` for the APIs (step 4). |
| `database "factory_catalog" does not exist` | Run `createdb` (option B) or start the Docker `postgres` service (option A). |
| `relation "..." already exists` during the restore | The database is not empty. Drop the schemas (see "Restore again from scratch") and restore again. |
| `connection refused` on 5432 | PostgreSQL is not running, or listens on another port: set `DATABASE_PORT` in `appsettings.Local.json` and use `-p <port>` with `psql`. |
| `address already in use` on 5010, 5011 or 8080 | Another process holds the port. Stop it; the UI must stay on 8080 for the Microsoft sign-in. |
| UI shows "No access … has no Factory Catalog role" | The UI could not reach the Factory Catalog API. Start it and reload the page. |
| `npm install` fails on peer dependencies | Use `npm install --legacy-peer-deps`. |
| `frontend/FactoryCatalogUI/bo-frontend` is empty, or the UI build cannot resolve `bo-frontend` | The submodule is missing: `git submodule update --init` (needs SSH access to `tenengroup/bo-frontend`). |
| Microsoft sign-in fails with a redirect URI error | The UI is not on `http://localhost:8080`. |

### Refresh the backup

From a working local database:

```bash
pg_dump -h localhost -U postgres -d factory_catalog --schema=catalog --schema=balancer --no-owner --no-privileges \
  -f backend/scripts/factory_catalog_backup.sql
sed -i 's/^CREATE SCHEMA \(catalog\|balancer\);/CREATE SCHEMA IF NOT EXISTS \1;/' backend/scripts/factory_catalog_backup.sql
```

The `sed` line keeps the restore working on the Docker database, which already has the schemas. `--no-owner
--no-privileges` keeps the dump restorable under any login. `backend/scripts/.gitattributes` keeps `.sql` files with LF
line endings on every checkout. Before committing, check that the dump has no passwords or personal data: the
repository is public.

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
