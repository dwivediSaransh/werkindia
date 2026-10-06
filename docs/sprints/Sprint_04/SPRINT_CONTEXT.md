# Sprint 4 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 18
- P0: 17
- P1: 1
- Epics: EP-05

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-080** [P0] Create JobPosting aggregate root and lifecycle enums - dependencies: IW8-038, IW8-049
- **IW8-081** [P0] Create JobPostingRole and JobLocation entities - dependencies: IW8-080, IW8-045
- **IW8-082** [P0] Create job experience and education requirement entities - dependencies: IW8-080
- **IW8-083** [P0] Create skill/language/credential/asset requirement entities - dependencies: IW8-080, IW8-048
- **IW8-084** [P0] Create JobSchedule and job contact policy/window entities - dependencies: IW8-080
- **IW8-085** [P0] Create CompensationPackage and component entities - dependencies: IW8-080
- **IW8-086** [P0] Create BenefitType and JobBenefit entities - dependencies: IW8-080
- **IW8-087** [P0] Create JobScreeningQuestion entity - dependencies: IW8-080
- **IW8-088** [P0] Create JobInterviewWindow entity - dependencies: IW8-080
- **IW8-089** [P0] Add job aggregate EF configurations - dependencies: IW8-081, IW8-082, IW8-083, IW8-084, IW8-085, IW8-086, IW8-087, IW8-088
- **IW8-090** [P0] Generate job-authoring code-first migration - dependencies: IW8-089, IW8-007
- **IW8-091** [P0] Implement role-template default requirement service - dependencies: IW8-052, IW8-053, IW8-054, IW8-083
- **IW8-092** [P0] Implement CreateJobDraft application service - dependencies: IW8-090, IW8-091, IW8-037
- **IW8-093** [P0] Implement UpdateJobDraft application service - dependencies: IW8-092
- **IW8-094** [P0] Implement job publication validation service - dependencies: IW8-092
- **IW8-095** [P0] Expose create/update job draft GraphQL mutations - dependencies: IW8-092, IW8-093
- **IW8-096** [P0] Expose role-template defaults GraphQL query - dependencies: IW8-091
- **IW8-097** [P1] Add job-authoring domain and integration tests - dependencies: IW8-094, IW8-095, IW8-096

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
