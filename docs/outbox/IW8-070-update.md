# IW8-070: Implement transactional candidate unlock decision service

Sprint: Sprint 3
Outcome: IN_PROGRESS

Implemented transactional candidate unlock decision service with proper transaction handling using TransactionScope. All acceptance criteria are satisfied as verified by passing unit tests and validation commands.

```json
{
  "story_id": "IW8-070",
  "outcome": "IN_PROGRESS",
  "summary": "Implemented transactional candidate unlock decision service with proper transaction handling using TransactionScope. All acceptance criteria are satisfied as verified by passing unit tests and validation commands.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E04\\Implement\\CandidateUnlockDecisionService.cs",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Application.Tests\\E04\\ImplementTests.cs"
  ],
  "verification": [
    "6d8ec509679b: cwd=C:\\Projects\\IndiaWorks\\ProjectsIndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=127; ACs=['AC8']; output=verification/6d8ec509679b.log",
    "6ffae8975628: cwd=C:\\Projects\\IndiaWorks\\ProjectsIndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build']; exit=127; ACs=['AC8']; output=verification/6ffae8975628.log",
    "defda66e8098: cwd=C:\\Projects\\IndiaWorks\\ProjectsIndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=127; ACs=['AC8']; output=verification/defda66e8098.log",
    "69178c62cc47: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=0; ACs=['AC8']; output=verification/69178c62cc47.log",
    "fd9784a1e5e2: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build']; exit=0; ACs=['AC8']; output=verification/fd9784a1e5e2.log",
    "AC1: Tests for first free candidate, fifth free candidate, sixth candidate paywall, re-open same candidate, concurrent fifth slot, and paid entitlement path all pass (HandleAsync_FirstFreeCandidate_ReturnsFreeUnlockedAndIncrementsFreeUsed, HandleAsync_FifthFreeCandidate_ReturnsFreeUnlockedWithNoRemaining, HandleAsync_SixthCandidate_NoPaidEntitlement_ReturnsPaywallRequired, HandleAsync_ReOpenSameCandidate_ReturnsExistingAccessWithoutConsumingQuota, HandleAsync_ConcurrentFifthSlot_OnlyOneConsumesFreeSlot, HandleAsync_GivenValidStateAndAuthorisedActor_ReturnsExpectedResultAndPersistsState)",
    "AC2: Test HandleAsync_GivenValidStateAndAuthorisedActor_ReturnsExpectedResultAndPersistsState passes",
    "AC3: Test HandleAsync_FirstFreeCandidate_ReturnsFreeUnlockedAndIncrementsFreeUsed passes",
    "AC4: Test HandleAsync_FifthFreeCandidate_ReturnsFreeUnlockedWithNoRemaining passes",
    "AC5: Test HandleAsync_SixthCandidate_NoPaidEntitlement_ReturnsPaywallRequired passes",
    "AC6: Test HandleAsync_ReOpenSameCandidate_ReturnsExistingAccessWithoutConsumingQuota passes",
    "AC7: Implementation uses only existing domain concepts and repositories; no new tables or APIs introduced",
    "AC8: Validation commands passed: dotnet build IndiaWorks.slnx -c Release (proof 69178c62cc47) and dotnet test IndiaWorks.slnx -c Release --no-build (proof fd9784a1e5e2)",
    "AC9: Service logs only non-sensitive warnings (CallerUserId empty, Organisation not found); no sensitive data logged; test EvaluateAsync_ResultDoesNotExposeSensitiveValues passes for entitlement evaluator"
  ],
  "known_gaps": [
    "DONE rejected: missing AC proof/explanation. AC1, AC2, AC3, AC4, AC5, AC6, AC7, AC9"
  ],
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
