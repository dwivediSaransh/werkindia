# IW8-020: Implement OTP policy service

Sprint: Sprint 1
Outcome: DONE

IW8-020 OTP policy service implemented and verified. Build succeeds (0 warnings, 0 errors). 13/13 ImplementTests pass covering happy path, expired OTP, rate limiting, attempt limits, and secret-logging absence. OtpPolicyService centralizes code generation (RandomNumberGenerator), SHA-256 hashing (Sha256OtpHasher), TTL/max-attempts/resend-cooldown via OtpSettings, and rate limiting (InMemoryOtpRateLimiter). Uses existing OtpChallenge domain entity from IW8-017; no new parallel concepts introduced.

```json
{
  "story_id": "IW8-020",
  "outcome": "DONE",
  "summary": "IW8-020 OTP policy service implemented and verified. Build succeeds (0 warnings, 0 errors). 13/13 ImplementTests pass covering happy path, expired OTP, rate limiting, attempt limits, and secret-logging absence. OtpPolicyService centralizes code generation (RandomNumberGenerator), SHA-256 hashing (Sha256OtpHasher), TTL/max-attempts/resend-cooldown via OtpSettings, and rate limiting (InMemoryOtpRateLimiter). Uses existing OtpChallenge domain entity from IW8-017; no new parallel concepts introduced.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\DependencyInjection.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E01\\Implement\\IOtpChallengeRepository.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E01\\Implement\\IOtpPolicyService.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E01\\Implement\\InMemoryOtpRateLimiter.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E01\\Implement\\OtpErrorCodes.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E01\\Implement\\OtpPolicyService.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E01\\Implement\\OtpSettings.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E01\\Implement\\Sha256OtpHasher.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\DependencyInjection.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Repositories\\OtpChallengeRepository.cs",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Application.Tests\\E01\\ImplementTests.cs"
  ],
  "verification": [
    "c1168a14c21b: cwd=C:\\Projects\\IndiaWorks\\src\\backend; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=1; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8']; output=verification/c1168a14c21b.log",
    "823a657802ba: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8']; output=verification/823a657802ba.log",
    "975149cfe3b5: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build', '--filter', 'FullyQualifiedName~ImplementTests']; exit=1; ACs=['AC2', 'AC3', 'AC4', 'AC5', 'AC6']; output=verification/975149cfe3b5.log",
    "8d6da243d1c2: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build', '--filter', 'FullyQualifiedName~ImplementTests']; exit=1; ACs=['AC2', 'AC3', 'AC4', 'AC5', 'AC6']; output=verification/8d6da243d1c2.log",
    "a6bbcd32265c: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8']; output=verification/a6bbcd32265c.log",
    "2fbd8153626d: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build', '--filter', 'FullyQualifiedName~ImplementTests']; exit=1; ACs=['AC2', 'AC3', 'AC4', 'AC5', 'AC6']; output=verification/2fbd8153626d.log",
    "9dc14a6dfedd: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8']; output=verification/9dc14a6dfedd.log",
    "723baf0d6911: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build', '--filter', 'FullyQualifiedName~ImplementTests']; exit=1; ACs=['AC2', 'AC3', 'AC4', 'AC5', 'AC6']; output=verification/723baf0d6911.log",
    "a03712dd526f: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build', '--filter', 'FullyQualifiedName~ImplementTests']; exit=1; ACs=['AC2', 'AC3', 'AC4', 'AC5', 'AC6']; output=verification/a03712dd526f.log",
    "42eae1f65883: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8']; output=verification/42eae1f65883.log",
    "e425d6755ab8: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build', '--filter', 'FullyQualifiedName~ImplementTests']; exit=0; ACs=['AC2', 'AC3', 'AC4', 'AC5', 'AC6']; output=verification/e425d6755ab8.log",
    "AC1: Build succeeds and OtpSettings/OtpPolicyService/Sha256OtpHasher/InMemoryOtpRateLimiter centralize code generation, hashing, TTL, max attempts, resend cooldown, rate-limit keys; no parallel source of truth introduced.",
    "AC2: Test GenerateAndStoreAsync_creates_challenge_and_verify_succeeds passes: generates OTP, persists challenge, returns metadata, GetActiveChallenge and CanResend work.",
    "AC3: Test VerifyAsync_with_expired_challenge_returns_otp_expired passes: returns OTP_EXPIRED, challenge not marked consumed, no side effects.",
    "AC4: Test Rate_limit_exceeded_returns_rate_limited passes: third request returns RATE_LIMITED after 2 requests in window; provider not repeatedly called (rate limiter blocks before generation).",
    "AC5: Test No_plaintext_otp_in_logs passes: OTP/code/contact plaintext absent from log entries on success and failure paths.",
    "AC6: No new parallel domain concept/table/API: reuses OtpChallenge entity and OtpPurpose enum from IW8-017; no new tables, no new GraphQL/REST operations owned by this story.",
    "AC7: dotnet build IndiaWorks.slnx -c Release succeeds (0 errors); dotnet test IndiaWorks.slnx -c Release --no-build --filter ImplementTests passes 13/13.",
    "AC8: Test No_plaintext_otp_in_logs verifies no OTP code, raw contact, or identity data in logs; MaskGuid used for identifiers; OtpResult never returns OTP code to callers."
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
