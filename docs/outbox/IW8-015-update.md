# IW8-015: Create CI pipeline baseline

Sprint: Sprint 0
Outcome: DONE

CI pipeline baseline completed: extended pipelines/github/workflows/ci.yml with SQL Server service container, migration validation step (dotnet ef database update), GraphQL schema archive + artifact upload, frontend unit tests (npm test) and production build (npm run build). scripts/verify passes all 5 ACs: dotnet format clean, frontend lint/format/typecheck clean, devops files present, backend tests green (6+1+1+5=13 passed, 3 skipped), no sensitive data in scripts.

```json
{
  "story_id": "IW8-015",
  "outcome": "DONE",
  "summary": "CI pipeline baseline completed: extended pipelines/github/workflows/ci.yml with SQL Server service container, migration validation step (dotnet ef database update), GraphQL schema archive + artifact upload, frontend unit tests (npm test) and production build (npm run build). scripts/verify passes all 5 ACs: dotnet format clean, frontend lint/format/typecheck clean, devops files present, backend tests green (6+1+1+5=13 passed, 3 skipped), no sensitive data in scripts.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\pipelines\\github\\workflows\\ci.yml"
  ],
  "verification": [
    "933e1e63fa6a: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Users\\\\DELL\\\\AppData\\\\Local\\\\hermes\\\\tools\\\\git-2.53.0+3-win32-x64\\\\usr\\\\bin\\\\bash.EXE', 'scripts/verify']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5']; output=verification/933e1e63fa6a.log",
    "AC1: dotnet format verify PASS + backend job in ci.yml restores/builds/tests (dotnet restore, dotnet build, dotnet format --verify-no-changes, dotnet test all present)",
    "AC2: npm run lint PASS + npm run format -- --check PASS + npm run typecheck PASS + frontend job in ci.yml installs/builds/tests (npm ci, npm run lint, npm run format -- --check, npm test -- --run, npm run typecheck, npm run build all present)",
    "AC3: test -f scripts/verify PASS + test -f src/web/package.json PASS + test -d pipelines PASS \u2014 pipelines/github/workflows/ci.yml is the CI provider configuration; no new parallel domain concept introduced",
    "AC4: dotnet test IndiaWorks.slnx PASS: 6 passed (Domain.Tests) + 1 passed (Application.Tests) + 1 passed (ArchitectureTests) + 5 passed (IntegrationTests, 3 skipped due to no SQL container) = 13 passed, 0 failed",
    "AC5: grep -rnE sensitive patterns in scripts/ returned no matches \u2014 PASS"
  ],
  "known_gaps": [],
  "blocker": null,
  "notes": [
    "Host-generated inventory, proof records and outbox handoff."
  ],
  "obsidian_status": "OUTBOX",
  "graphify_status": "OUTBOX"
}
```

No MCP sync claimed. Contract/rollout details, if applicable, must be recorded in the summary or AC explanations.
