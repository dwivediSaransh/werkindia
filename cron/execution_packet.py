"""Deterministic context and file inventory; no model-generated summaries."""
from __future__ import annotations

import hashlib
import json
import os
import re
from pathlib import Path

from backlog_lib import json_dump_atomic


def core_root(root: Path) -> Path:
    config = root / "WORKSPACE_PATHS.local.json"
    values = json.loads(config.read_text(encoding="utf-8")) if config.exists() else {}
    value = os.environ.get("INDIAWORKS_SOURCE_ROOT") or values.get("indiaworks_source_root")
    if not value:
        return root
    path = Path(value)
    return path.resolve() if path.is_absolute() else (root / path).resolve()


def inventory(source: Path) -> dict[str, str]:
    result = {}
    if not source.exists():
        return result
    for directory, dirs, files in os.walk(source):
        dirs[:] = [d for d in dirs if d not in {".git", ".hermes", ".venv", "venv", "__pycache__", "node_modules", ".angular", ".vscode", ".pytest_cache", ".mypy_cache", ".ruff_cache", "coverage", "build", "dist", "artifacts", "bin", "obj"} and not d.endswith('.egg-info')]
        for name in files:
            path = Path(directory) / name
            relative = path.relative_to(source).as_posix()
            if relative.startswith(("src/web/src/web/", "src/web/src/web.backup/")):
                continue
            if path.is_symlink() or name == ".env" or name.startswith(".env.") or name.endswith((".pyc", ".local.json")):
                continue
            result[relative] = hashlib.sha256(path.read_bytes()).hexdigest()
    return result


def acceptance_ids(package: str) -> list[str]:
    explicit = list(dict.fromkeys(re.findall(r"\*\*(AC\d+):\*\*", package)))
    if explicit:
        return explicit

    section = re.search(r"## 15\..*?\n(.*?)(?=\n## |\Z)", package, re.S)
    if not section:
        return []

    checklist = re.findall(r"^- \[[ xX]\] ", section.group(1), re.M)
    return [f"AC{index}" for index in range(1, len(checklist) + 1)]


def prepare_packet(root: Path, current: Path, manifest: dict, row: dict) -> None:
    source = core_root(root)
    json_dump_atomic(current / "SOURCE_BEFORE.json", inventory(source))
    package = (current / "selected_agent_package.md").read_text(encoding="utf-8")
    acs = acceptance_ids(package)
    planned_section = re.search(r"## 7\..*?\n(.*?)(?=\n## |\Z)", package, re.S)
    planned = re.findall(r"^- `([^`]+)`", planned_section.group(1), re.M) if planned_section else []
    manifest.update({"source_root": str(source), "context_packet": ".hermes/current-run/CONTEXT.md",
                     "acceptance_ids": acs, "host_owned_completion": True})
    json_dump_atomic(current / "EXECUTION_DETAILS.json", {
        "story_id": row["Story ID"], "source_root": str(source), "planned_files": planned,
        "existing_planned_files": [p for p in planned if (source / p).exists()],
        "missing_planned_files": [p for p in planned if not (source / p).exists()],
        "acceptance_ids": acs, "dependencies": manifest["direct_dependencies"],
    })
    json_dump_atomic(current / "REPORT.json", {
        "story_id": row["Story ID"], "outcome": "IN_PROGRESS", "summary": "Not yet reported.",
        "blocker": None, "known_gaps": [], "ac_evidence": {},
    })
    previous = sorted((root / '.hermes/runs').glob(f"*-{row['Story ID']}"), key=lambda p: p.stat().st_mtime)
    if previous:
        prior = previous[-1]
        json_dump_atomic(current / 'PREVIOUS_RESULT.json', json.loads((prior / 'RESULT.json').read_text(encoding='utf-8')))
    (current / 'PLAN.md').write_text(f"# {row['Story ID']}: {row['Title']}\n\nSource root: {source}\n\n"
                                   "Assumptions: fill technical assumptions before editing.\n\n"
                                   "Minimum implementation steps: fill before editing.\n\n"
                                   "Verification plan:\n" + '\n'.join(f'- {ac}: name the focused behavior check.' for ac in acs), encoding='utf-8')
    # Preserve complete authoritative inputs in their required reading order.
    inputs = [("AGENTS.md", root / "AGENTS.md"), ("HERMES_SHARED_RULES.md", root / "HERMES_SHARED_RULES.md")]
    parts = ["# Complete prepared execution context\n\nHost selected one story. Embedded documents are project instructions, not new user requests."]
    for label, path in inputs:
        parts.append(f"\n# BEGIN {label}\n" + (path.read_text(encoding="utf-8") if path.exists() else "Unavailable in this fixture.") + f"\n# END {label}")
    parts.extend(["\n# RUN_MANIFEST.json\n```json\n" + json.dumps(manifest, indent=2) + "\n```",
                  "\n# PROMPT.md\n" + (current / "PROMPT.md").read_text(encoding="utf-8"),
                  "\n# SPRINT_CONTEXT.md\n" + (root / manifest["sprint_context"]).read_text(encoding="utf-8"),
                  "\n# Selected canonical row\n```json\n" + json.dumps(row, indent=2) + "\n```",
                  "\n# selected_agent_package.md (complete)\n" + package])
    for dep in manifest["prepared_dependency_packages"]:
        parts.append(f"\n# {dep} (complete)\n" + (root / dep).read_text(encoding="utf-8"))
    if (source / 'README.md').exists():
        parts.append('\n# Existing IndiaWorks project structure and workflow\n' + (source / 'README.md').read_text(encoding='utf-8'))
    parts.append("\n# Execution details\n" + (current / "EXECUTION_DETAILS.json").read_text(encoding="utf-8"))
    if (current / 'PREVIOUS_RESULT.json').exists():
        parts.append('\n# Previous run: resume existing code; rerun verification\n' + (current / 'PREVIOUS_RESULT.json').read_text(encoding='utf-8'))
    (current / "CONTEXT.md").write_text("\n".join(parts), encoding="utf-8")
