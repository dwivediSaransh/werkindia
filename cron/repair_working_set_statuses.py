from __future__ import annotations

import argparse
import json
from pathlib import Path

from backlog_lib import ALLOWED_STATUSES, read_master, sprint_num, sync_sprint_views, write_master

OUTCOME_TO_STATUS = {"DONE": "DONE", "BLOCKED": "BLOCKED", "IN_PROGRESS": "IN PROGRESS"}


def latest_finalized_outcome(root: Path, story_id: str) -> str | None:
    runs = root / ".hermes" / "runs"
    if not runs.exists():
        return None
    matches = sorted(runs.glob(f"*-{story_id}"), key=lambda path: path.stat().st_mtime, reverse=True)
    for run in matches:
        finalization = run / "FINALIZATION.json"
        if not finalization.exists():
            continue
        try:
            outcome = str(json.loads(finalization.read_text(encoding="utf-8")).get("outcome", "")).upper().strip()
        except json.JSONDecodeError:
            continue
        status = OUTCOME_TO_STATUS.get(outcome)
        if status in ALLOWED_STATUSES:
            return status
    return None


def main() -> int:
    parser = argparse.ArgumentParser(description="Repair noncanonical IndiaWorks backlog statuses from host-owned archives.")
    parser.add_argument("--root", default=".")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    root = Path(args.root).resolve()

    headers, rows = read_master(root)
    repairs: list[dict[str, str]] = []
    for row in rows:
        status = row["Status"]
        if status in ALLOWED_STATUSES:
            continue
        upper = status.upper().strip()
        replacement = latest_finalized_outcome(root, row["Story ID"]) or (upper if upper in ALLOWED_STATUSES else None)
        if replacement is None:
            raise SystemExit(f"Cannot repair unsupported status for {row['Story ID']}: {status!r}")
        repairs.append({"story_id": row["Story ID"], "from": status, "to": replacement})
        row["Status"] = replacement

    by_sprint: dict[str, list[dict[str, str]]] = {}
    for row in rows:
        by_sprint.setdefault(row["Sprint"], []).append(row)
    active_sprint = min((row["Sprint"] for row in rows if row["Status"] != "DONE"), key=sprint_num, default=None)
    if active_sprint:
        in_progress = [row for row in by_sprint[active_sprint] if row["Status"] == "IN PROGRESS"]
        if len(in_progress) > 1:
            keep = in_progress[0]
            for row in in_progress[1:]:
                repairs.append({
                    "story_id": row["Story ID"],
                    "from": "IN PROGRESS",
                    "to": "TODO",
                    "reason": f"later accidental selection while {keep['Story ID']} remains IN PROGRESS",
                })
                row["Status"] = "TODO"

    if repairs and not args.dry_run:
        write_master(root, headers, rows)
        sync_sprint_views(root, headers, rows)

    print(json.dumps({"repairs": repairs, "dry_run": args.dry_run}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
