# Hermes Host-Owned One-Story Lifecycle

ONE CRON RUN -> ONE ACTIVE SPRINT -> ONE STORY -> ONE REPORT -> STOP.

The native no-agent cron invokes `cron/run_hermes_story.py`. Python validates the
working set, invokes `prepare_run.py` exactly once, acquires the lock and selects
from the canonical master CSV. Active sprint is the earliest unfinished sprint.
Resume its single `IN PROGRESS` story, after confirming all dependencies are
`DONE`; otherwise select dependency-ready `TODO` by `P0/P1/P2/P3` then canonical
order. Multiple `IN PROGRESS` stories fail safely. `BLOCKED` keeps the sprint
gate closed. No eligible work means stop; never move to later sprints.

Python assembles `CONTEXT.md` with complete `AGENTS.md`, these rules, manifest,
current prompt, active sprint context, selected row, complete selected package
and complete direct dependency packages in that order. Reading the complete
packet satisfies these input-reading requirements. Do not separately reopen
those inputs or scan other packages. Inspect only the selected story's code.

Implement exactly the selected story. Use existing platform/tooling. No silent
shared contract changes. If required contract/resource is missing or instructions
conflict, report `BLOCKED` and stop. If incomplete, report `IN_PROGRESS` with
remaining work and stop.

Use:

```powershell
python cron/story_runtime.py --root . check --acs AC1,AC2 --cwd <source-dir> --timeout 180 -- <command argv>
```

Then report:

```powershell
python cron/story_runtime.py --root . report --outcome DONE --summary "<facts>" --ac AC1="<evidence>"
```

Repeat `--ac` for every AC and `--gap` as needed. `BLOCKED` requires `--blocker`.
The host creates outbox artifacts and `RESULT.json`, updates CSVs, archives
evidence and releases the lock after the executor exits. The model must never
edit CSV status directly, finalize, select again or create duplicate handoff
documents. `DONE` requires source changes, successful proof and an explanation
for every AC.

Work in the existing checkout. Preserve dirty/resumable work. No blanket stash,
reset hard, destructive cleanup, dropping unknown stashes or unrelated commits.

Canonical selector, sprint gate, priority, IDs, dependencies and acceptance
criteria are product contracts; automation never invents or reorders them.
