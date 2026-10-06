# IndiaWorks Hermes Cron Job Prompt

Use this text as the recurring Hermes cron job instruction. Set the cron job
working directory to the repository root.

## Hard invariant

This run may implement exactly ONE Story ID. Never implement a second story in
this run.

## Step 1 - Prepare the canonical run

```powershell
python .\cron\prepare_run.py --root .
```

Exit codes:

- `0` = one story is prepared; continue.
- `20` = all stories are DONE; end successfully.
- `21` = active sprint has no eligible story; report the block and end.
- `22` = multiple IN PROGRESS stories or state conflict; request correction.
- `30` = another run holds the lock; end without changing anything.

Do not manually choose a Story ID.

## Step 2 - Load prepared instructions

Read completely:

1. `AGENTS.md`
2. `HERMES_SHARED_RULES.md`
3. `.hermes/current-run/CONTEXT.md`

The selected package is embedded in `CONTEXT.md`. Do not load another sprint CSV
or unrelated package.

## Step 3 - Implement and verify

Inspect only the files required by the selected story. Implement the smallest
complete change. Capture verification with `cron/story_runtime.py check`, then
write the outcome with `cron/story_runtime.py report`.

Do not create duplicate source roots, nested repositories, copied checkouts, or
paths such as `src/web/src/web` and `src/web/src/web.backup`. For Angular work,
use the existing `src/web` application root. For backend work, use the existing
.NET/HotChocolate/EF Core SQL Server structure. If repeated checks fail while no
selected-story source file changes, report `IN_PROGRESS` or `BLOCKED` with the
exact reason instead of consuming more turns.

## Step 4 - Stop

The host finalizes. Do not run `prepare_run.py` again.
