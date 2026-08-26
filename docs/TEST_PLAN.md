# Runtime state reporting test plan

## Objective and scope

Verify that the development plugin selects the fixed mapping for the summoned Companion, applies either Human or Monster appearance through the shared redraw lifecycle, and publishes a readable, atomic snapshot without moving Actor access outside the Dalamud Framework thread.

The fixed prototype mappings are:

- Companion `331` (`ファースト・ヤ・シュトラ`) to ENpcBase `1003782` (Human).
- Companion `232` (`マメット・スカアハ`) to BNpcBase `6479`, ModelChara `1689` (Monster).
- Companion `218` (`ニュー・アリゼー`) to ENpcBase `1017687` (Human).

NPC appearance correctness and visual acceptance are separate runtime checks.

## Environment and levels

- Static/build: Release build with Dalamud API 15.
- Runtime integration: real FF14 process, installed Dalamud Dev Plugin, standard plugin config directory.
- Observation: background reader opens `runtime-state.json`; it never accesses Actor pointers or invokes game actions.

## Entry, suspension, and exit

- Entry: source builds and the Dev Plugin loads.
- Suspend on plugin load failure, missing state path, invalid JSON, stale heartbeat over 5 seconds while Framework updates continue, or state-writer error.
- Exit: all three mappings, wrong-minion, Human/Monster verification, apply/failure, despawn, and unload states are distinguishable; build regression passes.

## Evidence and authority

Record source commit, DLL SHA-256, build output, Dalamud load log, state-file path/content/timestamps, and user visual observation separately. Build and JSON evidence do not prove visual success.
