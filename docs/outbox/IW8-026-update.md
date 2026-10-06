# IW8-026: Add session/device EF configuration and migration

Sprint: Sprint 1
Outcome: DONE

IW8-026 complete. Added session/device EF configurations with unique refresh-token hash index (UX_UserDevices_RefreshTokenHash) and active sessions composite index (IX_UserSessions_UserId_Active). All 7 acceptance criteria verified with fresh proof: build succeeds (0 errors 0 warnings), all 123 tests pass (3 skipped - Testcontainers unavailable), EF migrations list shows all 5 migrations including AddUniqueRefreshTokenHashIndex, EF database update applies cleanly (already up to date), AC5 audit confirms zero logging statements and only RefreshTokenHash as column/property names (no actual secrets/PII).

```json
{
  "story_id": "IW8-026",
  "outcome": "DONE",
  "summary": "IW8-026 complete. Added session/device EF configurations with unique refresh-token hash index (UX_UserDevices_RefreshTokenHash) and active sessions composite index (IX_UserSessions_UserId_Active). All 7 acceptance criteria verified with fresh proof: build succeeds (0 errors 0 warnings), all 123 tests pass (3 skipped - Testcontainers unavailable), EF migrations list shows all 5 migrations including AddUniqueRefreshTokenHashIndex, EF database update applies cleanly (already up to date), AC5 audit confirms zero logging statements and only RefreshTokenHash as column/property names (no actual secrets/PII).",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Migrations\\20260928045335_AddUniqueRefreshTokenHashIndex.Designer.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Migrations\\20260928045335_AddUniqueRefreshTokenHashIndex.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Migrations\\AppDbContextModelSnapshot.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Configurations\\E01\\UserDeviceConfiguration.cs"
  ],
  "verification": [
    "ebbb8af318eb: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.exe', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=0; ACs=['AC1', 'AC6']; output=verification/ebbb8af318eb.log",
    "76709e0d664b: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.exe', 'ef', 'migrations', 'list', '--project', 'src/backend/IndiaWorks.Infrastructure', '--startup-project', 'src/backend/IndiaWorks.Api']; exit=0; ACs=['AC1']; output=verification/76709e0d664b.log",
    "812717ae51bf: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.exe', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build']; exit=0; ACs=['AC4', 'AC6']; output=verification/812717ae51bf.log",
    "72091bc68e8c: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.exe', 'ef', 'database', 'update', '--project', 'src/backend/IndiaWorks.Infrastructure', '--startup-project', 'src/backend/IndiaWorks.Api']; exit=0; ACs=['AC3', 'AC7']; output=verification/72091bc68e8c.log",
    "4d476daee000: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Users\\\\DELL\\\\AppData\\\\Local\\\\hermes\\\\tools\\\\git-2.53.0+3-win32-x64\\\\usr\\\\bin\\\\bash.EXE', '-c', \"rg --files 'src/backend/IndiaWorks.Domain/E01/UserDevice.cs' 'src/backend/IndiaWorks.Domain/E01/UserSession.cs' 'src/backend/IndiaWorks.Infrastructure/Persistence/Configurations/E01/UserDeviceConfiguration.cs' 'src/backend/IndiaWorks.Infrastructure/Persistence/Configurations/E01/UserSessionConfiguration.cs' 'src/backend/IndiaWorks.Infrastructure/Migrations/20260928045335_AddUniqueRefreshTokenHashIndex.cs' 'src/backend/IndiaWorks.Infrastructure/Migrations/20260928045335_AddUniqueRefreshTokenHashIndex.Designer.cs' 'src/backend/IndiaWorks.Infrastructure/Migrations/20260928042631_AddUserDevicesAndSessions.cs' 'src/backend/IndiaWorks.Infrastructure/Migrations/20260928042631_AddUserDevicesAndSessions.Designer.cs' 'src/backend/IndiaWorks.Infrastructure/Migrations/AppDbContextModelSnapshot.cs' | xargs rg -l 'ILogger|Log\\\\.|logger\\\\.|Serilog|Write.*token|Write.*otp|Write.*refresh' 2>/dev/null || echo 'NO_SENSITIVE_LOGGING_FOUND'\"]; exit=0; ACs=['AC5']; output=verification/4d476daee000.log",
    "b4cd6a81be12: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Users\\\\DELL\\\\AppData\\\\Local\\\\hermes\\\\tools\\\\git-2.53.0+3-win32-x64\\\\usr\\\\bin\\\\bash.EXE', '-c', \"echo '=== UserDeviceConfiguration.cs ===' && grep -n 'HasIndex\\\\|IsUnique\\\\|HasDatabaseName' src/backend/IndiaWorks.Infrastructure/Persistence/Configurations/E01/UserDeviceConfiguration.cs && echo '=== UserSessionConfiguration.cs ===' && grep -n 'HasIndex\\\\|IsUnique\\\\|HasDatabaseName' src/backend/IndiaWorks.Infrastructure/Persistence/Configurations/E01/UserSessionConfiguration.cs && echo '=== AppDbContext.cs DbSets ===' && grep -n 'DbSet' src/backend/IndiaWorks.Infrastructure/Persistence/AppDbContext.cs && echo '=== Migration files ===' && ls -la src/backend/IndiaWorks.Infrastructure/Migrations/20260928045335_*.cs src/backend/IndiaWorks.Infrastructure/Migrations/20260928045335_*.Designer.cs src/backend/IndiaWorks.Infrastructure/Migrations/20260928042631_*UserDevices*.cs src/backend/IndiaWorks.Infrastructure/Migrations/20260928042631_*UserDevices*.Designer.cs 2>/dev/null\"]; exit=0; ACs=['AC2']; output=verification/b4cd6a81be12.log",
    "AC1: Build ebbb8af318eb: exit 0, 0 Warning(s) 0 Error(s). Migrations list 76709e0d664b: all 5 migrations listed including 20260928045335_AddUniqueRefreshTokenHashIndex.",
    "AC2: b4cd6a81be12: UserDeviceConfiguration.cs lines 63-65 HasIndex(x => x.RefreshTokenHash).IsUnique().HasDatabaseName(UX_UserDevices_RefreshTokenHash). UserSessionConfiguration.cs lines 60-61 HasIndex(x => new { x.UserId, x.IsRevoked, x.ExpiresOn }).HasDatabaseName(IX_UserSessions_UserId_Active). Both explicit IEntityTypeConfiguration<T>. Reuses IW8-025 entities and IW8-007 conventions. AppDbContext has DbSet<UserDevice> and DbSet<UserSession>. No parallel domain concepts introduced.",
    "AC3: 72091bc68e8c: dotnet ef database update exit 0, 'No migrations were applied. The database is already up to date.' N/A for transport-layer auth - pure EF/infrastructure with no REST/GraphQL surface.",
    "AC4: 812717ae51bf: dotnet test exit 0. 123 passed, 0 failed, 3 skipped (Testcontainers). DomainTests include UserDeviceTests (11) and UserSessionTests (10) covering happy path + validation edges.",
    "AC5: 4d476daee000: NO_SENSITIVE_LOGGING_FOUND. Zero ILogger/Log*/Serilog/token-write patterns across all 9 changed files. Only RefreshTokenHash as EF column/property/migration name, not actual secrets.",
    "AC6: ebbb8af318eb + 812717ae51bf: Build 0 warnings 0 errors. Configurations are explicit IEntityTypeConfiguration<T>. All tests pass.",
    "AC7: 76709e0d664b + 72091bc68e8c: All 5 migrations in sequence (InitialCreate, AddUserAccounts, AddOtpChallenges, AddUserDevicesAndSessions, AddUniqueRefreshTokenHashIndex). Database update applies cleanly."
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
