# Sprint 14 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 17
- P0: 17
- Epics: EP-15

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-262** [P0] Add Hot Chocolate DataLoaders for relationship-heavy fields - dependencies: IW8-202, IW8-150, IW8-217
- **IW8-263** [P0] Enable GraphQL cost/depth protection and trusted production settings - dependencies: IW8-005
- **IW8-264** [P0] Run SQL query-plan review and add final production indexes - dependencies: IW8-152, IW8-226, IW8-209
- **IW8-265** [P0] Create load test scripts for job search and candidate search - dependencies: IW8-264
- **IW8-266** [P0] Configure production CORS, security headers and HTTPS/HSTS - dependencies: IW8-004
- **IW8-267** [P0] Implement PII encryption/masking and secret-provider integration - dependencies: IW8-220, IW8-044
- **IW8-268** [P0] Add security log redaction tests - dependencies: IW8-267, IW8-035
- **IW8-269** [P0] Validate migration from empty database and previous release baseline - dependencies: IW8-264, IW8-015
- **IW8-270** [P0] Document backup/restore and production migration runbook - dependencies: IW8-269
- **IW8-271** [P0] Run Angular accessibility pass on candidate flows - dependencies: IW8-170, IW8-190
- **IW8-272** [P0] Run Angular accessibility pass on employer/admin flows - dependencies: IW8-209, IW8-261
- **IW8-273** [P0] Run browser/mobile responsive compatibility pass - dependencies: IW8-271, IW8-272
- **IW8-274** [P0] Create full marketplace regression E2E suite - dependencies: IW8-273, IW8-241, IW8-233
- **IW8-275** [P0] Create deterministic UAT/demo seed pack - dependencies: IW8-274
- **IW8-276** [P0] Finalize health, readiness and operational dashboards - dependencies: IW8-243, IW8-256
- **IW8-277** [P0] Create production deployment and smoke-test checklist - dependencies: IW8-270, IW8-276, IW8-274
- **IW8-278** [P0] Run final UAT defect triage and release-go/no-go gate - dependencies: IW8-275, IW8-277

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
