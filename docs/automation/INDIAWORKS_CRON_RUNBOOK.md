# IndiaWorks Hermes Cron Runbook

One invocation -> one Story ID -> verification -> result -> stop.

The one-story runner is `cron/run_hermes_story.py`. It validates the working set,
prepares the next eligible story, starts Hermes against the prepared context,
captures proof, finalizes the backlog, and archives the run under `.hermes/runs/`.
Keep native cron delivery on `local`; the controller writes concise progress
directly into the IndiaWorks profile output store. Native `bot-chat:*` delivery
can block dispatch after long runs because it starts a delivery turn after the
script already finished.

The installed one-story cron job must use `indiaworks_story.py` in no-agent
mode, matching the Scapier controller pattern. The script invokes
`cron/run_hermes_story.py`, which validates the working set, prepares one story,
starts a fresh Hermes one-shot process in the isolated IndiaWorks profile,
captures concrete source/check progress, finalizes the backlog, and archives the
run under `.hermes/runs/`.

The coding model still belongs to the Hermes cron job, not the repository
script. `indiaworks_story.py` reads the job's editable `provider`, `model`, and
`reasoning_effort` fields from Hermes `jobs.json` and passes them into the inner
executor. Change models through the Hermes cron edit modal or:

```powershell
hermes cron edit 2f464807b97a --provider openrouter --model poolside/laguna-s-2.1:free --reasoning-effort low
```

Do not pin the cron to time-limited promotional free models. The old
`upstage/solar-pro4:free` route on Nous and the
`nvidia/nemotron-3-super-120b-a12b` route have both produced credit or
availability failures in Hermes runs. As of 2026-10-06, the active IndiaWorks
one-story job is pinned to `openrouter` + `poolside/laguna-s-2.1:free` after a
successful live smoke run. If this free route becomes rate-limited, update the
job through `hermes cron edit <job-id> --provider <provider> --model <model>`;
keep `--script indiaworks_story.py --no-agent` so stdout delivery and progress
checks keep working.

Before scheduling:

```powershell
python cron/validate_working_set.py --root .
dotnet build IndiaWorks.slnx -c Release
dotnet test IndiaWorks.slnx -c Release --no-build
```

The active sprint is the earliest unfinished sprint. Later sprints are not
eligible while active-sprint work remains unfinished or blocked.

The watchdog entrypoint is `cron/cron_continue_watchdog.py`. It is intended to
retry only the same `IN PROGRESS` story and must not start fresh `TODO` work.
It follows the Scapier continue-watchdog pattern: one OS-held tick lock,
same-story retries, persisted retry/backoff, process identity checks before
recovery, and truthful finalization through the existing proof/outbox/finalizer
path. It must respect pauses, blockers, unmet dependencies, sprint gates, and
the original scheduler window.

The watchdog must classify common waste patterns before spending another model
turn:

- Nested duplicate source trees under `src/web/src/web` or
  `src/web/src/web.backup`.
- Repeated failed checks with no selected-story delivery files changed.
- Runs that exit without useful `REPORT.json` summary, blocker, or AC evidence.
- Progress that touches only automation, `.hermes`, generated docs, Angular
  cache/dist, `bin`, `obj`, or editor files when the story is not about those
  surfaces.

Those conditions should produce health flags, retry backoff, or human review;
they are not evidence of successful implementation.

Reference BRD, FRD, DFD, architecture, spreadsheet and diagram artifacts are
source material. Do not delete them as generated duplicates.

Agent package acceptance checklist items under section 15 are exposed to Hermes
as `AC1`, `AC2`, and so on when explicit `**ACn:**` labels are not present. Use
those IDs in `story_runtime.py check` and `story_runtime.py report` evidence so
each small agent has a deterministic way to prove completion.

For Angular stories, the canonical root is `src/web`; imports must not include
the `.ts` extension, and component template/style paths must resolve beside the
component. For backend stories, preserve the .NET modular monolith with
HotChocolate GraphQL as the primary product API, limited REST infrastructure
edges, and SQL Server/EF Core code-first migrations as the schema authority.
