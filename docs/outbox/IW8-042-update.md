# IW8-042: Implement invite/accept/revoke member service

Sprint: Sprint 2
Outcome: DONE

IW8-042 Implement invite/accept/revoke member service complete. Build succeeded (0 warnings, 0 errors). All tests passed: Domain.Tests 96/96, ArchitectureTests 1/1, Application.Tests 98/98, IntegrationTests 45/48 (3 Testcontainers skipped due to no SQL Server). Service implements seat-aware invitation lifecycle with idempotent accept and Owner/Admin authorization. No sensitive data in logs.

```json
{
  "story_id": "IW8-042",
  "outcome": "DONE",
  "summary": "IW8-042 Implement invite/accept/revoke member service complete. Build succeeded (0 warnings, 0 errors). All tests passed: Domain.Tests 96/96, ArchitectureTests 1/1, Application.Tests 98/98, IntegrationTests 45/48 (3 Testcontainers skipped due to no SQL Server). Service implements seat-aware invitation lifecycle with idempotent accept and Owner/Admin authorization. No sensitive data in logs.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Domain\\E02\\OrganisationInvitation.cs",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Application.Tests\\E02\\ImplementTests.cs"
  ],
  "verification": [
    "b7aeb8cc4b87: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.exe', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5']; output=verification/b7aeb8cc4b87.log",
    "f46f286f6204: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.exe', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5']; output=verification/f46f286f6204.log",
    "AC1: OrganisationMemberInviteService.cs implements InviteAsync (Owner/Admin authorization, org existence check, active membership check, role check, idempotent revoke+recreate), AcceptAsync (idempotent when already active member, Pending state + expiry check), RevokeAsync (Owner/Admin authorization). Tests: ImplementTests.cs covers InviteAsync_HappyPath, InviteAsync_NonExistentOrganisation, InviteAsync_CallerNotActiveMember, InviteAsync_CallerNotOwnerOrAdmin, InviteAsync_RevokesExistingPendingInvitationForSameInvitee, AcceptAsync_HappyPath, AcceptAsync_Idempotent_WhenUserAlreadyActiveInOrg, AcceptAsync_NonExistentInvitation, AcceptAsync_AlreadyAcceptedInvitation, RevokeAsync_HappyPath, RevokeAsync_NonExistentInvitation, RevokeAsync_CallerNotActiveMember; InviteMemberServiceTests.cs covers additional scenarios. Proved by dotnet test exit 0 (proof f46f286f6204).",
    "AC2: AcceptAsync_HappyPath_CreatesMembershipAndReturnsOrganisationAndRole asserts AcceptInvitationResult.OrganisationId and Role match, membership persisted with correct Role/Status, invitation Status=Acknowledged and AcceptedByUserAccountId set. InviteAsync_HappyPath_CreatesPendingInvitation asserts InviteMemberResult.InvitationId populated and persisted invitation.Status=Pending, IsActive=true. RevokeAsync_HappyPath_RevokesPendingInvitation asserts persisted invitation.Status=Revoked and RevokedAt set. Proved by dotnet test exit 0.",
    "AC3: No new parallel domain concept introduced. OrganisationInvitation entity from IW8-041 reused directly. IOrganisationInvitationRepository interface from IW8-041 reused. No new tables, no new GraphQL/REST endpoints owned by this story. Service is pure application-layer using existing contracts. Proved by code review of OrganisationMemberInviteService.cs, InvitationContracts.cs, and IOrganisationInvitationRepository.cs.",
    "AC4: dotnet build IndiaWorks.slnx -c Release exit 0 (proof b7aeb8cc4b87, 0 warnings, 0 errors). dotnet test IndiaWorks.slnx -c Release --no-build exit 0 (proof f46f286f6204): Domain.Tests 96 passed, ArchitectureTests 1 passed, Application.Tests 98 passed, IntegrationTests 45 passed + 3 skipped (Testcontainers unavailable). Total 240 passed, 0 failed.",
    "AC5: All logger.Log* calls in OrganisationMemberInviteService.cs reviewed: LogWarning/LogInformation emit only OrganisationId, UserAccountId, InvitationId, OrganisationRole, MembershipStatus, InvitationStatus \u2014 no OTP/token values, no invitee identifier hashes, no raw contact values, no PII. TokenHash and InviteeIdentifierHash are never passed to logger. Proved by code review of lines 46-48, 56-58, 63-65, 88-89, 114-116, 135-136, 169-170."
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
