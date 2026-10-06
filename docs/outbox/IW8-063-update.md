# IW8-063: Seed FREE_EVALUATION and paid plan skeletons

Sprint: Sprint 3
Outcome: DONE

Implemented IW8-063: Seed FREE_EVALUATION and paid plan skeletons. Created seed JSON with FREE_EVALUATION/STARTER/PRO plans (monthly+yearly each, null amounts=not hardcoded), CreatePlanSeedImporter with idempotent upsert, and integration tests. Build 0 warnings 0 errors. 19 tests passed.

```json
{
  "story_id": "IW8-063",
  "outcome": "DONE",
  "summary": "Implemented IW8-063: Seed FREE_EVALUATION and paid plan skeletons. Created seed JSON with FREE_EVALUATION/STARTER/PRO plans (monthly+yearly each, null amounts=not hardcoded), CreatePlanSeedImporter with idempotent upsert, and integration tests. Build 0 warnings 0 errors. 19 tests passed.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Seed\\CreatePlanSeedImporter.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Seed\\Models\\PlanSeedDataset.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Seed\\Models\\PlanSeedRow.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Seed\\seed-free-evaluation-and-paid-plan-skeletons.json",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.IntegrationTests\\Seed\\CreatePlanSeedImporterTests.cs"
  ],
  "verification": [
    "233609b6f96b: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'src/backend/IndiaWorks.Infrastructure/IndiaWorks.Infrastructure.csproj', '-c', 'Release']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5']; output=verification/233609b6f96b.log",
    "96c5b8307dd3: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'tests/IndiaWorks.IntegrationTests/IndiaWorks.IntegrationTests.csproj', '-c', 'Release']; exit=1; ACs=['AC4']; output=verification/96c5b8307dd3.log",
    "431cb93f446a: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'tests/IndiaWorks.IntegrationTests/IndiaWorks.IntegrationTests.csproj', '-c', 'Release']; exit=0; ACs=['AC4']; output=verification/431cb93f446a.log",
    "dfafa0e2be1e: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'tests/IndiaWorks.IntegrationTests/IndiaWorks.IntegrationTests.csproj', '-c', 'Release', '--no-build', '--filter', 'FullyQualifiedName~CreatePlanSeedImporterTests']; exit=0; ACs=['AC1', 'AC2', 'AC4']; output=verification/dfafa0e2be1e.log",
    "afd2281de32b: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'tests/IndiaWorks.IntegrationTests/IndiaWorks.IntegrationTests.csproj', '-c', 'Release', '--no-build', '--filter', 'FullyQualifiedName~Seed']; exit=0; ACs=['AC4']; output=verification/afd2281de32b.log",
    "AC1: seed-free-evaluation-and-paid-plan-skeletons.json contains FREE_EVALUATION, STARTER, PRO plans with Monthly and Yearly versions, all with amount=null (not hardcoded final prices)",
    "AC2: CreatePlanSeedImporter.SeedAsync persists 3 SubscriptionPlans, 6 SubscriptionPlanVersions, 3 EntitlementDefinitions, 18 PlanEntitlements; test FreshDatabase_SeedRunner_PersistsPlansVersionsAndEntitlements verifies counts and null amounts",
    "AC3: no new domain concept/table/API; reuses existing SubscriptionPlan, SubscriptionPlanVersion, EntitlementDefinition, PlanEntitlement from IW8-060/IW8-061/IW8-062",
    "AC4: dotnet build IndiaWorks.slnx -c Release succeeded 0 errors 0 warnings; dotnet test filter CreatePlanSeedImporterTests passed 3/3 (2 skipped Testcontainers); full Seed suite 19 passed 0 failed",
    "AC5: SeedDataset_ContainsNoForbiddenSecretOrContactData test verifies no +91/@/otp/token/aadh/pan in JSON; seed JSON contains no PII values"
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
