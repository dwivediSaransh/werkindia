"""Factual source/proof progress and bounded tool diagnostics; no raw transcripts."""
from __future__ import annotations

import json
import sqlite3
from collections import Counter
from pathlib import Path

from execution_packet import inventory


SOURCE_EXTENSIONS = {
    ".cs",
    ".csproj",
    ".sln",
    ".slnx",
    ".ts",
    ".html",
    ".scss",
    ".css",
    ".json",
    ".sql",
    ".ps1",
    ".py",
    ".toml",
}

NON_DELIVERY_PREFIXES = (
    ".hermes/",
    "cron/",
    "docs/",
    "src/web/.angular/",
    "src/web/dist/",
    "src/web/src/web/",
    "src/web/src/web.backup/",
)


def is_delivery_source(path: str) -> bool:
    normalized = path.replace("\\", "/")
    if normalized.startswith(NON_DELIVERY_PREFIXES):
        return False
    suffix = Path(normalized).suffix.lower()
    return suffix in SOURCE_EXTENSIONS or normalized in {
        "IndiaWorks.slnx",
        "Directory.Build.props",
        "Directory.Packages.props",
        "package.json",
        "package-lock.json",
    }


def snapshot(source: Path, current: Path, db_path: Path, session_source: str, started: float) -> dict:
    before = json.loads((current / 'SOURCE_BEFORE.json').read_text(encoding='utf-8'))
    after = inventory(source)
    changed = sorted(p for p in before.keys() | after.keys() if before.get(p) != after.get(p)
                     and is_delivery_source(p))
    proofs = []
    for p in sorted((current / 'verification').glob('*.json')):
        d = json.loads(p.read_text(encoding='utf-8'))
        proofs.append({'id': d['proof_id'], 'exit': d['exit_code']})
    result = {'changed_files': changed, 'source_digest': after, 'checks': proofs,
              'last_tool': None, 'last_command_exit': None, 'max_repeated_reads': 0}
    if not db_path.exists():
        return result
    try:
        db = sqlite3.connect(db_path.as_uri() + '?mode=ro', uri=True, timeout=2)
        try:
            session = db.execute('SELECT id FROM sessions WHERE source=? AND started_at>=? ORDER BY started_at DESC LIMIT 1',
                                 (session_source, started - 5)).fetchone()
            if not session:
                return result
            reads = Counter()
            for role, tool, content, calls in db.execute('SELECT role,tool_name,content,tool_calls FROM messages WHERE session_id=? ORDER BY id', session):
                if role == 'tool':
                    result['last_tool'] = tool
                    if tool == 'terminal':
                        try:
                            result['last_command_exit'] = json.loads(content)['exit_code']
                        except (ValueError, KeyError, TypeError):
                            pass
                if calls:
                    for call in json.loads(calls):
                        f = call.get('function', {})
                        if f.get('name') == 'read_file':
                            args = f.get('arguments', {})
                            if isinstance(args, str):
                                args = json.loads(args)
                            reads[args.get('path')] += 1
            result['max_repeated_reads'] = max(reads.values(), default=0)
        finally:
            db.close()
    except (sqlite3.Error, ValueError, TypeError):
        result['diagnostic_unavailable'] = True
    return result
