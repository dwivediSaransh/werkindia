"""Run the IndiaWorks watchdog with the canonical local Hermes profile."""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path


ROOT = Path(r"C:\Projects\IndiaWorks")
HERMES_HOME = Path(r"C:\Users\DELL\AppData\Local\hermes")


def main() -> int:
    command = [
        sys.executable,
        str(ROOT / "cron" / "cron_continue_watchdog.py"),
        "--root",
        str(ROOT),
        "--hermes-home",
        str(HERMES_HOME),
        "--hermes-python",
        str(HERMES_HOME / "hermes-agent" / "venv" / "Scripts" / "python.exe"),
        "--hermes-main",
        str(HERMES_HOME / "hermes-agent" / "hermes_cli" / "main.py"),
        "--job-id",
        "2f464807b97a",
        "--watchdog-id",
        "06ad1950449b",
    ]
    return subprocess.call(command, cwd=ROOT)


if __name__ == "__main__":
    raise SystemExit(main())
