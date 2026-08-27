# Frequently Asked Questions

## Does Minion Mirage require Penumbra or Glamourer?

No. Minion Mirage is standalone and does not inspect or invoke either plugin.

## Does it create a separate NPC actor?

No. It reuses the local player's existing Companion actor and changes its appearance. The game still owns summon, follow, replacement, and dismissal behavior.

## Why is a minion gray and labeled Not owned?

The current character has not unlocked that minion. The mapping remains visible in official game-data order, but its target and individual switch cannot be changed until the minion is owned.

## Can I add my own minion or NPC mapping?

Not in `0.1.0-beta.1`. Mappings are currently fixed in the plugin source.

## Why do some minions have a target selector?

Those minions have two or more verified NPC candidates. Minions with one target show only the fixed target.

## Does Minion Mirage replace weapons?

No. Weapon replacement is outside the current beta scope.

## What does the scale value mean?

It is a per-mapping multiplier written to the recreated NPC draw model. `Default` mappings leave the target's model scale unchanged by Minion Mirage.

## What does Enable minion summon (experimental) do?

It attempts to enable every minion icon and summon or dismiss the clicked minion in areas where the game normally prohibits minions. The feature may fail or crash the game.

## Does the experimental feature automatically respawn my minion?

No. It acts only when you click a game minion icon and does not queue, retry, or respawn a dismissed minion.

## Does Minion Mirage send logs or player data anywhere?

No. `runtime-state.json` remains in the local Dalamud plugin configuration directory and is not uploaded automatically.
