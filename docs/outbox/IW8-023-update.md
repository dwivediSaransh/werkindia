# IW8-023: Implement verify-OTP use case

Sprint: Sprint 1
Outcome: DONE

IW8-023 verify-OTP use case complete. VerifyOtpRequest/Result/UseCase implemented reusing IOtpPolicyService, IOtpChallengeRepository, and OtpErrorCodes from dependencies IW8-018/IW8-020. No new domain entities, tables, or parallel concepts introduced. 4 ImplementTests cover happy path, expired OTP, wrong code, and no-plaintext-OTP-in-logs. Build + full test suite pass: 37 Application + 41 Domain + 1 Architecture + 16 Integration = 95 passed, 0 failed, 3 skipped.

```json
{
  "story_id": "IW8-023",
  "outcome": "DONE",
  "summary": "IW8-023 verify-OTP use case complete. VerifyOtpRequest/Result/UseCase implemented reusing IOtpPolicyService, IOtpChallengeRepository, and OtpErrorCodes from dependencies IW8-018/IW8-020. No new domain entities, tables, or parallel concepts introduced. 4 ImplementTests cover happy path, expired OTP, wrong code, and no-plaintext-OTP-in-logs. Build + full test suite pass: 37 Application + 41 Domain + 1 Architecture + 16 Integration = 95 passed, 0 failed, 3 skipped.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\DependencyInjection.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E01\\Implement\\VerifyOtpRequest.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E01\\Implement\\VerifyOtpResult.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E01\\Implement\\VerifyOtpUseCase.cs",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Application.Tests\\E01\\ImplementTests.cs"
  ],
  "verification": [
    "d506a61b8069: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build', '--filter', 'FullyQualifiedName~ImplementTests']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8']; output=verification/d506a61b8069.log",
    "901b66b305ce: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build']; exit=0; ACs=['AC1', 'AC7']; output=verification/901b66b305ce.log",
    "AC1: dotnet build IndiaWorks.slnx -c Release exit=0 (proof 901b66b305ce)",
    "AC2: VerifyOtp_use_case_happy_path_returns_success passed: Executes VerifyOtpUseCase with valid challenge+code, returns success with AccountId, Provider='Otp', IsNewAccount=false (proof d506a61b8069)",
    "AC3: VerifyOtp_use_case_expired_challenge_returns_otp_expired passed: TtlSeconds=0 challenge returns OtpExpired error code with 'expired' in message (proof d506a61b8069)",
    "AC4: VerifyOtp_use_case_wrong_code_returns_otp_invalid passed: Wrong code returns OtpInvalid error code (proof d506a61b8069)",
    "AC5: VerifyOtp_use_case_no_plaintext_otp_in_logs passed: Assert.DoesNotContain(correctCode) and Assert.DoesNotContain(identifier) on VerifyOtpUseCase log entries (proof d506a61b8069)",
    "AC6: No parallel domain concept: VerifyOtpUseCase reuses existing IOtpPolicyService, IOtpChallengeRepository, OtpErrorCodes, OtpChallenge, VerifyOtpRequest/Result \u2014 no new entities, tables, enums, or API operations introduced; DI registers only VerifyOtpUseCase (src/backend/IndiaWorks.Application/DependencyInjection.cs:17) alongside existing RequestOtpUseCase (proof: source inspection + AC7 test pass)",
    "AC7: Full test suite passed: 37 Application + 41 Domain + 1 Architecture + 16 Integration = 95 passed, 0 failed, 3 skipped (proof 901b66b305ce)",
    "AC8: No forbidden data in logs: VerifyOtp_use_case_no_plaintext_otp_in_logs asserts correctCode ('654321') and normalized identifier ('+919****3210') absent from VerifyOtpUseCase log output; VerifyOtpUseCase masks ChallengeId and AccountId via MaskGuid() before logging (VerifyOtpUseCase.cs:25-27,53-56,65) (proof d506a61b8069)"
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
