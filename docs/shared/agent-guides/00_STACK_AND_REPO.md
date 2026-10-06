# IndiaWorks v9 implementation baseline

## Pinned stack

- Angular: **21.2.x** (standalone components/routes)
- Node: **20+ / current LTS-compatible**; TypeScript **5.9.x**
- Apollo Angular: introduced by `IW8-010`; use it for GraphQL transport/cache once that story lands.
- .NET: **10 LTS**, C# 14
- EF Core SQL Server: **10.0.12**
- Hot Chocolate GraphQL: **16.6.7 stable**
- Database: Microsoft SQL Server, **EF Core code-first migrations are the only schema authority**
- Testing: xUnit + WebApplicationFactory + Testcontainers; Angular tests; Playwright E2E

## Architectural style

Start as a **modular monolith**. Keep domain boundaries explicit in folders/projects and avoid network microservices until scaling/team ownership requires them. GraphQL is the primary product API. REST is reserved for OTP/session transport, provider webhooks, health/readiness, and file/public transport cases.

## Repository tree

```text
IndiaWorks/
├─ IndiaWorks.slnx
├─ Directory.Build.props
├─ Directory.Packages.props
├─ docker-compose.yml
├─ src/
│  ├─ backend/
│  │  ├─ IndiaWorks.Api/
│  │  │  ├─ GraphQL/<Feature>/
│  │  │  ├─ Rest/<Feature>/
│  │  │  └─ Program.cs
│  │  ├─ IndiaWorks.Domain/<Domain>/
│  │  ├─ IndiaWorks.Application/Features/<Feature>/<UseCase>/
│  │  ├─ IndiaWorks.Infrastructure/
│  │  │  ├─ Persistence/Configurations/
│  │  │  ├─ Persistence/Migrations/
│  │  │  ├─ Providers/
│  │  │  └─ Search/
│  │  ├─ IndiaWorks.Contracts/
│  │  └─ IndiaWorks.Workers/
│  └─ web/
│     └─ src/app/
│        ├─ core/{auth,graphql,http,guards,config}/
│        ├─ features/candidate/
│        ├─ features/employer/
│        ├─ features/admin/
│        └─ shared/{ui,models,utils}/
├─ tests/
│  ├─ IndiaWorks.Domain.Tests/
│  ├─ IndiaWorks.Application.Tests/
│  ├─ IndiaWorks.IntegrationTests/
│  └─ IndiaWorks.ArchitectureTests/
├─ e2e/
├─ scripts/
└─ docs/

```

## Non-negotiable rules

1. Domain entities do not use EF data annotations for relational mapping; use `IEntityTypeConfiguration<T>`.
2. Never edit generated EF migration code to implement domain behavior. Fix the model/configuration and regenerate unless the migration requires an intentional data transformation.
3. Never expose EF entities directly from GraphQL/REST.
4. Candidate contact/identity data must not enter search projection tables.
5. Every meterable/paywall/payment mutation accepts or derives an idempotency key.
6. The sixth distinct candidate unlock is paywalled organisation-wide unless a paid/add-on `CANDIDATE_UNLOCK` entitlement allows it.
7. Soft employer preferences warn; only explicit legal/safety/credential essentials hard-block candidate applications.
8. GraphQL connections use deterministic cursor ordering.
9. Use UTC in persistence; localize only at UI boundaries.
10. A story may touch only the files listed in its package unless an unavoidable compile fix is documented in handoff.

## Official version references

- Angular releases: https://angular.dev/reference/releases
- Angular compatibility: https://angular.dev/reference/versions
- .NET support: https://dotnet.microsoft.com/en-us/platform/support/policy
- EF Core 10: https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew
- Hot Chocolate: https://chillicream.com/docs/hotchocolate
- Apollo Angular: https://www.npmjs.com/package/apollo-angular
