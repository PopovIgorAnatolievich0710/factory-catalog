# DevOps — questions, assumptions and placeholders

> **Requirements:** Phase overview v4; Module 1: Components v5; Module 2: Process capacity v5; Module 3: SKUs v4; Module 4: Balancer v4

Each row was implemented with the recommended variant. Nothing was built, linted with `helm` / `terraform`, or
applied (Docker daemon, `helm` and `terraform` are not available on the authoring machine).

## Placeholders that need a real value before the first dev deploy

| # | Placeholder | Where | What to put |
|---|---|---|---|
| P-1 | `REPLACE_WITH_KMS_CIPHERTEXT` × 7 in `encrypted-secrets-dev` (× 7 in `encrypted-secrets-prod` for prod) | `devops/terraform/secrets.tf` | Output of `devops/scripts/encrypt_secret.sh factory-catalog '<value>'` for `POSTGRESQL_ADMIN_USERNAME`, `POSTGRESQL_ADMIN_PASSWORD`, `DB_PASSWORD_MIGRATIONS`, `API_SERVICE_USER_PASSWORD`, `DD_API_KEY`, `MASTER_CATALOG_PASSWORD`, `SHIPMENT_SERVICE_API_PASSWORD` |
| P-2 | `REPLACE_WITH_DEV_AZURE_API_CLIENT_ID`, `REPLACE_WITH_DEV_AZURE_UI_CLIENT_ID`, `REPLACE_WITH_SCOPE_NAME` (same for `STG`, `PROD`) | `devops/terraform/variables.tf` → `azure_app_registrations` | Azure AD app registration ids and API scope (open question A-6) |
| P-3 | `REPLACE_WITH_MASTER_CATALOG_USERNAME` | `variables.tf` → `master_catalog_username` | Basic auth user of the master catalog API for this service |
| P-4 | `TenenGroup.Supplier.Events.ItemProductionStatusChanged` | `variables.tf` → `supplier_event_types` | **Resolved (2026-11-25):** TG Supplier publishes no reproduced event; a reproduction is `ItemProductionStatusChanged` with `ProductionStatus = "Reproduction"` |
| P-5 | `ocs_order_items_filter_policy` (empty) | `variables.tf` | SNS filter policy JSON for the OCS paid-order-item event (I-7); empty delivers every OCS domain event to the queue |
| P-6 | `sap_export_writer_arns` (empty) | `variables.tf` | IAM principal of the job that uploads the SAP export (I-1) |
| P-7 | `REPLACE_WITH_WEBHOOK_TOKEN_SUFFIX` | `devops/Jenkinsfile` → `generic_webhook_trigger_token` | Token of the GitHub generic webhook for PR review events |
| P-8 | KMS key `alias/factory-catalog`, namespace + service account `factory-catalog-dev`, IRSA role `eks-tenenkube-factory-catalog-dev`, Jenkins job, Slack channel `factory-catalog-builds`, Docker Hub repositories `tenen/factory-catalog-{main,balancer,ui}`, GitHub repository `tenengroup/factory-catalog` | outside this repo | Created by the platform team; the names follow the shipment service convention and are referenced by the Jenkinsfile and Terraform |
| P-9 | `FC-*` role mappings and the "Factory Catalog" app entry | CDN `Config/DevGroupPermissions.json`, `Config/BackOfficeSystemsDev.json` | Added by whoever maintains the BO config files |

## Questions and assumptions

| # | Question | Implemented (recommended) | Alternatives |
|---|---|---|---|
| D-1 | Names of the Balancer settings (its project did not exist when this was written) | `FactoryCatalog__BaseUrl/Username/Password`, `ShipmentService__BaseUrl/Username/Password`, `OrderItems__QueueUrl`, `SupplierEvents__QueueUrl`, `BalancerEvents__AllocationDecidedTopicArn`, `BalancerEvents__QuoteProducedTopicArn`; the shared ones (`DATABASE_*`, `API_SERVICE_*`, `AzureAd__*`, `Aws__*`, `GROUP_PERMISSIONS_URL`, `CORS_ALLOWED_HOSTS`, `Kestrel__Endpoints__Http__Url`) as in the catalog. Rename in `devops/helm/chart/values.yaml` and `docker-compose.yml` after the merge if the Balancer uses other keys | Whatever `Tenengroup.Balancer.Api/appsettings.json` defines |
| D-2 | The Balancer is assumed to support `dotnet Tenengroup.Balancer.Api.dll migrate`, expose `/ping`, and have the test project `tests/unit-tests/Tenengroup.Balancer.Business.UnitTests` | Init container, probes and the image build rely on all three | Remove the init container / test step, or add the missing piece to the Balancer |
| D-3 | One SNS topic for all balancer events or one per event? | Two topics: `…-balancer-allocation-decided`, `…-balancer-quote-produced` | One `factory-catalog-<env>-balancer-events` topic, consumers filter on the `EventType` attribute (shipment service `domain-events` style) |
| D-4 | Project / release naming | Project `factory-catalog`: namespaces `factory-catalog-{dev,stg,prod}`, images `tenen/factory-catalog-{main,balancer,ui}`, hosts `factory-catalog-<branch>[-balancer|-ui].<env>.eks.tenengroup.com`, one Helm release with three deployments | Separate project / pipeline / chart for the Balancer |
| D-5 | Environment values: per-environment values files or Terraform lookups? | As in the reference: one `values.yaml` template, every per-environment value is a lookup in `terraform/helm.tf` | `values-dev.yaml` / `values-stg.yaml` / `values-prod.yaml` and a plain `helm upgrade` |
| D-6 | Secrets in the pod spec | As in the reference: KMS-decrypted by Terraform and rendered into `env_vars` of the Deployment | Kubernetes `Secret` + `env_vars_from_k8s_secrets` (the chart supports it) |
| D-7 | Database login of the running APIs | `migrations_user` for both the init containers and the services, because Hangfire creates its schema on start (the reference also runs with `migrations_user`). No `app_user` | Least-privilege `app_user` with DML grants on `catalog`, `balancer` and both Hangfire schemas |
| D-8 | Who creates the schemas in deployed environments? | EF migrations (`catalog`, `balancer`) and Hangfire (`hangfire`, `hangfire_balancer`) create them as `migrations_user`; the RDS init script only creates the login. The local compose init script creates all four | Create the schemas in `db_init_script.sql.tpl` |
| D-9 | AWS credentials of the pods | IRSA role `eks-tenenkube-<namespace>` through the service account; bucket, queue and topic resource policies grant that role. `Aws__AccessKey/SecretKey` stay empty; optional `AWS_ACCESS_KEY` / `AWS_SECRET_KEY` in the KMS map switch to static keys | Static keys in KMS as the shipment service does for S3 |
| D-10 | Who owns the SAP export bucket? | This project: `tg-factory-catalog-<env>`, prefix `sap-export`, SAP job gets `PutObject` through `sap_export_writer_arns` | Read an existing bucket of the SAP / data team (then drop `sap-export.tf` and set bucket / prefix) |
| D-11 | Source topics of the queues | `order-core-service-<env>-domain-events` (as the shipment service) and `supplier-<env>-events` | Dedicated topics published by OCS / TG Supplier for the balancer |
| D-12 | Is the "master catalog" the `tg-catalog` service the shipment service calls? | Yes: `https://tg-catalog-develop.dev.eks.tenengroup.com` / `…-release.stg…` / `https://catalog.tenengroup.com` | Another URL per environment |
| D-13 | Shipment service credentials for the Balancer | User `admin` (its `API_SERVICE_USERNAME`) with the password in `SHIPMENT_SERVICE_API_PASSWORD` | A dedicated machine user |
| D-14 | Do both APIs share one Azure AD API registration? | Yes: the UI has a single `AZURE_BACKEND_SCOPE`, so both APIs validate the same audience | Separate registrations and a second scope in the UI |
| D-15 | Public (`remote`) ingress | None; all three hosts are `tg-internal` (cluster + VPN) | `remote-dev` / `remote-prod` ingress if an external party must call the APIs |
| D-16 | Readiness probe | `/ping` for liveness and readiness (the APIs have no `/healthcheck`) | A readiness endpoint that checks the database |
| D-17 | UI container port | 80 — `nginx.conf` listens on 80 although the UI `Dockerfile` says `EXPOSE 8080`; compose maps 8080 → 80, the chart uses 80 | Change `nginx.conf` to 8080 (frontend owner) and the chart / compose with it |
| D-18 | Datadog | Tracer downloaded at image build (latest release, as the reference), `DD_SERVICE` `factory-catalog` / `factory-catalog-balancer`, `DD_API_KEY` from KMS instead of plain text in `values.yaml` | Pin the tracer version; one `DD_SERVICE` for both APIs |
| D-19 | Jenkins build agent image | Reuses `tenen/jenkins-agent-shipment-service:jdk21` | A `tenen/jenkins-agent-factory-catalog` image |
| D-20 | Where do build and unit tests run in CI? | Inside the Docker image builds (reference approach); no separate `dotnet test` / `npm` stage, no integration-test stage | Dedicated stages on an agent with the .NET SDK and Node |
| D-21 | Image tag of the Balancer | Hash of `backend/src` plus the Dockerfile path, because `devops/docker/balancer-api.Dockerfile` is outside its build context | Move the Dockerfile into `Tenengroup.Balancer.Api` after the merge |
| D-22 | Balancer Dockerfile restore layer | `COPY . .` before restore (project list unknown before the merge); the catalog Dockerfile copies the `.csproj` files first for layer caching | Same `.csproj`-first layout once the Balancer projects exist |
| D-23 | `Dockerfile.dockerignore` next to each API Dockerfile | Added (BuildKit picks it up for the `backend/src` context; the root `.dockerignore` only applies to a root context) | `backend/src/.dockerignore` owned by the foundation |
| D-24 | Local AWS | LocalStack 3.8 in `docker-compose.yml` (`appsettings.Development.json` already points at `localhost:4566`) | Real dev AWS resources with personal credentials |
| D-25 | Local master catalog / shipment service | Not in compose; URLs configurable, defaults `host.docker.internal:5020` / `:5005` | Mock containers |
| D-26 | Feature-branch environments | Kept as in the reference: in-cluster PostgreSQL, own queues and topics, dev bucket, no SNS subscriptions, dev Azure registration | Deploy only `develop` / `release` / `master` |
| D-27 | Chart quirk inherited from the reference: Deployment selectors match `app` + `release` only, so the three Deployments of a release have overlapping selectors (Services select on `deployment` too) | Templates copied unchanged | Add the `deployment` label to the selector (immutable field: needs a release re-create) |
| D-28 | RDS size | `db.t3.small`, 20 GiB (prod 80 GiB, Multi-AZ, 35-day backups), no read replica | Share an existing instance; other classes |
