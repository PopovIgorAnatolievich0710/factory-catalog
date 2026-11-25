# DevOps — Overview

> **Requirements:** Phase overview v4; Module 1: Components v5; Module 2: Process capacity v5; Module 3: SKUs v4; Module 4: Balancer v4

Modelled on the shipment service (`devops/` of that repository). Everything that could not be derived from its
conventions is a placeholder listed in `spec/questions/devops.md`.

## Repository Layout

```
docker-compose.yml                  # local development: PostgreSQL, LocalStack, both APIs, UI
.dockerignore
backend/src/app/Tenengroup.FactoryCatalog.Api/Dockerfile   # Factory Catalog API image
devops/
├── Jenkinsfile                     # multibranch pipeline
├── docker/
│   ├── balancer-api.Dockerfile     # Balancer API image (build context backend/src)
│   ├── postgres/01-schemas.sql     # local init script: schemas catalog, balancer, hangfire, hangfire_balancer
│   └── localstack/init-aws.sh      # local bucket, queues, topics
├── helm/
│   └── chart/
│       ├── Chart.yaml.tpl          # Helm chart metadata template
│       ├── values.yaml             # values template rendered by Terraform (deployments main, balancer, ui)
│       ├── configs/                # db_init_script.sql.tpl (migrations login)
│       ├── dependencies/           # vendored postgresql sub-chart (feature-branch environments)
│       └── templates/              # K8s resource templates (copied from the shipment service chart)
├── scripts/                        # encrypt_secret / decrypt_secret (KMS)
└── terraform/
    ├── main.tf, providers.tf, variables.tf, output.tf
    ├── db.tf                       # RDS PostgreSQL, DNS record, migrations login
    ├── secrets.tf                  # KMS-encrypted secrets
    ├── helm.tf                     # Helm release and every env var value per environment
    ├── sap-export.tf               # S3 bucket of the SAP export
    ├── ocs-order-items.tf          # SQS queue for OCS order items + subscription
    ├── supplier-events.tf          # SQS queue for TG Supplier sent / reproduced events + subscription
    ├── balancer-events.tf          # SNS topics balancer.allocation.decided / balancer.quote.produced
    └── sqs_to_sns_subscription/    # SQS → SNS subscription module
```

---

## Docker

### API images

| Image | Dockerfile | Build context | Port | `DD_SERVICE` |
|---|---|---|---|---|
| `tenen/factory-catalog-main` | `backend/src/app/Tenengroup.FactoryCatalog.Api/Dockerfile` | `backend/src` | 5010 | `factory-catalog` |
| `tenen/factory-catalog-balancer` | `devops/docker/balancer-api.Dockerfile` | `backend/src` | 5011 | `factory-catalog-balancer` |

```
Stage 1 (build):   mcr.microsoft.com/dotnet/sdk:10.0     restore (nuget.org only), unit tests, build
Stage 2 (publish): Datadog .NET tracer download, dotnet publish
Stage 3 (final):   mcr.microsoft.com/dotnet/aspnet:10.0  non-root appuser, Datadog profiler env
```

- No build arguments: there is no private NuGet feed, so no feed token.
- `dotnet test` of the service's unit-test project runs inside the image build; a failing test fails the build.
- `dotnet <Api>.dll migrate` applies the EF migrations and exits (used by the init containers and docker compose).
- The listen address comes from `Kestrel__Endpoints__Http__Url`.

### Frontend image

`tenen/factory-catalog-ui` from `frontend/FactoryCatalogUI/Dockerfile` (owned by the frontend): Node 20-alpine build,
nginx serving. nginx listens on port **80** inside the container; `docker-entrypoint.sh` fills `window.env` with
`envsubst`. The build context must contain the checked-out `bo-frontend` submodule.

### docker-compose (local)

| Service | Image | Host port | Profile |
|---|---|---|---|
| `postgres` | `postgres:15`, database `factory_catalog`, schemas from `01-schemas.sql` | 5432 | default |
| `localstack` | `localstack/localstack:3.8` (S3, SQS, SNS) | 4566 | default |
| `catalog-migrate`, `balancer-migrate` | API images, `migrate` | — | `apps` |
| `catalog-api` | `factory-catalog-main:local` | 5010 | `apps` |
| `balancer-api` | `factory-catalog-balancer:local` | 5011 | `apps` |
| `ui` | `factory-catalog-ui:local` | 8080 → 80 | `apps` |

`docker compose up -d` starts only the infrastructure (for `dotnet run` / `npm run dev`);
`docker compose --profile apps up -d --build` starts everything. LocalStack resources:
bucket `factory-catalog-dev-local`, queues `factory-catalog-local-ocs-order-items` and
`factory-catalog-local-supplier-events` (each with a `DLQ-` queue), topics
`factory-catalog-local-balancer-allocation-decided` and `factory-catalog-local-balancer-quote-produced`.
The master catalog and the shipment service are not part of the compose file; their URLs are taken from
`MASTER_CATALOG_BASE_URL` / `SHIPMENT_SERVICE_BASE_URL` (defaults point at the host).

---

## CI/CD — Jenkins

`devops/Jenkinsfile` is the shipment service multibranch pipeline with the new project name.

### Branch → Environment Mapping

| Branch | `deploy_env` | Namespace | Helm release |
|---|---|---|---|
| `master` | `prod` | `factory-catalog-prod` | `factory-catalog-master` |
| `release` | `stg` | `factory-catalog-stg` | `factory-catalog-release` |
| `develop` | `dev` | `factory-catalog-dev` | `factory-catalog-develop` |
| feature branches / PRs | branch name | `factory-catalog-dev` | `factory-catalog-<branch>` |

### Pipeline Stages

1. **Preparation** — Slack notification, manual approval on `master`, `build_init`, Terraform state file
   (`tg-terraform-state`, key `factory-catalog/factory-catalog-<deploy_env>.tfstate`), `terraform init`.
2. **Dockerize images** — for `main`, `balancer`, `ui`: the tag is the git hash of the build context plus the
   Dockerfile; an image that already exists is not rebuilt. Backend build and unit tests run in the API image
   builds, `npm ci` + `npm run build` in the UI image build. Both API images share the context `backend/src`,
   so a backend change rebuilds both.
3. **Backup DB** — RDS snapshot when `BACKUP_DB` is `true`.
4. **Deploy** — `terraform apply` in `devops/terraform`: AWS resources, database login, Helm release.
5. **PR checks / merge** — approvals, GitHub status, merge, as in the reference pipeline.

Database migrations are applied by the `sql-migrations` init container of each API deployment.
There is no integration-test stage (the project has no integration-test project).

---

## Kubernetes (Helm)

One chart, one release per branch, three deployments:

| Deployment | Container port | Service | Ingress host (dev / stg / prod) | Init container |
|---|---|---|---|---|
| `main` (Factory Catalog API) | 5010 | `<release>-main-main:80` | `factory-catalog-develop.dev.eks.tenengroup.com` / `factory-catalog-release.stg.eks.tenengroup.com` / `factory-catalog.eks.tenengroup.com` | `Tenengroup.FactoryCatalog.Api.dll migrate` |
| `balancer` (Balancer API) | 5011 | `<release>-balancer-main:80` | `factory-catalog-develop-balancer.dev…` / `factory-catalog-release-balancer.stg…` / `factory-catalog-balancer.eks…` | `Tenengroup.Balancer.Api.dll migrate` |
| `ui` | 80 | `<release>-ui-main:80` | `factory-catalog-develop-ui.dev…` / `factory-catalog-release-ui.stg…` / `factory-catalog-ui.eks…` | — |

- Ingress class `tg-internal` (cluster + VPN). No public (`remote`) ingress: nothing calls these services from
  outside the network.
- TLS: dev uses the wildcard secret `replica-wildcard-dev.eks.tenengroup.com`; stg / prod use cert-manager
  (`letsencrypt`).
- Liveness and readiness probes: `GET /ping` on every deployment (the UI answers it with `index.html`).
- 2 replicas per deployment, pod anti-affinity per host, priority class `dev-pr` / `stg` / `prod`, service account
  = namespace name (IRSA role `eks-tenenkube-<namespace>`).
- Env vars are set in `values.yaml` → `deployments.<name>.containers.main.env_vars`; the values (including the
  KMS-decrypted secrets) are filled by `terraform/helm.tf`, as in the reference chart.
- The Balancer calls the catalog inside the cluster: `FactoryCatalog__BaseUrl = http://<release>-main-main`.

### Environment values (dev column is what `develop` deploys)

| Variable | dev | stg | prod |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Staging` | `Staging` | `Production` |
| `DATABASE_HOST` | `factory-catalog-dev-db-rds.dev.eks.tenengroup.com` | `factory-catalog-stg-db-rds.stg.eks.tenengroup.com` | `factory-catalog-prod-db-rds.eks.tenengroup.com` |
| `DATABASE_NAME` / `DATABASE_USERNAME` | `factory_catalog` / `migrations_user` | same | same |
| `API_SERVICE_USERNAME` | `admin` | same | same |
| `GROUP_PERMISSIONS_URL` | `https://cdn-mnn.s3.eu-west-1.amazonaws.com/Config/DevGroupPermissions.json` | `…/Config/StgGroupPermissions.json` | `…/Config/GroupPermissions.json` |
| `SERVICES_CONFIG_URL_ENV_VAR` | `https://cdn-mnn.s3.eu-west-1.amazonaws.com/Config/BackOfficeSystemsDev.json` | same as dev | `…/Config/BackOfficeSystems.json` |
| `CORS_ALLOWED_HOSTS` | `https://factory-catalog-develop-ui.dev.eks.tenengroup.com` | `https://factory-catalog-release-ui.stg.eks.tenengroup.com` | `https://factory-catalog-ui.eks.tenengroup.com` |
| `AzureAd__TenantId` | `26c34b0b-8ce7-4c06-97fb-3d5915066e98` | same | same |
| `AzureAd__ClientId`, `AZURE_CLIENT_ID_ENV_VAR`, `AZURE_BACKEND_SCOPE_ENV_VAR` | placeholders (`azure_app_registrations.dev`) | placeholders | placeholders |
| `SapExport__Bucket` / `SapExport__Prefix` | `tg-factory-catalog-dev` / `sap-export` | `tg-factory-catalog-stg` | `tg-factory-catalog-prod` |
| `MasterCatalog__BaseUrl` | `https://tg-catalog-develop.dev.eks.tenengroup.com` | `https://tg-catalog-release.stg.eks.tenengroup.com` | `https://catalog.tenengroup.com` |
| `ShipmentService__BaseUrl` | `https://shipment-service-develop.dev.eks.tenengroup.com` | `https://shipment-service-release.stg.eks.tenengroup.com` | `https://shipment-service.eks.tenengroup.com` |
| `OrderItems__QueueUrl` | queue `factory-catalog-dev-ocs-order-items` | `factory-catalog-stg-ocs-order-items` | `factory-catalog-prod-ocs-order-items` |
| `SupplierEvents__QueueUrl` | queue `factory-catalog-dev-supplier-events` | `…-stg-…` | `…-prod-…` |
| `BalancerEvents__AllocationDecidedTopicArn` | topic `factory-catalog-dev-balancer-allocation-decided` | `…-stg-…` | `…-prod-…` |
| `BalancerEvents__QuoteProducedTopicArn` | topic `factory-catalog-dev-balancer-quote-produced` | `…-stg-…` | `…-prod-…` |
| `Serilog__MinimumLevel__Default`, `DD_LOGS_DIRECT_SUBMISSION_MINIMUM_LEVEL` | `Debug` | `Debug` | `Information` |
| `DD_ENV` | `dev` | `stg` | `prod` |
| `Aws__AccessKey` / `Aws__SecretKey` | empty (IRSA) | empty | empty |

The Balancer variable names (`FactoryCatalog__*`, `ShipmentService__*`, `OrderItems__QueueUrl`,
`SupplierEvents__QueueUrl`, `BalancerEvents__*`) were chosen before the Balancer project existed and must be
aligned with its `appsettings.json` after the merge (question D-1).

---

## Terraform

Run by the pipeline only; state in `s3://tg-terraform-state/factory-catalog/factory-catalog-<deploy_env>.tfstate`.

### db.tf

RDS PostgreSQL 15 (`db.t3.small`, 20 GiB; prod 80 GiB, Multi-AZ) for `develop` / `release` / `master`, database
`factory_catalog`, CNAME `factory-catalog-<env>-db-rds.<env domain>`. Feature branches get the in-cluster
PostgreSQL sub-chart instead. `configs/db_init_script.sql.tpl` creates the login `migrations_user`, which both APIs
use: it creates the schemas `catalog`, `balancer` (EF migrations) and `hangfire`, `hangfire_balancer` (Hangfire on
start).

### secrets.tf

KMS-encrypted ciphertext in `encrypted-secrets-dev` (dev, stg and feature branches) and `encrypted-secrets-prod`,
decrypted at plan time with the key `alias/factory-catalog`. New ciphertext:
`devops/scripts/encrypt_secret.sh factory-catalog <plaintext>`.

| Key | Used for |
|---|---|
| `POSTGRESQL_ADMIN_USERNAME`, `POSTGRESQL_ADMIN_PASSWORD` | RDS master login (also runs the init script) |
| `DB_PASSWORD_MIGRATIONS` | `migrations_user` → `DATABASE_PASSWORD` of both APIs |
| `API_SERVICE_USER_PASSWORD` | Basic auth of both APIs and the Hangfire dashboards; the Balancer's catalog client |
| `DD_API_KEY` | Datadog direct log submission |
| `MASTER_CATALOG_PASSWORD` | `MasterCatalog__Password` |
| `SHIPMENT_SERVICE_API_PASSWORD` | `ShipmentService__Password` |
| `AWS_ACCESS_KEY`, `AWS_SECRET_KEY` (optional) | static AWS credentials; absent → IRSA |

### sap-export.tf

Private bucket `tg-factory-catalog-<env>` (dev / stg / prod; feature branches read the dev bucket), public access
blocked, SSE-S3, versioning, `prevent_destroy`. The service role may list the bucket and read
`sap-export/*`; the principals in `sap_export_writer_arns` may put objects under the prefix.

### ocs-order-items.tf / supplier-events.tf

| Queue | DLQ | Subscribed to | Filter |
|---|---|---|---|
| `factory-catalog-<env>-ocs-order-items` | `DLQ-…`, 5 receives | `order-core-service-<env>-domain-events` | `ocs_order_items_filter_policy` (empty = every event) |
| `factory-catalog-<env>-supplier-events` | `DLQ-…`, 5 receives | `supplier-<env>-events` | `event_type` in `supplier_event_types` |

The queue policies let the topic send and the service role consume. Subscriptions exist for dev / stg / prod only.

### balancer-events.tf

SNS topics `factory-catalog-<env>-balancer-allocation-decided` and `factory-catalog-<env>-balancer-quote-produced`;
the service role may publish, principals of the account may subscribe.

### helm.tf

`helm_release` of the chart; `values.yaml` is rendered with `templatefile` (hosts, images, env vars, secrets).

---

## Observability

| Tool | Purpose |
|---|---|
| Serilog → stdout | Structured logs, shipped by the Datadog tracer's direct log submission |
| Datadog APM | Tracer and profiler baked into both API images; `DD_ENV`, `DD_SERVICE`, `DD_VERSION`, agent host from the chart |
| Hangfire Dashboard | `/hangfire` on each API (Basic auth) |
| `/ping` | Liveness and readiness probe |

---

## Bringing up the dev environment

Nothing below has been run. Steps 1–6 are one-time prerequisites; step 7 is the deploy.

1. **GitHub** — create `tenengroup/factory-catalog`, push `master`, create `develop`. Make sure the Jenkins
   credential `k8s-jenkins-credential-core-github-api-access-usr-pwd` can read it and the `bo-frontend` submodule.
2. **Platform (DevOps team)** — request for `factory-catalog-dev`:
   the namespace, the service account `factory-catalog-dev` with the IRSA role
   `eks-tenenkube-factory-catalog-dev`, the KMS key `alias/factory-catalog`, a Jenkins multibranch job on the
   repository, the Slack channel `factory-catalog-builds`, Docker Hub repositories `tenen/factory-catalog-main`,
   `tenen/factory-catalog-balancer`, `tenen/factory-catalog-ui`. Confirm the build agent image
   (`tenen/jenkins-agent-shipment-service:jdk21` is reused) and replace the webhook token suffix
   `REPLACE_WITH_WEBHOOK_TOKEN_SUFFIX` in `devops/Jenkinsfile`.
3. **Azure AD** — create (or decide to reuse) the API and UI app registrations for dev; add the redirect URI
   `https://factory-catalog-develop-ui.dev.eks.tenengroup.com/signInCallback`; put the ids and the scope into
   `azure_app_registrations.dev` in `devops/terraform/variables.tf`.
4. **Secrets** — choose the dev values and encrypt each with
   `devops/scripts/encrypt_secret.sh factory-catalog '<value>'`, then replace every `REPLACE_WITH_KMS_CIPHERTEXT`
   in `encrypted-secrets-dev` (`devops/terraform/secrets.tf`): `POSTGRESQL_ADMIN_USERNAME`,
   `POSTGRESQL_ADMIN_PASSWORD`, `DB_PASSWORD_MIGRATIONS`, `API_SERVICE_USER_PASSWORD` (new values);
   `DD_API_KEY`, `MASTER_CATALOG_PASSWORD`, `SHIPMENT_SERVICE_API_PASSWORD` (existing values from their owners).
   Set `master_catalog_username` in `variables.tf`.
5. **Integration owners** — confirm the topic names `order-core-service-dev-domain-events` and
   `supplier-dev-events`, the OCS filter and the TG Supplier "reproduced" event name
   (`ocs_order_items_filter_policy`, `supplier_event_types`); give the SAP export job's principal for
   `sap_export_writer_arns`.
6. **CDN config** — add the `FC-*` roles to `Config/DevGroupPermissions.json` and a "Factory Catalog" entry
   (`https://factory-catalog-develop-ui.dev.eks.tenengroup.com`) to `Config/BackOfficeSystemsDev.json`.
7. **Deploy** — merge to `develop`; the multibranch job builds the three images and runs `terraform apply`
   (RDS instance, bucket, queues, topics, Helm release). The first run creates the RDS instance and takes longer.
8. **Migrations** — both `Initial` migrations are applied on every deploy by the `sql-migrations` init containers.
   To apply them by hand (VPN required), use the same entry point with the dev database:

   ```bash
   export DATABASE_HOST=factory-catalog-dev-db-rds.dev.eks.tenengroup.com DATABASE_PORT=5432
   export DATABASE_NAME=factory_catalog DATABASE_USERNAME=migrations_user DATABASE_PASSWORD='<DB_PASSWORD_MIGRATIONS>'
   dotnet run --project backend/src/app/Tenengroup.FactoryCatalog.Api -- migrate
   dotnet run --project backend/src/app/Tenengroup.Balancer.Api -- migrate
   ```

   or with the EF tool:

   ```bash
   CONNECTION='Server=factory-catalog-dev-db-rds.dev.eks.tenengroup.com;Port=5432;Database=factory_catalog;User Id=migrations_user;Password=<DB_PASSWORD_MIGRATIONS>;'
   dotnet ef database update --context FactoryCatalogDbContext \
     --project backend/src/app/Tenengroup.FactoryCatalog.DataAccess \
     --startup-project backend/src/app/Tenengroup.FactoryCatalog.DataAccess --connection "$CONNECTION"
   dotnet ef database update --context BalancerDbContext \
     --project backend/src/app/Tenengroup.Balancer.DataAccess \
     --startup-project backend/src/app/Tenengroup.Balancer.Api --connection "$CONNECTION"
   ```
9. **Check** — `https://factory-catalog-develop.dev.eks.tenengroup.com/ping`,
   `https://factory-catalog-develop-balancer.dev.eks.tenengroup.com/ping`, Swagger at `/` of both,
   `https://factory-catalog-develop-ui.dev.eks.tenengroup.com`.

stg and prod follow the same steps with their own namespace, IRSA role, app registrations, CDN entries and (prod)
the `encrypted-secrets-prod` map.

---

## Local Development

See the root `readme.md`.

## Submodules

`frontend/FactoryCatalogUI/bo-frontend` (shared BO components). Clone with `--recurse-submodules` or run
`git submodule update --init`.
