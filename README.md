# MinionToNPC

Local-first Dalamud plugin prototype.

## Initial scope

- Detect the local player's fixed prototype Companions: `RowId 331` (`ファースト・ヤ・シュトラ`), `RowId 232` (`マメット・スカアハ`), `RowId 218` (`ニュー・アリゼー`), `RowId 398` (`マメット・ガイア`), `RowId 534` (`マメット・ペルペル`), `RowId 325` (`マメット・フラン`), `RowId 298` (`マメット・シロ`), `RowId 394` (`オートマトン２Ｂ`), `RowId 395` (`オートマトン２Ｐ`), `RowId 98` (`マメット・ミンフィリア`), `RowId 260` (`マメット・クロ`), and `RowId 332` (`マメット・リーン`).
- Replace Row 331 with Human `BNpcBase 13910` (`Y'shtola`, `BNpcCustomize 526`, `NpcEquip 2269`) and set the recreated draw model to the NPC row's `ModelScale=1.0f`, matching the User-provided Glamourer screenshot.
- Replace Row 232 with `BNpcBase 6479` / `ModelChara 1689` (`Scathach`, Monster).
- Replace Row 218 with `ENpcBase 1017687` (`Alisaie`, matching the clothed Anamnesis NPC appearance, Human) and set the recreated draw model to `ModelScale=0.97f`, matching the User-confirmed current player multiplier.
- Replace Row 398 (`Mammet Gaia`) with `BNpcBase 17830` / `ModelChara 4436` (`Gaia`, DemiHuman Type 2 / Model 1041 / Base 1 / Variant 1), matching the User-provided screenshot.
- Replace Row 534 (`Mammet Pelupelu`, source ModelChara `4391`) with `ENpcBase 1046564` (`Quiet Pelupelu`, Human BodyType 4), matching the User-provided screenshot.
- Replace Row 325 (`Mammet Fran`, source ModelChara `2412`) with `ENpcBase 1025589` / `ModelChara 2382` (`Fran`, DemiHuman Type 2 / Model 1034 / Base 1 / Variant 1), matching the User-provided screenshot.
- Replace Row 298 (`Wind-up Zhloe`, source ModelChara `2246`) with `ENpcBase 1044638` (`Zhloe Aliapoh`, Human BodyType 1), matching the User-provided screenshot.
- Replace Row 394 (`Automaton 2B`, source ModelChara `2991`) with `ENpcBase 1033925` / `ModelChara 2810` (`2B`, Human Type 1 / Model 201 / Base 1 / Variant 1, Midlander female BodyType 1), matching the User-provided screenshot.
- Replace Row 395 (`Automaton 2P`, source ModelChara `2992`) with `BNpcBase 11366` / `ModelChara 2810` (`2P`, Human Type 1 / Model 201 / Base 1 / Variant 1, Midlander female BodyType 1), matching the User-provided screenshot.
- Replace Row 98 (`Wind-up Minfilia`, source ModelChara `797`) with `ENpcBase 1006573` (`Minfilia`, Human BodyType 1), matching the User-provided screenshot.
- Replace Row 260 (`Wind-up Khloe`, source ModelChara `1897`) with `ENpcBase 1012445` (`Khloe Aliapoh`, Human Young NPC / BodyType 4) and set the recreated draw model to the User-requested trial `ModelScale=0.7f`, using 70% of the NPC's unscaled model size and matching the User-provided screenshot.
- Replace Row 332 (`Mammet Ryne`, source ModelChara `2486`) with `ENpcBase 1033894` (`Ryne`, Human / ModelChara 0) and set the recreated draw model to the requested `ModelScale=0.86f`, matching the User-provided screenshot authority.
- Provide a compact config window with each mapped minion's icon, localized name, per-mapping enable switch, enable-all/disable-all buttons, and Automatic/English/Japanese/German/French UI language selection.
- Keep the original Companion entity and its follow/despawn lifecycle.
- Restore or clean up safely when the Companion disappears or the plugin unloads.

The initial prototype has no user-editable mapping definitions, weapon replacement, appearance sync, or public release.

## Resolved appearance paths

- Human BattleNpc (`ModelChara.Type=1`): resolve and pass the target `BNpcCustomize` and `NpcEquip` creation inputs before the normal full redraw.
- EventNpc, including Young bodies: build the NPC-specific customize and equipment creation inputs from `ENpcBase` or its referenced `NpcEquip`; do not use the player's post-create equipment-slot API.
- Monster (`ModelChara.Type=3`): use the target `ModelChara` through the model-only redraw path.
- DemiHuman (`ModelChara.Type=2`): resolve and pass `ModelChara`, `BNpcCustomize`, and `NpcEquip`. DemiHuman must not be collapsed into the Monster model-only path.

Every path keeps the original Companion object and waits for its source `ModelChara` before applying a target, preventing a stale target appearance during mapped-minion switches. Mapping-specific draw-model scale is applied only after the recreated draw object exists.

## Development runtime state

While loaded, the plugin atomically updates `runtime-state.json` in its Dalamud plugin configuration directory. The snapshot records every visible Companion's RowId, ownership evidence, draw/model state, target-selection result, tracked apply stage, failure identity, and a two-second heartbeat. External tools may read this file, but all Actor inspection and mutation remains on the Dalamud Framework thread.

## Prototype acceptance

- Each fixed target Companion is redrawn using its mapped Human, Monster, or DemiHuman appearance path.
- A non-target Companion is not modified.
- The original Companion entity remains responsible for following and despawning.
- Unloading the plugin restores a still-present target through the same redraw path.
- A mapping-specific draw-model scale changes only `CharacterBase.ModelScale`; the captured original value is restored with the original Companion appearance.
- A failed backing write or redraw call stops further writes for the same actor; no failure-triggered rollback is performed.

## Status

Source implementation is present. Release build and in-game runtime acceptance are tracked separately.
