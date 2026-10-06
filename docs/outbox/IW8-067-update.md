# IW8-067: Create CandidatePaywallEvent entity

Sprint: Sprint 3
Outcome: DONE

IW8-067 CandidatePaywallEvent entity implemented with domain invariants, EF configuration, DbSet registration, generated migration, and domain/integration tests. Build, full solution tests, E04 focused tests, and EF migrations list passed.

```json
{
  "story_id": "IW8-067",
  "outcome": "DONE",
  "summary": "IW8-067 CandidatePaywallEvent entity implemented with domain invariants, EF configuration, DbSet registration, generated migration, and domain/integration tests. Build, full solution tests, E04 focused tests, and EF migrations list passed.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Domain\\E04\\CandidatePaywallEvent.cs",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Domain.Tests\\E04\\CandidatePaywallEventTests.cs",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.IntegrationTests\\Database\\E04\\CandidatePaywallEventModelConfigurationTests.cs"
  ],
  "verification": [
    "446b8aadbea6: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'tests\\\\IndiaWorks.Domain.Tests\\\\IndiaWorks.Domain.Tests.csproj', '-c', 'Release', '--filter', 'FullyQualifiedName~E04']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8', 'AC9']; output=verification/446b8aadbea6.log",
    "41f2e9eb8c71: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8', 'AC9']; output=verification/41f2e9eb8c71.log",
    "1ea50b0d039d: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'tests\\\\IndiaWorks.IntegrationTests\\\\IndiaWorks.IntegrationTests.csproj', '-c', 'Release', '--filter', 'FullyQualifiedName~E04']; exit=0; ACs=['AC1', 'AC2', 'AC7', 'AC8', 'AC9']; output=verification/1ea50b0d039d.log",
    "37a57f7878da: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'ef', 'migrations', 'list', '--project', 'src\\\\backend\\\\IndiaWorks.Infrastructure', '--startup-project', 'src\\\\backend\\\\IndiaWorks.Api']; exit=0; ACs=['AC1', 'AC7', 'AC8']; output=verification/37a57f7878da.log",
    "0634798f85fb: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build']; exit=0; ACs=['AC8']; output=verification/0634798f85fb.log",
    "AC1: CandidatePaywallEvent persists shown Reason, JobContext, FreeLimit, FreeUsed, FreeRemaining, AccessSource, CorrelationId and CreatedAt; proof 41f2e9eb8c71 build and 37a57f7878da EF migrations list passed.",
    "AC2: CandidatePaywallEventTests.Create_WithValidParameters_CapturesCommercialContext proves valid state is returned from the entity factory; proof 446b8aadbea6 passed.",
    "AC3: Entity records AccessSource.FreeAllowance with freeUsed=5/freeRemaining=0 and validates free counters; proof 446b8aadbea6 passed.",
    "AC4: Counter invariants enforce freeUsed cannot exceed freeLimit and freeRemaining must equal freeLimit-freeUsed; proof 446b8aadbea6 passed.",
    "AC5: PAYWALL_REQUIRED reason and paywall event context are persisted without candidate contact fields; CandidatePaywallEvent has no phone/email/Aadhaar/PAN fields; proofs 446b8aadbea6 and 1ea50b0d039d passed.",
    "AC6: Re-open existing candidate behavior remains out of scope for this entity-only story; no unlock service behavior was changed and no duplicate concept was introduced.",
    "AC7: No parallel concept was introduced; CandidatePaywallEvents is the EF table for paywall-event audit only, with unique CorrelationId and org/candidate/CreatedAt lookup index; proofs 1ea50b0d039d and 37a57f7878da passed.",
    "AC8: dotnet build, full dotnet test --no-build, domain E04 tests, integration E04 tests, and dotnet ef migrations list all exited 0 via story_runtime proofs 41f2e9eb8c71, 0634798f85fb, 446b8aadbea6, 1ea50b0d039d, 37a57f7878da.",
    "AC9: No logging code or sensitive contact/identity fields were added; entity stores only IDs, commercial counters, source, reason, context and correlation identifier."
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
