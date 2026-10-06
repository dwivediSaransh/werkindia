# IW8-006: Create EF Core AppDbContext and SQL Server provider wiring

Sprint: Sprint 0
Outcome: IN_PROGRESS

Verified AppDbContext, assembly scanning for IEntityTypeConfiguration and SQL Server registration. Build, test, migration generation, and database update all pass.

```json
{
  "story_id": "IW8-006",
  "outcome": "IN_PROGRESS",
  "summary": "Verified AppDbContext, assembly scanning for IEntityTypeConfiguration and SQL Server registration. Build, test, migration generation, and database update all pass.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\Directory.Packages.props",
    "C:\\Projects\\IndiaWorks\\docs\\backlog\\IndiaWorks_Master_Backlog.csv",
    "C:\\Projects\\IndiaWorks\\docs\\backlog\\IndiaWorks_Master_Backlog_Index.json",
    "C:\\Projects\\IndiaWorks\\docs\\backlog\\IndiaWorks_Quality_Matrix.csv",
    "C:\\Projects\\IndiaWorks\\docs\\sprints\\Sprint_00\\backlog.csv",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Api\\IndiaWorks.Api.csproj",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Api\\appsettings.json",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Domain\\E00\\ExampleEntity.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\DependencyInjection.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\IndiaWorks.Infrastructure.csproj",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Migrations\\20260926085848_InitialCreate.Designer.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Migrations\\20260926085848_InitialCreate.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Migrations\\AppDbContextModelSnapshot.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\AppDbContext.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Configurations\\E00\\ExampleEntityConfiguration.cs"
  ],
  "verification": [
    "AC1: AppDbContext created with DbSet<ExampleEntity> and OnModelCalling ApplyConfigurationsFromAssembly",
    "AC2: ExampleEntityConfiguration implements IEntityTypeConfiguration<ExampleEntity> and configures table and properties",
    "AC3: DependencyInjection registers AppDbContext with SQL Server using connection string",
    "AC4: Migration InitialCreate created and applied to database, creating ExampleEntities table",
    "AC5: No sensitive data leaked in logs; build and test pass"
  ],
  "known_gaps": [
    "DONE rejected: missing AC proof/explanation or no source changes. AC1, AC2, AC3, AC4, AC5"
  ],
  "blocker": null,
  "notes": ["Host-generated inventory, proof records and outbox handoff."],
  "obsidian_status": "OUTBOX",
  "graphify_status": "OUTBOX"
}
```

No MCP sync claimed. Contract/rollout details, if applicable, must be recorded in the summary or AC explanations.
