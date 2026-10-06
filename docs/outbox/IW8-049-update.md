# IW8-049: Add taxonomy EF configurations and migration

Sprint: Sprint 2
Outcome: DONE

Added search-friendly indexes to all taxonomy entities (JobFamily, Role, Specialisation, WorkContext, Skill, CredentialType, AssetType) with 7 new non-unique indexes on Name/CanonicalName columns. Generated AddTaxonomySearchIndexes migration. All 77 tests pass, 13 skipped (Testcontainers). Build succeeds.

```json
{
  "story_id": "IW8-049",
  "outcome": "DONE",
  "summary": "Added search-friendly indexes to all taxonomy entities (JobFamily, Role, Specialisation, WorkContext, Skill, CredentialType, AssetType) with 7 new non-unique indexes on Name/CanonicalName columns. Generated AddTaxonomySearchIndexes migration. All 77 tests pass, 13 skipped (Testcontainers). Build succeeds.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Migrations\\AppDbContextModelSnapshot.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Configurations\\E03\\AssetTypeConfiguration.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Configurations\\E03\\CredentialTypeConfiguration.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Configurations\\E03\\JobFamilyConfiguration.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Configurations\\E03\\RoleConfiguration.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Configurations\\E03\\SkillConfiguration.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Configurations\\E03\\SpecialisationConfiguration.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Configurations\\E03\\WorkContextConfiguration.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Migrations\\20260929195426_AddTaxonomySearchIndexes.Designer.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Migrations\\20260929195426_AddTaxonomySearchIndexes.cs",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.IntegrationTests\\Database\\E03\\TaxonomyModelConfigurationTests.cs"
  ],
  "verification": [
    "c5e37243fcbc: cwd=C:\\Projects\\IndiaWorks\\src\\backend; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build', '--filter', 'FullyQualifiedName~TaxonomyModelConfigurationTests']; exit=1; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7']; output=verification/c5e37243fcbc.log",
    "28949dc4e1b2: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build', '--filter', 'FullyQualifiedName~TaxonomyModelConfigurationTests']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7']; output=verification/28949dc4e1b2.log",
    "97535ac62a9b: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=0; ACs=['AC1', 'AC2', 'AC3']; output=verification/97535ac62a9b.log",
    "0c88a297fe7a: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7']; output=verification/0c88a297fe7a.log",
    "1641f6013d66: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'ef', 'migrations', 'list', '--project', 'src/backend/IndiaWorks.Infrastructure', '--startup-project', 'src/backend/IndiaWorks.Api']; exit=0; ACs=['AC3']; output=verification/1641f6013d66.log",
    "AC1: Added 7 search-friendly indexes: IX_JobFamilies_Name, IX_Roles_CanonicalName, IX_Specialisations_Name, IX_WorkContexts_Name, IX_Skills_Name, IX_CredentialTypes_Name, IX_AssetTypes_Name. Existing versioned uniqueness (UQ_%) and parent relationship indexes preserved.",
    "AC2: dotnet build IndiaWorks.slnx -c Release succeeded: 0 Warnings, 0 Errors",
    "AC3: dotnet ef database update applied all migrations including new 20260929195426_AddTaxonomySearchIndexes successfully",
    "AC4: TaxonomyModelConfigurationTests verify unique/FK constraints (18 tests pass). Integration tests for constraint behavior exist but skipped without Docker.",
    "AC5: No parallel concepts introduced - only added non-unique search indexes to existing entity configurations",
    "AC6: All tests pass: 77 passed IntegrationTests, 181 passed DomainTests, 108 passed ApplicationTests, 1 passed ArchitectureTests; 13 skipped (Testcontainers unavailable)",
    "AC7: No sensitive data exposure - EF configurations contain no logging, no PII, no secrets"
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
