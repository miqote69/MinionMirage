# Feature Guide

## How appearance replacement works

Minion Mirage waits for a supported Companion owned by the local player to appear in the game object table. It then resolves the selected target and reuses the existing Companion actor for the NPC appearance.

- Human Battle NPC targets use their NPC customization and equipment data.
- Event NPC targets use their Event NPC customization and NPC-specific equipment data, including Young NPC bodies.
- DemiHuman targets use their model, customization, and equipment data.
- Monster targets use their model-only appearance path.

The game remains responsible for summon, follow, replacement, and dismissal behavior.

## GPose

When GPose creates a separate visible representation for the summoned Companion, Minion Mirage resolves it from the normally tracked Companion and applies the same selected NPC appearance and mapping-specific scale. Normal-field tracking and its captured restoration data remain separate and resume after leaving GPose.

Minion Mirage writes only when one unique GPose representation can be resolved. While the representation is unavailable or ambiguous, it waits without changing another actor.

## Configuration window

- **Cards / List:** switch between the two layouts with the view icons.
- **Search:** searches localized minion names and NPC target names.
- **Categories:** filters Adult Human, Young Human, DemiHuman, and Monster mappings.
- **Individual switch:** enables or disables one source-minion mapping.
- **Enable all / Disable all:** changes every mapping together.
- **Target selector:** appears only when a minion has multiple supported NPC targets.
- **Settings → UI language:** selects Automatic, English, Japanese, German, or French.

Mappings follow the game's `Companion.Order` value. Unowned minions remain in that sequence but are subdued, labeled **Not owned**, and cannot be changed individually until unlocked.

## Supported mappings

| Source minion | Companion Row | NPC target(s) | Category | Scale |
| --- | ---: | --- | --- | ---: |
| Y'shtola | 331 | BattleNpc 13910 | Human | 1.00 |
| Scathach | 232 | BattleNpc 6479 / ModelChara 1689 | Monster | 0.50 |
| Alisaie | 218 | EventNpc 1017687 | Human | 0.97 |
| Gaia | 398 | BattleNpc 17830 / ModelChara 4436 | DemiHuman | Default |
| Pelupelu | 534 | EventNpc 1046564 | Human | Default |
| Fran | 325 | EventNpc 1025589 / ModelChara 2382 | DemiHuman | Default |
| Zhloe | 298 | EventNpc 1015912 or 1044638 | Human | Default |
| Automaton 2B | 394 | EventNpc 1033925 / ModelChara 2810 | Human | Default |
| Automaton 2P | 395 | BattleNpc 11366 / ModelChara 2810 | Human | Default |
| Minfilia | 98 | EventNpc 1006573 or BattleNpc 13753 | Human | Default |
| Khloe | 260 | EventNpc 1012445 or 1058181 | Young Human | 0.70 |
| Ryne | 332 | EventNpc 1033894 or BattleNpc 10069 | Human | 0.86 |
| Azeyma | 451 | BattleNpc 14545 / ModelChara 3645 | Monster | Default |
| Wind-up Pixie | 354 | EventNpc 1031809, 1031890, or 1031806 / ModelChara 2520 | DemiHuman | 0.62 |
| Cirina | 293 | EventNpc 1018978 or 1044730 | Human | Default |
| Sadu | 294 | EventNpc 1018980 or 1044731 | Human | Default |
| Athena | 487 | EventNpc 1043513 or 1045553 | Human | Default |
| Heloise | 441 | EventNpc 1036935 / ModelChara 3439 (Venat) | Human | 0.97 |
| Kan-E-Senna | 73 | EventNpc 1026816 | Human | Default |
| Ysayle | 145 | EventNpc 1014847 | Human | Default |
| Mithra | 286 | EventNpc 1051960 | Human | Default |
| Lyse | 248 | EventNpc 1038813 | Human | Default |

`Default` means that Minion Mirage does not apply a mapping-specific model-scale override.

## Experimental summon unlock

**Enable minion summon (experimental)** attempts to make every game minion icon usable in minion-prohibited areas. A clicked icon is passed through the normal game action path first; when the area rejects it, Minion Mirage uses the game's local Companion transition for that exact minion. Clicking the currently active minion dismisses it.

The feature does not queue a minion, automatically retry, or respawn a dismissed minion.

## Runtime state

`runtime-state.json` records the selected mapping tuples, visible Companion actors, ownership evidence, tracked redraw stage, and experimental summon hook state. The file is diagnostic output only and is not used as a second appearance path.
