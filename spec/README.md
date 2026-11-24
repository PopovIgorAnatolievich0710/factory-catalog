# Factory Catalog + Balancer — Specification Index

## Structure

```
spec/
├── implementation-plan.md   # plan, conventions, module ownership
├── open-questions.md        # consolidated assumptions waiting for an answer
├── questions/               # per-agent assumptions (merged into open-questions.md)
├── requirements/            # Confluence pages, saved verbatim (source of truth)
│   ├── 0-phase-overview.md
│   ├── 1-components.md
│   ├── 2-process-capacity.md
│   ├── 3-skus.md
│   └── 4-balancer.md
├── mockup/                  # UI prototype and its change log
├── backend/
│   ├── overview.md          # architecture, layers, shared building blocks with usage examples
│   ├── api.md               # REST conventions and shared endpoints
│   ├── domain.md            # shared entities and enums
│   ├── database.md          # schema `catalog`: every table and column, migrations
│   ├── integrations.md      # AWS building blocks, pointers to module integrations
│   ├── permissions.md       # roles and permission matrix
│   └── infrastructure.md    # configuration, middleware, background jobs
├── modules/                 # one file per module, written by the module owner
│   ├── 1-components.md
│   ├── 2-process-capacity.md
│   ├── 3-skus.md
│   ├── 4a-quotes.md
│   └── 4b-balancer.md
├── frontend/
│   └── overview.md
└── devops/
    └── overview.md
```

Where the requirement pages and `implementation-plan.md` differ, the requirement pages win.

## Quick reference

| Area | Key technology |
|---|---|
| Backend | .NET 10, ASP.NET Core, EF Core 10, PostgreSQL 15 |
| Frontend | React 18, TypeScript, Vite, Redux, MUI, MSAL, CASL |
| Database | PostgreSQL `factory_catalog` — schemas `catalog`, `balancer`, `hangfire`, `hangfire_balancer` |
| Background jobs | Hangfire + PostgreSQL storage |
| Messaging / files | AWS SQS / SNS / S3 |
| Observability | Serilog → stdout |
| Auth | Basic Auth + Azure AD (MSAL/JWT), AD-group roles |

## Service purpose

- Components: daily SAP sync of the component master and stock per factory, critical levels, manual OOS, alternatives.
- Process capacity: process list, per-factory setup, capacity in 10% steps, labor cost.
- SKUs: read-only SKU view from the master catalog with components, processes and factory share.
- Balancer: production quotes per order item (catalog) and the routing decision (balancer service).
