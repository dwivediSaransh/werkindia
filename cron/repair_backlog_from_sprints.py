from __future__ import annotations

import argparse
import csv
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

from backlog_lib import MASTER_HEADERS, read_master, sync_sprint_views, write_master


BACKUP_CANDIDATES = (
    Path(".hermes/review-backup/backlog/IndiaWorks_Master_Backlog.csv"),
    Path(".hermes/docs-consolidation-backup/IndiaWorks_Master_Backlog.csv"),
)


def _read_rows(path: Path) -> list[dict[str, str]]:
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        if reader.fieldnames != MASTER_HEADERS:
            raise ValueError(f"{path} has noncanonical headers")
        rows = list(reader)
    if any(None in row or any(value is None for value in row.values()) for row in rows):
        raise ValueError(f"{path} has row width drift")
    return rows


def _latest_done_result(root: Path, story_id: str) -> dict[str, object] | None:
    runs = root / ".hermes" / "runs"
    for result_path in sorted(runs.glob(f"*-{story_id}/RESULT.json"), key=lambda p: p.stat().st_mtime, reverse=True):
        try:
            result = json.loads(result_path.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            continue
        if str(result.get("outcome", "")).upper() == "DONE":
            return result
    return None


def _apply_result(row: dict[str, str], result: dict[str, object]) -> None:
    row["Status"] = "DONE"
    files_changed = result.get("files_changed") or []
    verification = result.get("verification") or []
    known_gaps = result.get("known_gaps") or []
    notes = result.get("notes") or []
    row["Files Changed"] = "\n".join(str(item) for item in files_changed)
    row["What Was Verified"] = "\n".join(str(item) for item in verification)
    row["Known Gaps"] = "\n".join(str(item) for item in known_gaps)
    note_lines = [
        str(result.get("summary") or "").strip(),
        f"Obsidian status: {result.get('obsidian_status', 'UNKNOWN')}",
        f"Graphify status: {result.get('graphify_status', 'UNKNOWN')}",
        *[str(item) for item in notes],
    ]
    row["Notes"] = "\n".join(line for line in note_lines if line)


def main() -> int:
    parser = argparse.ArgumentParser(description="Recover the IndiaWorks master backlog from known-good host-owned data.")
    parser.add_argument("--root", default=".")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    root = Path(args.root).resolve()

    source = next((root / candidate for candidate in BACKUP_CANDIDATES if (root / candidate).exists()), None)
    if source is None:
        raise SystemExit("No canonical backlog backup found")

    rows = _read_rows(source)
    for row in rows:
        package = row["Agent Package"].replace("\\", "/")
        if package.startswith("sprints/"):
            row["Agent Package"] = "docs/" + package
    by_id = {row["Story ID"]: row for row in rows}
    applied: list[str] = []
    for story_id in sorted(by_id):
        result = _latest_done_result(root, story_id)
        if result:
            _apply_result(by_id[story_id], result)
            applied.append(story_id)

    if not args.dry_run:
        target = root / "docs" / "backlog" / "IndiaWorks_Master_Backlog.csv"
        backup = target.with_suffix(target.suffix + f".corrupt.{datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')}.bak")
        shutil.copy2(target, backup)
        write_master(root, MASTER_HEADERS, rows)
        sync_sprint_views(root, MASTER_HEADERS, rows)
        read_master(root)

    print(json.dumps({"source": str(source), "rows": len(rows), "applied_done_results": applied, "dry_run": args.dry_run}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
