# IW8-041: Create OrganisationInvitation entity and migration

Sprint: Sprint 2
Outcome: DONE

IW8-041: OrganisationInvitation entity, EF configuration, and migration 20260929034146_AddOrganisationInvitations verified. The entity models invite identifier, role, token hash, expiry, and lifecycle state (Pending/Accepted/Revoked/Expired). Explicit IEntityTypeConfiguration with cascade FK to Organisation, composite index on (OrgId, Status, ExpiresAt), and unique filtered index on (OrgId, InviteeIdentifierHash) where Status='Pending'. All 7 ACs proven: build clean (0 errors/warnings), 80/80 Domain tests passed, 96/96 Application tests passed, 45/45 Integration tests passed (3 skipped Testcontainers), migration applied successfully via dotnet ef database update, and OrganisationModelConfigurationTests validates the model against SQL Server.

```json
{
  "story_id": "IW8-041",
  "outcome": "DONE",
  "summary": "IW8-041: OrganisationInvitation entity, EF configuration, and migration 20260929034146_AddOrganisationInvitations verified. The entity models invite identifier, role, token hash, expiry, and lifecycle state (Pending/Accepted/Revoked/Expired). Explicit IEntityTypeConfiguration with cascade FK to Organisation, composite index on (OrgId, Status, ExpiresAt), and unique filtered index on (OrgId, InviteeIdentifierHash) where Status='Pending'. All 7 ACs proven: build clean (0 errors/warnings), 80/80 Domain tests passed, 96/96 Application tests passed, 45/45 Integration tests passed (3 skipped Testcontainers), migration applied successfully via dotnet ef database update, and OrganisationModelConfigurationTests validates the model against SQL Server.",
  "files_changed": [],
  "verification": [
    "be50fba694f3: cwd=C:\\Projects\\IndiaWorks\\src\\backend; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=1; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7']; output=verification/be50fba694f3.log",
    "b7c307a08530: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7']; output=verification/b7c307a08530.log",
    "9cef22357a0d: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build', '--verbosity', 'normal']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7']; output=verification/9cef22357a0d.log",
    "e9b66ac52fdc: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'ef', 'migrations', 'list', '--project', 'src/backend/IndiaWorks.Infrastructure', '--startup-project', 'src/backend/IndiaWorks.Api']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7']; output=verification/e9b66ac52fdc.log",
    "AC1: OrganisationInvitation.cs models Id, OrganisationId, InviteeIdentifierType, InviteeIdentifierHash (32-byte SHA-256), Role (OrganisationRole enum), TokenHash (64-byte), Status (InvitationStatus enum: Pending/Accepted/Revoked/Expired), ExpiresAt, CreatedAt, AcceptedByUserAccountId, AcceptedAt, RevokedAt. Factory Create() enforces non-empty IDs, non-blank identifier type, non-empty hashes, future expiry. Domain tests (11/11 passed) cover Create happy path, Accept transition, Revoke transition, IsActive, and 7 failure/edge cases.",
    "AC2: Entity/configuration/migration reuse E02 conventions from IW8-038. No new parallel table/concept introduced. Build succeeded with 0 errors, 0 warnings. All 80 Domain tests and 96 Application tests passed.",
    "AC3: This story owns no API surface (entity/migration only per package scope). Downstream IW8-042 owns the invite/accept/revoke service with stable error codes (VALIDATION_FAILED, FORBIDDEN, ORGANISATION_NOT_FOUND, MEMBERSHIP_REQUIRED, ROLE_NOT_ALLOWED). The artifact introduces no unstable codes.",
    "AC4: OrganisationInvitationTests (11 tests: happy path Create/Accept/Revoke/IsActive + 7 failure/edge cases for empty IDs, blank identifier type, past expiry, double Accept/Revoke). OrganisationModelConfigurationTests.OrganisationInvitation_ModelConfiguration_HasCorrectSchemaRequirements validates table name, PK, binary hash lengths (32/64), FK cascade, indexes, unique partial index. All tests passed.",
    "AC5: Entity stores only InviteeIdentifierHash (SHA-256, byte[32]) and TokenHash (byte[64]) - no plaintext contact/token/identity. Configuration and migration contain no plaintext secrets. Domain tests use Random.Shared.NextBytes for hashes. GraphQL integration test confirmed no sensitive data in logs.",
    "AC6: OrganisationInvitationConfiguration.cs is explicit: ToTable('OrganisationInvitations'), HasKey(x=>x.Id), required/nullable properties, HasMaxLength(30) for identifier type, HasMaxLength(32).IsFixedLength() for invitee hash, HasMaxLength(64).IsFixedLength() for token hash, HasPrecision(3) for datetimes, FK to Organisation with DeleteBehavior.Cascade, index IX_OrganisationInvitations_Org_Status_Expires, unique filtered index UX_OrganisationInvitations_Org_InviteeHash_Pending. Migration 20260929034146_AddOrganisationInvitations.cs generated and reviewed.",
    "AC7: dotnet ef database update applied '20260929034146_AddOrganisationInvitations' successfully (output: 'Applying migration... Done.'). dotnet ef migrations list confirms migration present. OrganisationModelConfigurationTests validates model against real SQL Server provider."
  ],
  "known_gaps": [],
  "blocker": null,
  "notes": [
    "Host-generated inventory, proof records and outbox handoff.",
    "No source files changed in this run because the selected story implementation already existed; DONE is based on fresh acceptance-criteria verification."
  ],
  "obsidian_status": "OUTBOX",
  "graphify_status": "OUTBOX",
  "no_source_changes_required": true
}
```

No MCP sync claimed. Contract/rollout details, if applicable, must be recorded in the summary or AC explanations.
