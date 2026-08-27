# Minion Mirage Wiki

Minion Mirage is an early-beta Dalamud plugin that replaces supported minions summoned by the local player with fixed NPC appearances.

It is standalone and does not require or inspect Penumbra, Glamourer, or another appearance plugin. The game keeps ownership of the Companion actor and its normal summon, follow, replacement, and dismissal lifecycle.

> **Important:** Minion Mirage `0.1.0-beta.6` performs native appearance redraws. The experimental summon feature also uses native game action and Companion transition paths. Incorrect actor states or game crashes cannot be ruled out.

## Start Here

- [Installation and First Run](Installation-and-First-Run.md)
- [Feature Guide](Feature-Guide.md)
- [Safety and Limitations](Safety-and-Limitations.md)
- [Frequently Asked Questions](Frequently-Asked-Questions.md)
- [Troubleshooting and Bug Reports](Troubleshooting-and-Bug-Reports.md)

## Quick Links

- [Repository](https://github.com/miqote69/MinionMirage)
- [README](../README.md)
- [Issues](https://github.com/miqote69/MinionMirage/issues)

## Current beta status

- Version: `0.1.0-beta.6`
- Distribution: beta through the public Minion Mirage distribution feed
- Settings command: `/minionmirage`
- Supported source minions: 23
- Appearance categories: Human, Young Human, DemiHuman, Monster
- GPose: selected NPC appearance is reapplied to the resolved visible Companion representation
- UI languages: Automatic, English, Japanese, German, French
