# Troubleshooting and Bug Reports

## The plugin does not load

1. Confirm that the Release build completed without errors.
2. Confirm that the Dev Plugin path points to `bin/Release/MinionMirage.dll`.
3. Confirm that the build uses Dalamud API 15.
4. Check the Dalamud log for the first Minion Mirage load error.

## A supported minion is not transformed

1. Confirm that the minion is owned and summoned by the local player.
2. Confirm that its individual mapping is ON.
3. If it has multiple targets, confirm the selected target.
4. Dismiss and summon the minion once through the normal game icon.
5. Check `runtime-state.json` for `selectionState`, `tracked.stage`, and `lastError`.

## The minion is shown as Not owned

Minion Mirage uses the current character's game unlock state. Unlock the minion normally, then reopen the configuration window. The plugin does not grant minion ownership.

## Experimental summon icons are active but no minion appears

The area may reject both the normal action and the local Companion transition. Turn the experimental switch off, reload the Dev Plugin, and reproduce once before collecting diagnostics.

## The appearance flickers, resizes, or fails to restore

Disable the affected mapping while the Companion is still present. If the problem remains, unload Minion Mirage and dismiss the minion normally. Do not repeatedly toggle the experimental summon switch during diagnosis.

## Information to include in a bug report

- Minion Mirage version and commit SHA
- Source minion name and Companion RowId
- Selected NPC target and RowId
- Whether the minion was summoned normally or through the experimental feature
- Territory and whether minions are normally prohibited there
- Relevant Dalamud log lines
- The current `runtime-state.json`
- A screenshot or short description of the visible result

Create reports in the private repository's [Issues](https://github.com/miqote69/MinionMirage/issues) page. Review diagnostic files before attaching them.
