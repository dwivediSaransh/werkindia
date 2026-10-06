# IndiaWorks Prepared Executor Prompt

The host has already selected exactly one Story ID and prepared
`.hermes/current-run/CONTEXT.md`.

Read `CONTEXT.md` completely in one file-read operation before editing. It
contains the repository rules, Hermes shared rules, selected row, selected
package, sprint context and direct dependency packages. If tool output is
truncated, read the remaining range. Do not reread embedded files individually.

Implement only the selected Story ID. Use:

```powershell
python cron/story_runtime.py --root . check --acs <AC_IDS> --cwd <source-dir> --timeout 180 -- <command argv>
python cron/story_runtime.py --root . report --outcome DONE|BLOCKED|IN_PROGRESS --summary "<facts>" --ac "AC1=<evidence>" --ac "AC2=<evidence>"
```

Execution rules for the small coding agent:

1. Read `.hermes/current-run/RUN_MANIFEST.json` and `.hermes/current-run/EXECUTION_DETAILS.json` first.
2. Use `source_root`, `planned_files`, and `existing_planned_files` from the packet. Inspect only needed implementation files, direct callers, direct dependencies, and tests.
3. Fill `.hermes/current-run/PLAN.md` with technical assumptions, minimum implementation steps, and the verification plan before editing.
4. Use the smallest connected change forest needed for that Story ID. Do not implement dependency or downstream stories.
5. Work in the existing checkout. Do not clone, create a git worktree, initialize a nested repo, copy the repository, create alternate source roots, or write under nested paths such as `src/web/src/web` or `src/web/src/web.backup`.
6. Preserve the approved stack: Angular standalone frontend under `src/web`, .NET backend, HotChocolate GraphQL as the primary product API, limited REST infrastructure endpoints, and SQL Server/EF Core code-first migrations for schema authority.
7. For Angular work, use `src/web` as the app root. TypeScript imports must not end in `.ts`, and component `templateUrl`/`styleUrl` paths must resolve beside the component. Do not create missing templates or styles in a duplicate nested tree.
8. Do not count automation files, generated docs, `.hermes`, Angular cache/dist, `bin`, `obj`, or editor files as story delivery unless the selected story explicitly owns that automation or documentation surface.
9. Make delivery progress: reproduce the selected failure or required behavior once using `story_runtime.py check`, patch the smallest relevant source path, and rerun the focused check. If a command fails, read its saved output and fix the specific path/configuration/code issue before running more broad checks.
10. Prefer normal repository file edits. If a patch fails, inspect the exact file content and retry with a smaller scoped edit.
11. After editing, run the selected package validation commands through `story_runtime.py check`, not directly. Map each command to the acceptance IDs it proves. Pass ordinary argv such as `npm run lint` or `dotnet test`; the runtime resolves Windows `.cmd` shims while keeping execution shell-free.
12. If repeated checks fail while no selected-story source file changes, report `IN_PROGRESS` or `BLOCKED` with the exact blocker; do not loop on reads, failed patch attempts, or unrelated command retries.
13. Before reporting `DONE`, confirm every selected `ACn` has both a passing `story_runtime.py check` proof and a matching `--ac "ACn=<specific evidence>"` explanation. A passing command plus an AC label is not enough proof by itself. If any AC lacks proof or explanation, report `IN_PROGRESS` with the missing item instead of `DONE`.

For `DONE`, include evidence for every acceptance criterion. For `BLOCKED`,
include the exact blocker. Stop after reporting; the host finalizes.
