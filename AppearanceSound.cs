namespace MinionMirage;

internal static class AppearanceSound
{
    // Game 2026.08.11 SetupRaw: Human sound selection uses race, sex and
    // Hyur tribe; other model types use ModelChara.SEPack directly.
    internal static ushort? ResolveVoiceId(byte modelType, ushort sePack, byte[] customize)
    {
        if (modelType != 1)
            return sePack;

        var female = customize[1] != 0;
        return customize[0] switch
        {
            1 => customize[4] == 1
                ? (ushort)(female ? 8804 : 8801)
                : (ushort)(female ? 8822 : 8819),
            2 => (ushort)(female ? 8816 : 8813),
            3 => (ushort)(female ? 8810 : 8807),
            4 => (ushort)(female ? 8828 : 8825),
            5 => (ushort)(female ? 8834 : 8831),
            6 => (ushort)(female ? 8841 : 8837),
            7 => (ushort)(female ? 8845 : 8844),
            8 => (ushort)(female ? 8847 : 8846),
            // SetupRaw leaves the current value untouched for an unknown race.
            _ => null,
        };
    }
}
