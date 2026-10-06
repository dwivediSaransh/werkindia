"""Persist factual automation output in its own Hermes chat, without inference."""
from __future__ import annotations

import sys
import time
from pathlib import Path

TITLE = 'IndiaWorks Cron Output'
SESSION_ID = 'indiaworks_cron_output'


def publish(home: Path, source: Path, message: str) -> None:
    sys.path.insert(0, str(source))
    from hermes_state import SessionDB

    # Dedicated output session: never inject into a user's active conversation.
    for target in (home, home / 'profiles' / 'indiaworks'):
        if not target.is_dir():
            continue
        db = SessionDB(db_path=target / 'state.db')
        try:
            db.ensure_session(SESSION_ID, source='cli')
            db.set_session_title(SESSION_ID, TITLE)
            db.append_message(SESSION_ID, 'assistant', content=message)
            db.set_session_pinned(SESSION_ID, True)
            db.set_session_read(SESSION_ID, False)
            # SessionDB.append_message stores the row but does not always refresh
            # sidebar activity metadata for externally-created output chats.
            db._conn.execute(
                "UPDATE sessions SET last_activity_at=?, last_activity_description=?, "
                "last_activity_provenance=?, last_read_at=? WHERE id=?",
                (time.time(), message[:240], "indiaworks_cron_output", 0.0, SESSION_ID),
            )
            db._conn.commit()
        finally:
            db.close()


def main() -> None:
    import argparse
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--home', type=Path, required=True)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--message', required=True)
    args = parser.parse_args()
    publish(args.home, args.source, args.message)


if __name__ == '__main__':
    main()
