"""Apply missing one-shot limits without editing the installed Hermes source."""
from __future__ import annotations

import os
import runpy
import sys
from pathlib import Path

from executor_limits import MAX_EXECUTOR_ITERATIONS, MAX_EXECUTOR_OUTPUT_TOKENS


def main() -> None:
    hermes_main = Path(sys.argv[1]).resolve()
    sys.argv = [str(hermes_main), *sys.argv[2:]]
    source = hermes_main.parent.parent
    # Resolve the isolated profile before importing Hermes/provider modules.
    os.environ['HERMES_HOME'] = str(source.parent / 'profiles' / 'indiaworks')
    sys.path.insert(0, str(source))
    import run_agent

    original = run_agent.AIAgent

    class BoundedAgent(original):
        def __init__(self, *args, **kwargs):
            kwargs.setdefault('max_iterations', MAX_EXECUTOR_ITERATIONS)
            kwargs.setdefault('max_tokens', MAX_EXECUTOR_OUTPUT_TOKENS)
            super().__init__(*args, **kwargs)

    # Installed oneshot omits these arguments and defaults to unlimited turns
    # and no output cap. Keep its normal auth/tools/usage/cleanup implementation.
    run_agent.AIAgent = BoundedAgent
    runpy.run_path(str(hermes_main), run_name='__main__')


if __name__ == '__main__':
    main()
