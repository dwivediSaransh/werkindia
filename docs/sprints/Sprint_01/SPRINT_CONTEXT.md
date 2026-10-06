# Sprint 1 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 20
- P0: 19
- P1: 1
- Epics: EP-01

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-016** [P0] Create UserAccount and LoginIdentifier domain entities - dependencies: IW8-006
- **IW8-017** [P0] Create OtpChallenge domain entity and OTP purpose enum - dependencies: IW8-016
- **IW8-018** [P0] Add EF configurations and migration for account and OTP tables - dependencies: IW8-016, IW8-017, IW8-007
- **IW8-019** [P0] Create IOtpSender abstraction and development fake provider - dependencies: IW8-017
- **IW8-020** [P0] Implement OTP policy service - dependencies: IW8-017
- **IW8-021** [P0] Implement request-OTP use case - dependencies: IW8-018, IW8-019, IW8-020
- **IW8-022** [P0] Expose request OTP REST endpoint - dependencies: IW8-021
- **IW8-023** [P0] Implement verify-OTP use case - dependencies: IW8-018, IW8-020
- **IW8-024** [P0] Create JWT access-token service - dependencies: IW8-023
- **IW8-025** [P0] Create UserDevice and UserSession entities - dependencies: IW8-016
- **IW8-026** [P0] Add session/device EF configuration and migration - dependencies: IW8-025, IW8-007
- **IW8-027** [P0] Implement refresh-token rotation service - dependencies: IW8-024, IW8-026
- **IW8-028** [P0] Expose verify, refresh and logout REST endpoints - dependencies: IW8-023, IW8-024, IW8-027
- **IW8-029** [P0] Implement candidate/employer persona resolution - dependencies: IW8-027
- **IW8-030** [P0] Expose session and switch-persona GraphQL operations - dependencies: IW8-029, IW8-005
- **IW8-031** [P0] Create Angular auth session store and token interceptor - dependencies: IW8-010, IW8-028
- **IW8-032** [P0] Build candidate mobile OTP login screen - dependencies: IW8-022, IW8-028, IW8-031
- **IW8-033** [P0] Build employer mobile/email OTP login screen - dependencies: IW8-022, IW8-028, IW8-031
- **IW8-034** [P0] Add Angular auth and persona route guards - dependencies: IW8-030, IW8-031
- **IW8-035** [P1] Add authentication integration and E2E tests - dependencies: IW8-032, IW8-033, IW8-034

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
