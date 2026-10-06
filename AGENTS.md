# IndiaWorks Operating Standard

These instructions apply repo-wide. Treat this workspace as a production implementation backlog and future application repository, not as a disposable planning folder.

## Mission

Act as the long-term technical owner for IndiaWorks.

- Optimize for correctness, security, maintainability, operability, and user trust before speed.
- Prefer simple, explicit designs that can be extended safely.
- Keep product, business, and engineering concerns connected: candidate privacy, employer trust, compliance, monetization, and supportability all matter.
- Do not create parallel sources of truth. Improve the existing handbook, backlog, sprint context, package, or reference file that owns the topic.

## Canonical Working Set

- Master handbook: `implementation.md`
- Canonical backlog/status ledger: `docs/backlog/IndiaWorks_Master_Backlog.csv`
- Master monitoring index: `docs/backlog/IndiaWorks_Master_Backlog_Index.json`
- Package quality matrix: `docs/backlog/IndiaWorks_Quality_Matrix.csv`
- Hermes automation index: `.hermes/backlog-index.json`
- Active-sprint context: `docs/sprints/Sprint_XX/SPRINT_CONTEXT.md`
- Active-sprint backlog view: `docs/sprints/Sprint_XX/backlog.csv`
- Story package: `docs/sprints/Sprint_XX/agent-packages/<StoryId>.md`
- Shared package guidance: `docs/shared/agent-guides/`
- Approved architecture references: `references/`

The master backlog is the source of truth for status. Sprint `backlog.csv` files are bounded working views and must stay consistent with the master ledger.

## Non-Negotiable Rules

1. Every code commit must reference a Story ID, for example `feat(auth): IW8-024 add OTP verification`.
2. Before implementation work, pull or otherwise verify the latest workspace state when a remote Git repository is available.
3. Preserve unrelated dirty work. Never reset, delete, or rewrite user changes unless explicitly asked.
4. Do not mark a story `DONE` without verification evidence for its acceptance criteria.
5. Never expose secrets, credentials, production connection strings, PII, Aadhaar/PAN values, raw contact values, or sensitive provider payloads in code, logs, docs, or responses.
6. Staging and local automation must fail closed when required non-production configuration is missing. Do not silently fall back to production services.

## Agent Skills And Review Habits

- Use a `ponytail:` code comment only for an intentional, bounded shortcut. The comment must name the ceiling and the upgrade path. Do not use it to excuse incomplete acceptance criteria.
- Apply the change forest habit before and after edits: identify the smallest connected set of files/contracts touched by the story, check direct callers/dependencies/tests/docs, and avoid unrelated branches of the repository.
- Prefer Graphify/codebase-inspection style navigation when available for broad relationship questions, then confirm with direct file reads before editing.
- Use `rg` first for repository search and keep Hermes context bounded to the selected story, direct dependencies, and affected implementation files.

## Hermes Execution Rules

1. One cron run implements exactly one Story ID.
2. Resume the single `IN PROGRESS` story in the active sprint when one exists.
3. Otherwise select only `TODO` stories in the active sprint whose dependencies are all `DONE`.
4. Sort eligible stories by priority (`P0`, `P1`, `P2`, `P3`), then canonical backlog row order.
5. Never jump to a later sprint while the active sprint has unfinished stories.
6. Never implement a dependency or downstream story inside the selected story.
7. If a dependency contract is missing or contradictory, mark the selected story `BLOCKED` with the exact blocker.
8. Keep `docs/backlog/IndiaWorks_Master_Backlog_Index.json`, `.hermes/backlog-index.json`, and `docs/backlog/IndiaWorks_Quality_Matrix.csv` aligned with backlog/package changes.

## Required Reading Per Story

Read only the bounded working set needed for the selected story:

1. `implementation.md`
2. Active sprint `SPRINT_CONTEXT.md`
3. Active sprint `backlog.csv`
4. Selected story package
5. Direct dependency packages only
6. Relevant files under `docs/shared/agent-guides/`
7. Architecture, GraphQL, SQL, ADR, or traceability references only when the story needs those contracts

Do not load every package into context.

## Architecture Rules

- Preserve the modular monolith architecture in `implementation.md`.
- Keep domain logic out of transport and persistence layers.
- Keep infrastructure details out of domain models.
- Use explicit contracts, ports, adapters, and focused services instead of scattered conditionals.
- Prefer extension through existing patterns before adding new abstractions.
- EF Core code-first migrations are the production schema authority.
- `references/IndiaWorks_Unified_SaaS_SQLServer.sql` is relationship/index guidance only and must not be executed as the application schema.
- GraphQL is the primary product API. REST is reserved for OTP/session, provider webhooks, health/readiness, public file transport, and similar infrastructure edges.

## Domain Guardrails

- Candidate privacy, consent, discoverability, block, and trust/safety checks must happen before payment or entitlement checks.
- `OrganisationCandidateUnlock`, `CandidateContactReveal`, `CandidateApplication`, and `EmployerCandidateRelationship` are separate concepts and must not be collapsed.
- Search projections must not contain raw candidate phone, email, Aadhaar, PAN, or raw credential numbers.
- Commercial rules, free quotas, payment gates, and reveal/audit history must be observable and auditable.
- Preserve stable machine-readable error/result codes for authorization, validation, rate limit, payment, and provider failures.

## Backend Rules

- Keep API/resolver/controller code thin: validate, authorize, delegate, and shape transport responses.
- Put orchestration and workflows in application services.
- Put canonical business rules in domain models/services.
- Put EF Core, provider clients, repositories, and external integrations behind infrastructure adapters.
- Avoid N+1 queries, unbounded reads, hidden production fallbacks, and provider-specific logic leaking upward.
- Use structured logging with redaction. Failures should be diagnosable without exposing sensitive data.

## Frontend Rules

- Use Angular standalone, typed, SSR-safe patterns when frontend stories begin.
- Keep pages lean and move reusable logic into feature services, utilities, or focused components.
- Handle loading, empty, error, and success states.
- Preserve semantic HTML, keyboard accessibility, focus states, useful labels, and responsive layouts.
- Avoid visual churn unless the story explicitly owns that UI surface.

## Testing And Verification

- Add or update meaningful tests for behavior changes whenever the repository supports it.
- At minimum, run the story package validation commands or record why they could not run.
- For schema work, verify EF migrations and an empty-database update path where applicable.
- For API work, verify authorization, validation, success, and at least one failure edge.
- For UI work, verify build/test commands and key user states.

## Status And Evidence

Use only these statuses in the master backlog and sprint views:

- `TODO`
- `IN PROGRESS`
- `BLOCKED`
- `DONE`

When completing or blocking a story, update:

- `Status`
- `Files Changed`
- `What Was Verified`
- `Known Gaps`

Verification notes must name the exact command, test, build, migration check, manual review, or blocker.

## Commit Gate

Every commit must include the Story ID:

- `feat(scope): IW8-xxx short summary`
- `fix(scope): IW8-xxx short summary`
- `chore(scope): IW8-xxx short summary`

`IW8-*` is a stable story namespace and must not be renamed.
