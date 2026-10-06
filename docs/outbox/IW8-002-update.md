# IW8-002: Add Directory.Build.props and central NuGet package versions

Sprint: Sprint 0
Outcome: DONE

Pinned and proved the .NET foundation baseline with global.json, Directory.Build.props, central package versions for EF Core 10.0.12 and Hot Chocolate 16.6.7, and a focused package-baseline test.

```json
{
  "story_id": "IW8-002",
  "outcome": "DONE",
  "summary": "Pinned and proved the .NET foundation baseline with global.json, Directory.Build.props, central package versions for EF Core 10.0.12 and Hot Chocolate 16.6.7, and a focused package-baseline test.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Domain.Tests\\Foundation\\PackageBaselineTests.cs",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Domain.Tests\\bin\\Release\\net10.0\\IndiaWorks.Domain.Tests.dll",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Domain.Tests\\bin\\Release\\net10.0\\IndiaWorks.Domain.Tests.pdb",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Domain.Tests\\obj\\Release\\net10.0\\IndiaWorks.Domain.Tests.csproj.CoreCompileInputs.cache",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Domain.Tests\\obj\\Release\\net10.0\\IndiaWorks.Domain.Tests.dll",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Domain.Tests\\obj\\Release\\net10.0\\IndiaWorks.Domain.Tests.pdb",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Domain.Tests\\obj\\Release\\net10.0\\ref\\IndiaWorks.Domain.Tests.dll",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Domain.Tests\\obj\\Release\\net10.0\\refint\\IndiaWorks.Domain.Tests.dll"
  ],
  "verification": [
    "5650bb83ab2c: cwd=C:\\Projects\\IndiaWorks; argv=['dotnet', 'build', 'IndiaWorks.slnx', '-c', 'Release', '--no-restore']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5']; output=verification/5650bb83ab2c.log",
    "673cf65beeef: cwd=C:\\Projects\\IndiaWorks; argv=['dotnet', 'test', 'IndiaWorks.slnx', '-c', 'Release', '--no-build']; exit=0; ACs=['AC1', 'AC2', 'AC4']; output=verification/673cf65beeef.log",
    "AC1: global.json, Directory.Build.props, and Directory.Packages.props define the .NET 10 and central package baseline; PackageBaselineTests asserts the required pinned versions.",
    "AC2: The dependency-ready solution builds and the package-baseline test passes.",
    "AC3: No domain concept, table, or API operation was introduced.",
    "AC4: dotnet build IndiaWorks.slnx -c Release --no-restore and dotnet test IndiaWorks.slnx -c Release --no-build passed through story_runtime proof.",
    "AC5: No logging/search/contact/identity surface was added by this package baseline work."
  ],
  "known_gaps": [],
  "blocker": null,
  "notes": ["Host-generated inventory, proof records and outbox handoff."],
  "obsidian_status": "OUTBOX",
  "graphify_status": "OUTBOX"
}
```

No MCP sync claimed. Contract/rollout details, if applicable, must be recorded in the summary or AC explanations.
