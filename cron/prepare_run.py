from __future__ import annotations

import argparse
import json
import os
import shutil
import sys
from datetime import datetime, timezone
from pathlib import Path
from execution_packet import prepare_packet

from backlog_lib import (
    canonical_package_path,
    eligible_todo_rows,
    find_active_sprint,
    json_dump_atomic,
    parse_dependencies,
    read_master,
    row_map,
    sprint_dir_name,
    sync_sprint_views,
    write_master,
)

EXIT_NO_MORE_WORK = 20
EXIT_NO_ELIGIBLE_WORK = 21
EXIT_CONFLICT = 22
EXIT_LOCKED = 30


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def acquire_lock(root: Path, recover_stale_lock: bool) -> Path:
    lock_path = root / ".hermes" / "run.lock"
    lock_path.parent.mkdir(parents=True, exist_ok=True)

    if lock_path.exists():
        if not recover_stale_lock:
            print(f"ERROR: run lock exists: {lock_path}", file=sys.stderr)
            print("Confirm no Hermes run is active. Use --recover-stale-lock only for a verified stale lock.", file=sys.stderr)
            raise SystemExit(EXIT_LOCKED)
        backup = lock_path.with_name(f"run.lock.recovered.{datetime.now().strftime('%Y%m%d%H%M%S')}")
        lock_path.replace(backup)

    try:
        fd = os.open(lock_path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
    except FileExistsError:
        raise SystemExit(EXIT_LOCKED)

    with os.fdopen(fd, "w", encoding="utf-8") as f:
        json.dump({"status": "PREPARING", "started_at": utc_now(), "pid": os.getpid()}, f, indent=2)
    return lock_path


def release_lock(lock_path: Path) -> None:
    lock_path.unlink(missing_ok=True)


def clean_current_run(root: Path) -> Path:
    current = root / ".hermes" / "current-run"
    if current.exists():
        shutil.rmtree(current)
    (current / "dependencies").mkdir(parents=True, exist_ok=True)
    return current


def build_prompt(manifest: dict, row: dict[str, str]) -> str:
    dep_names = ", ".join(manifest["direct_dependencies"]) or "None"
    return f"""# Hermes Current Run

## Canonical selection
- Story ID: **{row['Story ID']}**
- Sprint: **{row['Sprint']}**
- Priority: **{row['Priority']}**
- Epic: **{row['Epic']}**
- Title: **{row['Title']}**
- Selection reason: **{manifest['selection_reason']}**
- Direct dependencies: **{dep_names}**

## Mandatory behavior
1. Read `CONTEXT.md` completely: it embeds the complete root rules, manifest, this prompt, active sprint context, selected row, selected package and direct dependency packages in order. Do not reopen those same inputs individually.
7. Inspect only the code/tests needed by this Story ID.
8. Implement **only {row['Story ID']}**.
9. Verify every acceptance criterion.
10. Use `cron/story_runtime.py check` to capture verification output and `report` to record the outcome, summary, blockers and AC explanations.
11. The host creates outbox artifacts and RESULT.json, updates CSVs, archives evidence and releases the lock after you exit. Do not finalize yourself.
13. **STOP. Do not select or implement another story.**

## Selected package
`{manifest['prepared_agent_package']}`

## Active sprint working files
- `{manifest['sprint_context']}`
- `{manifest['sprint_backlog']}`

## If blocked
Do not guess. Use story_runtime.py report with outcome BLOCKED and the exact blocker/question, then stop. The host finalizes.
"""


def main() -> int:
    parser = argparse.ArgumentParser(description="Canonically select exactly one Hermes story for this cron run.")
    parser.add_argument("--root", default=".", help="Working-set root.")
    parser.add_argument("--recover-stale-lock", action="store_true", help="Recover a verified stale run lock.")
    parser.add_argument("--expected-story", help="Watchdog guard: resume this IN PROGRESS story only.")
    args = parser.parse_args()

    root = Path(args.root).resolve()
    lock_path = acquire_lock(root, args.recover_stale_lock)

    try:
        headers, rows = read_master(root)
        active_sprint = find_active_sprint(rows)
        if active_sprint is None:
            release_lock(lock_path)
            print("COMPLETE: all backlog stories are DONE.")
            return EXIT_NO_MORE_WORK

        sprint_rows = [row for row in rows if row["Sprint"] == active_sprint]
        in_progress = [row for row in sprint_rows if row["Status"] == "IN PROGRESS"]
        if args.expected_story and (len(in_progress) != 1 or in_progress[0]['Story ID'] != args.expected_story):
            release_lock(lock_path)
            print('Watchdog target is no longer the sole IN PROGRESS story; no selection performed.')
            return EXIT_NO_ELIGIBLE_WORK

        if len(in_progress) > 1:
            release_lock(lock_path)
            print(
                f"ERROR: {active_sprint} has multiple IN PROGRESS stories: "
                + ", ".join(row["Story ID"] for row in in_progress),
                file=sys.stderr,
            )
            return EXIT_CONFLICT

        if len(in_progress) == 1:
            selected = in_progress[0]
            by_id = row_map(rows)
            unmet = [dep for dep in parse_dependencies(selected["Dependencies"])
                     if by_id.get(dep, {}).get("Status") != "DONE"]
            if unmet:
                release_lock(lock_path)
                print("ERROR: resumed story has unfinished dependencies: " + ", ".join(unmet), file=sys.stderr)
                return EXIT_CONFLICT
            selection_reason = "resume_existing_in_progress"
        else:
            eligible = eligible_todo_rows(rows, active_sprint)
            if not eligible:
                no_work = {
                    "timestamp": utc_now(),
                    "active_sprint": active_sprint,
                    "reason": "no_eligible_todo_story",
                    "blocked": [r["Story ID"] for r in sprint_rows if r["Status"] == "BLOCKED"],
                    "todo": [r["Story ID"] for r in sprint_rows if r["Status"] == "TODO"],
                }
                json_dump_atomic(root / ".hermes" / "last-no-work.json", no_work)
                release_lock(lock_path)
                print(f"NO ELIGIBLE WORK in {active_sprint}; later sprints will not be selected.")
                return EXIT_NO_ELIGIBLE_WORK

            selected = eligible[0]
            selection_reason = "highest_priority_dependency_ready_todo"

            # Update only canonical row.
            for row in rows:
                if row["Story ID"] == selected["Story ID"]:
                    row["Status"] = "IN PROGRESS"
                    selected = row
                    break
            write_master(root, headers, rows)
            sync_sprint_views(root, headers, rows)

        current = clean_current_run(root)
        by_id = row_map(rows)

        canonical_pkg = canonical_package_path(root, selected)
        if not canonical_pkg.exists():
            raise FileNotFoundError(f"Selected agent package does not exist: {canonical_pkg}")

        selected_copy = current / "selected_agent_package.md"
        shutil.copy2(canonical_pkg, selected_copy)

        direct_dependencies = parse_dependencies(selected["Dependencies"])
        dependency_paths = []
        for dep in direct_dependencies:
            dep_row = by_id.get(dep)
            if dep_row is None:
                raise RuntimeError(f"Dependency Story ID not found in master backlog: {dep}")
            dep_pkg = canonical_package_path(root, dep_row)
            if not dep_pkg.exists():
                raise FileNotFoundError(f"Dependency package missing: {dep_pkg}")
            dep_target = current / "dependencies" / f"{dep}.md"
            shutil.copy2(dep_pkg, dep_target)
            dependency_paths.append(str(dep_target.relative_to(root)))

        sprint_dir = root / "docs" / "sprints" / sprint_dir_name(active_sprint)
        manifest = {
            "prepared_at": utc_now(),
            "story_id": selected["Story ID"],
            "sprint": active_sprint,
            "priority": selected["Priority"],
            "epic": selected["Epic"],
            "title": selected["Title"],
            "selection_reason": selection_reason,
            "canonical_master_backlog": str((root / "docs" / "backlog" / "IndiaWorks_Master_Backlog.csv").relative_to(root)),
            "sprint_backlog": str((sprint_dir / "backlog.csv").relative_to(root)),
            "sprint_context": str((sprint_dir / "SPRINT_CONTEXT.md").relative_to(root)),
            "canonical_agent_package": str(canonical_pkg.relative_to(root)),
            "prepared_agent_package": str(selected_copy.relative_to(root)),
            "direct_dependencies": direct_dependencies,
            "prepared_dependency_packages": dependency_paths,
            "result_path": str((current / "RESULT.json").relative_to(root)),
            "rule_files": ["AGENTS.md", "HERMES_SHARED_RULES.md"],
        }
        json_dump_atomic(current / "RUN_MANIFEST.json", manifest)
        json_dump_atomic(current / "story-row.json", selected)
        (current / "PROMPT.md").write_text(build_prompt(manifest, selected), encoding="utf-8")
        prepare_packet(root, current, manifest, selected)
        json_dump_atomic(current / "RUN_MANIFEST.json", manifest)

        lock_data = {
            "status": "ACTIVE",
            "started_at": utc_now(),
            "pid": os.getpid(),
            "story_id": selected["Story ID"],
            "sprint": active_sprint,
            "manifest": str((current / "RUN_MANIFEST.json").relative_to(root)),
        }
        json_dump_atomic(lock_path, lock_data)

        print(json.dumps({
            "status": "READY",
            "story_id": selected["Story ID"],
            "sprint": active_sprint,
            "priority": selected["Priority"],
            "prompt": str(current / "PROMPT.md"),
            "agent_package": str(selected_copy),
        }, indent=2))
        return 0

    except SystemExit:
        raise
    except Exception:
        # Leave no active lock when preparation itself failed.
        release_lock(lock_path)
        raise


if __name__ == "__main__":
    raise SystemExit(main())
