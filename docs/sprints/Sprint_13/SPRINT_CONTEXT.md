# Sprint 13 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 18
- P0: 17
- P1: 1
- Epics: EP-13, EP-14

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-244** [P0] Create CandidateOrganisationBlock entity - dependencies: IW8-115, IW8-036
- **IW8-245** [P0] Implement block/unblock enforcement service - dependencies: IW8-244
- **IW8-246** [P0] Create MarketplaceReport entity and moderation states - dependencies: IW8-016
- **IW8-247** [P0] Expose block and report GraphQL operations - dependencies: IW8-245, IW8-246
- **IW8-248** [P0] Create admin authorization policy and audit wrapper - dependencies: IW8-029
- **IW8-249** [P0] Expose job moderation queue GraphQL operations - dependencies: IW8-108, IW8-248
- **IW8-250** [P0] Build admin job moderation queue UI - dependencies: IW8-249
- **IW8-251** [P0] Expose organisation verification/admin operations - dependencies: IW8-044, IW8-248
- **IW8-252** [P0] Build admin organisation verification UI - dependencies: IW8-251
- **IW8-253** [P0] Build marketplace reports and block review UI - dependencies: IW8-247, IW8-248
- **IW8-254** [P0] Create application and candidate-access analytics events - dependencies: IW8-173, IW8-067, IW8-220
- **IW8-255** [P0] Implement employer funnel aggregate queries - dependencies: IW8-151, IW8-254, IW8-204, IW8-206
- **IW8-256** [P0] Build employer dashboard KPI cards - dependencies: IW8-255
- **IW8-257** [P0] Implement paywall conversion analytics query - dependencies: IW8-067, IW8-075
- **IW8-258** [P0] Build admin plan and entitlement configuration UI - dependencies: IW8-062, IW8-248
- **IW8-259** [P0] Build admin audit search page - dependencies: IW8-248, IW8-254
- **IW8-260** [P0] Add abuse/rate-limit policy for OTP, apply, contact, report and unlock - dependencies: IW8-020, IW8-175, IW8-181, IW8-247, IW8-070
- **IW8-261** [P1] Add admin/trust/analytics integration tests - dependencies: IW8-250, IW8-252, IW8-253, IW8-256, IW8-259, IW8-260

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
