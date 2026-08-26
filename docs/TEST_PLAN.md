# Runtime state reporting test plan

## Objective and scope

Verify that the development plugin selects the fixed mapping for the summoned Companion, waits until the game's backing contains that Companion row's own ModelChara, writes its target ModelChara and category-required customize/equipment backing once, requests the normal full redraw, applies the mapping's optional draw-model scale, and publishes a readable, atomic snapshot without moving Actor access outside the Dalamud Framework thread. Type `2` DemiHuman BattleNpc targets must use their `BNpcCustomize` and `NpcEquip` creation inputs; they must not be collapsed into the Type `3` Monster model-only path. Verify the minimal configuration UI that lists the four mapped Companion icons and localized names, persists one enable/disable control per mapping, provides one enable-all and one disable-all button, and provides the same Automatic/English/Japanese/German/French UI-language selection used by Minion Scaler.

The fixed prototype mappings are:

- Companion `331` (`ファースト・ヤ・シュトラ`) to BNpcBase `13910` (Human), using BNpcCustomize `526`, NpcEquip `2269`, and `CharacterBase.ModelScale=1.0f` from the NPC identified in the User-provided Glamourer screenshot.
- Companion `232` (`マメット・スカアハ`) to BNpcBase `6479`, ModelChara `1689` (Monster).
- Companion `218` (`ニュー・アリゼー`) to ENpcBase `1017687` (`アリゼー`, Human), matching the clothed Anamnesis NPC appearance, with `CharacterBase.ModelScale=0.97f` to match the User-confirmed current player multiplier.
- Companion `398` (`マメット・ガイア`) to BNpcBase `17830` (`ガイア`), ModelChara `4436`, Type `2` / Model `1041` / Base `1` / Variant `1` (DemiHuman), as identified in the User-provided screenshot.

NPC appearance correctness and visual acceptance are separate runtime checks. Weapon replacement remains excluded.

Companion switches may reuse the same ObjectIndex/GameObjectId while changing BaseId before the new source model is installed. That transient state must report `source_model_pending` and must not receive either target appearance.

## Environment and levels

- Static/build: Release build with Dalamud API 15.
- Runtime integration: real FF14 process, installed Dalamud Dev Plugin, standard plugin config directory.
- Observation: background reader opens `runtime-state.json`; it never accesses Actor pointers or invokes game actions.
- UI: actual Dalamud configuration window using the game icon texture provider and localized Companion sheet names. Static/build evidence does not replace visual review.

## Entry, suspension, and exit

- Entry: source builds and the Dev Plugin loads.
- Suspend on plugin load failure, missing state path, invalid JSON, stale heartbeat over 5 seconds while Framework updates continue, state-writer error, unavailable target backing, missing `BNpcCustomize` or `NpcEquip` for a Type `2` DemiHuman target, an exception from the normal `DisableDraw` / `EnableDraw` path, or inability to access the recreated `CharacterBase` for a requested model-scale write.
- Exit: all four mappings, wrong-minion, source-model pending, redraw request/failure, mapped-to-mapped switch, despawn, unload, persisted individual and bulk enable/disable behavior, and UI-language selection are distinguishable; build regression passes. Actual row layout, icons, labels, and controls require visual review in the Dalamud window.

## Evidence and authority

Record source commit, DLL SHA-256, build output, Dalamud load log, written backing values, requested/applied model scale, state-file path/content/timestamps, and user visual observation separately. Glamourer `ui_config.json` `SelectedNpc` is only the last NPC-tab selection and is not evidence of the appearance currently applied to an Actor. The User-provided Glamourer screenshots are the fixed authorities for the Y'shtola correction and the Gaia addition: Y'shtola is Battle NPC ID `13910` / Name ID `10570`; Gaia is Model ID `4436`, Source `BattleNpc #17830`, category `DemiHuman`, Type `2`, Model `1041`, Base `1`, Variant `1`. Current game data resolves BNpcBase `13910` as Human ModelChara `0`, BNpcCustomize `526`, NpcEquip `2269`, and Scale `1.0`. The escaped Alisaie regression selected ENpcBase `1026567` and expected Body `0x123CE` (`e9166`), while the successful Anamnesis NPC appearance uses ENpcBase `1017687` with Body `0x1239A` (`e9114`). The corrected Alisaie backing must contain Body `0x1239A`, Hands `0x126AF`, Legs `0x1239A`, and Feet `0x1239A` before the normal full redraw. The recreated Y'shtola and Alisaie `CharacterBase` objects must then receive their mapping-specific model scales; normal restore must receive each captured pre-conversion model scale. A completed redraw or scale write is not a visual verdict; that remains User acceptance.
