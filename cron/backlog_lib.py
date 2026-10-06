from __future__ import annotations

import csv
import json
import re
from pathlib import Path
from typing import Iterable

# Increase CSV field size limit to handle large Notes fields
csv.field_size_limit(10 * 1024 * 1024)  # 10 MB

MASTER_REL = Path("docs/backlog/IndiaWorks_Master_Backlog.csv")
ALLOWED_STATUSES = {"TODO", "IN PROGRESS", "BLOCKED", "DONE"}
PRIORITY_ORDER = {"P0": 0, "P1": 1, "P2": 2, "P3": 3}
MASTER_HEADERS = ['Story ID', 'Sprint', 'Epic', 'Title', 'Priority', 'Owner', 'Layer', 'Agent Package',
                  'Status', 'Dependencies', 'User Story', 'Scope Summary', 'Acceptance Criteria',
                  'Files Changed', 'What Was Verified', 'Known Gaps', 'Notes']


def sprint_num(label: str) -> int:
    match = re.search(r"(\d+)", label or "")
    return int(match.group(1)) if match else 999


def sprint_dir_name(label: str) -> str:
    return f"Sprint_{sprint_num(label):02d}"


def parse_dependencies(raw: str | None) -> list[str]:
    raw = (raw or "").strip()
    if not raw or raw == "None":
        return []
    return [item.strip() for item in raw.split(",") if item.strip()]


def master_path(root: Path) -> Path:
    return root / MASTER_REL


def read_master(root: Path) -> tuple[list[str], list[dict[str, str]]]:
    path = master_path(root)
    with path.open("r", encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        rows = list(reader)
        if reader.fieldnames != MASTER_HEADERS:
            raise ValueError("Master CSV header differs from canonical schema; no automatic column relocation")
        if any(None in row or any(value is None for value in row.values()) for row in rows):
            raise ValueError("Master CSV row width drift; refusing ledger mutation")
        if len({row['Story ID'] for row in rows}) != len(rows):
            raise ValueError("Duplicate Story IDs; refusing ledger mutation")
        return list(reader.fieldnames or []), rows


def write_master(root: Path, headers: list[str], rows: list[dict[str, str]]) -> None:
    if headers != MASTER_HEADERS:
        raise ValueError("Refusing to write master CSV with noncanonical headers")
    header_set = set(headers)
    for row in rows:
        extra = set(row) - header_set
        missing = header_set - set(row)
        if extra or missing or any(row.get(header) is None for header in headers):
            story_id = row.get("Story ID", "<unknown>")
            raise ValueError(f"Refusing to write malformed master row for {story_id}")
    path = master_path(root)
    tmp = path.with_suffix(path.suffix + ".tmp")
    with tmp.open("w", encoding="utf-8-sig", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=headers)
        writer.writeheader()
        writer.writerows(rows)
    tmp.replace(path)


def row_map(rows: Iterable[dict[str, str]]) -> dict[str, dict[str, str]]:
    return {row["Story ID"]: row for row in rows}


def sync_sprint_views(root: Path, headers: list[str], rows: list[dict[str, str]]) -> None:
    by_sprint: dict[str, list[dict[str, str]]] = {}
    for row in rows:
        by_sprint.setdefault(row["Sprint"], []).append(row)

    for sprint, sprint_rows in by_sprint.items():
        sprint_dir = root / "docs" / "sprints" / sprint_dir_name(sprint)
        sprint_dir.mkdir(parents=True, exist_ok=True)
        local_rows: list[dict[str, str]] = []
        for row in sprint_rows:
            local = dict(row)
            local["Agent Package"] = f"agent-packages/{row['Story ID']}.md"
            local_rows.append(local)

        path = sprint_dir / "backlog.csv"
        tmp = path.with_suffix(path.suffix + ".tmp")
        with tmp.open("w", encoding="utf-8-sig", newline="") as f:
            writer = csv.DictWriter(f, fieldnames=headers)
            writer.writeheader()
            writer.writerows(local_rows)
        tmp.replace(path)

    runtime = root / ".hermes"
    runtime.mkdir(exist_ok=True)
    by_id = row_map(rows)
    json_dump_atomic(runtime / "backlog-index.json", {
        "active_sprint": find_active_sprint(rows),
        "sprints": [{"sprint": sprint,
                     "status_counts": {status: sum(r['Status'] == status for r in sprint_rows) for status in sorted(ALLOWED_STATUSES)},
                     "dependency_ready_by_priority": [r['Story ID'] for r in eligible_todo_rows(rows, sprint)],
                     "blocking_dependencies": {r['Story ID']: [dep for dep in parse_dependencies(r['Dependencies'])
                                                if by_id.get(dep, {}).get('Status') != 'DONE']
                                                for r in sprint_rows if r['Status'] != 'DONE'}}
                    for sprint, sprint_rows in sorted(by_sprint.items(), key=lambda item: sprint_num(item[0]))],
    })


def find_active_sprint(rows: list[dict[str, str]]) -> str | None:
    unfinished = [row["Sprint"] for row in rows if row["Status"] != "DONE"]
    if not unfinished:
        return None
    return min(unfinished, key=sprint_num)


def eligible_todo_rows(rows: list[dict[str, str]], sprint: str) -> list[dict[str, str]]:
    by_id = row_map(rows)
    result = []
    for index, row in enumerate(rows):
        if row["Sprint"] != sprint or row["Status"] != "TODO":
            continue
        dependencies = parse_dependencies(row["Dependencies"])
        if all(by_id.get(dep, {}).get("Status") == "DONE" for dep in dependencies):
            copy = dict(row)
            copy["_canonical_index"] = str(index)
            result.append(copy)

    result.sort(
        key=lambda r: (
            PRIORITY_ORDER.get(r["Priority"], 99),
            int(r["_canonical_index"]),
            r["Story ID"],
        )
    )
    return result


def canonical_package_path(root: Path, row: dict[str, str]) -> Path:
    return root / row["Agent Package"]


def json_dump_atomic(path: Path, value: object) -> None:
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(value, indent=2, ensure_ascii=False), encoding="utf-8")
    tmp.replace(path)
