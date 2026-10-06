# Sprint 7 - Hermes Working Context

## Scope

This sprint folder is the only backlog/package working set Hermes should load while this sprint is active.

## Sprint statistics

- Stories: 20
- P0: 19
- P1: 1
- Epics: EP-07, EP-14

## Canonical story selection

1. Resume the single `IN PROGRESS` story if one exists.
2. Otherwise select only `TODO` stories whose dependencies are all `DONE`.
3. Sort eligible stories by P0 -> P1 -> P2 -> P3, then canonical backlog row order.
4. Select exactly one Story ID for the cron run.
5. Never implement a second story in the same run.
6. Never jump to a later sprint while this sprint contains unfinished stories.

## Stories

- **IW8-133** [P0] Create JobSearchProjection entity/read model - dependencies: IW8-090, IW8-110
- **IW8-134** [P0] Create search facet bridge tables - dependencies: IW8-133
- **IW8-135** [P0] Add job search projection migration - dependencies: IW8-133, IW8-134, IW8-007
- **IW8-136** [P0] Implement job search projector from canonical job aggregate - dependencies: IW8-135, IW8-109
- **IW8-137** [P0] Implement full projection rebuild command - dependencies: IW8-136
- **IW8-138** [P0] Implement canonical JobSearchInput parser - dependencies: IW8-135
- **IW8-139** [P0] Implement taxonomy/work-mode/engagement facets - dependencies: IW8-138
- **IW8-140** [P0] Implement education/experience/course facets - dependencies: IW8-138
- **IW8-141** [P0] Implement compensation, shift, freshness and urgency filters - dependencies: IW8-138
- **IW8-142** [P0] Implement geo radius and nearest sorting - dependencies: IW8-138, IW8-045
- **IW8-143** [P0] Implement skills/language/credential/asset/benefit filters - dependencies: IW8-134, IW8-138
- **IW8-144** [P0] Implement employer trust/source/sector/industry filters - dependencies: IW8-138, IW8-036
- **IW8-145** [P0] Implement stable cursor pagination and job sort modes - dependencies: IW8-141, IW8-142
- **IW8-146** [P0] Implement candidate-to-job eligibility evaluator - dependencies: IW8-121, IW8-090
- **IW8-147** [P0] Implement baseline job relevance scoring with explanation codes - dependencies: IW8-146, IW8-136
- **IW8-148** [P0] Expose jobSearch and jobSearchFacets GraphQL queries - dependencies: IW8-139, IW8-140, IW8-141, IW8-142, IW8-143, IW8-144, IW8-145
- **IW8-149** [P0] Expose personalised jobFeed GraphQL query - dependencies: IW8-147, IW8-145
- **IW8-150** [P0] Create structured JobDetailView resolver - dependencies: IW8-090, IW8-136
- **IW8-151** [P0] Create JobImpression and JobSearchEvent entities - dependencies: IW8-133
- **IW8-152** [P1] Add job search, facets, feed and matching integration tests - dependencies: IW8-148, IW8-149, IW8-150, IW8-151

## Exit gate

The sprint is complete only when every story row is `DONE`. `BLOCKED` keeps the sprint active.
