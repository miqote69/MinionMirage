# Safety and Limitations

> **Early beta:** Minion Mirage `0.1.0-beta.1` changes native actor appearance backing and redraw state. The experimental summon feature also intercepts native minion actions. Game crashes and incorrect transient actor states cannot be ruled out.

## Safety boundaries

- Only a Companion owned by the local player is eligible for appearance replacement.
- Only mappings fixed in the plugin are applied.
- Minion Mirage does not inspect or operate Penumbra, Glamourer, or another plugin.
- The game remains responsible for Companion summon, follow, replacement, and dismissal.
- The plugin does not automatically respawn a dismissed Companion.
- A failed appearance operation stops further writes for the same failed actor identity.
- GPose appearance replacement targets only one uniquely resolved Companion representation and does not replace normal-field restoration tracking.
- Local runtime diagnostics are not uploaded automatically.

## Current limitations

- Mapping definitions cannot yet be edited by the user.
- Weapons are not replaced.
- An unowned minion cannot be summoned until unlocked on the current character.
- NPC equipment and appearance depend on current game data and may change after an FFXIV update.
- Actor recreation, territory changes, or incompatible external actor changes can affect appearance persistence or restoration.
- A GPose Companion remains unchanged when no unique matching representation is available.
- The experimental summon unlock may fail or crash in minion-prohibited areas.
- A successful build or `runtime-state.json` update does not prove visual correctness; the result must be checked in game.

## Experimental summon guidance

Enable the experimental switch only when you specifically need to test minions in an area where the game normally prohibits them. Turn it off when the test is finished. If summon, replacement, or dismissal behavior becomes abnormal, disable the switch and reload the Dev Plugin before continuing.
