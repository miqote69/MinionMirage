# Runtime state reporting test specification

| ID | Path | Action and oracle |
| --- | --- | --- |
| MNP-STATE-001 | Build | `dotnet restore --locked-mode`, then `dotnet build -c Release --no-restore`; both exit 0 with no errors. |
| MNP-STATE-002 | Load/heartbeat | Load the real Dev Plugin. `runtime-state.json` parses as schema 2, lists all three fixed mappings, and `observedAtUtc` advances within 5 seconds. |
| MNP-STATE-003 | No Companion | With no minion, `selectionState=no_companion` and `companions=[]`. |
| MNP-STATE-004 | Wrong Companion | Summon a Companion other than 331, 232, or 218. Its `baseId` appears and `selectionState=source_not_present`; it is not tracked. |
| MNP-STATE-005 | Target ownership | Summon Companion 331, 232, or 218. Exactly one entry reports `isOwnedByLocalPlayer=true`, an ownership reason, and `selectionState=target_ready`. |
| MNP-STATE-006 | Human apply regression | Summon Companion 331. `tracked.stage=Applied`, `modelType=Human`, and the Y'shtola mapping is recorded. |
| MNP-STATE-007 | Despawn/recovery | Dismiss the target. Tracking clears and selection returns to a no-target state without modifying another Companion. |
| MNP-STATE-008 | Unload | Unload with target present. The final snapshot reports `pluginState=disposed`; restoration remains separately confirmed from log/runtime observation. |
| MNP-STATE-009 | Atomic reader | Repeated background JSON reads during state changes never observe partial or malformed JSON. |
| MNP-STATE-010 | Monster apply | Summon Companion 232. `tracked.stage=Applied`, `modelCharaId=1689`, `modelType=Monster`, and no Human equipment-finalization failure occurs. |
| MNP-STATE-011 | Monster lifecycle | Dismiss and resummon Companion 232. Original minion restoration/despawn remains normal and the BOSS appearance is reapplied once. |
| MNP-STATE-012 | Alisaie Avatar Human apply | Summon Companion 218. BNpcBase 10067 resolves BNpcCustomize 646 and NpcEquip 1713; `tracked.stage=Applied`, `targetKind=BattleNpc`, `targetRowId=10067`, `modelType=Human`, and the Alisaie Avatar mapping is recorded. Weapon replacement is excluded. |
| MNP-STATE-013 | Alisaie Avatar lifecycle | Dismiss and resummon Companion 218. Original minion restoration/despawn remains normal and the Alisaie Avatar appearance is reapplied once. |

All runtime cases use the actual plugin and FF14 object table. No fixture or synthetic Companion can satisfy MNP-STATE-002 through MNP-STATE-013.
