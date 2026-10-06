# IW8-021: Implement request-OTP use case

Sprint: Sprint 1
Outcome: DONE

IW8-021 implemented: RequestOtpUseCase orchestrates OTP request flow (validate, rate-limit, generate challenge, send via IOtpSender, return metadata). Added 6 new tests. Build and all 83 tests pass.

```json
{
  "story_id": "IW8-021",
  "outcome": "DONE",
  "summary": "IW8-021 implemented: RequestOtpUseCase orchestrates OTP request flow (validate, rate-limit, generate challenge, send via IOtpSender, return metadata). Added 6 new tests. Build and all 83 tests pass.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\DependencyInjection.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E01\\Create\\DevelopmentOtpSender.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E01\\Implement\\RequestOtpUseCase.cs",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Application.Tests\\E01\\ImplementTests.cs"
  ],
  "verification": [
    "3b31c4de46fa: cwd=C:\\Projects\\IndiaWorks\\src\\backend; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=1; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8']; output=verification/3b31c4de46fa.log",
    "73892634c87f: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8']; output=verification/73892634c87f.log",
    "ce12213ce460: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8']; output=verification/ce12213ce460.log",
    "bd9c39589f4d: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'ef', 'migrations', 'list', '--project', 'src/backend/IndiaWorks.Infrastructure', '--startup-project', 'src/backend/IndiaWorks.Api']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8']; output=verification/bd9c39589f4d.log",
    "AC1: RequestOtpUseCase.cs created in src/backend/IndiaWorks.Application/Features/E01/Implement/ - orchestrates policy application via OtpPolicyService, sends via IOtpSender, returns OtpChallengeMetadata",
    "AC2: Happy path test RequestOtp_use_case_happy_path_creates_challenge_and_sends passes - challenge persisted, OTP sent via IOtpSender, metadata returned",
    "AC3: Expired OTP error handling covered by existing OtpPolicyService tests (VerifyAsync_with_expired_challenge_returns_otp_expired passes, 41 domain tests pass)",
    "AC4: Rate limit test RequestOtp_use_case_rate_limited_does_not_send passes - RATE_LIMITED returned, provider not called again",
    "AC5: No plaintext OTP in logs test RequestOtp_use_case_no_plaintext_otp_in_logs passes - OTP codes (123456, 000000), full identifier (+919****3210), full GUIDs absent from logs",
    "AC6: Source review: RequestOtpUseCase reuses existing OtpChallenge, OtpPurpose, IOtpSender, IOtpPolicyService, OtpSettings - no new parallel domain concept introduced",
    "AC7: dotnet build IndiaWorks.slnx -c Release exit 0 (proof 73892634c87f); dotnet test IndiaWorks.slnx -c Release --no-build exit 0 (proof ce12213ce47f), 83 tests pass (41 domain + 33 application + 1 architecture + 8 integration, 3 skipped)",
    "AC8: Source review confirms: RequestOtpUseCase masks account IDs (MaskGuid), DevelopmentOtpSender masks identifiers and account IDs in provider message IDs, no forbidden secret/contact/identity data in logs"
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
