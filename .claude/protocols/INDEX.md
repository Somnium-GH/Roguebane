# Protocol index

Not hot-path. `loop.md` links here; don't inline these into loop.md/STATUS.md/CLAUDE.md.

| Doc | Read when |
|---|---|
| `anti_block.md` | A gate/check fails and you're deciding STOP vs PARK-and-continue. |
| `drift_guardrails.md` | A change would touch `ui_baseline.json`, gate thresholds, masks, or drives. |
| `metrics.md` | Appending a run to `metrics.csv`, or querying it to tune slice size. |
| `metrics.csv` | Raw data — one row per completed loop run/slice. Greppable, no prose. |
| `RETRO.md` | Onboarding a new agent to this project's loop history, or adding a new retro entry. |
