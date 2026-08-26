# Runtime state reporting test specification

| ID | Path | Action and oracle |
| --- | --- | --- |
| MNP-STATE-001 | Build | `dotnet restore --locked-mode`, then `dotnet build -c Release --no-restore`; both exit 0 with no errors. |
| MNP-STATE-002 | Load/heartbeat | Load the real Dev Plugin. `runtime-state.json` parses as schema 1 and `observedAtUtc` advances within 5 seconds. |
| MNP-STATE-003 | No Companion | With no minion, `selectionState=no_companion` and `companions=[]`. |
| MNP-STATE-004 | Wrong Companion | Summon a non-91 minion. Its `baseId` appears and `selectionState=source_not_present`; it is not tracked. |
| MNP-STATE-005 | Target ownership | Summon Companion 91. Exactly one entry reports `isOwnedByLocalPlayer=true`, an ownership reason, and `selectionState=target_ready`. |
| MNP-STATE-006 | Apply lifecycle | For the target, `tracked.stage` advances and reaches `Applied`; model/draw fields remain readable. Failure instead produces `pluginState=error` or `failedActor` and a transition identifying the failed path. |
| MNP-STATE-007 | Despawn/recovery | Dismiss the target. Tracking clears and selection returns to a no-target state without modifying another Companion. |
| MNP-STATE-008 | Unload | Unload with target present. The final snapshot reports `pluginState=disposed`; restoration remains separately confirmed from log/runtime observation. |
| MNP-STATE-009 | Atomic reader | Repeated background JSON reads during state changes never observe partial or malformed JSON. |

All runtime cases use the actual plugin and FF14 object table. No fixture or synthetic Companion can satisfy MNP-STATE-002 through MNP-STATE-009.
