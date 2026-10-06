# Sprint 10 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 19
- P0: 18
- P1: 1
- Epics: EP-10

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-191** [P0] Create EmployerCandidateRelationship entity - dependencies: IW8-036, IW8-115
- **IW8-192** [P0] Create relationship job, note and tag entities - dependencies: IW8-191
- **IW8-193** [P0] Add recruiting CRM EF configuration and migration - dependencies: IW8-191, IW8-192, IW8-007
- **IW8-194** [P0] Implement masked job applicants query - dependencies: IW8-173, IW8-193, IW8-071
- **IW8-195** [P0] Expose jobApplicantsV7 GraphQL connection - dependencies: IW8-194
- **IW8-196** [P0] Build employer applicants list with masked cards - dependencies: IW8-195, IW8-113
- **IW8-197** [P0] Integrate five-free unlock into applicant detail - dependencies: IW8-196, IW8-077
- **IW8-198** [P0] Implement recruiting relationship stage service - dependencies: IW8-193
- **IW8-199** [P0] Implement recruiter owner assignment service - dependencies: IW8-198, IW8-037
- **IW8-200** [P0] Implement employer candidate notes service - dependencies: IW8-193
- **IW8-201** [P0] Implement employer candidate tags service - dependencies: IW8-193
- **IW8-202** [P0] Expose CRM stage/owner/note/tag GraphQL operations - dependencies: IW8-198, IW8-199, IW8-200, IW8-201
- **IW8-203** [P0] Build employer candidate profile workspace - dependencies: IW8-197, IW8-202
- **IW8-204** [P0] Create Interview entity and scheduling service - dependencies: IW8-171, IW8-198
- **IW8-205** [P0] Expose interview GraphQL operations and employer UI - dependencies: IW8-204, IW8-203
- **IW8-206** [P0] Create JobOffer entity and lifecycle service - dependencies: IW8-171, IW8-198
- **IW8-207** [P0] Expose offer GraphQL operations and employer UI - dependencies: IW8-206, IW8-203
- **IW8-208** [P0] Synchronize application status with selected CRM actions - dependencies: IW8-204, IW8-206, IW8-198
- **IW8-209** [P1] Add employer applicant/CRM Playwright tests - dependencies: IW8-205, IW8-207, IW8-208

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
