# Sprint 8 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 18
- P0: 17
- P1: 1
- Epics: EP-07, EP-09, EP-13

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-153** [P0] Create Angular candidate jobs feature store - dependencies: IW8-148, IW8-149, IW8-010
- **IW8-154** [P0] Build mobile job card component - dependencies: IW8-153
- **IW8-155** [P0] Build infinite-scroll job list page - dependencies: IW8-154
- **IW8-156** [P0] Build job search text and taxonomy filter sheet - dependencies: IW8-155, IW8-058
- **IW8-157** [P0] Build location/radius and work-mode/type filter controls - dependencies: IW8-156
- **IW8-158** [P0] Build experience/education/course filter controls - dependencies: IW8-156
- **IW8-159** [P0] Build salary/shift/freshness/urgency filter controls - dependencies: IW8-156
- **IW8-160** [P0] Build skill/language/credential/asset/benefit filters - dependencies: IW8-156
- **IW8-161** [P0] Build employer trust/source filters and sort menu - dependencies: IW8-156
- **IW8-162** [P0] Build structured job detail page - dependencies: IW8-150, IW8-154
- **IW8-163** [P0] Implement SavedJob entity service and GraphQL mutation - dependencies: IW8-115, IW8-080
- **IW8-164** [P0] Add save/unsave behavior to Angular job list/detail - dependencies: IW8-163, IW8-162
- **IW8-165** [P0] Create JobPublicLink and JobShareLink entities - dependencies: IW8-080
- **IW8-166** [P0] Implement public job/share-link GraphQL and REST resolution - dependencies: IW8-165, IW8-150
- **IW8-167** [P0] Configure Angular App Links/Universal Link compatible routes - dependencies: IW8-166, IW8-162
- **IW8-168** [P0] Add WhatsApp/Facebook/Telegram/native job share UI - dependencies: IW8-166, IW8-167
- **IW8-169** [P0] Implement candidate report-job mutation - dependencies: IW8-162
- **IW8-170** [P1] Add candidate job discovery Playwright suite - dependencies: IW8-164, IW8-168, IW8-169

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
