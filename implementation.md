# IndiaWorks implementation.md — execution handbook v9

This file is the **operating manual for implementation agents and human developers**. It sits above every Jira story package and explains how the IndiaWorks v7 architecture is converted into production code through the v9 implementation backlog.

## 1. Product being built

IndiaWorks is a two-sided hiring SaaS for India:

- **Candidates** authenticate primarily by mobile OTP, browse/search/filter jobs, view structured job details, save/share jobs, apply with minimal friction, receive alerts, and optionally make their profile discoverable to verified employers.
- **Employers** authenticate by mobile/email OTP, operate inside an `Organisation` tenant, post structured jobs, review applicants, search the candidate database, manage recruiting workflow, promote jobs, and pay monthly/yearly/add-on fees for premium access.
- **Commercial rule:** an organisation/legal entity receives **5 lifetime free distinct candidate profile unlocks**. Search/applicant cards may be shown masked. The 6th distinct full-profile unlock requires an active paid/add-on `CANDIDATE_UNLOCK` entitlement. Re-opening one of the same five candidates does not consume another unlock. The same candidate across multiple recruiters/jobs counts once.
- **Candidate privacy wins over payment:** payment or plan entitlement can never bypass candidate discoverability, block, consent, or trust/safety rules.

## 2. Authoritative source hierarchy

When two artifacts appear to disagree, use this order:

1. `implementation.md` — implementation execution rules and v9 clarifications.
2. The story's own `agent-packages/<Sprint>/<Story>.md` — local executable contract.
3. `references/IndiaWorks_Unified_SaaS_Architecture.md` — approved product/technical architecture.
4. `references/IndiaWorks_Unified_SaaS_GraphQL.graphql` — conceptual GraphQL contract.
5. `references/IndiaWorks_Unified_SaaS_SQLServer.sql` — **relationship reference only**. It is not the production schema authority.
6. Shared coding documents under `agent-packages/_shared/`.

If an agent finds a material contradiction, stop the story and report it rather than silently choosing a new architecture.

## 3. Pinned technology baseline

### Frontend

- Angular **21.2.x**.
- Standalone components/routes; do not create NgModules.
- TypeScript **5.9.x**.
- Node.js **20+ / current LTS-compatible** runtime.
- Apollo Angular will be introduced by the dedicated GraphQL client story (`IW8-010`) for GraphQL transport/cache; do not replace Angular with another frontend framework.
- Signals for local/view state; RxJS for asynchronous streams/interoperability.
- Angular Material/CDK may be used for accessible primitives, overlays, dialogs and controls.
- Mobile-first candidate experience; responsive employer/admin experience.

### Backend

- .NET **10 LTS**, ASP.NET Core 10, C# 14.
- Hot Chocolate **16.6.7 stable**.
- EF Core **10.0.12**, SQL Server provider.
- Modular monolith. Do not create microservices in this implementation backlog.
- `IEntityTypeConfiguration<T>` owns relational mapping.
- Domain/application projects must not depend on ASP.NET Core or Infrastructure.

### Database

- Microsoft SQL Server.
- **EF Core code-first migrations are the only schema authority.**
- Migrations must be generated from the EF model/configuration.
- The v7 `.sql` file is for understanding relationships, indexes and intent only.

### Testing

- xUnit for domain/application tests.
- `WebApplicationFactory` for API integration tests.
- Testcontainers SQL Server for real relational behavior.
- Angular unit/component tests.
- Playwright for end-to-end critical paths.

## 4. Implementation sprint plan

Hermes works from the canonical master backlog and sprint-local working sets:

- Master backlog: `docs/backlog/IndiaWorks_Master_Backlog.csv`
- Sprint working sets: `docs/sprints/Sprint_XX/`
- Sprint context: `docs/sprints/Sprint_XX/SPRINT_CONTEXT.md`
- Sprint backlog view: `docs/sprints/Sprint_XX/backlog.csv`
- Sprint packages: `docs/sprints/Sprint_XX/agent-packages/<StoryId>.md`

The generated `.hermes/backlog-index.json` identifies the active sprint and dependency-ready story IDs for automation.

| Sprint    | Theme                                           | Duration | Exit goal                                                              | Stories | Points |
| --------- | ----------------------------------------------- | -------: | ---------------------------------------------------------------------- | ------: | -----: |
| Sprint 0  | Engineering foundation                          |   1 week | Repository, build, local environment, schema/API foundations           |      15 |     19 |
| Sprint 1  | Identity and authentication                     |  2 weeks | OTP, JWT/session, device security, candidate/employer personas         |      20 |     26 |
| Sprint 2  | Organisation and taxonomy                       |  2 weeks | Tenant/RBAC, invitations, commercial identity, canonical role taxonomy |      24 |     28 |
| Sprint 3  | Billing, entitlements and 5-candidate gate      |  2 weeks | Plans, subscriptions, free-five candidate access, paywall              |      20 |     27 |
| Sprint 4  | Employer job authoring backend                  |  2 weeks | Code-first job aggregate, requirements, validation, GraphQL mutations  |      18 |     22 |
| Sprint 5  | Employer job authoring frontend and publication |  2 weeks | Angular wizard, moderation, lifecycle, employer jobs                   |      17 |     20 |
| Sprint 6  | Candidate onboarding and profile                |  2 weeks | Progressive profile, preferences, consent, discoverability             |      18 |     21 |
| Sprint 7  | Job search, filters and matching backend        |  2 weeks | Search projections, facets, geo, ranking and job detail                |      20 |     25 |
| Sprint 8  | Candidate job discovery UX                      |  2 weeks | Job list, filters, details, save/share/report/deep links               |      18 |     20 |
| Sprint 9  | Applications, direct contact and alerts         |  2 weeks | Readiness, apply, contact policy, saved searches and alerts            |      20 |     23 |
| Sprint 10 | Employer applicants and recruiting CRM          |  2 weeks | Applicant workspace, unlock/paywall, stages, notes, interviews/offers  |      19 |     23 |
| Sprint 11 | Paid candidate database                         |  2 weeks | Candidate projection, paid search, unlock/reveal, job-based matching   |      17 |     20 |
| Sprint 12 | Promotions, notifications and billing polish    |  2 weeks | Boosts, attribution, notifications, invoices/add-ons                   |      17 |     18 |
| Sprint 13 | Trust, admin and analytics                      |  2 weeks | Moderation/admin, reporting/blocking, dashboards and audit             |      18 |     20 |
| Sprint 14 | Hardening, UAT and launch                       |  2 weeks | Performance, security, accessibility, regression, migration/release    |      17 |     25 |

## 5. Repository structure

This bundle describes the target repository. If `src/`, `tests/`, `e2e/`, `scripts/` or `docs/` are absent, execute S0 foundation stories first; do not start dependent feature stories by inventing missing dependency outputs.

```text
IndiaWorks/
├─ IndiaWorks.slnx
├─ Directory.Build.props
├─ Directory.Packages.props
├─ docker-compose.yml
├─ implementation.md
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

## 6. Story execution protocol

Every story is a bounded work order. Agents execute stories in `Sprint` + `SprintOrder` and never start until every dependency story is `DONE`.

### Before coding

1. Read `implementation.md`.
2. Read the story package completely.
3. Read every direct dependency package's **Handoff Contract / Outputs expected** section.
4. Inspect the real repository state; filenames in packages are expected targets, not permission to overwrite existing code blindly.
5. Run the story's preflight commands.
6. If a required dependency output is absent, mark the story `BLOCKED`; do not recreate the dependency inside the current story.

### While coding

1. Make the smallest change that satisfies the story.
2. Reuse existing concepts and names from the architecture.
3. Add tests with the production change, not after another story.
4. Do not refactor unrelated code.
5. Do not create temporary parallel tables/API operations to “make it work.”
6. Add one adjacent registration file only when required for DI/routes/DbContext/GraphQL registration, and document it.

### Before handoff

1. Build and test.
2. Apply any migration against a real SQL Server Testcontainer/local container.
3. Validate the story's acceptance checklist.
4. Confirm no sensitive value appears in logs, snapshots or search projections.
5. Write the handoff exactly in the package format.

## 7. Branch, commit and merge convention

Recommended branch:

```text
feature/IW8-146-candidate-job-eligibility
fix/IW8-xxx-short-description
```

Required commit format:

```text
feat(candidate): IW8-146 implement candidate job eligibility evaluator
fix(employer): IW8-xxx correct applicant unlock state
chore(platform): IW8-xxx update migration conventions
```

Merge rule:

- dependencies merge before dependents;
- migration stories merge in dependency order;
- never squash two unrelated agent stories into one implementation branch;
- if two stories touch the same registration file, later story rebases and preserves earlier registration.

## 8. Backend layering rules

### Domain

Contains entities, value objects, domain enums and invariants. It does not know GraphQL, REST, EF Core migrations, payment SDKs or UI.

### Application

Contains feature/use-case handlers, validators, authorization decisions, service interfaces and product-facing result/error codes.

### Infrastructure

Contains EF Core, SQL Server configuration, external provider adapters, search projection implementations and migration code.

### API

Contains Hot Chocolate types/resolvers, REST transport endpoints, authentication transport, problem/error mapping and composition root.

### Workers

Contains background execution for alerts, projection rebuilds, reconciliation and scheduled tasks. Workers invoke application services; business rules are not duplicated in workers.

## 9. Database / migration workflow

For every schema-changing story:

1. Add/change the domain model.
2. Add/change `IEntityTypeConfiguration<T>`.
3. Add required `DbSet<T>` only when direct access is useful; absence of a `DbSet` does not prevent mapping.
4. Generate migration:

```bash
dotnet ef migrations add <MeaningfulName> \
  --project src/backend/IndiaWorks.Infrastructure \
  --startup-project src/backend/IndiaWorks.Api
```

5. Review generated operations, FK delete behavior, precision, nullable columns and indexes.
6. Apply to an empty/local/Testcontainers SQL Server database.
7. Add integration coverage for the relationship/unique constraint that matters.
8. Do not manually edit a generated migration merely to hide a model bug. Correct the model and regenerate. Data backfills may be intentionally coded in a migration when the story explicitly requires them.

### Mandatory relational conventions

- `datetime2`/UTC for timestamps.
- explicit decimal precision for money.
- filtered unique indexes when nullable uniqueness is required.
- no nullable member inside a primary key.
- soft/operational statuses are explicit enums/codes, not inferred from missing rows.
- use restrictive deletes for auditable business history; do not cascade-delete applications, payment history, usage ledgers or access audit.

### Mandatory schema invariants

- `OrganisationCandidateUnlock` must enforce unique `(OrganisationId, CandidateProfileId)` so retries, multiple recruiters and multiple jobs cannot double-consume the free allowance.
- Externally retried commands and provider callbacks must use idempotency keys or inbox records before producing durable side effects.
- Candidate search/read projections must not store raw phone, email, Aadhaar, PAN or raw credential numbers.
- Candidate privacy, discoverability, consent and `CandidateOrganisationBlock` checks must run before entitlement or payment checks.
- `CandidateContactReveal` remains a separate audited concept from `OrganisationCandidateUnlock`.
- Subscription plan versions are immutable once sold; new commercial terms require a new version.
- Quota-like commercial state must use transaction/concurrency protection; two simultaneous unlocks must not turn 4 free unlocks into 6.
- Audit, payment, usage, application and moderation history use restrictive deletes and remain queryable for operations.

## 10. GraphQL implementation rules

GraphQL is the primary authenticated product API.

### Resolver rule

Resolvers should be thin:

```csharp
[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class CandidateMutations
{
    public Task<UnlockCandidatePayload> UnlockCandidateProfile(
        UnlockCandidateInput input,
        [Service] UnlockCandidateHandler handler,
        CancellationToken ct) => handler.HandleAsync(input, ct);
}
```

Business rules live in application handlers/services.

### Connections

All large list endpoints use cursor pagination with deterministic ordering. Always include a unique tie-breaker (`Id`) after the primary sort.

### N+1

Use Hot Chocolate DataLoaders for relationship fan-out. Never resolve 20 cards by issuing 20 independent organisation/taxonomy SQL queries.

### Errors

Use stable codes in payload/errors. Examples:

- `UNAUTHENTICATED`
- `FORBIDDEN`
- `VALIDATION_FAILED`
- `NOT_FOUND`
- `CONFLICT`
- `RATE_LIMITED`
- `JOB_NOT_OPEN`
- `HARD_REQUIREMENT_MISSING`
- `PAYWALL_REQUIRED`
- `ENTITLEMENT_EXHAUSTED`
- `CANDIDATE_NOT_DISCOVERABLE`
- `CONTACT_REVEAL_NOT_ALLOWED`

Do not make frontend behavior depend on English error text.

## 11. REST boundary

REST exists only where HTTP/provider transport behavior is more appropriate:

```text
POST /api/auth/otp/request
POST /api/auth/otp/verify
POST /api/auth/refresh
POST /api/auth/logout
POST /api/webhooks/payments/{provider}
GET  /health
GET  /health/ready
```

The current foundation exposes REST-style operational endpoints through ASP.NET Core minimal APIs (`/`, `/ready`, `/health`). Feature REST endpoints should live under `src/backend/IndiaWorks.Api/Rest/<Feature>/` as they are implemented. Future uploads/downloads/public redirect transport may also be REST. Do not mirror a GraphQL business mutation in REST.

## 12. Authentication/session rules

- One `UserAccount` may be both a candidate and an employer/recruiter.
- `UserSession` stores the active persona; available personas are derived from candidate profile + organisation memberships.
- Access JWTs are short lived.
- Refresh tokens are random, one-time rotated and stored only as hashes.
- OTP values are hashed, short-lived, attempt-limited and single-use.
- Do not log OTPs, access tokens or refresh tokens.
- Revoke/rotate sessions on sensitive identifier changes or known compromise.

## 13. Employer tenancy and RBAC

Commercial and recruiting state belongs to `Organisation`, not a single recruiter user.

Roles:

- `OWNER`
- `ADMIN`
- `RECRUITER`
- `VIEWER`
- `BILLING_ADMIN`

Every employer operation requires both:

1. valid organisation membership/role;
2. commercial entitlement when the feature is premium.

## 14. Candidate onboarding rule

Mobile OTP creates/authenticates the account and candidate persona. The candidate can immediately browse jobs.

Do **not** require, by default:

- resume;
- Aadhaar;
- PAN;
- complete education;
- complete employment history.

Profile completion improves matching but does not become a universal application gate.

## 15. Job taxonomy and authoring

Canonical job identity is:

```text
Industry (employer axis)
Role family → Role → Specialisation → Work context
```

Independent axes include:

- engagement type;
- work mode;
- location;
- schedule;
- compensation;
- skills;
- credentials;
- assets;
- education;
- language;
- urgency.

Never reintroduce `Work From Home`, `Government`, `Gig Worker` or collar labels as occupations.

## 16. Candidate application gating

Requirement importance and application gate are separate:

```text
RequirementLevel: REQUIRED | PREFERRED | OPTIONAL
ApplicationGate:   BLOCK | WARN | NONE
```

A preferred experience/education mismatch normally warns. A licence/registration required to perform a regulated role may block.

Application flow:

1. load live job;
2. confirm `PUBLISHED` and discoverable;
3. evaluate candidate against hard/soft requirements;
4. return warnings/missing fields;
5. collect only missing hard-required data/questions;
6. on submit, recheck job state and requirements;
7. create `CandidateApplication`, apply-time snapshot and `APPLIED` event transactionally;
8. duplicate submission returns the existing result/conflict deterministically.

## 17. Five-candidate commercial gate

Canonical algorithm:

```text
INPUT organisationId, candidateProfileId, context(job/search/applicant), idempotencyKey

1. Authorize recruiter membership.
2. Enforce candidate privacy/block/discoverability policy.
3. Check OrganisationCandidateUnlock(org,candidate).
   - If present -> return existing access. Consume nothing.
4. Lock/read OrganisationCommercialAccessState for the commercial identity.
5. Count/track distinct FREE_ALLOWANCE unlocks.
6. If free used < 5:
   - insert unique OrganisationCandidateUnlock(source=FREE_ALLOWANCE)
   - commit
   - return FREE_UNLOCKED
7. Otherwise ask central EntitlementService for CANDIDATE_UNLOCK.
8. If allowed:
   - create unlock(source=PAID_ENTITLEMENT or ADD_ON)
   - append idempotent EntitlementUsageEvent when metered
   - commit
   - return PAID_UNLOCKED
9. Else:
   - append CandidatePaywallEvent
   - return PAYWALL_REQUIRED with current plan/usage context
```

Concurrency must not allow two simultaneous requests to turn 4 free unlocks into 6 free unlocks.

## 18. Search/filter architecture

Search/list UI must not issue large normalized OLTP joins for every filter toggle.

Canonical write model remains normalized. Derived read/search projections support:

- keyword/title/aliases;
- family/role/specialisation/work context;
- city/locality/PIN/radius/distance;
- work mode;
- engagement type;
- experience;
- education/course;
- compensation and pay frequency;
- bonus/incentive;
- shift;
- freshness;
- urgency;
- language;
- skills;
- credentials;
- assets;
- benefits;
- employer verification/source/sector/industry/size;
- call/WhatsApp availability.

Facet counts are contextual to the current filter set.

Search projections **must not contain raw candidate phone, email, Aadhaar/PAN or raw credential numbers**.

## 19. Job details, save, share and deep links

Job detail is structured data; do not parse critical facts from free-text description.

Job detail includes pay, work mode/type, location, schedule, requirements, benefits, interview data, employer trust and candidate contact actions.

Job sharing uses stable HTTPS public links:

```text
https://<domain>/j/<publicCode>/<slug>
```

The same URL must support Android App Links, iOS Universal Links and mobile web fallback. Do not use Firebase Dynamic Links.

## 20. Alerts and notifications

Saved job search is the source of truth for alert criteria. Alert execution uses:

```text
SavedJobSearch
  -> JobAlertSubscription
      -> JobAlertRun
          -> JobAlertDelivery
```

Workers use a watermark and per-job delivery dedupe. Respect channel consent, quiet hours, blocks and closed jobs. Notification provider failures are retryable and must not duplicate a successful delivery.

## 21. Employer-candidate relationship model

Three separate concepts must remain separate:

```text
CandidateApplication           Candidate <-> Job
EmployerCandidateRelationship  Candidate <-> Organisation recruiting CRM
OrganisationCandidateUnlock    Candidate <-> Organisation commercial access
```

Never infer one merely because another exists.

## 22. Privacy, trust and safety

- Candidate talent-pool discoverability is explicit.
- Payment cannot override discoverability or blocks.
- `CandidateOrganisationBlock` is centrally enforced in feed/contact/search/reveal/alerts as applicable.
- Reports and moderation are auditable.
- Job lifecycle and moderation lifecycle are independent.
- Search/analytics events contain IDs and safe dimensions rather than revealed contact data.

## 23. Observability

All requests should have a correlation/trace id.

Structured logs include:

- operation/use-case;
- safe user/organisation ids;
- correlation id;
- outcome/error code;
- elapsed time.

Never log:

- OTP;
- JWT/refresh tokens;
- full phone/email when not necessary;
- Aadhaar/PAN/raw credential numbers;
- payment instrument data.

Key product/ops metrics include:

- OTP send/verify failure rate;
- GraphQL latency/error rate;
- search latency and facet latency;
- application conversion;
- unlock/paywall conversion;
- webhook retries;
- notification failures;
- background worker lag.

## 24. Testing pyramid and minimum story test matrix

Every story package gives concrete cases, but the baseline is:

### Domain/Application

- happy path;
- invalid input/invariant;
- authorization/ownership when relevant;
- idempotency/retry for externally repeated operations;
- concurrency when a quota/counter is consumed.

### Integration

Use real SQL Server Testcontainers for:

- FK/unique constraints;
- transactions/concurrency;
- migrations;
- GraphQL/REST contract;
- cursor ordering;
- projection persistence.

### Angular

Test:

- loading;
- empty;
- success;
- error;
- user interaction/state change;
- correct operation variables.

### E2E critical paths

1. candidate OTP → filter → job detail → apply;
2. employer OTP → organisation → job draft → publish;
3. employer applicants → unlock candidates 1–5 → 6th paywall;
4. subscribe/add-on → 6th unlock succeeds;
5. candidate search → unlock → contact reveal → CRM;
6. alert delivery/deep link → job detail;
7. moderation/block prevents disallowed access.

## 25. Acceptance criteria standard

Acceptance criteria must be observable and binary. Each package contains story-specific Given/When/Then checks. A story is not DONE because “code was added”; it is DONE only when its behavior can be demonstrated through tests or the public API/UI.

## 26. Sprint exit policy

A sprint exits when:

- all P0 stories are DONE;
- no unresolved defect blocks a next-sprint P0 dependency;
- migrations in the sprint apply in order from clean baseline;
- GraphQL schema generation succeeds;
- backend/frontend test suites required by completed stories are green;
- handoff packages are updated if a dependency contract changed.

P1/P2 may move only when they do not block future P0 dependencies.

## 27. Agent orchestration loop

Pseudo-orchestrator:

```text
for sprint in S0..S14:
    while stories remain:
        ready = stories where all Dependencies == DONE
        choose lowest SprintOrder, then P0 before P1
        give exactly one story package to one implementation agent
        agent implements + tests + handoff
        reviewer validates package acceptance criteria
        if accepted:
            mark DONE
        else:
            return same story to correction; do not open dependent story
    run sprint integration gate
```

Parallelism is allowed only for ready stories that do not write the same schema/API contract or depend on each other's outputs.

## 28. Definition of DONE for the entire product increment

A story is DONE only when:

- dependency assumptions are satisfied;
- code builds;
- schema/API/UI contract is implemented;
- automated tests pass;
- security/privacy rules are respected;
- migration is included and validated when applicable;
- generated code is regenerated when GraphQL operations change;
- package acceptance criteria are checked;
- changed files/commands/migration/limitations are present in handoff;
- no unexplained out-of-scope code is changed.
