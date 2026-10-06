from __future__ import annotations

import argparse
import csv
import json
from collections import OrderedDict
from datetime import datetime, timezone
from pathlib import Path

from backlog_lib import (
    ALLOWED_STATUSES,
    PRIORITY_ORDER,
    eligible_todo_rows,
    json_dump_atomic,
    parse_dependencies,
    read_master,
    row_map,
    sprint_dir_name,
    sprint_num,
)


def note_value(raw: str, name: str) -> str:
    if not raw.strip():
        return ""
    try:
        value = json.JSONDecoder().raw_decode(raw.lstrip())[0].get(name, "")
    except json.JSONDecodeError:
        return ""
    return str(value) if value is not None else ""


def rebuild(root: Path) -> None:
    _, rows = read_master(root)
    by_id = row_map(rows)
    by_sprint: OrderedDict[str, list[dict[str, str]]] = OrderedDict()
    for row in sorted(rows, key=lambda item: (sprint_num(item["Sprint"]), int(item["Story ID"].split("-")[-1]))):
        by_sprint.setdefault(row["Sprint"], []).append(row)

    sprints = []
    for sprint, sprint_rows in by_sprint.items():
        blocking = {}
        for row in sprint_rows:
            unmet = [dep for dep in parse_dependencies(row["Dependencies"]) if by_id.get(dep, {}).get("Status") != "DONE"]
            if row["Status"] != "DONE" and unmet:
                blocking[row["Story ID"]] = unmet
        sprint_number = sprint_num(sprint)
        sprints.append(
            {
                "sprint": sprint,
                "sprintFolder": f"docs/sprints/{sprint_dir_name(sprint)}",
                "backlog": f"docs/sprints/{sprint_dir_name(sprint)}/backlog.csv",
                "context": f"docs/sprints/{sprint_dir_name(sprint)}/SPRINT_CONTEXT.md",
                "totalStories": len(sprint_rows),
                "statusCounts": {status: sum(row["Status"] == status for row in sprint_rows) for status in sorted(ALLOWED_STATUSES)},
                "dependencyReadyByPriority": [row["Story ID"] for row in eligible_todo_rows(rows, sprint)],
                "blockingDependencies": blocking,
                "storyIds": [row["Story ID"] for row in sprint_rows],
                "order": sprint_number,
            }
        )

    status_counts = {status: sum(row["Status"] == status for row in rows) for status in sorted(ALLOWED_STATUSES)}
    priority_counts = {priority: sum(row["Priority"] == priority for row in rows) for priority in sorted(PRIORITY_ORDER)}
    layer_counts = OrderedDict()
    for row in rows:
        layer_counts[row["Layer"]] = layer_counts.get(row["Layer"], 0) + 1

    active = next((sprint["sprint"] for sprint in sprints if sprint["statusCounts"].get("DONE", 0) != sprint["totalStories"]), None)
    active_ready = next((sprint["dependencyReadyByPriority"] for sprint in sprints if sprint["sprint"] == active), [])
    index = {
        "schema": "indiaworks.hermes.backlog-index.v1",
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "activeSprint": active,
        "sourceFiles": {
            "masterBacklog": "docs/backlog/IndiaWorks_Master_Backlog.csv",
            "qualityMatrix": "docs/backlog/IndiaWorks_Quality_Matrix.csv",
            "implementationHandbook": "implementation.md",
            "sprintRoot": "docs/sprints",
            "sharedGuides": "docs/shared/agent-guides",
        },
        "totals": {
            "stories": len(rows),
            "sprints": len(sprints),
            "agentPackages": len(list((root / "docs/sprints").glob("Sprint_*/agent-packages/IW8-*.md"))),
            "status": status_counts,
            "priority": priority_counts,
            "layer": layer_counts,
        },
        "nextReadyStoryIds": active_ready,
        "sprints": sprints,
    }
    json_dump_atomic(root / ".hermes/backlog-index.json", index)
    json_dump_atomic(root / "docs/backlog/IndiaWorks_Master_Backlog_Index.json", index)

    quality_path = root / "docs/backlog/IndiaWorks_Quality_Matrix.csv"
    with quality_path.open("w", encoding="utf-8-sig", newline="") as handle:
        fieldnames = [
            "Story ID",
            "Sprint",
            "Epic",
            "Priority",
            "Layer",
            "Owner",
            "Agent Package",
            "Status",
            "Package Quality",
            "Pseudocode Steps",
            "Concrete Test Cases",
            "Acceptance Criteria Count",
            "Quality Issues",
        ]
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        for row in rows:
            writer.writerow(
                {
                    "Story ID": row["Story ID"],
                    "Sprint": row["Sprint"],
                    "Epic": row["Epic"],
                    "Priority": row["Priority"],
                    "Layer": row["Layer"],
                    "Owner": row["Owner"],
                    "Agent Package": row["Agent Package"],
                    "Status": row["Status"],
                    "Package Quality": note_value(row["Notes"], "PackageQuality"),
                    "Pseudocode Steps": note_value(row["Notes"], "PseudocodeSteps"),
                    "Concrete Test Cases": note_value(row["Notes"], "ConcreteTestCases"),
                    "Acceptance Criteria Count": note_value(row["Notes"], "AcceptanceCriteriaCount"),
                    "Quality Issues": "" if note_value(row["Notes"], "PackageQuality") == "PASS" else "Review package quality fields",
                }
            )


def main() -> int:
    parser = argparse.ArgumentParser(description="Rebuild IndiaWorks monitoring index and quality matrix.")
    parser.add_argument("--root", default=".")
    args = parser.parse_args()
    rebuild(Path(args.root).resolve())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
