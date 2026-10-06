# IndiaWorks full-reference watchdog adaptation.
# All original reference function bodies are retained below. IndiaWorks adapters
# at the end provide the active entrypoint and safe project-specific controls.
# Historical Coefficiant/mobile constants below are reference history only;
# configure_indiaworks() overrides roots, job IDs, ledger and profile before use.
"""Cron watchdog: drive EVERY watched cron run to STORY COMPLETION -- multi-job.

Watches an arbitrary set of cron jobs (configured via --jobs-file or a single
default job) and, for each, detects failed / hung / quiet / stuck / storage-locked
sessions and resumes ("continue") them, reusing the session's existing context,
until the session completes and (when a story id is embedded) the ledger story is
marked Done.

PRIMARY GOAL (non-negotiable): lift a failed / hung / quiet / stuck session and
KEEP pushing it toward completion across ticks until the story it picked is DONE.
The watchdog never permanently abandons a run -- it persists (with backoff) until
either the session completes with cron_complete AND the ledger story is marked
Done, or a hard human-intervention ceiling is reached after sustained failure.

Why multi-job:
- A single watchdog process now repairs + watches many cron jobs, so one scheduled
  tick (every 3 min) services all of them. Each job has its own state/log file
  (watch_<job_id>.json / .log) so runs never cross-talk.
- The GLOBAL single-resume-child guard spans ALL watched jobs: under the gateway's
  DELETE journal mode, two concurrent --resume children (for ANY job) wedge the
  single state.db write lock -> "session storage could not be written". So at most
  ONE resume child may be in flight across every watched job at once.

Jobs config (--jobs-file), JSON list of job objects:
  [
    {
      "job_id": "f28a1a7eafbc",                       # required: cron job id
      "ledger": "C:/.../indi_blog_mobile_app_stories.csv",  # optional: ledger to mark Done
      "stuck_minutes": 15,                            # optional: open+inactive >= N min => stuck
      "since_hours": 24,                              # optional (reserved)
      "require_story": false                          # optional: only mark ledger if story id found
    }
  ]
If --jobs-file is omitted, the watchdog watches a single default job whose id comes
from env HERMES_MOBILE_JOB_ID (fallback f28a1a7eafbc).

Safety:
- Self-lock (watchdog.lock) so concurrent scheduler ticks can't double-run.
- Backup/stray cleanup is safe every tick. Live state.db retention is delegated
  to hermes_state_retention.py behind a separate lock/cooldown/backup gate and
  only runs when the DB crosses the configured size trigger.
- The in-flight child guard prevents multiple --resume children for one session, and
  the global guard prevents more than one resume child across all jobs.
"""

import json
import os
import re
import sqlite3
import subprocess
import sys
import time
import hashlib
from datetime import datetime, timezone
from pathlib import Path

HERMES_HOME = Path(os.environ.get("HERMES_HOME", Path.home() / "AppData" / "Local" / "hermes"))
DRY_RUN = "--dry-run" in sys.argv

STATE_DIR = HERMES_HOME / "cron" / "output"
SELF_LOCK = HERMES_HOME / "cron" / "watchdog.lock"

STATE_DB = HERMES_HOME / "state.db"
RECOVERY_DIR = HERMES_HOME / "cron" / "recovery-backups"
RETENTION_SCRIPT = Path(__file__).with_name("hermes_state_retention.py")
# Ledger the completed story is marked in once the session truly completes.
# The repo can live at C:\Projects\Coefficiant or C:\Users\<user>\Projects\Coefficiant
# depending on machine; try the known candidates and use the first that exists.
LEDGER_CANDIDATES = [
    Path(r"C:\Projects\Coefficiant\apps\blog-mobile\indi_blog_mobile_app_stories.csv"),
    Path.home() / "Projects" / "Coefficiant" / "apps" / "blog-mobile" / "indi_blog_mobile_app_stories.csv",
    HERMES_HOME.parent.parent / "Projects" / "Coefficiant" / "apps" / "blog-mobile" / "indi_blog_mobile_app_stories.csv",
]
LEDGER = next((p for p in LEDGER_CANDIDATES if p.exists()), LEDGER_CANDIDATES[0])

DEFAULT_JOB_ID = os.environ.get("HERMES_MOBILE_JOB_ID", "f28a1a7eafbc")


def bool_env(name, default):
    raw = os.environ.get(name)
    if raw is None:
        return default
    return str(raw).strip().lower() not in ("0", "false", "no", "off")


def int_env(name, default):
    try:
        return int(os.environ.get(name, str(default)))
    except ValueError:
        return default

# How long to back off after a storage/lock failure before retrying a resume.
# The gateway does long DB writes (e.g. context compression) under DELETE journal
# mode; hammering a resume child every 3 min during that window just re-wedges the
# lock. Backing off gives the gateway room to finish its write, then we retry.
STORAGE_BACKOFF_S = 15 * 60  # 15 minutes
GUARD_SECONDS = 90  # start a retry once the scheduled run is >90s away (3-min cadence)
SIZE_TRIGGER_MB = int_env("HERMES_WATCHDOG_STATE_DB_SIZE_TRIGGER_MB", 500)
SIZE_TRIGGER_BYTES = SIZE_TRIGGER_MB * 1024 * 1024  # run retention when live state.db crosses this

# Storage-hygiene tuning (the only thing that touches disk).
BACKUP_KEEP = 3                      # keep the newest N recovery backups
BACKUP_CAP_BYTES = 4 * 1024 * 1024 * 1024   # prune recovery-backups above 4 GB
STRAY_REPAIR_MAX_AGE_S = 3600        # only delete one-time repair leftovers older than 1h
SELF_LOCK_TTL = 600                  # seconds; stale self-lock is stolen after 10 min

# Persistence model. The watchdog DOES NOT permanently give up on a run. It keeps
# nudging (with backoff) until the session completes. GIVEUP_AFTER_S is only a
# ceiling for when sustained failure has gone on so long that a human should look
# -- even then the watchdog still nudges occasionally (once per GIVEUP_NUDGE_MIN)
# rather than going silent, so a stuck run is never truly abandoned.
GIVEUP_AFTER_S = 24 * 3600           # 24h of continuous failure before flagging "needs review"
GIVEUP_NUDGE_MIN = 60                # after the ceiling, still nudge at most once/hour

# A cron run that HANGS (starts a session, makes no progress, but never records
# an error in jobs.json) leaves last_status='ok' while the session is silently
# stuck open. The watchdog must detect this independently of jobs.json status.
# Treat the latest open cron_* session as STUCK if it has had no new message
# written for at least this long (it is open AND inactive) AND no other cron
# session for this job is actively progressing (so we never disturb a run that is
# merely thinking). Raised from 8m -> 15m because the model can think silently.
STUCK_INACTIVITY_S = 15 * 60         # 15 minutes of inactivity on an open session = hung
# A spawned `hermes chat --resume cron_*` child that has produced no transcript
# activity for this long is stale. Older versions only checked whether ANY
# child existed; one wedged child then made the global watchdog back off forever.
RESUME_CHILD_STALE_S = int_env("HERMES_WATCHDOG_RESUME_CHILD_STALE_MIN", 45) * 60
# The watchdog should be global by default: configured watch_jobs.json entries are
# still honored, but every recurring Hermes cron job is merged in unless disabled.
AUTO_DISCOVER_CRON_JOBS = False  # IndiaWorks is scoped to its explicitly configured job.
# If a watched recurring job is paused/disabled without an explicit opt-out reason,
# resume it. This repairs accidental UI/API pauses while respecting opt-out text.
AUTO_RESUME_PAUSED_JOBS = False  # Human pauses are persistent instructions.
PAUSE_OPT_OUT_RE = re.compile(r"manual|operator|do not (?:auto-)?resume|hold|maintenance|intentional", re.IGNORECASE)
WATCHDOG_JOB_ID = os.environ.get("HERMES_WATCHDOG_JOB_ID", "307c1a2c831d")
EXPECTED_WATCHDOG_SCHEDULE = os.environ.get("HERMES_WATCHDOG_EXPECTED_SCHEDULE", "*/3 * * * *")

# Story id embedded in a session's first user message, e.g. "MOB-STATS-002".
STORY_RE = re.compile(r"\b((?:MOB|COEFF|WEB|BLOG)-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d+)\b")
PLACEHOLDER_STORY_IDS = {"DEPENDENCY-STORY-ID", "STORY-ID", "SELECTED-STORY-ID"}
_LEDGER_STORY_CACHE = {}
GATE_PAUSE_RE = re.compile(
    r"(GATE PAUSE REPORT|No executable stories available|Selected story:\s*NONE|"
    r"No story selected|frontier_size['\"]?\s*[:=]\s*0|^\s*\[SILENT\]\s*$)",
    re.IGNORECASE | re.MULTILINE,
)
SETTLED_SESSION_RE = re.compile(
    r"(All clean\. Work complete|All gates passed|Final Report|Final story status|"
    r"RUN COMPLETE|No further action (?:needed|required)|work(?:ing tree)? (?:left )?clean)",
    re.IGNORECASE,
)
DONE_CANDIDATE_BLOCKER_RE = re.compile(
    r"(not started|blocked|unavailable|not producible|cannot be satisfied|"
    r"remaining (?:AC|work|criterion)|only open gap|do not close without|pending verification)",
    re.IGNORECASE,
)
DONE_CANDIDATE_ARTIFACT_RE = re.compile(
    r"(^|[;,\s])(?:[A-Za-z]:[\\/])?(?:\.github|apps|packages|services|docs|scripts|libs)[\\/][^;,\n]+",
    re.IGNORECASE,
)
DONE_CANDIDATE_VERIFY_RE = re.compile(
    r"(npm|node|python|npx|jest|typecheck|py_compile|verify-update|post-commit|exit\s+0|PASS)",
    re.IGNORECASE,
)

# Progress must mean accepted delivery work, not any recent transcript chatter.
# Background review, context compression, skill curation, or "thinking" messages
# do not suppress the watchdog anymore. A live run is considered progressing only
# when it produces a repo diff/commit, a validation signal, or a ledger/status
# transition.
ACCEPTED_PROGRESS_RE = re.compile(
    r"\b("
    r"validation\s+pass(?:ed)?(?![a-z])|typecheck\s+pass(?:ed)?(?![a-z])|jest\s+pass(?:ed)?(?![a-z])|"
    r"build\s+(?:pass(?:ed)?|succeed(?:ed)?)(?![a-z])|lint\s+pass(?:ed)?(?![a-z])|py_compile\s+pass(?:ed)?(?![a-z])|"
    r"preflight\s+pass(?:ed)?(?![a-z])|verify-update\s+pass(?:ed)?(?![a-z])|post-commit\s+pass(?:ed)?(?![a-z])|"
    r"graphify\s+update\s+pass(?:ed)?(?![a-z])|focused\s+validation\s+pass(?:ed)?(?![a-z])|"
    r"passed!\s+-\s+failed:\s+0|"
    r"commit(?:ted)?\s+[0-9a-f]{7,40}|[0-9a-f]{7,40}\s+(?:feat|fix|chore|test|docs)\b|"
    r"ledger\s+[^.\n]{0,120}->\s*(?:in progress|done candidate|done)|"
    r"status\s+(?:moved|transition(?:ed)?)"
    r")\b",
    re.IGNORECASE,
)

NON_PROGRESS_RE = re.compile(
    r"background review|context compression|compress(?:ing|ion)|skill(?:_| )manage|"
    r"curator|thread title|reviewing|thinking|actively progressing|\\[SILENT\\]",
    re.IGNORECASE,
)
PROGRESS_SIGNAL_ROLES = ("assistant", "tool")
PASSIVE_PROGRESS_SOURCE_RE = re.compile(
    r"audit-related ledger rows|ledger rows|story id|acceptance criteria|files changed|"
    r"what was verified|known gaps|agent package|\"content\"\s*:",
    re.IGNORECASE,
)
ACTIVE_VALIDATION_SOURCE_RE = re.compile(
    r"===\s*(?:full solution build|unittests|architecturetests|[^=]*tests?)\s*===|"
    r"test run for|build succeeded|passed!\s+-\s+failed:\s+0",
    re.IGNORECASE,
)


# --- logging (per-job log file) -----------------------------------------------
def log(msg, log_file=None):
    target = log_file or (STATE_DIR / "watchdog.log")
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now().astimezone().strftime("%Y-%m-%d %H:%M:%S %Z")
    line = f"{stamp} {msg}\n".encode()
    with open(target, "ab") as f:
        f.write(line)
    # Also mirror to the aggregate watchdog.log for a single timeline view.
    if log_file and log_file != (STATE_DIR / "watchdog.log"):
        with open(STATE_DIR / "watchdog.log", "ab") as f:
            f.write(line)


def fmt_mb(b):
    return f"{b / (1024 * 1024):.1f} MB"


def live_db_size_bytes():
    """Size of the live state.db + its WAL/SHM siblings (informational only)."""
    total = 0
    for suffix in ("", "-wal", "-shm"):
        p = STATE_DB if not suffix else STATE_DB.parent / (STATE_DB.name + suffix)
        try:
            total += p.stat().st_size
        except OSError:
            pass
    return total


def recovery_backups_size():
    """Bytes of actual backup payload under recovery-backups/."""
    total = 0
    if RECOVERY_DIR.exists():
        for p in RECOVERY_DIR.iterdir():
            if p.is_file() and p.name.startswith("state_") and p.name.endswith(".db"):
                total += p.stat().st_size
    return total


# --- self-lock (prevent watchdog double-run across scheduler ticks) ---------
def acquire_self_lock():
    now = time.time()
    if SELF_LOCK.exists():
        try:
            data = json.loads(SELF_LOCK.read_text(encoding="utf-8"))
            if now - data.get("ts", 0) < SELF_LOCK_TTL and data.get("pid") not in (0, os.getpid()):
                return False  # another live watchdog holds it
        except (OSError, ValueError):
            pass
    SELF_LOCK.write_text(json.dumps({"pid": os.getpid(), "ts": now}), encoding="utf-8")
    return True


def release_self_lock():
    try:
        SELF_LOCK.unlink()
    except OSError:
        pass


# --- wedged child process cleanup --------------------------------------------
def kill_wedged_resume(session_id):
    """Kill only `--resume <session_id>` child procs for the failed session."""
    return False  # Exact PID+creation identity recovery belongs to recover().
    if DRY_RUN:
        log(f"watchdog[DRY-RUN]: would kill --resume {session_id} procs")
        return
    if os.name != "nt":
        subprocess.run(["pkill", "-f", f"--resume {session_id}"], check=False)
        return
    try:
        out = subprocess.run(
            ["wmic", "process", "where", f"commandline like '%--resume {session_id}%'",
             "get", "processid"],
            capture_output=True, text=True, timeout=20,
        ).stdout
        pids = [l.strip() for l in out.splitlines() if l.strip().isdigit()]
    except Exception as exc:
        log(f"watchdog: enumerate resume procs failed: {exc}")
        return
    for pid in pids:
        try:
            subprocess.run(["taskkill", "/F", "/T", "/PID", pid], check=False,
                           capture_output=True, timeout=20)
            log(f"watchdog: killed wedged --resume proc {pid} for {session_id}")
        except Exception as exc:
            log(f"watchdog: kill {pid} failed: {exc}")


# --- storage hygiene (safe: only touches backup / stray files) ---------------
def prune_recovery_backups():
    """Keep only the newest BACKUP_KEEP `state_*.db` (+wal/shm); fall under cap."""
    return 0  # No unreviewed backup deletion.
    if not RECOVERY_DIR.exists():
        return 0
    main_files = [p for p in RECOVERY_DIR.iterdir()
                  if p.is_file() and p.name.startswith("state_") and p.name.endswith(".db")]
    main_files.sort(key=lambda p: p.stat().st_mtime, reverse=True)
    freed = 0
    keep = main_files[:BACKUP_KEEP]
    for p in main_files[BACKUP_KEEP:]:
        for sib in (p, p.with_name(p.name + "-wal"), p.with_name(p.name + "-shm")):
            if sib.exists():
                size = sib.stat().st_size
                if DRY_RUN:
                    log(f"watchdog[DRY-RUN]: would delete {sib} ({fmt_mb(size)})")
                else:
                    try:
                        sib.unlink()
                        freed += size
                    except OSError as exc:
                        log(f"watchdog: could not delete {sib}: {exc}")
    while keep and recovery_backups_size() > BACKUP_CAP_BYTES:
        oldest = min(keep, key=lambda p: p.stat().st_mtime)
        keep.remove(oldest)
        for sib in (oldest, oldest.with_name(oldest.name + "-wal"),
                    oldest.with_name(oldest.name + "-shm")):
            if sib.exists():
                size = sib.stat().st_size
                if DRY_RUN:
                    log(f"watchdog[DRY-RUN]: would delete (over cap) {sib} ({fmt_mb(size)})")
                else:
                    try:
                        sib.unlink()
                        freed += size
                    except OSError as exc:
                        log(f"watchdog: could not delete {sib}: {exc}")
    return freed


def cleanup_stray_repair_files():
    """Delete one-time repair/corrupt leftovers next to state.db (if old enough)."""
    return 0  # No shared-home artifact deletion.
    patterns = ("state.db.corrupt_*.bak", "state.db.pre-update-emergency-*.bak",
                "state.db.repaired")
    cutoff = time.time() - STRAY_REPAIR_MAX_AGE_S
    freed = 0
    for pat in patterns:
        for p in HERMES_HOME.glob(pat):
            if not p.is_file():
                continue
            try:
                if p.stat().st_mtime > cutoff:
                    continue
                size = p.stat().st_size
                if DRY_RUN:
                    log(f"watchdog[DRY-RUN]: would delete stray {p.name} ({fmt_mb(size)})")
                else:
                    p.unlink()
                    freed += size
                    log(f"watchdog: deleted stray repair file {p.name} ({fmt_mb(size)})")
            except OSError as exc:
                log(f"watchdog: could not delete {p.name}: {exc}")
    return freed


def storage_hygiene(note=""):
    """Prune backups + clean stray files. Safe every tick. Returns bytes freed."""
    return 0  # Retained reference; automatic deletion is not enabled for IndiaWorks.
    before = recovery_backups_size()
    freed = prune_recovery_backups()
    freed += cleanup_stray_repair_files()
    after = recovery_backups_size()
    if freed > 0 or before > BACKUP_CAP_BYTES:
        log(f"watchdog: storage hygiene {note}- freed {fmt_mb(freed)}; "
            f"recovery-backups now {fmt_mb(after)}")
    return freed


def state_retention_maintenance(note="threshold"):
    """Run bounded live state.db retention through the dedicated SOC script.

    This function never performs SQL itself. It only launches the retention
    control when the watchdog's global resume-child guard says no Hermes
    `--resume` worker is currently writing session storage. The script has its
    own lock, cooldown, online backup, schema checks, WAL checkpoint, and VACUUM.
    """
    return False  # Requires a separately reviewed profile-scoped retention tool.
    if any_job_resume_child_alive():
        log("watchdog: state retention skipped; a --resume child is alive and "
            "may need the state.db write lock.")
        return False
    if not RETENTION_SCRIPT.exists():
        log(f"watchdog: state retention skipped; script missing: {RETENTION_SCRIPT}")
        return False

    cmd = [
        sys.executable,
        str(RETENTION_SCRIPT),
        "--emergency",
        "--size-trigger-mb", str(SIZE_TRIGGER_MB),
    ]
    if DRY_RUN:
        cmd.append("--dry-run")
    try:
        proc = subprocess.run(cmd, capture_output=True, text=True, timeout=180)
    except Exception as exc:
        log(f"watchdog: state retention {note} failed to start: {exc}")
        return False

    stdout = (proc.stdout or "").strip()
    stderr = (proc.stderr or "").strip()
    if proc.returncode != 0:
        log(f"watchdog: state retention {note} failed rc={proc.returncode}: "
            f"{(stderr or stdout)[-1000:]}")
        return False

    try:
        result = json.loads(stdout) if stdout else {}
    except ValueError:
        result = {"action": "unparsed", "stdout": stdout[-1000:]}
    action = result.get("action", "unknown")
    if action == "pruned":
        log("watchdog: state retention pruned live state.db "
            f"({result.get('live_size_before')} -> {result.get('live_size_after')}; "
            f"sessions pruned={result.get('pruned_session_count')}; "
            f"stale finalized={result.get('finalized_stale_open_count')})")
        return True
    log(f"watchdog: state retention {note} {action}")
    return True


# --- job / session inspection (parameterised by job_id) ----------------------
def load_job(job_id):
    data = json.loads((HERMES_HOME / "cron" / "jobs.json").read_text(encoding="utf-8"))
    jobs = data["jobs"] if isinstance(data, dict) and "jobs" in data else data
    if isinstance(jobs, dict):
        jobs = list(jobs.values())
    for j in jobs:
        if j.get("id") == job_id or j.get("job_id") == job_id:
            return j
    return None


def parse_iso(s):
    if not s:
        return None
    try:
        return datetime.fromisoformat(s)
    except ValueError:
        return None


def infer_repo_root(ledger_path=None):
    """Infer the working repo from a configured mobile ledger path."""
    candidates = []
    if ledger_path:
        try:
            p = Path(ledger_path)
            candidates.extend([p.parents[2], p.parents[1], p.parent])
        except Exception:
            pass
    candidates.extend([
        Path(r"C:\Projects\Coefficiant"),
        Path.home() / "Projects" / "Coefficiant",
    ])
    for c in candidates:
        if c and (c / ".git").exists():
            return c
    return None


def _git(repo, *args):
    try:
        return subprocess.run(
            ["git", "-C", str(repo), *args],
            capture_output=True, text=True, timeout=15, check=False,
        )
    except Exception:
        return None


def _fingerprint(text):
    return hashlib.sha256((text or "").encode("utf-8", "replace")).hexdigest()


def is_passive_progress_source(text):
    """True when a progress-looking phrase came from inspection, not work.

    Ledger rows, backlog text, source-file reads, and agent packages often contain
    historical evidence such as "build PASS". Those are not new progress by the
    current cron session and must not keep the watchdog quiet.
    """
    text = text or ""
    return bool(PASSIVE_PROGRESS_SOURCE_RE.search(text))


def recent_session_progress_signal(job_id, window_s, session_id=None):
    """Return a concrete transcript progress reason, or None.

    This deliberately ignores generic recent messages. It also ignores the
    initial cron user prompt: prompts often contain phrases like "build passes"
    in acceptance criteria, and treating those as delivery progress made stuck
    PRX/backend jobs look healthy. Only assistant/tool output from the selected
    incomplete session can suppress a nudge.
    """
    try:
        con = sqlite3.connect(
            f"file:{STATE_DB}?mode=ro&busy_timeout=5000",
            uri=True, timeout=15,
        )
        try:
            cutoff = time.time() - window_s
            if session_id:
                rows = con.execute(
                    "SELECT m.session_id, m.role, m.content FROM messages m "
                    "WHERE m.session_id = ? AND m.role IN (?, ?) AND m.timestamp >= ? "
                    "ORDER BY m.timestamp DESC LIMIT 80",
                    (session_id, *PROGRESS_SIGNAL_ROLES, cutoff),
                ).fetchall()
            else:
                rows = con.execute(
                    "SELECT s.id, m.role, m.content FROM messages m JOIN sessions s ON m.session_id = s.id "
                    "WHERE s.id LIKE ? AND m.role IN (?, ?) AND m.timestamp >= ? "
                    "ORDER BY m.timestamp DESC LIMIT 80",
                    (f"cron_{job_id}%", *PROGRESS_SIGNAL_ROLES, cutoff),
                ).fetchall()
        finally:
            con.close()
    except Exception as exc:
        log(f"watchdog: recent_session_progress_signal error: {exc}")
        return None

    for sid, _role, content in rows:
        text = content or ""
        if is_passive_progress_source(text):
            continue
        if NON_PROGRESS_RE.search(text) and not ACCEPTED_PROGRESS_RE.search(text):
            continue
        m = ACCEPTED_PROGRESS_RE.search(text)
        if m:
            return f"transcript signal in {sid}: {m.group(0)[:80]}"
    return None


def repo_progress_signal(job_id, repo_root, ledger_path, state):
    """Return a concrete repo/ledger progress reason, or None.

    A changed HEAD, newly changed working-tree diff, or changed ledger digest is
    accepted progress. The first observation seeds the baseline only; it never
    counts an already-dirty tree as progress.
    """
    if not repo_root:
        return None
    repo_state = state.setdefault("_repo_progress", {})
    key = str(repo_root).lower()
    entry = repo_state.setdefault(key, {})

    head_proc = _git(repo_root, "rev-parse", "HEAD")
    head = (head_proc.stdout.strip() if head_proc and head_proc.returncode == 0 else "")
    status_proc = _git(repo_root, "status", "--porcelain=v1", "--untracked-files=normal")
    dirty = (status_proc.stdout.strip() if status_proc and status_proc.returncode == 0 else "")
    dirty_hash = _fingerprint(dirty)

    ledger_hash = ""
    if ledger_path and Path(ledger_path).exists():
        try:
            ledger_hash = hashlib.sha256(Path(ledger_path).read_bytes()).hexdigest()
        except OSError:
            ledger_hash = ""

    reason = None
    if entry.get("head") and head and entry.get("head") != head:
        reason = f"repo commit {entry.get('head')[:7]} -> {head[:7]}"
    elif dirty and entry.get("dirty_hash") and entry.get("dirty_hash") != dirty_hash:
        reason = "working-tree diff changed"
    elif ledger_hash and entry.get("ledger_hash") and entry.get("ledger_hash") != ledger_hash:
        reason = "ledger content changed"
    # Deliberately do NOT count a pre-existing dirty tree as progress on the
    # first observation. Both CoEfficiant mobile and PRX Hermes jobs can have
    # unrelated in-flight dirty work; treating that as progress made the watchdog
    # stand down forever while the actual cron job stayed paused or wedged.

    entry.update({
        "head": head,
        "dirty_hash": dirty_hash,
        "dirty_present": bool(dirty),
        "ledger_hash": ledger_hash,
        "observed_at": datetime.now(timezone.utc).isoformat(),
    })
    return reason


def job_has_accepted_progress(job_id, job_cfg, stuck_s, log_file, session_id=None):
    """True only when the selected session has delivery progress, not chatter."""
    state_file = STATE_DIR / f"watch_{job_id}.json"
    state = load_state(state_file)
    ledger_path = job_cfg.get("ledger")
    repo_root = Path(job_cfg.get("repo")) if job_cfg.get("repo") else infer_repo_root(ledger_path)

    reason = recent_session_progress_signal(job_id, stuck_s, session_id=session_id)
    if not reason:
        reason = repo_progress_signal(job_id, repo_root, ledger_path, state)
    save_state(state_file, state)
    if reason:
        log(f"watchdog[{job_id}]: accepted progress detected for {session_id or 'job'} ({reason}) -- not disturbing the live run", log_file)
        return True
    return False


def session_inactive_seconds(session_id):
    """Seconds since the last message in a session, or None if unknown."""
    try:
        con = sqlite3.connect(
            f"file:{STATE_DB}?mode=ro&busy_timeout=5000",
            uri=True, timeout=15,
        )
        try:
            row = con.execute(
                "SELECT started_at FROM sessions WHERE id = ?",
                (session_id,),
            ).fetchone()
            if not row:
                return None
            last_msg = con.execute(
                "SELECT MAX(timestamp) FROM messages WHERE session_id = ?",
                (session_id,),
            ).fetchone()[0]
            base = float(last_msg) if last_msg is not None else float(row[0])
            return max(0, time.time() - base)
        finally:
            con.close()
    except Exception as exc:
        log(f"watchdog: session_inactive_seconds error for {session_id}: {exc}")
        return None


def session_progressing_now(session_id, window_s=STUCK_INACTIVITY_S):
    """True if the selected session had assistant/tool activity recently.

    User prompts are excluded so a newly fired cron prompt cannot mask a stuck
    sibling session or count as implementation progress.
    """
    try:
        con = sqlite3.connect(
            f"file:{STATE_DB}?mode=ro&busy_timeout=5000",
            uri=True, timeout=15,
        )
        try:
            cutoff = time.time() - window_s
            row = con.execute(
                "SELECT 1 FROM messages m "
                "WHERE m.session_id = ? AND m.role IN (?, ?) AND m.timestamp >= ? LIMIT 1",
                (session_id, *PROGRESS_SIGNAL_ROLES, cutoff),
            ).fetchone()
            return row is not None
        finally:
            con.close()
    except Exception as exc:
        log(f"watchdog: session_progressing_now error: {exc}")
        return False


def job_progressing_now(job_id):
    """Compatibility helper: any assistant/tool activity for this job recently."""
    try:
        con = sqlite3.connect(
            f"file:{STATE_DB}?mode=ro&busy_timeout=5000",
            uri=True, timeout=15,
        )
        try:
            cutoff = time.time() - STUCK_INACTIVITY_S
            row = con.execute(
                "SELECT 1 FROM messages m JOIN sessions s ON m.session_id = s.id "
                "WHERE s.id LIKE ? AND m.role IN (?, ?) AND m.timestamp >= ? LIMIT 1",
                (f"cron_{job_id}%", *PROGRESS_SIGNAL_ROLES, cutoff),
            ).fetchone()
            return row is not None
        finally:
            con.close()
    except Exception as exc:
        log(f"watchdog: job_progressing_now error: {exc}")
        return False


def session_row_if_incomplete(session_id):
    try:
        con = sqlite3.connect(
            f"file:{STATE_DB}?mode=ro&busy_timeout=5000",
            uri=True, timeout=15,
        )
        try:
            return con.execute(
                "SELECT id, started_at, ended_at FROM sessions "
                "WHERE id = ? AND (ended_at IS NULL OR end_reason IS NULL OR end_reason != 'cron_complete')",
                (session_id,),
            ).fetchone()
        finally:
            con.close()
    except Exception as exc:
        log(f"watchdog: session_row_if_incomplete error for {session_id}: {exc}")
        return None


def active_resume_session_for_job(job_id):
    """Return an incomplete session currently owned by a live --resume child."""
    prefix = f"cron_{job_id}_"
    for child in list_resume_children():
        session_id = child.get("session_id")
        if session_id and session_id.startswith(prefix):
            row = session_row_if_incomplete(session_id)
            if row:
                return row
    return None


def latest_incomplete_session(job_id):
    """Return the best session to push toward completion for job_id, or None.

    Prefer a session that already has a live watchdog --resume child. PRX/backend
    jobs can have multiple open cron sessions when the scheduler fires while an
    older session is being repaired; selecting only the newest open row hides the
    actually-running repaired session and lets the job look healthy while work is
    still unfinished.
    """
    active = active_resume_session_for_job(job_id)
    if active:
        return active
    try:
        con = sqlite3.connect(
            f"file:{STATE_DB}?mode=ro&busy_timeout=5000",
            uri=True, timeout=15,
        )
        try:
            row = con.execute(
                "SELECT id, started_at, ended_at FROM sessions "
                "WHERE id LIKE ? AND ended_at IS NULL "
                "ORDER BY started_at DESC LIMIT 1",
                (f"cron_{job_id}%",),
            ).fetchone()
            if row:
                return row
            row = con.execute(
                "SELECT id, started_at, ended_at FROM sessions "
                "WHERE id LIKE ? AND (end_reason IS NULL OR end_reason != 'cron_complete') "
                "ORDER BY started_at DESC LIMIT 1",
                (f"cron_{job_id}%",),
            ).fetchone()
            return row
        finally:
            con.close()
    except Exception as exc:
        log(f"watchdog: latest_incomplete_session error: {exc}")
        return None


def latest_completed_session(job_id):
    """Most recent cron_complete session for job_id (for ledger Done marking)."""
    try:
        con = sqlite3.connect(
            f"file:{STATE_DB}?mode=ro&busy_timeout=5000",
            uri=True, timeout=15,
        )
        try:
            return con.execute(
                "SELECT id, started_at, ended_at FROM sessions "
                "WHERE id LIKE ? AND end_reason = 'cron_complete' "
                "ORDER BY started_at DESC LIMIT 1",
                (f"cron_{job_id}%",),
            ).fetchone()
        finally:
            con.close()
    except Exception as exc:
        log(f"watchdog: latest_completed_session error: {exc}")
        return None


def last_message_is_error(session_id):
    try:
        con = sqlite3.connect(
            f"file:{STATE_DB}?mode=ro&busy_timeout=5000",
            uri=True, timeout=15,
        )
        try:
            row = con.execute(
                "SELECT content FROM messages WHERE session_id = ? "
                "AND role = 'assistant' ORDER BY timestamp DESC LIMIT 1",
                (session_id,),
            ).fetchone()
            if not row:
                return False
            return bool(ERROR_RE.search(row[0] or ""))
        finally:
            con.close()
    except Exception:
        return False


def last_assistant_text(session_id):
    try:
        con = sqlite3.connect(
            f"file:{STATE_DB}?mode=ro&busy_timeout=5000",
            uri=True, timeout=15,
        )
        try:
            row = con.execute(
                "SELECT content FROM messages WHERE session_id = ? "
                "AND role = 'assistant' ORDER BY timestamp DESC LIMIT 1",
                (session_id,),
            ).fetchone()
            return (row[0] or "") if row else ""
        finally:
            con.close()
    except Exception:
        return ""


def session_is_gate_pause(session_id):
    """A prior no-executable/gate-pause session is complete audit evidence, not
    pending work to keep nudging forever.
    """
    return bool(GATE_PAUSE_RE.search(last_assistant_text(session_id)))


def session_is_settled(session_id):
    text = last_assistant_text(session_id)
    return bool(text and SETTLED_SESSION_RE.search(text) and not ERROR_RE.search(text))


def ledger_story_ids(ledger_path):
    if not ledger_path or not Path(ledger_path).exists():
        return None
    key = str(Path(ledger_path))
    if key in _LEDGER_STORY_CACHE:
        return _LEDGER_STORY_CACHE[key]
    try:
        import csv
        with open(ledger_path, newline="", encoding="utf-8-sig") as f:
            ids = {r.get("Story ID", "").strip() for r in csv.DictReader(f) if r.get("Story ID")}
    except Exception as exc:
        log(f"watchdog: could not read ledger story ids from {ledger_path}: {exc}")
        ids = set()
    _LEDGER_STORY_CACHE[key] = ids
    return ids


def done_candidate_has_close_evidence(record):
    """Conservative quality gate for watchdog Done promotion.

    The watchdog may promote Done Candidate -> Done only when the row carries a
    real artifact path and a verification command/result, and does not still
    advertise unresolved blockers. This prevents scheduler success or weak
    analysis rows from becoming dependency-satisfying Done rows.
    """
    files = record.get("Files Changed", "") or ""
    verified = record.get("What Was Verified", "") or ""
    gaps = record.get("Known Gaps", "") or ""
    notes = record.get("Notes", "") or ""
    if not DONE_CANDIDATE_ARTIFACT_RE.search(files):
        return False, "missing real artifact/source path in Files Changed"
    if not DONE_CANDIDATE_VERIFY_RE.search(verified):
        return False, "missing verification command/result in What Was Verified"
    if DONE_CANDIDATE_BLOCKER_RE.search("\n".join([gaps, notes])):
        return False, "row still records blocker/remaining gap"
    return True, "artifact and verification evidence present"


def extract_story_id(session_id, ledger_path=None):
    """Find a story id (MOB-XXX-NNN) in the session's first user message."""
    try:
        con = sqlite3.connect(
            f"file:{STATE_DB}?mode=ro&busy_timeout=5000",
            uri=True, timeout=15,
        )
        try:
            row = con.execute(
                "SELECT content FROM messages WHERE session_id = ? AND role = 'user' "
                "ORDER BY timestamp ASC LIMIT 1",
                (session_id,),
            ).fetchone()
            if row:
                matches = []
                for m in STORY_RE.finditer(row[0] or ""):
                    sid = m.group(1)
                    if sid in PLACEHOLDER_STORY_IDS:
                        continue
                    matches.append(sid)
                if ledger_path:
                    valid = ledger_story_ids(ledger_path)
                    if valid is not None:
                        matches = [sid for sid in matches if sid in valid]
                unique = []
                for sid in matches:
                    if sid not in unique:
                        unique.append(sid)
                return unique[0] if len(unique) == 1 else None
        finally:
            con.close()
    except Exception:
        return None
    return None


def mark_ledger_done(story_id, ledger_path=None):
    """Truthfully promote Done Candidate -> Done after a completed session.

    Do not blind-flip In Progress/To Do rows just because a cron session ended:
    recent mobile runs showed gate-pause and re-verification sessions can close
    cleanly while the story remains externally blocked.
    """
    return False  # IndiaWorks finalizer exclusively owns evidence-gated ledger updates.
    ledger = Path(ledger_path) if ledger_path else LEDGER
    if not story_id or not ledger.exists():
        return False
    try:
        import csv
        rows = list(csv.reader(open(ledger, newline="", encoding="utf-8")))
        header = rows[0]
        status_idx = header.index("Status") if "Status" in header else 8
        updated = False
        out = []
        for r in rows:
            if r and r[0] == story_id:
                current = r[status_idx].strip()
                if current.lower() == "done":
                    out.append(r)
                    continue
                if current.lower() != "done candidate":
                    log(f"watchdog: not marking ledger {story_id} -> Done; current status is {current!r}, not Done Candidate")
                    out.append(r)
                    continue
                record = {header[i]: (r[i] if i < len(r) else "") for i in range(len(header))}
                ok, reason = done_candidate_has_close_evidence(record)
                if not ok:
                    log(f"watchdog: not promoting {story_id} Done Candidate -> Done: {reason}")
                    out.append(r)
                    continue
                r[status_idx] = "DONE"
                updated = True
            out.append(r)
        if updated and not DRY_RUN:
            with open(ledger, "w", newline="", encoding="utf-8") as f:
                csv.writer(f, lineterminator="\n").writerows(out)
            log(f"watchdog: marked ledger {story_id} -> DONE (session completed)")
        elif updated:
            log(f"watchdog[DRY-RUN]: would mark ledger {story_id} -> DONE")
        return updated
    except Exception as exc:
        log(f"watchdog: mark_ledger_done failed: {exc}")
        return False


# --- wedged child process state (per-session, shared across jobs) ------------
def load_state(state_file):
    try:
        return json.loads(state_file.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return {}


def save_state(state_file, state):
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    state_file.write_text(json.dumps(state, indent=1), encoding="utf-8")


def _resume_child_key(session_id):
    return f"resume_pid::{session_id}"


def _resume_child_alive(pid):
    """True if a previously-spawned --resume child PID is still running."""
    if not pid:
        return False
    try:
        out = subprocess.run(
            ["tasklist", "//FI", f"PID eq {pid}"],
            capture_output=True, text=True, timeout=10,
        ).stdout
        return str(pid) in out
    except Exception:
        return False


def _kill_resume_child(pid):
    return False  # Never kill unrelated/shared-home session workers.
    if not pid:
        return
    if DRY_RUN:
        log(f"watchdog[DRY-RUN]: would kill stale/wedged --resume child {pid}")
        return
    try:
        subprocess.run(["taskkill", "//F", "//PID", str(pid)],
                       capture_output=True, timeout=10)
        log(f"watchdog: killed wedged prior --resume child {pid}")
    except Exception as exc:
        log(f"watchdog: could not kill prior --resume child {pid}: {exc}")


def _parse_resume_session_id(commandline):
    m = re.search(r"--resume\s+['\"]?(cron_[^\s'\"]+)", commandline or "")
    return m.group(1) if m else None


def list_resume_children():
    """Return active Hermes `--resume cron_*` children with pid/session metadata."""
    if os.name != "nt":
        try:
            out = subprocess.run(["pgrep", "-af", "--resume cron_"],
                                 capture_output=True, text=True, timeout=20).stdout
            children = []
            for line in out.splitlines():
                parts = line.strip().split(maxsplit=1)
                if not parts or not parts[0].isdigit():
                    continue
                cmdline = parts[1] if len(parts) > 1 else ""
                children.append({
                    "pid": int(parts[0]),
                    "commandline": cmdline,
                    "session_id": _parse_resume_session_id(cmdline),
                })
            return children
        except Exception:
            return []
    # Prefer CIM over deprecated WMIC and parse JSON so command lines with spaces
    # remain intact. Restrict to Hermes.exe so the watchdog's own PowerShell
    # command line does not match its search pattern.
    ps = (
        "$p = Get-CimInstance Win32_Process | "
        "Where-Object { ($_.Name -ieq 'Hermes.exe' -or $_.Name -ieq 'hermes.exe') "
        "-and $_.CommandLine -like '*--resume cron_*' }; "
        "$p | Select-Object ProcessId,CommandLine,CreationDate | ConvertTo-Json -Compress"
    )
    try:
        out = subprocess.run(["powershell", "-NoProfile", "-Command", ps],
                             capture_output=True, text=True, timeout=20).stdout.strip()
        if not out:
            return []
        data = json.loads(out)
        if isinstance(data, dict):
            data = [data]
        children = []
        for item in data:
            cmdline = item.get("CommandLine") or ""
            pid = item.get("ProcessId")
            if not pid:
                continue
            children.append({
                "pid": int(pid),
                "commandline": cmdline,
                "session_id": _parse_resume_session_id(cmdline),
            })
        return children
    except Exception as exc:
        log(f"watchdog: enumerate job resume procs failed: {exc}")
        return []


def any_job_resume_child_alive():
    """True if a fresh `hermes chat --resume cron_` child is currently alive.

    Older watchdog builds returned true forever when a single resume child wedged,
    so every watched job backed off on every tick. This version classifies each
    active child by transcript inactivity and kills only stale resume children;
    fresh children still preserve the global one-child DB-lock guard.
    """
    alive = False
    for child in list_resume_children():
        pid = child.get("pid")
        session_id = child.get("session_id")
        inactive_s = session_inactive_seconds(session_id) if session_id else None
        if inactive_s is not None and inactive_s >= RESUME_CHILD_STALE_S:
            log(f"watchdog: stale global --resume child {pid} for {session_id} "
                f"inactive {int(inactive_s/60)}m >= {int(RESUME_CHILD_STALE_S/60)}m; killing")
            _kill_resume_child(pid)
            continue
        alive = True
    return alive


def nudge(session_id, last_error, reason, job_id):
    """Resume the session, persisting across ticks (backoff, never silent give-up).

    Logic:
      - Global guard: if any --resume child for ANY watched job is alive, skip
        (prevents the state.db lock pile-up that caused 'session storage could not
        be written').
      - Per-session in-flight guard (resume_pid::<session>) prevents duplicate
        children for the same session.
      - Track first_failure_at. Back off after GIVEUP_AFTER_S; flag 'needs review'
        but keep nudging (once/hour) so a stuck run is never silently abandoned.
    """
    return False  # IndiaWorks continues through the canonical, expected-story host below.
    now = datetime.now(timezone.utc)
    state_file = STATE_DIR / f"watch_{job_id}.json"
    state = load_state(state_file)
    entry = state.get(session_id, {})
    attempts = entry.get("continue_attempts", 0) + 1
    first_failure = entry.get("first_failure_at")
    if not first_failure:
        first_failure = now.isoformat()
        entry["first_failure_at"] = first_failure

    # GLOBAL resume-child guard: never have more than ONE --resume child for any
    # watched job in flight. Under DELETE journal mode two concurrent children
    # wedge the single DB write lock -> 'session storage could not be written'.
    if any_job_resume_child_alive():
        log(f"watchdog: a --resume child for a watched job is already alive; backing "
            f"off this tick (will retry next tick) to avoid DB lock contention.")
        entry["last_attempt_at"] = now.isoformat()
        state[session_id] = entry
        save_state(state_file, state)
        return attempts

    # Storage/lock backoff: after a storage failure we wait STORAGE_BACKOFF_S before
    # retrying, so the gateway can finish its long writes (context compression etc.)
    # instead of us re-wedging the lock every 3 minutes.
    last_storage = parse_iso(entry.get("last_storage_fail_at"))
    if last_storage and (now - last_storage).total_seconds() < STORAGE_BACKOFF_S:
        remaining = STORAGE_BACKOFF_S - (now - last_storage).total_seconds()
        log(f"watchdog: storage backoff active for {session_id} "
            f"(retry in {remaining/60:.0f}m) -- letting gateway finish DB writes.")
        entry["last_attempt_at"] = now.isoformat()
        state[session_id] = entry
        save_state(state_file, state)
        return attempts

    first_ts = parse_iso(first_failure) or now
    failed_for_s = (now - first_ts).total_seconds()

    if failed_for_s >= GIVEUP_AFTER_S:
        last_nudge = parse_iso(entry.get("last_attempt_at"))
        if last_nudge and (now - last_nudge).total_seconds() < GIVEUP_NUDGE_MIN * 60:
            log(f"watchdog: {session_id} has failed {int(failed_for_s/3600)}h; backing off "
                f"(next nudge in {(GIVEUP_NUDGE_MIN*60 - (now-last_nudge).total_seconds())/60:.0f}m). "
                f"Flagged for human review but NOT abandoned.")
            entry["needs_review"] = True
            state[session_id] = entry
            save_state(state_file, state)
            return attempts
        if not entry.get("flagged_review"):
            entry["needs_review"] = True
            entry["flagged_review"] = True
            log(f"watchdog: {session_id} failed >{int(GIVEUP_AFTER_S/3600)}h -- flag for human "
                f"review, but watchdog keeps nudging (completion is still the goal).")

    resume_msg = (
        f"continue  [auto-resumed by cron watchdog, nudge {attempts} "
        f"after: {reason}: {last_error[:120]}]"
    )
    if DRY_RUN:
        log(f"watchdog[DRY-RUN]: would send 'continue' to {session_id} (nudge {attempts})")
        entry["continue_attempts"] = attempts
        entry["last_attempt_at"] = now.isoformat()
        state[session_id] = entry
        save_state(state_file, state)
        return attempts

    # Per-session in-flight guard.
    rkey = _resume_child_key(session_id)
    prior_pid = state.get(rkey)
    if _resume_child_alive(prior_pid):
        log(f"watchdog: skip 'continue' for {session_id} -- a prior --resume child "
            f"(pid {prior_pid}) is still alive; will retry next tick.")
        return attempts
    if prior_pid:
        _kill_resume_child(prior_pid)
        state.pop(rkey, None)

    if os.name == "nt":
        hermes_exe = str(HERMES_HOME / "hermes-agent" / "venv" / "Scripts" / "hermes.exe")
        if not Path(hermes_exe).exists():
            hermes_exe = "hermes"
        cmd = [hermes_exe, "chat", "--resume", session_id, "-Q", "-q", resume_msg]
        creationflags = 0x08000000  # CREATE_NO_WINDOW
    else:
        cmd = ["hermes", "chat", "--resume", session_id, "-Q", "-q", resume_msg]
        creationflags = 0

    log(f"watchdog: nudging (continue) {session_id} -- nudge {attempts} "
        f"(failed {int(failed_for_s/60)}m, reason: {reason})")
    with open(STATE_DIR / f"watch_{job_id}.log", "ab") as log_fh:
        log_fh.write(f"===== {now.isoformat()} nudge {attempts} on {session_id} =====\n".encode())
        child = subprocess.Popen(
            cmd, stdout=log_fh, stderr=subprocess.STDOUT,
            stdin=subprocess.DEVNULL, creationflags=creationflags, shell=False,
        )
    state[rkey] = child.pid
    entry["continue_attempts"] = attempts
    entry["last_attempt_at"] = now.isoformat()
    state[session_id] = entry
    if len(state) > 24:
        for k in sorted(state, key=lambda k: state[k].get("last_attempt_at", "") if isinstance(state[k], dict) else ""):
            if k.startswith("resume_pid::"):
                continue
            state.pop(k, None)
            if len(state) <= 24:
                break
    save_state(state_file, state)
    return attempts


def db_writable():
    """Quick write probe on the live state.db (BEGIN IMMEDIATE in rw mode)."""
    try:
        con = sqlite3.connect(
            f"file:{STATE_DB}?mode=rw&busy_timeout=3000", uri=True, timeout=5)
        try:
            con.execute("BEGIN IMMEDIATE")
            con.rollback()
            return True
        finally:
            con.close()
    except Exception as exc:
        log(f"watchdog: db_writable probe failed: {exc}")
        return False


def latest_stuck_session(job_id):
    """Return the latest OPEN cron_<job_id> session with no recent activity."""
    try:
        con = sqlite3.connect(
            f"file:{STATE_DB}?mode=ro&busy_timeout=5000",
            uri=True, timeout=15,
        )
        try:
            row = con.execute(
                "SELECT id, started_at, ended_at FROM sessions "
                "WHERE id LIKE ? AND ended_at IS NULL "
                "ORDER BY started_at DESC LIMIT 1",
                (f"cron_{job_id}%",),
            ).fetchone()
            if not row:
                return None
            session_id = row[0]
            last_msg = con.execute(
                "SELECT MAX(timestamp) FROM messages WHERE session_id = ?",
                (session_id,),
            ).fetchone()[0]
            now = time.time()
            if last_msg is None:
                inactive_s = now - float(row[1])
            else:
                inactive_s = now - float(last_msg)
            if inactive_s >= STUCK_INACTIVITY_S:
                return (session_id, inactive_s)
            return None
        finally:
            con.close()
    except Exception as exc:
        log(f"watchdog: latest_stuck_session error: {exc}")
        return None


# Classification patterns for the last error string from jobs.json.
# STORAGE_RE: lock / disk / DB-write contention. These are TRANSIENT under the
# gateway's DELETE journal mode -- a resume child hits BEGIN IMMEDIATE and fails
# when the gateway is mid-write. We back off, clear wedged procs, then retry.
STORAGE_RE = re.compile(
    r"session storage could not be written|could not be written|database table is locked|"
    r"database is locked|database or disk is full|no space left on device|disk i/o error|"
    r"locking protocol|SQLITE_BUSY|database is closed|readonly database",
    re.IGNORECASE,
)
# TRANSIENT_RE: throttling / temporary unavailability -- safe to continue through.
TRANSIENT_RE = re.compile(
    r"rate.?limit|429|too many requests|quota|timeout|timed out|temporarily|try again|"
    r"service unavailable|503|connection reset|econnreset|socket hang",
    re.IGNORECASE,
)
# ERROR_RE used by last_message_is_error
ERROR_RE = re.compile(
    r"Traceback|Error:|RuntimeError|Exception|failed|could not be written|"
    r"session storage", re.IGNORECASE)


# --- jobs config --------------------------------------------------------------
def _jobs_json_records():
    try:
        data = json.loads((HERMES_HOME / "cron" / "jobs.json").read_text(encoding="utf-8"))
        jobs = data["jobs"] if isinstance(data, dict) and "jobs" in data else data
        if isinstance(jobs, dict):
            jobs = list(jobs.values())
        return jobs if isinstance(jobs, list) else []
    except Exception as exc:
        log(f"watchdog: could not read jobs.json for discovery: {exc}")
        return []


def _extract_ledger_from_job(job):
    prompt = job.get("prompt") or ""
    # Conservative Windows absolute CSV path extraction from cron prompts.
    for m in re.finditer(r"[A-Za-z]:[\\/][^\r\n\"']+?\.csv", prompt):
        candidate = m.group(0).rstrip("`),.; ")
        if Path(candidate).exists():
            return candidate
    return None


def _is_watchdog_job(job):
    script = (job.get("script") or "").replace("\\", "/").split("/")[-1].lower()
    name = (job.get("name") or "").lower()
    return script == Path(__file__).name.lower() or "watchdog cron continue" in name


def discover_watchable_cron_jobs():
    """Auto-discover recurring Hermes cron jobs so future jobs are covered.

    Explicit watch_jobs.json entries still win. Discovery skips the watchdog itself
    to avoid self-resume loops, and skips one-shot/completed records.
    """
    discovered = []
    for job in _jobs_json_records():
        job_id = job.get("id") or job.get("job_id")
        if not job_id or _is_watchdog_job(job):
            continue
        schedule = job.get("schedule") or {}
        if schedule.get("kind") not in ("cron", "interval"):
            continue
        if job.get("watchdog_managed") is False:
            continue
        cfg = {"job_id": job_id, "auto_discovered": True}
        ledger = _extract_ledger_from_job(job)
        if ledger:
            cfg["ledger"] = ledger
        discovered.append(cfg)
    return discovered


def load_watch_jobs():
    """Resolve watched jobs by merging config + all recurring Hermes cron jobs.

    Priority:
      1. --jobs-file <path> / $HERMES_HOME/cron/watch_jobs.json entries
      2. auto-discovered recurring jobs from jobs.json (default enabled)
      3. single DEFAULT_JOB_ID fallback if no jobs resolve
    """
    configured = []
    for i, arg in enumerate(sys.argv):
        if arg == "--jobs-file" and i + 1 < len(sys.argv):
            path = Path(sys.argv[i + 1])
            if not path.exists():
                log(f"watchdog: --jobs-file {path} not found; falling back to defaults")
                break
            try:
                data = json.loads(path.read_text(encoding="utf-8"))
                if isinstance(data, list):
                    configured = data
                else:
                    log(f"watchdog: --jobs-file {path} is not a JSON list; falling back to defaults")
            except Exception as exc:
                log(f"watchdog: --jobs-file {path} parse error: {exc}; falling back to defaults")
            break
    if not configured:
        default_file = HERMES_HOME / "cron" / "watch_jobs.json"
        if default_file.exists():
            try:
                data = json.loads(default_file.read_text(encoding="utf-8"))
                if isinstance(data, list):
                    configured = data
            except Exception as exc:
                log(f"watchdog: {default_file} parse error: {exc}; using discovered/default jobs")

    merged = {}
    for cfg in configured:
        job_id = cfg.get("job_id") or cfg.get("id") if isinstance(cfg, dict) else None
        if job_id:
            merged[str(job_id)] = dict(cfg)
    if AUTO_DISCOVER_CRON_JOBS:
        for cfg in discover_watchable_cron_jobs():
            job_id = str(cfg["job_id"])
            if job_id not in merged:
                merged[job_id] = cfg
            else:
                for k, v in cfg.items():
                    merged[job_id].setdefault(k, v)
    if not merged:
        merged[DEFAULT_JOB_ID] = {"job_id": DEFAULT_JOB_ID}
    return list(merged.values())


def hermes_cli_executable():
    exe = HERMES_HOME / "hermes-agent" / "venv" / "Scripts" / "hermes.exe"
    return str(exe) if exe.exists() else "hermes"


def job_is_paused(job):
    return (job.get("enabled") is False or job.get("state") == "paused" or bool(job.get("paused_at")))


def pause_allows_auto_resume(job):
    reason = (job.get("paused_reason") or "").strip()
    return not (reason and PAUSE_OPT_OUT_RE.search(reason))


def ensure_watchdog_self_schedule():
    """Repair accidental drift of this watchdog's own cron record.

    A stale writer or UI/CLI edit can leave the global watchdog as a one-shot or
    paused job; then it looks active in the list but never fires. When this script
    gets any chance to run, force its own durable record back to the expected
    recurring no-agent watchdog shape.
    """
    return False  # Scoped schedule repair below preserves manual pauses.
    job = load_job(WATCHDOG_JOB_ID)
    if not job:
        return False
    schedule = job.get("schedule") or {}
    needs_edit = (
        schedule.get("kind") != "cron"
        or schedule.get("expr") != EXPECTED_WATCHDOG_SCHEDULE
        or job.get("script") != Path(__file__).name
        or job.get("no_agent") is not True
        or job.get("deliver") != "local"
    )
    if not needs_edit and not job_is_paused(job):
        return False
    if DRY_RUN:
        log(f"watchdog[DRY-RUN]: would repair self schedule/enabled state for {WATCHDOG_JOB_ID}")
        return True
    try:
        if needs_edit:
            proc = subprocess.run(
                [hermes_cli_executable(), "cron", "edit", WATCHDOG_JOB_ID,
                 "--schedule", EXPECTED_WATCHDOG_SCHEDULE,
                 "--no-agent", "--script", Path(__file__).name, "--deliver", "local"],
                capture_output=True, text=True, timeout=30, check=False,
            )
            if proc.returncode != 0:
                detail = (proc.stderr or proc.stdout or "").strip().replace("\n", " ")[:400]
                log(f"watchdog: self schedule repair failed rc={proc.returncode}: {detail}")
                return False
        repaired = load_job(WATCHDOG_JOB_ID) or job
        if job_is_paused(repaired):
            proc = subprocess.run(
                [hermes_cli_executable(), "cron", "resume", WATCHDOG_JOB_ID],
                capture_output=True, text=True, timeout=30, check=False,
            )
            if proc.returncode != 0:
                detail = (proc.stderr or proc.stdout or "").strip().replace("\n", " ")[:400]
                log(f"watchdog: self resume repair failed rc={proc.returncode}: {detail}")
                return False
        log(f"watchdog: repaired self cron record {WATCHDOG_JOB_ID} -> {EXPECTED_WATCHDOG_SCHEDULE} no-agent local")
        return True
    except Exception as exc:
        log(f"watchdog: self schedule repair failed to start: {exc}")
        return False


def resume_paused_job(job_id, job, log_file):
    return False  # Never auto-unpause a human-paused IndiaWorks job.
    if not AUTO_RESUME_PAUSED_JOBS:
        log(f"watchdog[{job_id}]: job is paused/disabled; auto-resume disabled by env", log_file)
        return False
    if not pause_allows_auto_resume(job):
        log(f"watchdog[{job_id}]: job is paused with opt-out reason {job.get('paused_reason')!r}; not auto-resuming", log_file)
        return False
    if DRY_RUN:
        log(f"watchdog[DRY-RUN][{job_id}]: would resume paused recurring job", log_file)
        return True
    try:
        proc = subprocess.run(
            [hermes_cli_executable(), "cron", "resume", job_id],
            capture_output=True, text=True, timeout=30, check=False,
        )
    except Exception as exc:
        log(f"watchdog[{job_id}]: auto-resume failed to start: {exc}", log_file)
        return False
    if proc.returncode == 0:
        detail = (proc.stdout or proc.stderr or "").strip().replace("\n", " ")[:240]
        log(f"watchdog[{job_id}]: auto-resumed paused/disabled recurring job via hermes cron resume" + (f" ({detail})" if detail else ""), log_file)
        return True
    detail = (proc.stderr or proc.stdout or "").strip().replace("\n", " ")[:400]
    log(f"watchdog[{job_id}]: auto-resume failed rc={proc.returncode}: {detail}", log_file)
    return False


def process_job(job_cfg, log_file):
    """Run the full detect-and-nudge cycle for one watched job."""
    job_id = job_cfg.get("job_id") or job_cfg.get("id")
    if not job_id:
        log(f"watchdog: job config missing job_id: {job_cfg!r}", log_file)
        return
    ledger_path = job_cfg.get("ledger")
    stuck_minutes = int(job_cfg.get("stuck_minutes", STUCK_INACTIVITY_S // 60))
    stuck_s = stuck_minutes * 60

    job = load_job(job_id)
    if not job:
        log(f"watchdog: job {job_id} not found in jobs.json -- skipping", log_file)
        return

    if job_is_paused(job):
        resume_paused_job(job_id, job, log_file)
        job = load_job(job_id) or job

    # ---- completion loop: if no incomplete session, close the story + report.
    row = latest_incomplete_session(job_id)
    if not row:
        completed = latest_completed_session(job_id)
        if completed:
            story_id = extract_story_id(completed[0], ledger_path)
            if story_id:
                mark_ledger_done(story_id, ledger_path)
            log(f"watchdog[{job_id}]: all sessions complete; last completed {completed[0]}"
                + (f" (story {story_id})" if story_id else "")
                + " -- nothing to push, run healthy", log_file)
        else:
            log(f"watchdog[{job_id}]: no cron sessions yet; nothing to do", log_file)
        return

    session_id = row[0]
    last_error = job.get("last_error") or ""
    if session_is_gate_pause(session_id):
        log(f"watchdog[{job_id}]: {session_id} ended in a no-executable/gate-pause state; not nudging stale audit context", log_file)
        return
    if row[2] is not None and session_is_settled(session_id):
        log(f"watchdog[{job_id}]: {session_id} has a settled terminal report ({row[2]}); not nudging stale completed context", log_file)
        return

    if STORAGE_RE.search(last_error):
        state_file = STATE_DIR / f"watch_{job_id}.json"
        state = load_state(state_file)
        entry = state.get(session_id, {})
        entry["last_storage_fail_at"] = datetime.now(timezone.utc).isoformat()
        state[session_id] = entry
        save_state(state_file, state)
        kill_wedged_resume(session_id)
        storage_hygiene("storage-error")
        if db_writable() and not any_job_resume_child_alive():
            nudge(session_id, last_error,
                  "storage/lock error (DB writable after clearing wedged procs)", job_id)
        else:
            log(f"watchdog[{job_id}]: STORAGE/LOCK error and DB not yet writable (or a "
                f"resume child is still alive) -- backing off this tick.", log_file)
        return

    if job.get("last_status") == "error" and not TRANSIENT_RE.search(last_error):
        nudge(session_id, last_error, "non-transient error", job_id)
        return

    # ---- hung / silent: jobs.json may say ok but session is open+quiet ----
    if job_has_accepted_progress(job_id, job_cfg, stuck_s, log_file, session_id=session_id):
        return

    progressing = session_progressing_now(session_id, stuck_s)
    if progressing:
        log(f"watchdog[{job_id}]: {session_id} has recent assistant/tool activity but no accepted progress "
            f"(no repo diff/commit, validation output, or ledger transition in the "
            f"last {stuck_minutes}m) -- will not disturb unless this session is stuck", log_file)

    inactive_s = session_inactive_seconds(session_id)
    if inactive_s is not None and inactive_s >= stuck_s:
        nudge(session_id, last_error or "(no job error; session open & inactive)",
              f"stuck (open & inactive {int(inactive_s/60)}m)", job_id)
    elif progressing:
        return
    else:
        if not last_error:
            last_error = "(no job error; incomplete selected session, not currently active)"
        nudge(session_id, last_error, "incomplete selected run, not currently active", job_id)


def main():
    if not acquire_self_lock():
        return  # another watchdog tick is mid-run; do nothing this tick
    try:
        ensure_watchdog_self_schedule()
        storage_hygiene("proactive")

        live = live_db_size_bytes()
        if live >= SIZE_TRIGGER_BYTES:
            log(f"watchdog: WARN live state.db at {fmt_mb(live)} >= {SIZE_TRIGGER_MB} MB. "
                f"Attempting bounded retention maintenance.")
            state_retention_maintenance("size-trigger")

        jobs = load_watch_jobs()
        log(f"watchdog: tick -- watching {len(jobs)} job(s): "
            + ", ".join(str(j.get('job_id') or j.get('id')) for j in jobs))
        for job_cfg in jobs:
            job_id = job_cfg.get("job_id") or job_cfg.get("id")
            log_file = STATE_DIR / f"watch_{job_id}.log"
            try:
                process_job(job_cfg, log_file)
            except Exception as exc:
                log(f"watchdog[{job_id}]: process_job error: {exc}", log_file)
        # Repair again after child job resume/edit operations; this protects the
        # watchdog from stale concurrent jobs.json writers that clobber its own
        # schedule during the same tick.
        ensure_watchdog_self_schedule()
    finally:
        release_self_lock()



reference_main = main
# ===== IndiaWorks active adapters: preserve the full reference above =====
import argparse
import shutil
from contextlib import closing
from backlog_lib import eligible_todo_rows, find_active_sprint, json_dump_atomic
from execution_packet import core_root, inventory
import indiaworks_watchdog_controller as _indiaworks
from indiaworks_watchdog_controller import decision, recover


def configure_indiaworks(root, args):
    """Override every historical reference root before active diagnostics run."""
    global HERMES_HOME, STATE_DIR, SELF_LOCK, STATE_DB, RECOVERY_DIR
    global LEDGER, LEDGER_CANDIDATES, DEFAULT_JOB_ID, WATCHDOG_JOB_ID, DRY_RUN
    HERMES_HOME = Path(args.hermes_home).resolve()
    STATE_DIR = root / '.hermes/watchdog'
    SELF_LOCK = STATE_DIR / 'tick.lock'
    STATE_DB = HERMES_HOME / 'profiles/indiaworks/state.db'
    RECOVERY_DIR = HERMES_HOME / 'profiles/indiaworks/cron/recovery-backups'
    LEDGER = root / 'docs/backlog/IndiaWorks_Master_Backlog.csv'
    LEDGER_CANDIDATES = [LEDGER]
    DEFAULT_JOB_ID = args.job_id
    WATCHDOG_JOB_ID = args.watchdog_id
    DRY_RUN = args.dry_run


def _log_tail(path):
    """Bounded inspection only: never copy raw provider/tool output to telemetry."""
    if not path or not Path(path).is_file():
        return ''
    with Path(path).open('rb') as handle:
        handle.seek(max(0, Path(path).stat().st_size - 65536))
        return handle.read(65536).decode('utf-8', 'replace')


def _classify_error(text):
    if STORAGE_RE.search(text):
        return 'STORAGE_ERROR'
    if re.search(r'(?<![A-Za-z0-9-])(?:401|403)(?![A-Za-z0-9-])|invalid.api.key|unauthorized|authentication.failed', text, re.I):
        return 'AUTH_ERROR'
    if TRANSIENT_RE.search(text):
        return 'TRANSIENT_PROVIDER_ERROR'
    if re.search(r'context.*(?:length|limit|exceed)|compression.*(?:fail|error)|maximum.*tokens', text, re.I):
        return 'CONTEXT_COMPRESSION_ERROR'
    if ERROR_RE.search(text):
        return 'EXECUTOR_ERROR'
    return None


def _failure_excerpt(text):
    """Ignore instruction/code listings mentioning timeouts or error handling."""
    failure = re.compile(r'^\s*(?:E\s+)?(?:Traceback|Error\b|RuntimeError\b|Exception\b|APIError\b)|'
                         r'(?:HTTP(?:Error)?|status(?:_code)?)\s*[=: ]+\s*(?:401|403|429|5\d\d)|'
                         r'session storage could not be written|SQLITE_BUSY|no space left on device', re.I)
    return '\n'.join(line for line in text.splitlines() if failure.search(line))


def _owned_session(execution):
    """Find only this run's session in the isolated IndiaWorks profile, not siblings."""
    if not STATE_DB.exists():
        return None
    # Source marker is explicit for new executors. Older runs can use usage ID.
    usage = execution.get('usage_file')
    sid = json.loads(Path(usage).read_text()).get('session_id') if usage and Path(usage).is_file() else None
    with closing(sqlite3.connect(STATE_DB.as_uri() + '?mode=ro', uri=True, timeout=2)) as connection:
        if sid:
            row = connection.execute('SELECT id,started_at,ended_at FROM sessions WHERE id=?', (sid,)).fetchone()
        elif execution.get('session_source'):
            row = connection.execute('SELECT id,started_at,ended_at FROM sessions WHERE source=? AND started_at>=? ORDER BY started_at DESC LIMIT 1',
                                     (execution['session_source'], execution['started_at'] - 2)).fetchone()
        else:
            return None
        return row


def _profile_worker_present():
    """Read-only guard for manual/sibling workers in this same isolated profile."""
    command = ("$p = Get-CimInstance Win32_Process | Where-Object { "
               "($_.Name -match '^(python|pythonw|hermes)\\.exe$') -and "
               "($_.CommandLine -match '(?:^|\\s)-p\\s+indiaworks(?:\\s|$)') }; "
               "@($p).Count")
    checked = subprocess.run(['powershell', '-NoProfile', '-Command', command],
                             capture_output=True, text=True, timeout=15)
    if checked.returncode:
        raise RuntimeError('Cannot inspect IndiaWorks profile workers')
    return int(checked.stdout.strip() or '0') > 0


INDIAWORKS_FORBIDDEN_SOURCE_PREFIXES = (
    'src/web/src/web/',
    'src/web/src/web.backup/',
)

INDIAWORKS_NON_DELIVERY_PREFIXES = (
    '.hermes/',
    'cron/',
    'docs/',
    'src/web/.angular/',
    'src/web/dist/',
    'src/web/src/web/',
    'src/web/src/web.backup/',
)

INDIAWORKS_DELIVERY_EXTENSIONS = {
    '.cs',
    '.csproj',
    '.sln',
    '.slnx',
    '.ts',
    '.html',
    '.scss',
    '.css',
    '.json',
    '.sql',
    '.ps1',
    '.py',
    '.toml',
}

INDIAWORKS_DELIVERY_FILENAMES = {
    'IndiaWorks.slnx',
    'Directory.Build.props',
    'Directory.Packages.props',
    'package.json',
    'package-lock.json',
}


def _is_indiaworks_delivery_source(path):
    normalized = path.replace('\\', '/')
    if normalized.startswith(INDIAWORKS_NON_DELIVERY_PREFIXES):
        return False
    return Path(normalized).suffix.lower() in INDIAWORKS_DELIVERY_EXTENSIONS or normalized in INDIAWORKS_DELIVERY_FILENAMES


def _changed_delivery_files(root, current):
    before_path = current / 'SOURCE_BEFORE.json'
    if not before_path.exists():
        return []
    before = json.loads(before_path.read_text(encoding='utf-8'))
    after = inventory(core_root(root))
    return sorted(p for p in before.keys() | after.keys()
                  if before.get(p) != after.get(p) and _is_indiaworks_delivery_source(p))


def _nested_duplicate_files(root):
    found = []
    for prefix in INDIAWORKS_FORBIDDEN_SOURCE_PREFIXES:
        base = root / Path(prefix)
        if not base.exists():
            continue
        for path in sorted(p for p in base.rglob('*') if p.is_file()):
            found.append(path.relative_to(root).as_posix())
            if len(found) >= 20:
                return found
    return found


def _proof_summary(current):
    checks = []
    for path in sorted((current / 'verification').glob('*.json')):
        try:
            proof = json.loads(path.read_text(encoding='utf-8'))
            checks.append({'id': proof.get('proof_id'), 'exit': proof.get('exit_code'),
                           'acs': proof.get('acceptance_ids', [])})
        except (OSError, ValueError, TypeError):
            checks.append({'id': path.stem, 'exit': 'unreadable', 'acs': []})
    failed = [c for c in checks if c.get('exit') not in (0, '0')]
    passed = [c for c in checks if c.get('exit') in (0, '0')]
    return {'checks': checks, 'failed_count': len(failed), 'passed_count': len(passed)}


def _report_quality(current):
    report_path = current / 'REPORT.json'
    if not report_path.exists():
        return {'missing': True, 'useful': False, 'outcome': None, 'ac_count': 0}
    try:
        report = json.loads(report_path.read_text(encoding='utf-8'))
    except (OSError, ValueError, TypeError):
        return {'missing': False, 'useful': False, 'outcome': 'unreadable', 'ac_count': 0}
    summary = str(report.get('summary') or '').strip()
    ac_evidence = report.get('ac_evidence') or {}
    useful = bool(summary and summary != 'Not yet reported.') or bool(ac_evidence) or bool(report.get('blocker'))
    return {'missing': False, 'useful': useful, 'outcome': report.get('outcome'), 'ac_count': len(ac_evidence)}


def collect_indiaworks_health(root, args, job, rows, state, now):
    """Full-reference stall detectors adapted to IndiaWorks's host-owned lifecycle."""
    configure_indiaworks(root, args)
    flags = []
    health = {'flags': flags, 'profile_db': str(STATE_DB), 'retry_inhibited': None}
    current = root / '.hermes/current-run'
    metadata = current / 'EXECUTOR_STATE.json'
    execution = json.loads(metadata.read_text(encoding='utf-8')) if metadata.exists() else {}
    if execution.get('story_id'):
        health['current_story_id'] = execution['story_id']
    if execution.get('host'):
        health['current_child'] = execution['host']
    # A source digest catches further edits to an already-dirty/untracked file;
    # plain `git status` in the reference misses those. Proof changes also count.
    source_digest = _fingerprint(json.dumps(inventory(core_root(root)), sort_keys=True))
    proofs = sorted((p.name, p.stat().st_mtime_ns) for p in (current / 'verification').glob('*') if p.is_file())
    digest = _fingerprint(source_digest + json.dumps(proofs))
    delivery_files = _changed_delivery_files(root, current)
    health['changed_delivery_files'] = delivery_files[:40]
    nested_duplicates = _nested_duplicate_files(root)
    if nested_duplicates:
        flags.append('NESTED_SOURCE_DUPLICATES')
        health['nested_duplicate_files'] = nested_duplicates
        health['retry_inhibited'] = 'Nested Angular duplicate source files exist; reconcile into src/web before retry.'
    proof_status = _proof_summary(current)
    health['proof_summary'] = proof_status
    report_status = _report_quality(current)
    health['report_quality'] = report_status
    progress = state.setdefault('delivery_progress', {})
    run_key = (execution.get('story_id'), execution.get('started_at'))
    if progress.get('run_key') != list(run_key):
        progress.clear()
        progress.update(run_key=list(run_key), digest=digest, last_change=now)
    elif digest != progress.get('digest'):
        progress.update(digest=digest, last_change=now)
    # Reuse complete reference repo/progress filters as additional diagnostics.
    health['repo_progress'] = repo_progress_signal(args.job_id, root, str(LEDGER), state)
    health['seconds_without_delivery_change'] = max(0, now - progress['last_change'])
    session = None
    try:
        session = _owned_session(execution)
        if session:
            health['session_id'] = session[0]
            health['session_inactive_seconds'] = session_inactive_seconds(session[0])
            health['recent_delivery_signal'] = recent_session_progress_signal(args.job_id, 900, session_id=session[0])
            if health['recent_delivery_signal']:
                progress['last_change'] = now
            health['session_settled_text'] = session_is_settled(session[0])
            health['session_gate_pause_text'] = session_is_gate_pause(session[0])
            if session[2] is not None:
                flags.append('SESSION_ENDED_STORY_UNFINISHED')
            with closing(sqlite3.connect(STATE_DB.as_uri() + '?mode=ro', uri=True, timeout=2)) as connection:
                available = {r[1] for r in connection.execute('PRAGMA table_info(sessions)')}
                fields = [f for f in ['compression_failure_cooldown_until', 'compression_recovery_deadline',
                                     'compression_ineffective_count', 'compression_fallback_streak'] if f in available]
                if fields:
                    values = connection.execute('SELECT ' + ','.join(fields) + ' FROM sessions WHERE id=?', (session[0],)).fetchone()
                    health['compression_state'] = dict(zip(fields, values))
                    if (health['compression_state'].get('compression_failure_cooldown_until') or 0) > now:
                        flags.append('COMPRESSION_COOLDOWN_ACTIVE')
                    if (health['compression_state'].get('compression_ineffective_count') or 0) > 0:
                        flags.append('COMPRESSION_INEFFECTIVE')
            # Neither settled text nor cron_complete ever promotes IndiaWorks CSVs.
            error = _classify_error(_failure_excerpt(last_assistant_text(session[0])))
            if error:
                flags.append(error)
    except (sqlite3.Error, OSError, ValueError) as error:
        flags.append('SESSION_INSPECTION_UNAVAILABLE')
        health['session_inspection_error_type'] = type(error).__name__
    inactive = health.get('session_inactive_seconds')
    health['stalled'] = bool(execution and health['seconds_without_delivery_change'] >= 900 and inactive is not None and inactive >= 900)
    if execution and health['seconds_without_delivery_change'] >= 900:
        flags.append('NO_DELIVERY_PROGRESS')
    if health['stalled']:
        flags.append('OPEN_SESSION_SILENT')
    if execution and proof_status['failed_count'] >= 3 and not delivery_files:
        flags.append('FAILED_CHECK_CHURN_WITHOUT_DELIVERY')
    if execution and not delivery_files and proof_status['failed_count'] and not proof_status['passed_count']:
        flags.append('NO_STORY_DELIVERY_FILES')
    execution_done = bool(execution and execution.get('status') in {'EXITED', 'TIMED_OUT', 'STALLED'})
    if execution_done and not report_status['useful']:
        flags.append('UNREPORTED_OR_UNEVIDENCED_RUN')
    # Do not let a stale scheduler-level last_error poison the health of a new
    # active run. The executor log belongs to the current run; jobs.json
    # last_error belongs to the previous scheduler tick until Hermes overwrites it.
    current_error_text = _failure_excerpt(_log_tail(execution.get('log_file')))
    scheduler_error_text = str(job.get('last_error') or '')
    last_run = parse_iso(job.get('last_run_at'))
    master_backlog = root / 'docs/backlog/IndiaWorks_Master_Backlog.csv'
    scheduler_error_stale = bool(
        scheduler_error_text
        and last_run
        and master_backlog.exists()
        and master_backlog.stat().st_mtime > last_run.timestamp()
    )
    error_text = current_error_text if execution or scheduler_error_stale else scheduler_error_text
    if scheduler_error_stale:
        health['previous_scheduler_error_present'] = True
    elif execution and scheduler_error_text:
        health['previous_scheduler_error_present'] = True
    error = _classify_error(error_text)
    if error and error not in flags:
        flags.append(error)
    if job.get('last_status') == 'error' and not execution and not scheduler_error_stale:
        flags.append('ORIGINAL_CRON_FAILED')
    if not job.get('enabled') or job.get('state') == 'paused':
        flags.append('ORIGINAL_CRON_PAUSED')
    if job.get('enabled') and not job.get('next_run_at'):
        flags.append('ORIGINAL_NEXT_RUN_MISSING')
    if job.get('last_delivery_error'):
        flags.append('DELIVERY_FAILED')
    if job.get('last_delivery_unverified'):
        flags.append('DELIVERY_UNVERIFIED')
    # Disk/storage health is scoped to IndiaWorks; reference cleanup bodies stay
    # retained but disabled pending a separately reviewed retention tool.
    free = shutil.disk_usage(root).free
    health['disk_free_bytes'] = free
    if free < 512 * 1024 * 1024:
        flags.append('DISK_SPACE_LOW')
        health['retry_inhibited'] = 'Insufficient disk space; no new inference child until space recovers.'
    health['profile_db_bytes'] = live_db_size_bytes()
    if health['profile_db_bytes'] >= SIZE_TRIGGER_BYTES:
        flags.append('PROFILE_DB_SIZE_THRESHOLD')
    if error == 'AUTH_ERROR':
        health['retry_inhibited'] = 'Gateway authentication failed; correct credentials before retry.'
    if not execution and (root / '.hermes/run.lock').exists():
        flags.append('PREPARATION_OR_UNKNOWN_LOCK')
    review_flags = {
        'NESTED_SOURCE_DUPLICATES',
        'FAILED_CHECK_CHURN_WITHOUT_DELIVERY',
        'UNREPORTED_OR_UNEVIDENCED_RUN',
    }
    active_review_flags = sorted(review_flags.intersection(flags))
    if active_review_flags and not health.get('retry_inhibited'):
        health['retry_inhibited'] = '; '.join(active_review_flags)
    if active_review_flags and not execution:
        health['requires_review'] = '; '.join(active_review_flags)
    if error in {'STORAGE_ERROR', 'TRANSIENT_PROVIDER_ERROR'}:
        fingerprint = _fingerprint(error_text)
        prior = state.setdefault('error_backoff', {})
        if fingerprint != prior.get('fingerprint'):
            prior.update(fingerprint=fingerprint, retry_after=now + STORAGE_BACKOFF_S)
        if now < prior.get('retry_after', 0):
            health['retry_inhibited'] = error + ': 15-minute error backoff.'
    # Do not test writes while an executor can own session storage. The reference
    # write probe is a rolled-back lock acquisition, never DB mutation/repair.
    if STATE_DB.exists() and not execution and error == 'STORAGE_ERROR' and not args.dry_run:
        if not db_writable():
            flags.append('PROFILE_DB_WRITE_LOCKED')
            health['retry_inhibited'] = 'IndiaWorks profile storage write lock unavailable.'
    ticker = HERMES_HOME / 'cron/ticker_last_success'
    heartbeat = None
    if ticker.exists():
        try:
            heartbeat = float(ticker.read_text().strip())
        except ValueError:
            heartbeat = ticker.stat().st_mtime
    health['scheduler_age_seconds'] = None if heartbeat is None else max(0, now - heartbeat)
    scheduler_live = heartbeat is not None and now - heartbeat < 300
    if not scheduler_live:
        flags.append('SCHEDULER_HEARTBEAT_STALE')
    overdue = False
    scheduled = parse_iso(job.get('next_run_at'))
    if scheduled:
        overdue = now - scheduled.timestamp() > 300
    if overdue and job.get('enabled'):
        flags.append('ORIGINAL_CRON_OVERDUE')
    active = find_active_sprint(rows)
    pending = [r for r in rows if r['Sprint'] == active and r['Status'] == 'IN PROGRESS']
    health['trigger_original'] = bool(overdue and scheduler_live and job.get('enabled') and job.get('state') != 'paused'
        and not (root / '.hermes/run.lock').exists() and not pending and active
        and eligible_todo_rows(rows, active) and now >= state.get('retry_after', 0) and not health['retry_inhibited'])
    if not execution and (pending or health['trigger_original']):
        if _profile_worker_present():
            flags.append('INDIAWORKS_PROFILE_WORKER_ACTIVE')
            health['retry_inhibited'] = 'Another IndiaWorks profile executor is alive; avoid duplicate inference/storage writers.'
            health['trigger_original'] = False
    # Repair only active watchdog schedule/shape drift, using Hermes' native API.
    # Never force self/other paused jobs active or directly rewrite jobs.json.
    if args.watchdog_id:
        own = load_job(args.watchdog_id)
        if own and own.get('enabled'):
            schedule = own.get('schedule', {})
            drift = (schedule.get('kind') != 'interval' or schedule.get('minutes') != 3
                     or own.get('script') != 'indiaworks_continue_watchdog.py' or not own.get('no_agent'))
            if drift:
                flags.append('WATCHDOG_SCHEDULE_DRIFT')
                if not args.dry_run:
                    command = [args.hermes_python, args.hermes_main, 'cron', 'edit', args.watchdog_id,
                               '--schedule', 'every 3m', '--script', 'indiaworks_continue_watchdog.py', '--no-agent']
                    repaired = subprocess.run(command, capture_output=True, timeout=30)
                    health['self_schedule_repair_exit_code'] = repaired.returncode
                    if repaired.returncode:
                        flags.append('WATCHDOG_SCHEDULE_REPAIR_FAILED')
    health['flags'] = sorted(set(flags))
    return health


def main():
    """Active full-reference IndiaWorks entrypoint; never execute reference_main()."""
    try:
        return _indiaworks.main(collect_health=collect_indiaworks_health)
    except Exception as error:
        # Report structural/process-inspection failure without leaking raw logs,
        # prompts or credentials. Retain project story locks on uncertain state.
        print('IndiaWorks watchdog requires review: ' + type(error).__name__)
        raise


if __name__ == '__main__':
    raise SystemExit(main())
