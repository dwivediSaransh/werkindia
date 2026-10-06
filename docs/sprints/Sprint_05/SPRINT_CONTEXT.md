# Sprint 5 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 17
- P0: 16
- P1: 1
- Epics: EP-05, EP-13

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-098** [P0] Create Angular employer job wizard shell - dependencies: IW8-056, IW8-058, IW8-095
- **IW8-099** [P0] Build role/specialisation/work-context wizard step - dependencies: IW8-098, IW8-096
- **IW8-100** [P0] Build vacancy, urgency and job-location wizard step - dependencies: IW8-098, IW8-058
- **IW8-101** [P0] Build experience, education and language requirement step - dependencies: IW8-099
- **IW8-102** [P0] Build skills, credential and asset requirement step - dependencies: IW8-099
- **IW8-103** [P0] Build compensation and benefits wizard step - dependencies: IW8-098
- **IW8-104** [P0] Build schedule and contact-policy wizard step - dependencies: IW8-098
- **IW8-105** [P0] Build screening and interview wizard step - dependencies: IW8-098
- **IW8-106** [P0] Build job description, preview and save-draft step - dependencies: IW8-100, IW8-101, IW8-102, IW8-103, IW8-104, IW8-105
- **IW8-107** [P0] Create JobModerationReview entity and status model - dependencies: IW8-080
- **IW8-108** [P0] Add moderation EF migration and review service - dependencies: IW8-107, IW8-007
- **IW8-109** [P0] Create JobPublicationEvent and status-history service - dependencies: IW8-080
- **IW8-110** [P0] Implement publish/pause/resume/close job services - dependencies: IW8-094, IW8-108, IW8-109, IW8-064
- **IW8-111** [P0] Expose job lifecycle GraphQL mutations - dependencies: IW8-110
- **IW8-112** [P0] Build employer job list and status filters - dependencies: IW8-111, IW8-106
- **IW8-113** [P0] Build employer job detail and lifecycle actions - dependencies: IW8-112
- **IW8-114** [P1] Add employer job-authoring Playwright tests - dependencies: IW8-113

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
