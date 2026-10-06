"""Capture bounded proof and report one prepared story; the host owns the ledger."""
from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from uuid import uuid4

from backlog_lib import json_dump_atomic
from execution_packet import inventory


def canonical_command(command: list[str], cwd: Path) -> list[str]:
    result = list(command)
    if not result:
        return result
    executable = result[0]
    if '/' in executable or '\\' in executable:
        path = Path(executable)
        result[0] = str(path.resolve() if path.is_absolute() else (cwd / path).resolve())
    elif sys.platform == "win32":
        # subprocess.Popen does not apply PATHEXT the same way cmd.exe does.
        # Resolve npm/npx/dotnet/etc. while keeping checks shell-free.
        resolved = shutil.which(executable)
        if resolved:
            result[0] = resolved
    return result


def complete(root: Path) -> None:
    current = root / ".hermes/current-run"
    manifest = json.loads((current / "RUN_MANIFEST.json").read_text(encoding="utf-8"))
    report = json.loads((current / "REPORT.json").read_text(encoding="utf-8"))
    if report.get("story_id") != manifest["story_id"]:
        raise ValueError("Report does not match selected story")
    source = Path(manifest["source_root"])
    before = json.loads((current / "SOURCE_BEFORE.json").read_text(encoding="utf-8"))
    after = inventory(source)
    changed = [str(source / p) for p in sorted(before.keys() | after.keys()) if before.get(p) != after.get(p)]
    previous = current / 'PREVIOUS_RESULT.json'
    if previous.exists():
        prior = json.loads(previous.read_text(encoding='utf-8'))
        changed = sorted(set(changed) | set(prior.get('files_changed', [])))
    proofs = []
    for path in sorted((current / "verification").glob("*.json")):
        proof = json.loads(path.read_text(encoding="utf-8"))
        output = current / "verification" / proof["output_file"]
        if proof.get("story_id") != manifest["story_id"] or not output.is_file():
            raise ValueError("Invalid proof record")
        proofs.append(proof)
    proofs.sort(key=lambda p: p['recorded_at'])
    latest = {(p['cwd'], tuple(canonical_command(p['command'], Path(p['cwd'])))): p for p in proofs}
    verified = {ac for p in latest.values() if p["exit_code"] == 0 for ac in p["acceptance_ids"]}
    explanations = report.get("ac_evidence", {})
    missing = [ac for ac in manifest["acceptance_ids"] if ac not in verified or not explanations.get(ac)]
    outcome = report["outcome"]
    gaps = list(report.get("known_gaps", []))
    no_source_changes_required = outcome == "DONE" and not changed and not missing
    if outcome == "DONE" and missing:
        outcome = "IN_PROGRESS"
        gaps.append("DONE rejected: missing AC proof/explanation. " + ", ".join(missing))
    verification = [f"{p['proof_id']}: cwd={p['cwd']}; argv={p['command']}; exit={p['exit_code']}; ACs={p['acceptance_ids']}; output=verification/{p['output_file']}" for p in proofs]
    verification.extend(f"{ac}: {text}" for ac, text in explanations.items())
    result = {"story_id": manifest["story_id"], "outcome": outcome, "summary": report["summary"],
              "files_changed": changed, "verification": verification, "known_gaps": gaps,
              "blocker": report.get("blocker"), "notes": ["Host-generated inventory, proof records and outbox handoff."],
              "obsidian_status": "OUTBOX", "graphify_status": "OUTBOX",
              "no_source_changes_required": no_source_changes_required}
    if no_source_changes_required:
        result["notes"].append(
            "No source files changed in this run because the selected story implementation already existed; "
            "DONE is based on fresh acceptance-criteria verification."
        )
    outbox = root / "docs/outbox"
    outbox.mkdir(parents=True, exist_ok=True)
    text = f"# {manifest['story_id']}: {manifest['title']}\n\nSprint: {manifest['sprint']}\nOutcome: {outcome}\n\n{report['summary']}\n\n"
    text += "```json\n" + json.dumps(result, indent=2) + "\n```\n\nNo MCP sync claimed. Contract/rollout details, if applicable, must be recorded in the summary or AC explanations.\n"
    (outbox / f"{manifest['story_id']}-update.md").write_text(text, encoding="utf-8")
    json_dump_atomic(outbox / f"{manifest['story_id']}-graphify.json", result)
    json_dump_atomic(current / "RESULT.json", result)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", default=".")
    sub = parser.add_subparsers(dest="action", required=True)
    check = sub.add_parser("check", help="Run argv without a shell and save output/exit/AC metadata")
    check.add_argument("--acs", required=True, help="Comma-separated AC IDs")
    check.add_argument("--cwd", required=True)
    check.add_argument("--timeout", type=int, default=180)
    check.add_argument("command", nargs=argparse.REMAINDER)
    report = sub.add_parser("report", help="Record outcome; never edit CSVs")
    report.add_argument("--outcome", required=True, choices=["DONE", "BLOCKED", "IN_PROGRESS"])
    report.add_argument("--summary", required=True)
    report.add_argument("--blocker")
    report.add_argument("--gap", action="append", default=[])
    report.add_argument("--ac", action="append", default=[], help="AC1=Explanation of decisive assertion/output; repeat per AC")
    sub.add_parser("complete", help="Host-only result and outbox generation")
    args = parser.parse_args()
    root = Path(args.root).resolve()
    current = root / ".hermes/current-run"
    manifest = json.loads((current / "RUN_MANIFEST.json").read_text(encoding="utf-8"))
    if not (root / ".hermes/run.lock").exists():
        parser.error("No active prepared run")
    if args.action == "complete":
        complete(root)
        return 0
    if args.action == "report":
        if args.outcome == "BLOCKED" and not args.blocker:
            parser.error("BLOCKED requires --blocker")
        acs = {}
        for item in args.ac:
            ac, sep, explanation = item.partition("=")
            if not sep or ac not in manifest["acceptance_ids"] or not explanation.strip():
                parser.error("--ac requires a selected AC ID and nonempty explanation")
            acs[ac] = explanation
        json_dump_atomic(current / "REPORT.json", {"story_id": manifest["story_id"], "outcome": args.outcome,
                         "summary": args.summary, "blocker": args.blocker, "known_gaps": args.gap, "ac_evidence": acs})
        return 0
    cwd = Path(args.cwd).resolve()
    if not cwd.is_relative_to(Path(manifest["source_root"]).resolve()):
        parser.error("Verification cwd must be inside the configured source root")
    acs = args.acs.split(",")
    if not set(acs).issubset(manifest["acceptance_ids"]):
        parser.error("Unknown AC ID")
    command = args.command[1:] if args.command[:1] == ["--"] else args.command
    if not command or not 1 <= args.timeout <= 1800:
        parser.error("Need command argv and timeout in 1..1800 seconds")
    # Windows CreateProcess resolves executable paths before applying child cwd.
    # Resolve service-relative venv executables here, retaining shell-free argv.
    command = canonical_command(command, cwd)
    proof_id = uuid4().hex[:12]
    destination = current / "verification"
    destination.mkdir(exist_ok=True)
    output_file = proof_id + ".log"
    with (destination / output_file).open("w", encoding="utf-8") as output:
        try:
            process = subprocess.Popen(command, cwd=cwd, stdout=output, stderr=subprocess.STDOUT,
                                       creationflags=subprocess.CREATE_NO_WINDOW if sys.platform == "win32" else 0)
            try:
                code = process.wait(timeout=args.timeout)
            except subprocess.TimeoutExpired:
                if sys.platform == "win32":
                    subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"], capture_output=True, timeout=30)
                else:
                    process.kill()
                process.wait(timeout=30)
                code = 124
                output.write("\nCHECK DEADLINE EXCEEDED\n")
        except OSError as error:
            output.write(str(error))
            code = 127
    json_dump_atomic(destination / (proof_id + ".json"), {"proof_id": proof_id, "story_id": manifest["story_id"],
                     "command": command, "cwd": str(cwd), "exit_code": code, "acceptance_ids": acs,
                     "output_file": output_file, "recorded_at": datetime.now(timezone.utc).isoformat()})
    print(json.dumps({"proof_id": proof_id, "exit_code": code, "output": str(destination / output_file)}))
    return code


if __name__ == "__main__":
    raise SystemExit(main())
