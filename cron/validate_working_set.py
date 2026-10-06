from __future__ import annotations

import argparse
import csv
import json
import sys
from pathlib import Path

from backlog_lib import (
    ALLOWED_STATUSES,
    canonical_package_path,
    parse_dependencies,
    read_master,
    sprint_dir_name,
)


FORBIDDEN_SOURCE_ROOTS = (
    "src/web/src/web",
    "src/web/src/web.backup",
)


def forbidden_nested_sources(root: Path) -> list[str]:
    offenders: list[str] = []
    for value in FORBIDDEN_SOURCE_ROOTS:
        path = root / Path(value)
        if not path.exists():
            continue
        files = [p for p in path.rglob("*") if p.is_file()]
        if files:
            offenders.extend(str(p.relative_to(root)).replace("\\", "/") for p in files[:10])
            if len(files) > 10:
                offenders.append(f"{value}/... ({len(files) - 10} more)")
    return offenders


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate the Hermes working set and sprint split.")
    parser.add_argument("--root", default=".")
    args = parser.parse_args()
    root = Path(args.root).resolve()

    headers, rows = read_master(root)
    by_id = {r["Story ID"]: r for r in rows}
    errors: list[str] = []
    warnings: list[str] = []

    # Basic uniqueness/status/dependency checks.
    if len(by_id) != len(rows):
        errors.append("Duplicate Story IDs exist in master backlog.")

    for row in rows:
        sid = row["Story ID"]
        if row["Status"] not in ALLOWED_STATUSES:
            errors.append(f"{sid}: unsupported status {row['Status']!r}")
        for dep in parse_dependencies(row["Dependencies"]):
            if dep not in by_id:
                errors.append(f"{sid}: dependency {dep} is missing.")
        pkg = canonical_package_path(root, row)
        if not pkg.exists():
            errors.append(f"{sid}: agent package missing: {pkg}")
        else:
            text = pkg.read_text(encoding="utf-8")
            for heading in (
                "## 4. Dependency prerequisites",
                "## 9. Detailed implementation sequence",
                "## 14. Required test matrix",
            ):
                if heading not in text:
                    errors.append(f"{sid}: package lacks required heading {heading!r}.")

    if not (root / "implementation.md").exists():
        errors.append("implementation.md is missing.")
    if not (root / "AGENTS.md").exists():
        errors.append("AGENTS.md is missing.")
    duplicates = forbidden_nested_sources(root)
    if duplicates:
        errors.append(
            "Nested duplicate Angular source tree exists under src/web/src/web or "
            "src/web/src/web.backup; move any missing real work into src/web and "
            "remove the duplicate files before Hermes runs. Examples: "
            + ", ".join(duplicates)
        )

    # Dependency cycle detection.
    state: dict[str, int] = {}
    stack: list[str] = []

    def dfs(node: str) -> bool:
        state[node] = 1
        stack.append(node)
        for dep in parse_dependencies(by_id[node]["Dependencies"]):
            if state.get(dep, 0) == 0:
                if dfs(dep):
                    return True
            elif state.get(dep) == 1:
                errors.append("Dependency cycle: " + " -> ".join(stack[stack.index(dep):] + [dep]))
                return True
        stack.pop()
        state[node] = 2
        return False

    for sid in by_id:
        if state.get(sid, 0) == 0 and dfs(sid):
            break

    # Sprint view equivalence checks.
    sprint_labels = sorted({r["Sprint"] for r in rows}, key=lambda s: int(s.split()[-1]))
    for sprint in sprint_labels:
        sprint_path = root / "docs" / "sprints" / sprint_dir_name(sprint) / "backlog.csv"
        if not sprint_path.exists():
            errors.append(f"{sprint}: sprint backlog missing.")
            continue
        with sprint_path.open("r", encoding="utf-8-sig", newline="") as f:
            sprint_rows = list(csv.DictReader(f))
        expected = [r for r in rows if r["Sprint"] == sprint]
        if [r["Story ID"] for r in sprint_rows] != [r["Story ID"] for r in expected]:
            errors.append(f"{sprint}: sprint Story ID sequence differs from master.")
        for local, master in zip(sprint_rows, expected):
            for field in headers:
                if field == "Agent Package":
                    continue
                if local.get(field) != master.get(field):
                    errors.append(f"{sprint}/{master['Story ID']}: field {field!r} differs from master.")

        in_progress = [r["Story ID"] for r in expected if r["Status"] == "IN PROGRESS"]
        if len(in_progress) > 1:
            errors.append(f"{sprint}: more than one IN PROGRESS story: {in_progress}")

    report = {
        "stories": len(rows),
        "sprints": len(sprint_labels),
        "errors": errors,
        "warnings": warnings,
        "valid": not errors,
    }
    print(json.dumps(report, indent=2))
    return 0 if not errors else 2

if __name__ == "__main__":
    raise SystemExit(main())
