# Sprint 6 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 18
- P0: 17
- P1: 1
- Epics: EP-06

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-115** [P0] Create lightweight CandidateProfile entity and defaults - dependencies: IW8-029, IW8-045
- **IW8-116** [P0] Create CandidateOccupation entity with primary/secondary role model - dependencies: IW8-115, IW8-047
- **IW8-117** [P0] Create candidate education, skill and language entities - dependencies: IW8-115, IW8-048
- **IW8-118** [P0] Create candidate credential and asset entities - dependencies: IW8-115, IW8-048
- **IW8-119** [P0] Create work and location preference entities - dependencies: IW8-115, IW8-045
- **IW8-120** [P0] Create candidate consent and discoverability entities - dependencies: IW8-115
- **IW8-121** [P0] Add candidate profile EF configurations and migration - dependencies: IW8-116, IW8-117, IW8-118, IW8-119, IW8-120, IW8-007
- **IW8-122** [P0] Implement candidate profile query service - dependencies: IW8-121
- **IW8-123** [P0] Implement update candidate basics and preferences service - dependencies: IW8-121
- **IW8-124** [P0] Implement candidate occupations/skills/education update services - dependencies: IW8-121
- **IW8-125** [P0] Implement credential/asset and discoverability update services - dependencies: IW8-121
- **IW8-126** [P0] Expose candidate profile GraphQL operations - dependencies: IW8-122, IW8-123, IW8-124, IW8-125
- **IW8-127** [P0] Build candidate profile completion shell - dependencies: IW8-126, IW8-032
- **IW8-128** [P0] Build candidate occupation and experience editor - dependencies: IW8-127, IW8-058
- **IW8-129** [P0] Build candidate education, skills and languages editor - dependencies: IW8-127
- **IW8-130** [P0] Build candidate credentials, assets and work-preference editor - dependencies: IW8-127
- **IW8-131** [P0] Build candidate privacy/discoverability controls - dependencies: IW8-126, IW8-127
- **IW8-132** [P1] Add candidate profile integration/E2E tests - dependencies: IW8-128, IW8-129, IW8-130, IW8-131

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
