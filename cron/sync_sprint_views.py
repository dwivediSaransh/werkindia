from __future__ import annotations
import argparse
from pathlib import Path
from backlog_lib import read_master, sync_sprint_views

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default=".")
    args = parser.parse_args()
    root = Path(args.root).resolve()
    headers, rows = read_master(root)
    sync_sprint_views(root, headers, rows)
    print(f"Synchronized sprint backlog views from canonical master: {len(rows)} stories")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
