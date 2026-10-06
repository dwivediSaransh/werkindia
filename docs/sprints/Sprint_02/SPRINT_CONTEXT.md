# Sprint 2 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 24
- P0: 23
- P1: 1
- Epics: EP-02, EP-03

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-036** [P0] Create Organisation and OrganisationIdentifier entities - dependencies: IW8-016
- **IW8-037** [P0] Create OrganisationMembership and RBAC role model - dependencies: IW8-036, IW8-016
- **IW8-038** [P0] Add organisation EF configurations and migration - dependencies: IW8-036, IW8-037, IW8-007
- **IW8-039** [P0] Implement organisation creation service - dependencies: IW8-038
- **IW8-040** [P0] Expose organisation create/list GraphQL operations - dependencies: IW8-039, IW8-005
- **IW8-041** [P0] Create OrganisationInvitation entity and migration - dependencies: IW8-038
- **IW8-042** [P0] Implement invite/accept/revoke member service - dependencies: IW8-041
- **IW8-043** [P0] Expose organisation invitation GraphQL operations - dependencies: IW8-042
- **IW8-044** [P0] Implement organisation commercial identity fingerprint - dependencies: IW8-038
- **IW8-045** [P0] Create Geography hierarchy entity and configuration - dependencies: IW8-006
- **IW8-046** [P0] Seed India geography starter dataset for development - dependencies: IW8-045
- **IW8-047** [P0] Create taxonomy core entities - dependencies: IW8-006
- **IW8-048** [P0] Create skill, credential and asset reference entities - dependencies: IW8-047
- **IW8-049** [P0] Add taxonomy EF configurations and migration - dependencies: IW8-047, IW8-048, IW8-007
- **IW8-050** [P0] Create versioned taxonomy seed importer - dependencies: IW8-049
- **IW8-051** [P0] Seed core job families and canonical roles - dependencies: IW8-050
- **IW8-052** [P0] Seed specialisations and aliases for Driver and Delivery - dependencies: IW8-051
- **IW8-053** [P0] Seed specialisations for Technician and Facilities roles - dependencies: IW8-051
- **IW8-054** [P0] Seed professional and healthcare specialisations - dependencies: IW8-051
- **IW8-055** [P0] Expose taxonomy and geography GraphQL lookup queries - dependencies: IW8-049, IW8-046
- **IW8-056** [P0] Build employer organisation setup UI - dependencies: IW8-040, IW8-033
- **IW8-057** [P0] Build employer team invitation UI - dependencies: IW8-043, IW8-056
- **IW8-058** [P0] Create Angular taxonomy lookup service and cached selectors - dependencies: IW8-055, IW8-010
- **IW8-059** [P1] Add organisation/taxonomy integration tests - dependencies: IW8-057, IW8-058

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
