# MinionToNPC

Local-first Dalamud plugin prototype.

## Initial scope

- Detect `Companion RowId 91` (`wind-up Y'shtola`) when summoned by the local player.
- Replace only that Companion's local draw appearance with `ENpcBase RowId 1003782` (`Y'shtola`).
- Keep the original Companion entity and its follow/despawn lifecycle.
- Restore or clean up safely when the Companion disappears or the plugin unloads.

The initial prototype has no settings UI, multiple mappings, weapon replacement, appearance sync, or public release.

## Prototype acceptance

- The fixed target Companion is redrawn as a Human using the fixed Y'shtola appearance.
- A non-target Companion is not modified.
- The original Companion entity remains responsible for following and despawning.
- Unloading the plugin restores a still-present target through the same redraw path.
- Any failed apply performs one rollback attempt and does not retry the same actor.

## Status

Source implementation is present. Release build and in-game runtime acceptance are tracked separately.
