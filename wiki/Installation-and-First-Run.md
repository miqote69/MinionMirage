# Installation and First Run

Minion Mirage is currently distributed as a beta through a Dalamud custom plugin repository.

## Install the beta

Add the following URL to **Dalamud Settings → Experimental → Custom Plugin Repositories**:

```text
https://raw.githubusercontent.com/miqote69/MinionMirage-Distribution/main/.dalamud/2d478e48ca56c3c2453cc2e69d41d02e54c41e23c6c69cf805b332a916273011/repo.json
```

Open the Dalamud Plugin Installer, install **Minion Mirage**, and use `/minionmirage` in the in-game chat to open its settings.

## Development build

## Requirements

- Final Fantasy XIV with Dalamud
- Dalamud API 15 development environment
- .NET SDK compatible with `Dalamud.NET.Sdk/15.0.0`
- Access to the private Minion Mirage repository

## Build

From the repository root, run:

```text
dotnet build MinionMirage.csproj -c Release
```

The development DLL is created at:

```text
bin/Release/MinionMirage.dll
```

### Register the Dev Plugin

1. Open Dalamud Settings.
2. Open the Dev Plugins page.
3. Add the full path to `bin/Release/MinionMirage.dll`.
4. Load Minion Mirage.
5. Open **Settings** from the Dev Plugin entry.

## First run

1. Confirm that the mapped minion list is visible.
2. Unowned minions should remain visible but show **Not owned** and disabled item controls.
3. Summon an owned supported minion normally from the game's minion list.
4. Confirm that its configured NPC appearance is applied after the native summon transition.
5. Use the mapping switch to disable or re-enable an individual appearance.

Do not enable **Enable minion summon (experimental)** until you have read [Safety and Limitations](Safety-and-Limitations.md).

## Updating

Normal installations update through the Dalamud Plugin Installer. Development installations rebuild the current commit in Release configuration and reload `bin/Release/MinionMirage.dll`. Configuration language, mapping ON/OFF state, selected targets, and the experimental switch are stored in the normal Dalamud plugin configuration.
