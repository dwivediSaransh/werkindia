# IW8-014: Add formatting, linting and pre-commit validation scripts

Sprint: Sprint 0
Outcome: DONE

IW8-014 implemented: added dotnet format verify (backend), frontend lint/format/typecheck npm scripts, updated CI pipeline with frontend checks, and rewrote scripts/verify as single validation entry point. Fixed whitespace formatting issues in 2 files (AppDbContextDesignTimeFactory.cs, SqlServerIntegrationTests.cs). All checks pass: dotnet format clean, prettier lint/format/typecheck clean, backend tests green (13 passed, 3 skipped integration tests), no sensitive data in scripts.

```json
{
  "story_id": "IW8-014",
  "outcome": "DONE",
  "summary": "IW8-014 implemented: added dotnet format verify (backend), frontend lint/format/typecheck npm scripts, updated CI pipeline with frontend checks, and rewrote scripts/verify as single validation entry point. Fixed whitespace formatting issues in 2 files (AppDbContextDesignTimeFactory.cs, SqlServerIntegrationTests.cs). All checks pass: dotnet format clean, prettier lint/format/typecheck clean, backend tests green (13 passed, 3 skipped integration tests), no sensitive data in scripts.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\pipelines\\github\\workflows\\ci.yml",
    "C:\\Projects\\IndiaWorks\\scripts\\verify",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\AppDbContextDesignTimeFactory.cs",
    "C:\\Projects\\IndiaWorks\\src\\web\\package.json",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.IntegrationTests\\Database\\SqlServerIntegrationTests.cs"
  ],
  "verification": [
    "2b1d2d80dc9e: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Users\\\\DELL\\\\AppData\\\\Local\\\\hermes\\\\tools\\\\git-2.53.0+3-win32-x64\\\\usr\\\\bin\\\\bash.EXE', 'scripts/verify']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5']; output=verification/2b1d2d80dc9e.log",
    "AC1: scripts/verify exits 0 and runs dotnet format --verify-no-changes + npm run lint + npm run format -- --check + npm run typecheck + dotnet test successfully",
    "AC2: Independent subcommand verification: npm run lint passes (prettier --check), npm run format -- --check passes, npm run typecheck passes (ng build), dotnet format --verify-no-changes passes",
    "AC3: Only devops/script/CI files changed: scripts/verify rewritten, src/web/package.json scripts added (format, typecheck), pipelines/github/workflows/ci.yml extended with frontend job. No new domain concepts, APIs, or tables introduced.",
    "AC4: Backend tests pass: dotnet test IndiaWorks.slnx -c Debug --no-build --verbosity minimal: 0 Failed, 13 Passed (1 Application, 6 Domain, 1 Architecture, 5 Integration with 3 skipped Testcontainer tests)",
    "AC5: scripts/verify contains no forbidden secret/contact/identity data patterns (grep for PASSWORD|SECRET|TOKEN|API_KEY|PRIVATE_KEY|AADHAAR|PAN excludes self-reference and fails on none). No sensitive data leakage in logs or scripts."
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
