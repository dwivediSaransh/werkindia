# Sprint 0 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 15
- P0: 14
- P1: 1
- Epics: EP-00

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-001** [P0] Create repository folder structure and root solution layout - dependencies: None
- **IW8-002** [P0] Add Directory.Build.props and central NuGet package versions - dependencies: IW8-001
- **IW8-003** [P0] Create backend solution projects and project references - dependencies: IW8-001, IW8-002
- **IW8-004** [P0] Bootstrap ASP.NET Core API composition root - dependencies: IW8-003
- **IW8-005** [P0] Bootstrap Hot Chocolate GraphQL endpoint - dependencies: IW8-004
- **IW8-006** [P0] Create EF Core AppDbContext and SQL Server provider wiring - dependencies: IW8-003
- **IW8-007** [P0] Create EF design-time factory and migration conventions - dependencies: IW8-006
- **IW8-008** [P0] Create Angular standalone workspace - dependencies: IW8-001
- **IW8-009** [P0] Create Angular application shell and lazy route groups - dependencies: IW8-008
- **IW8-010** [P0] Configure Apollo Angular and typed GraphQL code generation - dependencies: IW8-005, IW8-008
- **IW8-011** [P0] Add local SQL Server container and developer configuration - dependencies: IW8-006
- **IW8-012** [P0] Create backend unit, integration and architecture test projects - dependencies: IW8-003, IW8-006
- **IW8-013** [P0] Create Angular unit and Playwright E2E test foundations - dependencies: IW8-008
- **IW8-014** [P0] Add formatting, linting and pre-commit validation scripts - dependencies: IW8-003, IW8-008
- **IW8-015** [P1] Create CI pipeline baseline - dependencies: IW8-011, IW8-012, IW8-013, IW8-014

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
