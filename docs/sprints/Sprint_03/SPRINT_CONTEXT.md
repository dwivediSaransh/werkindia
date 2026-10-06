# Sprint 3 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 20
- P0: 19
- P1: 1
- Epics: EP-04

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-060** [P0] Create subscription plan and entitlement entities - dependencies: IW8-038
- **IW8-061** [P0] Create organisation subscription and usage entities - dependencies: IW8-060
- **IW8-062** [P0] Add billing EF configurations and migration - dependencies: IW8-060, IW8-061, IW8-007
- **IW8-063** [P0] Seed FREE_EVALUATION and paid plan skeletons - dependencies: IW8-062
- **IW8-064** [P0] Implement central entitlement evaluator - dependencies: IW8-062
- **IW8-065** [P0] Create OrganisationCommercialAccessState entity - dependencies: IW8-044, IW8-062
- **IW8-066** [P0] Create OrganisationCandidateUnlock entity - dependencies: IW8-065
- **IW8-067** [P0] Create CandidatePaywallEvent entity - dependencies: IW8-065
- **IW8-068** [P0] Add candidate-access EF migration and unique indexes - dependencies: IW8-065, IW8-066, IW8-067, IW8-007
- **IW8-069** [P0] Provision five free distinct candidate unlocks per verified organisation - dependencies: IW8-068, IW8-044
- **IW8-070** [P0] Implement transactional candidate unlock decision service - dependencies: IW8-069, IW8-064
- **IW8-071** [P0] Expose candidateAccessStatus GraphQL query - dependencies: IW8-070
- **IW8-072** [P0] Expose unlockCandidateProfile GraphQL mutation - dependencies: IW8-070
- **IW8-073** [P0] Create payment gateway abstraction and checkout session model - dependencies: IW8-062
- **IW8-074** [P0] Create BillingWebhookInbox and payment transaction entities - dependencies: IW8-073
- **IW8-075** [P0] Expose payment webhook REST endpoint with inbox idempotency - dependencies: IW8-074
- **IW8-076** [P0] Expose plans, subscription and checkout GraphQL operations - dependencies: IW8-064, IW8-073, IW8-075
- **IW8-077** [P0] Build Angular five-candidate paywall modal - dependencies: IW8-071, IW8-072, IW8-076
- **IW8-078** [P0] Build employer billing and subscription page - dependencies: IW8-076, IW8-056
- **IW8-079** [P1] Add concurrent free-five and billing idempotency tests - dependencies: IW8-072, IW8-075

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
