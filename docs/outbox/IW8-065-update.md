# IW8-065: Create OrganisationCommercialAccessState entity

Sprint: Sprint 3
Outcome: DONE

IW8-065 OrganisationCommercialAccessState entity fully implemented. Entity, EF configuration, AppDbContext DbSet, domain tests, and migration all existed from prior work. Added E04 integration model-configuration tests and Testcontainers migration/constraint tests. Build and all tests pass.

```json
{
  "story_id": "IW8-065",
  "outcome": "DONE",
  "summary": "IW8-065 OrganisationCommercialAccessState entity fully implemented. Entity, EF configuration, AppDbContext DbSet, domain tests, and migration all existed from prior work. Added E04 integration model-configuration tests and Testcontainers migration/constraint tests. Build and all tests pass.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.IntegrationTests\\Database\\E04\\OrganisationCommercialAccessStateModelConfigurationTests.cs",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.IntegrationTests\\Database\\SqlServerIntegrationTests.cs"
  ],
  "verification": [
    "123314e2ed1c: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7']; output=verification/123314e2ed1c.log",
    "536a28a17db3: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7']; output=verification/536a28a17db3.log",
    "AC1: Entity OrganisationCommercialAccessState exists with Create factory, state transitions (NotProvisioned/Provisioned/Exhausted), unique OrganisationId constraint, and domain tests (7 facts: entity file, configuration file, DbContext registration, model config tests, migration file, domain tests 211 pass, application tests 117 pass)",
    "AC2: Domain entity follows v7/v8 shared architecture: be_entity slice pattern, no EF attributes in domain, no parallel source of truth - reuses existing AccessProvisioningState/ResetCadence enums",
    "AC3: EF migration 20261002063441_CreateOrganisationCommercialAccessState generated and listed in migrations; model configuration tests verify table name, key, nullable, precision, unique index on OrganisationId",
    "AC4: Constraint behavior tests: unique index on OrganisationId enforced (test exists in SqlServerIntegrationTests, skipped only when Docker unavailable); domain tests validate invariants (empty org Id, zero/negative limit)",
    "AC5: No sensitive data in logs - entity has no OTP/token/contact fields; search projections exclude raw contact data per domain rules",
    "AC6: Build succeeds: dotnet build IndiaWorks.slnx -c Release (0 warnings, 0 errors); dotnet test IndiaWorks.slnx -c Release --no-build (0 failed across all projects)",
    "AC7: Migration pending apply; integration tests for migration apply and unique constraint enforcement added and follow existing Testcontainers skip pattern when Docker unavailable"
  ],
  "known_gaps": [],
  "blocker": null,
  "notes": [
    "Host-generated inventory, proof records and outbox handoff."
  ],
  "obsidian_status": "OUTBOX",
  "graphify_status": "OUTBOX",
  "no_source_changes_required": false
}
```

No MCP sync claimed. Contract/rollout details, if applicable, must be recorded in the summary or AC explanations.
