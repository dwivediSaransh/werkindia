# IW8-040: Expose organisation create/list GraphQL operations

Sprint: Sprint 2
Outcome: IN_PROGRESS

Not yet reported.

```json
{
  "story_id": "IW8-040",
  "outcome": "IN_PROGRESS",
  "summary": "Not yet reported.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Api\\GraphQL\\E02\\CreateOrganisationInput.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Api\\GraphQL\\E02\\OrganisationMutations.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Api\\GraphQL\\E02\\OrganisationQueries.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Api\\GraphQL\\E02\\Types\\CreateOrganisationPayloadType.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Api\\GraphQL\\E02\\Types\\OrganisationSummaryType.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Api\\Program.cs",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.IntegrationTests\\GraphQL\\E02\\ExposeGraphQLTests.cs"
  ],
  "verification": [
    "658a38aae098: cwd=C:\\Projects\\IndiaWorks\\src\\backend; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=1; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5']; output=verification/658a38aae098.log",
    "87a007593ff9: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'build', 'IndiaWorks.slnx', '-c', 'Release']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5']; output=verification/87a007593ff9.log",
    "25689d729134: cwd=C:\\Projects\\IndiaWorks; argv=['C:\\\\Program Files\\\\dotnet\\\\dotnet.EXE', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build', '--verbosity', 'normal']; exit=1; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5']; output=verification/25689d729134.log"
  ],
  "known_gaps": [
    "Watchdog recovered confirmed terminated executor; completion remains unverified."
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
