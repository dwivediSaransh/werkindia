"""Execute one canonical story using the isolated IndiaWorks Hermes profile."""
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from story_runtime import complete
from execution_packet import core_root
from backlog_lib import json_dump_atomic
from process_identity import own_identity
from execution_progress import snapshot
from executor_limits import (
    MAX_EXECUTOR_ITERATIONS,
    MAX_EXECUTOR_OUTPUT_TOKENS,
    NO_DELIVERY_STALL_SECONDS,
)


def progress_signal(progress: dict) -> tuple:
    """Stable delivery signal; excludes tool chatter that can repeat unchanged facts."""
    checks = tuple((c.get('id'), c.get('exit')) for c in progress.get('checks', []))
    return (tuple(progress.get('changed_files', [])), checks)


def progress_message(story_id: str, progress: dict, previous: dict | None) -> str:
    changed = progress.get('changed_files', [])
    checks = progress.get('checks', [])
    passed = sum(1 for c in checks if c.get('exit') in (0, '0'))
    failed = len(checks) - passed
    previous_checks = {c.get('id') for c in (previous or {}).get('checks', [])}
    new_checks = [c for c in checks if c.get('id') not in previous_checks]
    new_suffix = f"; new checks={new_checks}" if new_checks else ""
    if not changed and not checks:
        return (
            f"IndiaWorks {story_id}: no selected-story source files or proof checks yet; "
            "waiting for first concrete delivery signal."
        )
    return (
        f"IndiaWorks {story_id}: delivery progress changed; "
        f"source files={len(changed)}, checks={len(checks)} "
        f"(passed={passed}, failed={failed}){new_suffix}."
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", required=True)
    parser.add_argument("--hermes-python", required=True)
    parser.add_argument("--hermes-main", required=True)
    parser.add_argument("--timeout", type=int, default=2700)
    parser.add_argument("--expected-story", help="Watchdog: resume only this selected Story ID.")
    parser.add_argument('--notification-home', type=Path, help='Hermes home for direct output chat; no inference.')
    args = parser.parse_args()
    root = Path(args.root).resolve()
    def notify(message: str) -> None:
        print(message, flush=True)
        if args.notification_home:
            try:
                delivered = subprocess.run([args.hermes_python, str(root / 'cron/hermes_output.py'),
                    '--home', str(args.notification_home), '--source', str(Path(args.hermes_main).parent.parent),
                    '--message', message], capture_output=True, text=True, timeout=30)
                if delivered.returncode:
                    raise RuntimeError('Output store failed')
            except (OSError, RuntimeError, subprocess.TimeoutExpired):
                print('Direct output chat delivery failed; local executor output remains available.', file=sys.stderr)
    if (root / ".git").exists():
        checkout = subprocess.run(["git", "rev-parse", "--show-toplevel"], cwd=root,
                                  capture_output=True, text=True, timeout=30, check=True)
        source = core_root(root)
        if Path(checkout.stdout.strip()).resolve() != root or not source.is_relative_to(root):
            raise ValueError("IndiaWorks cron requires the existing root checkout and source folder inside it")
        nested = subprocess.run(["git", "rev-parse", "--show-toplevel"], cwd=source,
                                capture_output=True, text=True, timeout=30, check=True)
        if Path(nested.stdout.strip()).resolve() != root:
            raise ValueError("Nested source repository is not allowed")
    # The selector owns the lock and backlog. Never select a second story here.
    for script in ("validate_working_set.py", "prepare_run.py"):
        script_command = [sys.executable, str(root / "cron" / script), "--root", str(root)]
        if script == 'prepare_run.py' and args.expected_story:
            script_command += ['--expected-story', args.expected_story]
        checked = subprocess.run(script_command,
                                 cwd=root, capture_output=True, text=True, timeout=120)
        if checked.returncode:
            if checked.returncode in (20, 21, 30) and script == "prepare_run.py":
                print(checked.stdout.strip() or "IndiaWorks: no new executor started; current run, sprint gate or completion prevents selection.", flush=True)
                return 0
            print(checked.stdout + checked.stderr, file=sys.stderr)
            return checked.returncode

    current = root / ".hermes" / "current-run"
    manifest = json.loads((current / "RUN_MANIFEST.json").read_text(encoding="utf-8"))
    story_id = manifest["story_id"]
    logs = root / ".hermes" / "executor-logs"
    logs.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    prompt = (root / "docs" / "automation" / "INDIAWORKS_PREPARED_EXECUTOR_PROMPT.md").read_text(encoding="utf-8")
    prompt += f"\nThe host has already selected {story_id}. Do not call prepare_run.py."
    env = os.environ.copy()
    env['PYTHONUNBUFFERED'] = '1'
    env['HERMES_SESSION_SOURCE'] = f'indiaworks:{story_id}'
    # Do not inherit another profile's inference overrides or credentials.
    for name in tuple(env):
        if name.startswith(("HERMES_INFERENCE_", "OPENAI_", "ANTHROPIC_", "OPENROUTER_", "KILOCODE_")):
            env.pop(name, None)
    # Complete project rules are already in CONTEXT.md; skip duplicate automatic
    # context/memory/skill preloading, not the requirement to read those rules.
    command = [args.hermes_python, args.hermes_main, "-p", "indiaworks", "--in", str(root), "--ignore-rules",
               '--toolsets', 'terminal,file',
               "--usage-file", str(logs / f"{stamp}-{story_id}-usage.json"), "-z", prompt]
    if Path(args.hermes_main).parent.name == 'hermes_cli':
        command.insert(1, str(root / 'cron/hermes_executor_entry.py'))
    notify(
        f"IndiaWorks {story_id}: starting coding executor "
        "using the indiaworks profile model/fallback chain "
        f"({MAX_EXECUTOR_ITERATIONS} turns, {MAX_EXECUTOR_OUTPUT_TOKENS} output tokens). "
        f"Progress reports show source changes and captured checks. "
        f"Log: {logs / f'{stamp}-{story_id}.log'}"
    )
    flags = subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0
    with (logs / f"{stamp}-{story_id}.log").open("w", encoding="utf-8") as output:
        process = subprocess.Popen(command, cwd=root, env=env, stdout=output,
                                   stderr=subprocess.STDOUT, creationflags=flags)
        state = {'story_id': story_id, 'host': own_identity(os.getpid()) if os.name == 'nt' else {'pid': os.getpid()},
                 'executor': own_identity(process.pid) if os.name == 'nt' else {'pid': process.pid},
                 'started_at': time.time(), 'deadline': time.time() + args.timeout,
                 'status': 'RUNNING', 'tree_terminated': False,
                 'session_source': f'indiaworks:{story_id}',
                 'log_file': str(logs / f'{stamp}-{story_id}.log'),
                 'usage_file': str(logs / f'{stamp}-{story_id}-usage.json')}
        json_dump_atomic(current / 'EXECUTOR_STATE.json', state)
        last_progress = None
        last_reported_signal = None
        last_delivery_change = time.time()
        try:
            while True:
                remaining = state['deadline'] - time.time()
                if remaining <= 0:
                    raise subprocess.TimeoutExpired(command, args.timeout)
                try:
                    code = process.wait(timeout=min(60, remaining))
                    break
                except subprocess.TimeoutExpired:
                    if args.notification_home:
                        db_path = args.notification_home / 'profiles/indiaworks/state.db'
                        progress = snapshot(core_root(root), current, db_path, state['session_source'], state['started_at'])
                        if last_progress is not None and (progress['source_digest'], progress['checks']) != (last_progress['source_digest'], last_progress['checks']):
                            last_delivery_change = time.time()
                        signal = progress_signal(progress)
                        if signal != last_reported_signal:
                            notify(progress_message(story_id, progress, last_progress))
                            last_reported_signal = signal
                        last_progress = progress
                        no_delivery = not progress['changed_files'] and not progress['checks']
                        stalled_reads = progress['max_repeated_reads'] >= 8
                        if time.time() - last_delivery_change >= NO_DELIVERY_STALL_SECONDS and (stalled_reads or no_delivery):
                            report_path = current / 'REPORT.json'
                            report = json.loads(report_path.read_text(encoding='utf-8'))
                            report['outcome'] = 'IN_PROGRESS'
                            report.setdefault('known_gaps', []).append(
                                'Stopped stalled executor: no selected-story source/proof progress for 10 minutes.'
                            )
                            json_dump_atomic(report_path, report)
                            state['stalled'] = True
                            notify(f'IndiaWorks {story_id}: stalled; no selected-story source/proof progress for 10 minutes. Stopping executor for bounded recovery.')
                            raise subprocess.TimeoutExpired(command, NO_DELIVERY_STALL_SECONDS)
        except subprocess.TimeoutExpired:
            if os.name == "nt":
                killed = subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"],
                               capture_output=True, timeout=30, creationflags=flags)
            else:
                process.kill()
            process.wait(timeout=30)
            state.update(status='STALLED' if state.get('stalled') else 'TIMED_OUT', tree_terminated=os.name == 'nt' and killed.returncode == 0,
                         ended_at=time.time())
            json_dump_atomic(current / 'EXECUTOR_STATE.json', state)
            print(f"{story_id}: executor deadline exceeded. Lock retained; confirm process termination before recovery.", file=sys.stderr)
            return 124
        state.update(status='EXITED', exit_code=code, ended_at=time.time())
        json_dump_atomic(current / 'EXECUTOR_STATE.json', state)

    # Models report facts only. Python owns proof collation, outbox and finalization.
    report = json.loads((current / "REPORT.json").read_text(encoding="utf-8"))
    usage_path = Path(state['usage_file'])
    # Hermes can exit zero after displaying a provider error. Its usage record
    # distinguishes inference failure from a successful unfinished coding run.
    usage = json.loads(usage_path.read_text(encoding='utf-8')) if usage_path.exists() else {}
    provider_failed = usage.get('failed') is True
    executor_incomplete = usage.get('completed') is False
    if provider_failed or executor_incomplete:
        code = code or 1
        report['outcome'] = 'IN_PROGRESS' if report.get('outcome') == 'DONE' else report.get('outcome', 'IN_PROGRESS')
        if provider_failed:
            gap = 'Provider/inference failed; see executor log before retrying.'
            incomplete_reason = 'provider_failure'
        elif int(usage.get('api_calls') or 0) >= MAX_EXECUTOR_ITERATIONS:
            gap = (
                f'Executor exhausted the configured {MAX_EXECUTOR_ITERATIONS} API-call limit '
                'before completing its proof/report; preserve its source changes and resume '
                'with a bounded checkpoint.'
            )
            incomplete_reason = 'iteration_limit'
        else:
            gap = 'Executor/inference ended incomplete; see executor log before retrying.'
            incomplete_reason = 'executor_incomplete'
        report.setdefault('known_gaps', []).append(gap)
        json_dump_atomic(current / 'REPORT.json', report)
        state['exit_code'] = code
        state['inference_failed'] = provider_failed
        state['incomplete_reason'] = incomplete_reason
        json_dump_atomic(current / 'EXECUTOR_STATE.json', state)
    result_path = current / 'RESULT.json'
    lock = root / ".hermes" / "run.lock"
    if lock.exists():
        if not (current / "RESULT.json").exists():
            if code:
                report_path = current / "REPORT.json"
                report = json.loads(report_path.read_text(encoding="utf-8"))
                if report.get('outcome') == 'DONE':
                    report['outcome'] = 'IN_PROGRESS'
                    report.setdefault('known_gaps', []).append(f'Executor exited with code {code}; review process log before completion.')
                    report_path.write_text(json.dumps(report, indent=2), encoding='utf-8')
            complete(root)
        finalized = subprocess.run([sys.executable, str(root / "cron" / "finalize_run.py"), "--root", str(root)],
                                   cwd=root, capture_output=True, text=True, timeout=120)
        if finalized.returncode:
            print(finalized.stdout + finalized.stderr, file=sys.stderr)
            return finalized.returncode
        finalization = json.loads(finalized.stdout)
        result_path = Path(finalization['archive']) / 'RESULT.json'
        report = json.loads(result_path.read_text(encoding='utf-8'))
    print(f"IndiaWorks {story_id}: executor finished (exit {code}).")
    print(f"Outcome: {report.get('outcome', 'UNKNOWN')}. {report.get('summary', '')}")
    if report.get('blocker'):
        print(f"Blocker: {report['blocker']}")
    for gap in report.get('known_gaps', []):
        print(f"Remaining: {gap}")
    print(f"Evidence/result: {result_path}\nExecutor log: {logs / f'{stamp}-{story_id}.log'}")
    notify(f"IndiaWorks {story_id}: {report.get('outcome', 'UNKNOWN')} (executor exit {code}). {report.get('summary', '')}\n"
           f"Blocker: {report.get('blocker') or 'none reported'}. Remaining: {report.get('known_gaps', [])}\nResult: {result_path}")
    return code


if __name__ == "__main__":
    raise SystemExit(main())
