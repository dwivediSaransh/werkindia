# IW8-033: Build employer mobile/email OTP login screen

Sprint: Sprint 1
Outcome: DONE

IW8-033: Employer OTP login screen complete. Added /employer/otp-login route to employer.routes.ts wiring up the existing EmployerOtpLoginComponent (mobile/email identifier selection, OTP request/verify, loading/error/success states, resend cooldown, stable error code mapping, secret-logging-safe). Validation: lint=pass (proof 3aaffad73921), test=89/89 passed across 12 files (proof 3467f1b42fe8), build=pass with pre-existing SCSS budget warnings unrelated to this story (proof 4ec0dd627f32).

```json
{
  "story_id": "IW8-033",
  "outcome": "DONE",
  "summary": "IW8-033: Employer OTP login screen complete. Added /employer/otp-login route to employer.routes.ts wiring up the existing EmployerOtpLoginComponent (mobile/email identifier selection, OTP request/verify, loading/error/success states, resend cooldown, stable error code mapping, secret-logging-safe). Validation: lint=pass (proof 3aaffad73921), test=89/89 passed across 12 files (proof 3467f1b42fe8), build=pass with pre-existing SCSS budget warnings unrelated to this story (proof 4ec0dd627f32).",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\docs\\backlog\\IndiaWorks_Master_Backlog.csv",
    "C:\\Projects\\IndiaWorks\\docs\\sprints\\Sprint_01\\backlog.csv",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\employer\\employer.routes.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\employer\\otp-login\\employer-otp-login.component.html",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\employer\\otp-login\\employer-otp-login.component.scss",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\employer\\otp-login\\employer-otp-login.component.spec.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\employer\\otp-login\\employer-otp-login.component.ts"
  ],
  "verification": [
    "3aaffad73921: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\Users\\\\DELL\\\\AppData\\\\Local\\\\hermes\\\\tools\\\\node-26.7.0-win32-x64\\\\npm.CMD', 'run', 'lint']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8', 'AC9']; output=verification/3aaffad73921.log",
    "3467f1b42fe8: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\Users\\\\DELL\\\\AppData\\\\Local\\\\hermes\\\\tools\\\\node-26.7.0-win32-x64\\\\npm.CMD', 'test', '--', '--watch=false']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8', 'AC9']; output=verification/3467f1b42fe8.log",
    "4ec0dd627f32: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\Users\\\\DELL\\\\AppData\\\\Local\\\\hermes\\\\tools\\\\node-26.7.0-win32-x64\\\\npm.CMD', 'run', 'build']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8', 'AC9']; output=verification/4ec0dd627f32.log",
    "AC1: EmployerOtpLoginComponent template renders mobile/email toggle buttons; spec tests describe('identifier type toggle') verify switching between mobile and email.",
    "AC2: Spec tests describe('OTP request happy path - mobile'), describe('OTP request happy path - email'), describe('OTP verify happy path') verify full request->verify->session-set->success flow via HttpTestingController.",
    "AC3: Spec test describe('OTP expiry error') verifies OTP_EXPIRED stable error code mapped to actionable message; mapError extracts Code from API error response.",
    "AC4: Spec test describe('rate limit error') verifies RATE_LIMITED mapping; component maps 429 status to RATE_LIMITED code.",
    "AC5: Spec tests describe('secret logging') verify access token, device id, raw identifier, and challenge id are absent from console.error/log output during success and failure flows.",
    "AC6: Spec tests describe('loading state and double-submit prevention') verify canSubmitIdentifier/canSubmitOtp disabled during loading; spinner shown in template.",
    "AC7: No new domain concepts: component uses existing AuthService (IW8-022/IW8-028 REST contracts) and AuthSessionStore (IW8-031); no parallel store, interceptor, or domain model introduced.",
    "AC8: npm run lint exit 0 (proof 3aaffad73921); npm test -- --watch=false 12/12 files, 89/89 tests pass (proof 3467f1b42fe8); npm run build exit 0 (proof 4ec0dd627f32).",
    "AC9: Component never writes OTP codes, access tokens, refresh tokens, raw phone/email to console; masked identifiers used in UI messages only; spec tests assert absence of secret values in log output."
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
