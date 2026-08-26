# Runtime state reporting test plan

## Objective and scope

Verify that the development plugin publishes a readable, atomic snapshot of local-player, Companion selection, ownership, draw/model, tracking, apply, failure, and disposal state without moving Actor access outside the Dalamud Framework thread.

The fixed prototype mapping remains Companion `91` to ENpcBase `1003782`. NPC appearance correctness and visual acceptance are separate runtime checks.

## Environment and levels

- Static/build: Release build with Dalamud API 15.
- Runtime integration: real FF14 process, installed Dalamud Dev Plugin, standard plugin config directory.
- Observation: background reader opens `runtime-state.json`; it never accesses Actor pointers or invokes game actions.

## Entry, suspension, and exit

- Entry: source builds and the Dev Plugin loads.
- Suspend on plugin load failure, missing state path, invalid JSON, stale heartbeat over 5 seconds while Framework updates continue, or state-writer error.
- Exit: nominal, wrong-minion, target, apply/failure, despawn, and unload states are distinguishable; build regression passes.

## Evidence and authority

Record source commit, DLL SHA-256, build output, Dalamud load log, state-file path/content/timestamps, and user visual observation separately. Build and JSON evidence do not prove visual success.
