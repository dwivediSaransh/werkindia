# Sprint 12 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 17
- P0: 16
- P1: 1
- Epics: EP-12, EP-09, EP-04, EP-14

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-227** [P0] Create PromotionProduct and JobPromotion entities - dependencies: IW8-080, IW8-062
- **IW8-228** [P0] Add promotion EF configuration and migration - dependencies: IW8-227, IW8-007
- **IW8-229** [P0] Implement promotion purchase/activation service - dependencies: IW8-228, IW8-064, IW8-110
- **IW8-230** [P0] Apply bounded promotion boost after eligibility/relevance - dependencies: IW8-229, IW8-147
- **IW8-231** [P0] Create promotion attribution event model - dependencies: IW8-230, IW8-151
- **IW8-232** [P0] Expose promotion products and promoteJob GraphQL operations - dependencies: IW8-229
- **IW8-233** [P0] Build employer promote-job UI - dependencies: IW8-232, IW8-113
- **IW8-234** [P0] Create NotificationPreference and NotificationDelivery entities - dependencies: IW8-120
- **IW8-235** [P0] Implement unified notification delivery service - dependencies: IW8-234, IW8-188
- **IW8-236** [P0] Create in-app notification query and read state - dependencies: IW8-235
- **IW8-237** [P0] Build notification center and preference UI - dependencies: IW8-236, IW8-189
- **IW8-238** [P0] Create billing invoice and payment transaction queries - dependencies: IW8-075
- **IW8-239** [P0] Build invoice/history section on employer billing page - dependencies: IW8-238, IW8-078
- **IW8-240** [P0] Implement one-time entitlement add-on purchase flow - dependencies: IW8-075, IW8-064
- **IW8-241** [P0] Build add-on purchase UI - dependencies: IW8-240, IW8-078
- **IW8-242** [P0] Implement renewal, past-due and cancellation state worker - dependencies: IW8-075, IW8-064
- **IW8-243** [P1] Add promotion/notification/billing operational telemetry - dependencies: IW8-232, IW8-235, IW8-242

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
