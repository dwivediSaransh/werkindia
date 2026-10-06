"""No-inference watchdog for one IndiaWorks cron: recover and retry its story only."""
from __future__ import annotations

import argparse
import json
import os
import msvcrt
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

from backlog_lib import find_active_sprint, json_dump_atomic, parse_dependencies, read_master, row_map
from process_identity import alive, identity, own_identity
from story_runtime import complete


def decision(job: dict, rows: list[dict], state: dict, locked: bool, now: float) -> tuple[str, str]:
    if not job.get('enabled') or job.get('state') == 'paused':
        return 'PAUSED', 'Original cron pause is respected.'
    sprint = find_active_sprint(rows)
    if sprint is None:
        return 'COMPLETE', 'All stories DONE.'
    pending = [r for r in rows if r['Sprint'] == sprint and r['Status'] == 'IN PROGRESS']
    if len(pending) > 1:
        return 'REVIEW', 'Multiple IN PROGRESS stories.'
    if locked:
        return 'INSPECT_LOCK', 'Inspect recorded process identity before recovery.'
    if not pending:
        return 'WAIT_ORIGINAL', 'Only the original cron starts a new story; blockers remain gated.'
    row = pending[0]
    if any(row_map(rows).get(dep, {}).get('Status') != 'DONE' for dep in parse_dependencies(row['Dependencies'])):
        return 'REVIEW', 'Current story has unfinished dependencies.'
    if state.get('story_id') == row['Story ID'] and now < state.get('retry_after', 0):
        return 'BACKOFF', 'Retry cooldown active.'
    next_run = job.get('next_run_at')
    if next_run:
        from datetime import datetime
        gap = datetime.fromisoformat(next_run).timestamp() - now
        if -300 <= gap <= 90:
            return 'WAIT_ORIGINAL', 'Original scheduler run is imminent or recently overdue.'
    return 'RETRY', row['Story ID']


def recover(root: Path, now: float, dry_run: bool) -> tuple[str, str]:
    current = root / '.hermes/current-run'
    metadata = current / 'EXECUTOR_STATE.json'
    if not metadata.exists():
        if current.exists() and any(current.iterdir()):
            return 'REVIEW', 'Lock has no executor identity; human process confirmation required.'
        lock_path = root / '.hermes/run.lock'
        if not lock_path.exists():
            return 'REVIEW', 'Lock disappeared before recovery could inspect it.'
        try:
            lock = json.loads(lock_path.read_text(encoding='utf-8'))
            story_id = lock['story_id']
            pid = int(lock['pid'])
        except (KeyError, TypeError, ValueError, json.JSONDecodeError) as exc:
            return 'REVIEW', f'Lock has no usable executor identity: {exc}'
        try:
            live_stamp = identity(pid)
        except OSError as exc:
            return 'REVIEW', f'Could not verify stale lock PID {pid}: {exc}'
        if live_stamp is not None:
            return 'REVIEW', 'Lock-only run still has a live recorded PID; human process confirmation required.'
        if dry_run:
            return 'WOULD_RECOVER', f'Dead lock-only run for {story_id}; would archive recovery note and release lock.'
        stamp = datetime.fromtimestamp(now, timezone.utc).strftime('%Y%m%dT%H%M%SZ')
        archive = root / '.hermes/runs' / f'{stamp}-{story_id}-LOCK-RECOVERED'
        archive.mkdir(parents=True, exist_ok=False)
        json_dump_atomic(archive / 'RECOVERY.json', {
            'story_id': story_id,
            'outcome': 'LOCK_RECOVERED',
            'reason': 'run.lock referenced a dead PID but current-run packet was missing',
            'lock': lock,
            'recovered_at': datetime.fromtimestamp(now, timezone.utc).isoformat(),
        })
        lock_path.unlink()
        return 'RECOVERED', f'Stale lock-only run for {story_id} archived; next tick can resume the same story.'
    execution = json.loads(metadata.read_text(encoding='utf-8'))
    manifest = json.loads((current / 'RUN_MANIFEST.json').read_text(encoding='utf-8'))
    if execution['story_id'] != manifest['story_id']:
        return 'REVIEW', 'Process record and selected story differ.'
    if alive(execution['host']):
        if now > execution['deadline'] + 300:
            return 'REVIEW', 'Host exceeded executor deadline and finalization grace; do not kill a possible ledger write.'
        return 'RUNNING', 'Host is alive; its bounded executor owns recovery.'
    child_alive = alive(execution['executor'])
    if child_alive:
        if now < execution['deadline'] + 120:
            return 'RUNNING', 'Executor remains within deadline; no silence-based kill.'
        if dry_run:
            return 'WOULD_TERMINATE', 'Orphaned executor exceeded deadline; would terminate its exact process tree.'
        # Reverify immediately before targeting a PID; never kill a reused PID.
        if not alive(execution['executor']):
            return 'REVIEW', 'Executor vanished before tree termination could be confirmed.'
        killed = subprocess.run(['taskkill', '/PID', str(execution['executor']['pid']), '/T', '/F'],
                                capture_output=True, timeout=30, creationflags=subprocess.CREATE_NO_WINDOW)
        if killed.returncode or alive(execution['executor']):
            return 'REVIEW', 'Could not confirm exact executor tree termination.'
        execution.update(status='TIMED_OUT', tree_terminated=True, ended_at=now)
        json_dump_atomic(metadata, execution)
    result_path = current / 'RESULT.json'
    if result_path.exists():
        try:
            result = json.loads(result_path.read_text(encoding='utf-8'))
        except json.JSONDecodeError as exc:
            return 'REVIEW', f'Result JSON is unreadable; lock retained: {exc}'
        if str(result.get('outcome', '')).upper().strip() == 'DONE':
            if dry_run:
                return 'WOULD_FINALIZE', 'Completed DONE result exists; would archive evidence and release stale lock.'
            finalized = subprocess.run([sys.executable, str(root / 'cron/finalize_run.py'), '--root', str(root)],
                                       capture_output=True, text=True, timeout=120)
            if finalized.returncode:
                return 'REVIEW', 'Completed-result finalizer rejected result; lock retained: ' + finalized.stderr.strip()
            return 'RECOVERED', 'Completed run archived and stale lock released; next tick can select eligible work.'
    if not execution.get('tree_terminated'):
        return 'REVIEW', 'Owner/executor ended but descendant termination is unconfirmed; lock retained.'
    if dry_run:
        return 'WOULD_RECOVER', 'Confirmed terminated tree; would archive incomplete result and release lock.'
    report_path = current / 'REPORT.json'
    report = json.loads(report_path.read_text(encoding='utf-8'))
    if report['outcome'] != 'BLOCKED':
        report['outcome'] = 'IN_PROGRESS'
        report.setdefault('known_gaps', []).append('Watchdog recovered confirmed terminated executor; completion remains unverified.')
    json_dump_atomic(report_path, report)
    complete(root)
    finalized = subprocess.run([sys.executable, str(root / 'cron/finalize_run.py'), '--root', str(root)],
                               capture_output=True, text=True, timeout=120)
    if finalized.returncode:
        return 'REVIEW', 'Recovery finalizer rejected result; lock retained: ' + finalized.stderr.strip()
    return 'RECOVERED', 'Timed-out run archived truthfully; next tick can retry the same story.'


def main(collect_health=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', required=True)
    parser.add_argument('--hermes-home', required=True)
    parser.add_argument('--hermes-python', required=True)
    parser.add_argument('--hermes-main', required=True)
    parser.add_argument('--job-id', required=True)
    parser.add_argument('--dry-run', action='store_true')
    parser.add_argument('--watchdog-id', default='')
    args = parser.parse_args()
    root = Path(args.root).resolve()
    runtime = root / '.hermes/watchdog'
    runtime.mkdir(parents=True, exist_ok=True)
    lock = runtime / 'tick.lock'
    handle = lock.open('a+b')
    if handle.tell() == 0:
        handle.write(b'0')
        handle.flush()
    handle.seek(0)
    try:
        msvcrt.locking(handle.fileno(), msvcrt.LK_NBLCK, 1)
    except OSError:
        handle.close()
        return 0
    try:
        now = time.time()
        path = runtime / 'state.json'
        state = json.loads(path.read_text(encoding='utf-8')) if path.exists() else {}
        data = json.loads((Path(args.hermes_home) / 'cron/jobs.json').read_text(encoding='utf-8'))
        jobs = data.get('jobs', []) if isinstance(data, dict) else data
        job = next(j for j in jobs if j['id'] == args.job_id)
        agent_backed_job = not job.get('no_agent')
        legacy_script_job = job.get('script') == 'indiaworks_story.py' and job.get('no_agent')
        if not (agent_backed_job or legacy_script_job):
            raise ValueError('Original job shape changed; refusing unattended recovery')
        _, rows = read_master(root)
        health = collect_health(root, args, job, rows, state, now) if collect_health else {}
        state['health'] = health
        locked = (root / '.hermes/run.lock').exists()
        sprint = find_active_sprint(rows)
        pending = [r for r in rows if r['Sprint'] == sprint and r['Status'] == 'IN PROGRESS'] if sprint else []
        if health.get('current_story_id'):
            state['story_id'] = health['current_story_id']
            if health.get('current_child'):
                state['child'] = health['current_child']
        elif not locked and not pending:
            for key in ('story_id', 'child', 'attempts', 'first_retry_at', 'retry_after', 'needs_review'):
                state.pop(key, None)
        action, detail = decision(job, rows, state, locked, now)
        if action not in {'PAUSED', 'COMPLETE', 'INSPECT_LOCK'} and health.get('requires_review'):
            action, detail = 'HEALTH_REVIEW', health['requires_review']
        if action == 'RETRY' and health.get('retry_inhibited'):
            action, detail = 'HEALTH_BACKOFF', health['retry_inhibited']
        if action == 'WAIT_ORIGINAL' and health.get('trigger_original'):
            action, detail = 'RUN_ORIGINAL', 'Original cron is overdue, scheduler is alive and eligible work exists.'
        # Single detached retry guard spans all ticks; identity prevents PID reuse.
        if action not in {'PAUSED', 'INSPECT_LOCK'} and state.get('child') and alive(state['child']):
            action, detail = 'RUNNING', 'A watchdog retry host is still alive.'
        elif action == 'INSPECT_LOCK':
            action, detail = recover(root, now, args.dry_run)
        elif action in {'RETRY', 'RUN_ORIGINAL'}:
            story = detail
            if args.dry_run:
                action, detail = ('WOULD_TRIGGER_ORIGINAL' if action == 'RUN_ORIGINAL' else 'WOULD_RETRY'), story
            else:
                command = ([args.hermes_python, args.hermes_main, 'cron', 'run', args.job_id]
                           if action == 'RUN_ORIGINAL' or agent_backed_job else
                           [sys.executable, str(root / 'cron/run_hermes_story.py'), '--root', str(root),
                           '--hermes-python', args.hermes_python, '--hermes-main', args.hermes_main,
                           '--notification-home', args.hermes_home,
                           '--expected-story', story])
                if action == 'RUN_ORIGINAL':
                    story = '_original-scheduler-trigger'
                attempts = state.get('attempts', 0) + 1 if state.get('story_id') == story else 1
                first = state.get('first_retry_at', now) if state.get('story_id') == story else now
                with (runtime / 'retry.log').open('a', encoding='utf-8') as output:
                    child = subprocess.Popen(command, cwd=root, stdin=subprocess.DEVNULL, stdout=output,
                                             stderr=subprocess.STDOUT, creationflags=subprocess.CREATE_NO_WINDOW)
                state.update(story_id=story, child=own_identity(child.pid), attempts=attempts,
                             first_retry_at=first, retry_after=now + min(3600, 900 * 2 ** min(attempts-1, 2)),
                             needs_review=now-first >= 86400)
                action, detail = ('ORIGINAL_TRIGGERED' if action == 'RUN_ORIGINAL' else 'RETRY_STARTED'), f'{story}: attempt {attempts}; isolated IndiaWorks profile, same checkout.'
        if action == 'RUNNING' and health.get('stalled'):
            action, detail = 'RUNNING_STALLED', 'No new code/proof or session activity for 15 minutes; deadline remains enforced.'
        if args.dry_run:
            print(json.dumps({'action': action, 'detail': detail}))
            return 0
        changed = (state.get('action'), state.get('detail')) != (action, detail)
        state.update(action=action, detail=detail, checked_at=now, watched_job=args.job_id)
        json_dump_atomic(path, state)
        if changed:
            with (runtime / 'events.log').open('a', encoding='utf-8') as output:
                output.write(json.dumps({'at': now, 'action': action, 'detail': detail}) + '\n')
            if action in {'REVIEW', 'RECOVERED', 'RETRY_STARTED', 'ORIGINAL_TRIGGERED', 'RUNNING_STALLED', 'HEALTH_BACKOFF', 'HEALTH_REVIEW'}:
                print(f'IndiaWorks watchdog: {action}: {detail}')
        flags = health.get('flags', [])
        if flags != state.get('reported_health_flags', []) and flags:
            print('IndiaWorks watchdog health: ' + ', '.join(flags))
        state['reported_health_flags'] = flags
        json_dump_atomic(path, state)
        return 0
    finally:
        handle.seek(0)
        msvcrt.locking(handle.fileno(), msvcrt.LK_UNLCK, 1)
        handle.close()


if __name__ == '__main__':
    raise SystemExit(main())
