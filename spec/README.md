# Factory Catalog + Balancer — Specification Index

> **Requirements:** Phase overview v4; Module 1: Components v5; Module 2: Process capacity v5; Module 3: SKUs v4; Module 4: Balancer v4

## What is built

| Part | Location | Purpose |
|---|---|---|
| Factory Catalog API | `backend/src/app/Tenengroup.FactoryCatalog.*`, port 5010, schema `catalog` | Modules 1–3 (components, process capacity, SKUs) and the catalog side of Module 4: production quotes and the legacy balancer endpoints |
| Balancer API | `backend/src/app/Tenengroup.Balancer.*`, port 5011, schema `balancer` | Module 4 routing: consumes OCS orders and TG Supplier events, requests quotes from the catalog, picks the factory, publishes allocation events |
| Factory Catalog UI | `frontend/FactoryCatalogUI` | One React admin UI over both APIs: Configurations, Overview and Balancer pages |

## Structure

```
spec/
├── implementation-plan.md    # plan, conventions, module ownership
├── open-questions.md         # consolidated questions for the product owner (answer there)
├── questions/                # per-agent assumptions, the detailed source of open-questions.md
├── requirements/             # Confluence pages, saved verbatim (source of truth)
│   ├── 0-phase-overview.md
│   ├── 1-components.md
│   ├── 2-process-capacity.md
│   ├── 3-skus.md
│   └── 4-balancer.md
├── mockup/                   # UI prototype (Factory Catalog.html, lib/) and its change log (Phase 1 UI Changes.md)
├── backend/
│   ├── overview.md           # architecture, layers, shared building blocks with usage examples
│   ├── api.md                # REST conventions and shared endpoints
│   ├── domain.md             # shared entities and enums
│   ├── database.md           # schema `catalog`: every table and column, migrations
│   ├── integrations.md       # AWS building blocks, pointers to module integrations
│   ├── permissions.md        # roles and permission matrix
│   └── infrastructure.md     # configuration, middleware, background jobs
├── modules/                  # one file per module, written by the module owner
│   ├── 1-components.md       # Module 1: SAP sync, stock, OOS flags, alternatives
│   ├── 2-process-capacity.md # Module 2: processes, per-factory setup, capacity
│   ├── 3-skus.md             # Module 3: master catalog sync, mappings, factory share
│   ├── 4a-quotes.md          # Module 4, catalog side: production quotes, legacy balancer endpoints
│   └── 4b-balancer.md        # Module 4, Balancer service: routing, schema `balancer`, events, UI
├── frontend/
│   └── overview.md
└── devops/
    └── overview.md           # Docker, docker-compose, Jenkins, Helm, Terraform
```

Where the requirement pages and `implementation-plan.md` differ, the requirement pages win.

## Requirement versions in spec files

Every spec file records which requirement pages, in which Confluence version, it was written against. The version is
the one in the header of the saved page in `requirements/` (`<!-- Source: … | title: … | version N (…) -->`).

- **File header.** Right under the title: `> **Requirements:** Phase overview v4; Module 4: Balancer v4` — one entry
  per page the file depends on, title + `v` + version, separated by `;`.
- **Changed content.** A section, table row or paragraph added or changed against a newer version than the file
  header names it inline at its end, e.g. `(Module 4: Balancer v5, B-04)`; story ids (`C-`, `P-`, `S-`, `B-`) when
  the change concerns specific stories. When the whole file is brought up to the newer version, update the header
  and drop the inline marks.
- **Answers from other sources.** Content changed because an open question was answered (by the product owner or
  from another system's code) keeps the requirement version it was written against and names the answer, e.g.
  `(Module 4: Balancer v4; answers OQ-8, 2026-11-25)`.
- **New Confluence version.** Save the new page in `requirements/` first; every spec file whose header names an older
  version of that page is then reviewed and its header raised, or the differences are recorded inline.
- Files in `requirements/` are verbatim Confluence exports and carry their version in the source header only.

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
- Balancer: production quotes per order item (catalog, `modules/4a-quotes.md`) and the routing decision (Balancer
  service, `modules/4b-balancer.md`).
