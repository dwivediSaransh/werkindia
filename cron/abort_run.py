from __future__ import annotations

import argparse
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

from backlog_lib import json_dump_atomic

def main():
    parser = argparse.ArgumentParser(description="Release a failed cron process without resetting the selected story.")
    parser.add_argument("--root", default=".")
    parser.add_argument("--reason", required=True)
    args = parser.parse_args()
    root = Path(args.root).resolve()

    lock = root / ".hermes" / "run.lock"
    current = root / ".hermes" / "current-run"
    story_id = "UNKNOWN"
    manifest_path = current / "RUN_MANIFEST.json"
    if manifest_path.exists():
        story_id = json.loads(manifest_path.read_text(encoding="utf-8")).get("story_id", "UNKNOWN")

    if current.exists() and any(current.iterdir()):
        stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
        archive = root / ".hermes" / "runs" / f"{stamp}-{story_id}-ABORTED"
        shutil.copytree(current, archive)
        json_dump_atomic(archive / "ABORTED.json", {
            "story_id": story_id,
            "reason": args.reason,
            "aborted_at": datetime.now(timezone.utc).isoformat(),
            "status_rule": "Story remains IN PROGRESS so next cron resumes it.",
        })
        shutil.rmtree(current)
        current.mkdir(parents=True, exist_ok=True)

    lock.unlink(missing_ok=True)
    print(f"Run aborted for {story_id}. Story status was not reset; next cron should resume it.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
