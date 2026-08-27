# Minion Mirage

Minion Mirage is a standalone Dalamud plugin that replaces supported minions summoned by the local player with fixed NPC appearances.

It preserves the game's Companion actor, follow behavior, summon, replacement, and dismissal lifecycle while changing only the client-side appearance. It does not require or inspect Penumbra, Glamourer, or another appearance plugin.

> [!CAUTION]
> Minion Mirage `0.1.0-beta.1` is an early beta and remains under active development. Native appearance redraws and the experimental summon feature may cause incorrect actor states or crash the game. Use the plugin at your own risk.

## Install for beta testing

Add the following URL to **Dalamud Settings → Experimental → Custom Plugin Repositories**:

```text
https://raw.githubusercontent.com/miqote69/MinionMirage-Distribution/main/.dalamud/2d478e48ca56c3c2453cc2e69d41d02e54c41e23c6c69cf805b332a916273011/repo.json
```

The URL is intentionally difficult to guess, but it is not an authentication mechanism. Anyone who receives the URL can use it.

## Features

### Appearance mappings

- Replaces supported local-player minions with Human, Young Human, DemiHuman, or Monster NPC appearances.
- Uses the existing game Companion actor instead of creating a separate actor.
- Supports multiple selectable NPC targets for selected minions.
- Supports mapping-specific model scale values.
- Reapplies the selected NPC appearance and scale to the separate visible Companion representation created for GPose.
- Keeps each minion's normal game summon, replacement, follow, and dismissal behavior.
- Restores a still-present transformed minion when its mapping is switched off or the plugin unloads.

### Configuration UI

- Lists mappings in the same order as the game's `Companion.Order` data.
- Supports card and list layouts, search, and category filters.
- Provides individual ON/OFF switches and enable-all/disable-all actions.
- Shows unowned minions in a subdued style with an explicit localized **Not owned** label.
- Disables target selection and individual toggling for unowned minions without changing their stored configuration.
- Supports Automatic, English, Japanese, German, and French UI languages.

### Experimental summon unlock

The **Enable minion summon (experimental)** switch attempts to enable every game minion icon and normal minion summon behavior in areas where minions are normally prohibited.

This feature uses native game action and Companion transition paths. It may stop working after a game update and may crash the game.

### Runtime diagnostics

While loaded, the plugin atomically updates `runtime-state.json` in its Dalamud configuration directory. The file records mapped target tuples, visible Companion state, tracked redraw state, and experimental summon status. It is local diagnostic data and is not uploaded automatically.

## Current limitations

- Mapping definitions are fixed in the plugin and are not user-editable.
- Only explicitly supported minions and targets are available.
- Weapons are not replaced.
- An unowned minion cannot be summoned until it is unlocked by the current character.
- FFXIV, Dalamud, or FFXIVClientStructs updates may break native appearance or summon behavior.
- Visual appearance, equipment availability, and restoration can be affected by game actor recreation or incompatible external changes.
- The experimental summon unlock is not guaranteed to work in every prohibited area.
- If GPose does not expose one uniquely matching Companion representation, Minion Mirage leaves it unchanged instead of writing to an ambiguous actor.

## Documentation

- [Documentation Wiki](wiki/Home.md)
- [Feature Guide](wiki/Feature-Guide.md)
- [Safety and Limitations](wiki/Safety-and-Limitations.md)
- [Troubleshooting and Bug Reports](wiki/Troubleshooting-and-Bug-Reports.md)
- [Test Plan](docs/TEST_PLAN.md)
- [Test Specification](docs/TEST_SPECIFICATION.md)

## Privacy and scope

Minion Mirage is a cosmetic, client-side plugin. It does not automate combat or gameplay, contact an external service, upload actor information, or collect player data.
