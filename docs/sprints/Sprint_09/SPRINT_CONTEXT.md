# Sprint 9 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 20
- P0: 19
- P1: 1
- Epics: EP-08, EP-09

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-171** [P0] Create CandidateApplication entity and unique candidate/job constraint - dependencies: IW8-115, IW8-080
- **IW8-172** [P0] Create ApplicationSnapshot, answer and event entities - dependencies: IW8-171, IW8-087
- **IW8-173** [P0] Add application EF configurations and migration - dependencies: IW8-171, IW8-172, IW8-007
- **IW8-174** [P0] Implement prepareJobApplication readiness service - dependencies: IW8-146, IW8-173
- **IW8-175** [P0] Implement idempotent applyToJob service - dependencies: IW8-174
- **IW8-176** [P0] Expose prepare/apply GraphQL operations - dependencies: IW8-175
- **IW8-177** [P0] Build candidate application confirmation flow - dependencies: IW8-176, IW8-162
- **IW8-178** [P0] Expose myApplications query and application detail timeline - dependencies: IW8-173
- **IW8-179** [P0] Build candidate applications list and timeline UI - dependencies: IW8-178
- **IW8-180** [P0] Implement job contact-policy availability service - dependencies: IW8-084, IW8-115
- **IW8-181** [P0] Create CandidateContactEvent entity and mutation - dependencies: IW8-180
- **IW8-182** [P0] Add direct call/message actions to job detail - dependencies: IW8-181, IW8-162
- **IW8-183** [P0] Create SavedJobSearch entity - dependencies: IW8-148, IW8-115
- **IW8-184** [P0] Create JobAlertSubscription, JobAlertRun and JobAlertDelivery entities - dependencies: IW8-183
- **IW8-185** [P0] Add alert EF configurations and migration - dependencies: IW8-183, IW8-184, IW8-007
- **IW8-186** [P0] Expose save-search and job-alert GraphQL operations - dependencies: IW8-185
- **IW8-187** [P0] Implement job alert evaluation worker - dependencies: IW8-186, IW8-148
- **IW8-188** [P0] Create notification provider abstraction and dev provider - dependencies: IW8-187
- **IW8-189** [P0] Build candidate saved search and alert preference UI - dependencies: IW8-186, IW8-153
- **IW8-190** [P1] Add application/contact/alert integration tests - dependencies: IW8-179, IW8-182, IW8-189

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
