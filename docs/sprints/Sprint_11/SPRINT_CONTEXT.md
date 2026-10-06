# Sprint 11 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 17
- P0: 16
- P1: 1
- Epics: EP-11

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-210** [P0] Create CandidateSearchProjection read model - dependencies: IW8-121, IW8-193
- **IW8-211** [P0] Create candidate search facet bridge tables and indexes - dependencies: IW8-210
- **IW8-212** [P0] Add candidate search projection migration - dependencies: IW8-210, IW8-211, IW8-007
- **IW8-213** [P0] Implement candidate search projector - dependencies: IW8-212, IW8-125
- **IW8-214** [P0] Implement full candidate projection rebuild command - dependencies: IW8-213
- **IW8-215** [P0] Implement CandidateSearchInput query service - dependencies: IW8-212
- **IW8-216** [P0] Add candidate-search entitlement and audit enforcement - dependencies: IW8-215, IW8-064
- **IW8-217** [P0] Expose candidateSearch GraphQL connection - dependencies: IW8-216
- **IW8-218** [P0] Implement jobCandidateMatches query - dependencies: IW8-215, IW8-146
- **IW8-219** [P0] Expose jobCandidateMatches GraphQL query - dependencies: IW8-218
- **IW8-220** [P0] Create CandidateContactReveal entity and access service - dependencies: IW8-070, IW8-120
- **IW8-221** [P0] Implement revealCandidateContact mutation - dependencies: IW8-220, IW8-064
- **IW8-222** [P0] Build employer candidate database search page - dependencies: IW8-217, IW8-071
- **IW8-223** [P0] Build candidate database filter drawer - dependencies: IW8-222
- **IW8-224** [P0] Integrate unlock/paywall in candidate database result/detail - dependencies: IW8-222, IW8-077
- **IW8-225** [P0] Integrate contact reveal in unlocked candidate view - dependencies: IW8-221, IW8-224
- **IW8-226** [P1] Add candidate search/unlock/reveal integration tests - dependencies: IW8-219, IW8-225

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
