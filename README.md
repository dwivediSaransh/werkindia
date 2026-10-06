# IndiaWorks implementation bundle

This is the Hermes-ready implementation package for the approved IndiaWorks architecture. It keeps the **278 stable IW8 Jira IDs** so existing Jira imports/references do not break, while organizing the backlog into a canonical master ledger and sprint-local working sets.

## Start here

1. `implementation.md` — mandatory master implementation handbook.
2. `docs/backlog/IndiaWorks_Master_Backlog.csv` — canonical groomed backlog and status ledger.
3. `docs/backlog/IndiaWorks_Master_Backlog_Index.json` — master monitoring index derived from the backlog.
4. `docs/backlog/IndiaWorks_Quality_Matrix.csv` — package quality gate matrix for automation/review.
5. `docs/sprints/Sprint_XX/SPRINT_CONTEXT.md` — active sprint instructions and story order.
6. `docs/sprints/Sprint_XX/backlog.csv` — sprint-local backlog view.
7. `docs/sprints/Sprint_XX/agent-packages/<StoryId>.md` — one complete work order per story.

## Source-of-truth map

Use these artifacts for one purpose each:

| Artifact                                                                               | Role                                                                                                                |
| -------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| `implementation.md`                                                                    | Master execution handbook, architecture rules, schema invariants, workflow, and definition of done.                 |
| `docs/backlog/IndiaWorks_Master_Backlog.csv`                                           | Canonical status ledger for all 278 stable `IW8-*` stories.                                                         |
| `docs/backlog/IndiaWorks_Master_Backlog_Index.json`                                    | Durable master monitoring index with sprint counts, dependency readiness, story metadata, and source-file pointers. |
| `docs/backlog/IndiaWorks_Quality_Matrix.csv`                                           | Quality gate matrix rebuilt from backlog notes and package content.                                                 |
| `.hermes/backlog-index.json`                                                           | Hermes-facing copy of the master monitoring index for automation.                                                   |
| `docs/sprints/Sprint_XX/SPRINT_CONTEXT.md`                                             | Sprint-local Hermes context, selection rules, and story list.                                                       |
| `docs/sprints/Sprint_XX/backlog.csv`                                                   | Small sprint-local backlog view generated from the master ledger.                                                   |
| `docs/sprints/Sprint_XX/agent-packages/<StoryId>.md`                                   | Executable local contract for exactly one story.                                                                    |
| `docs/shared/agent-guides/`                                                            | Shared stack, backend, Angular, testing, and API-boundary guidance.                                                 |
| `references/IndiaWorks_Unified_SaaS_BRD.docx` / `.pdf`                                 | Business requirements source document. Retain both editable Word and reviewable PDF forms.                          |
| `references/IndiaWorks_Unified_SaaS_FRD.docx` / `.pdf`                                 | Functional requirements source document. Retain both editable Word and reviewable PDF forms.                        |
| `references/IndiaWorks_Unified_SaaS_DFD.docx` / `.pdf` and `references/DFD_*.png`      | Data-flow source documentation and diagrams.                                                                        |
| `references/IndiaWorks_Unified_SaaS_Technical_Architecture.docx` / `.pdf`              | Technical architecture source document.                                                                             |
| `references/IndiaWorks_Unified_SaaS_Backlog_Dependency_Graph.png`                      | Visual dependency graph retained for review and monitoring context.                                                 |
| `references/IndiaWorks_Unified_SaaS_Data_Model.xlsx`                                   | Approved source data model workbook.                                                                                |
| `references/IndiaWorks_Unified_SaaS_Architecture.md`                                   | Approved product and technical architecture intent.                                                                 |
| `references/IndiaWorks_Unified_SaaS_GraphQL.graphql`                                   | Conceptual GraphQL contract; final runtime schema is generated from Hot Chocolate code-first implementation.        |
| `references/IndiaWorks_Unified_SaaS_SQLServer.sql`                                     | Relationship/index reference only; never execute it as a production schema script.                                  |
| `references/IndiaWorks_India_Workforce_Taxonomy_DB_GraphQL_Architecture.docx` / `.pdf` | Workforce taxonomy source architecture reference.                                                                   |
| `references/IndiaWorks_Workforce_Taxonomy_Data_Model.xlsx`                             | Workforce taxonomy source data model workbook.                                                                      |

The application source tree now exists. Current foundation code includes the Angular app under `src/web`, the .NET modular-monolith backend under `src/backend`, EF Core SQL Server persistence, Hot Chocolate GraphQL at `/graphql`, and REST-style operational endpoints for `/`, `/ready`, and `/health`.

## Coverage

- Jira stories: **278**
- Agent packages: **278**
- Packages with prerequisites/pseudocode/security/observability/rollback: **278/278**
- Concrete test cases documented across packages: **1236**
- Package QA issues: **0**
- Master backlog index rows: **278**
- Quality matrix rows: **278**

## Important

Story IDs remain `IW8-xxx` intentionally. The `IW8` prefix is a stable Jira/story namespace, not a stale package-version marker.

Reference BRD, FRD, DFD, technical architecture, PDF, Word, spreadsheet, and diagram artifacts are source material. Do not delete them as duplicate generated output. If a derived Markdown/backlog artifact is improved, preserve the original business/reference artifact unless the user explicitly asks to remove it.

## Final package-quality gate

- Minimum concrete test cases per story: **4**
- Minimum pseudocode steps per story: **5**
- Minimum binary acceptance criteria per story: **5**
- Packages passing automated content QA: **278/278**

## Automation maintenance

When backlog status, dependencies, package paths, or quality metadata change, update these together:

1. `docs/backlog/IndiaWorks_Master_Backlog.csv`
2. matching `docs/sprints/Sprint_XX/backlog.csv`
3. selected `docs/sprints/Sprint_XX/agent-packages/<StoryId>.md`
4. `docs/backlog/IndiaWorks_Master_Backlog_Index.json`
5. `.hermes/backlog-index.json`
6. `docs/backlog/IndiaWorks_Quality_Matrix.csv`
