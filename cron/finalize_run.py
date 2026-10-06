from __future__ import annotations

import argparse
import json
import shutil
import sys
from datetime import datetime, timezone
from pathlib import Path

from backlog_lib import json_dump_atomic, read_master, sync_sprint_views, write_master
from rebuild_monitoring_artifacts import rebuild

ALLOWED_OUTCOMES = {"DONE", "BLOCKED", "IN_PROGRESS"}


def utc_stamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def normalize_list(value) -> list[str]:
    if value is None:
        return []
    if isinstance(value, list):
        return [str(x).strip() for x in value if str(x).strip()]
    text = str(value).strip()
    return [text] if text else []


def main() -> int:
    parser = argparse.ArgumentParser(description="Finalize exactly one prepared Hermes story run.")
    parser.add_argument("--root", default=".")
    args = parser.parse_args()
    root = Path(args.root).resolve()

    lock_path = root / ".hermes" / "run.lock"
    current = root / ".hermes" / "current-run"
    manifest_path = current / "RUN_MANIFEST.json"
    result_path = current / "RESULT.json"

    if not lock_path.exists():
        print("ERROR: no run lock exists.", file=sys.stderr)
        return 30
    if not manifest_path.exists():
        print("ERROR: current RUN_MANIFEST.json is missing.", file=sys.stderr)
        return 31
    if not result_path.exists():
        print("ERROR: RESULT.json is missing. Do not finalize an unreported run.", file=sys.stderr)
        return 32

    manifest = load_json(manifest_path)
    result = load_json(result_path)

    story_id = manifest["story_id"]
    if result.get("story_id") != story_id:
        print(f"ERROR: result story_id {result.get('story_id')} does not match selected {story_id}.", file=sys.stderr)
        return 33

    outcome = str(result.get("outcome", "")).upper().strip()
    if outcome not in ALLOWED_OUTCOMES:
        print(f"ERROR: invalid outcome {outcome!r}.", file=sys.stderr)
        return 34

    files_changed = normalize_list(result.get("files_changed"))
    verification = normalize_list(result.get("verification"))
    known_gaps = normalize_list(result.get("known_gaps"))
    notes = normalize_list(result.get("notes"))
    blocker = str(result.get("blocker") or "").strip()

    no_source_changes_required = bool(result.get("no_source_changes_required"))

    if outcome == "DONE":
        if not verification:
            print("ERROR: DONE requires non-empty verification evidence.", file=sys.stderr)
            return 35
        if not files_changed and not no_source_changes_required:
            print(
                "ERROR: DONE requires non-empty files_changed unless no_source_changes_required is true.",
                file=sys.stderr,
            )
            return 36
    if outcome == "BLOCKED" and not blocker:
        print("ERROR: BLOCKED requires a blocker.", file=sys.stderr)
        return 37

    headers, rows = read_master(root)
    target = None
    for row in rows:
        if row["Story ID"] == story_id:
            target = row
            break
    if target is None:
        print(f"ERROR: Story ID {story_id} not found in master backlog.", file=sys.stderr)
        return 38

    # A previous agent or manual repair may already have applied a valid DONE
    # result while the run lock stayed behind. Treat that as idempotent cleanup:
    # archive the evidence and release the lock without duplicating ledger notes.
    idempotent_done_cleanup = outcome == "DONE" and target["Status"] == "DONE"

    # Protect against finalizing a different story state unexpectedly.
    if target["Status"] != "IN PROGRESS" and not idempotent_done_cleanup:
        print(f"ERROR: selected story status is {target['Status']}, expected IN PROGRESS.", file=sys.stderr)
        return 39

    if not idempotent_done_cleanup:
        target["Status"] = "IN PROGRESS" if outcome == "IN_PROGRESS" else outcome
        if files_changed:
            target["Files Changed"] = "\n".join(files_changed)
        if verification:
            target["What Was Verified"] = "\n".join(verification)
        target["Known Gaps"] = "\n".join(known_gaps)

        audit_notes = []
        summary = str(result.get("summary") or "").strip()
        if summary:
            audit_notes.append(f"Run summary: {summary}")
        if blocker:
            audit_notes.append(f"Blocker: {blocker}")
        audit_notes.append(f"Obsidian status: {result.get('obsidian_status', 'NOT_REPORTED')}")
        audit_notes.append(f"Graphify status: {result.get('graphify_status', 'NOT_REPORTED')}")
        audit_notes.extend(notes)
        if audit_notes:
            existing = (target.get("Notes") or "").rstrip()
            appended = "\n".join(audit_notes)
            target["Notes"] = (existing + "\n" + appended).strip()

        write_master(root, headers, rows)
        sync_sprint_views(root, headers, rows)
        rebuild(root)

    archive_dir = root / ".hermes" / "runs" / f"{utc_stamp()}-{story_id}"
    archive_dir.parent.mkdir(parents=True, exist_ok=True)
    shutil.copytree(current, archive_dir)
    json_dump_atomic(archive_dir / "FINALIZATION.json", {
        "story_id": story_id,
        "outcome": outcome,
        "finalized_at": datetime.now(timezone.utc).isoformat(),
        "idempotent_cleanup": idempotent_done_cleanup,
    })

    shutil.rmtree(current)
    current.mkdir(parents=True, exist_ok=True)
    lock_path.unlink(missing_ok=True)

    print(json.dumps({
        "status": "FINALIZED",
        "story_id": story_id,
        "outcome": outcome,
        "archive": str(archive_dir),
        "instruction": "STOP. Do not prepare another story in this cron run.",
    }, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
