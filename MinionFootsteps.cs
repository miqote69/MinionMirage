using Dalamud.Hooking;
using Dalamud.Plugin.Services;

namespace MinionMirage;

internal readonly record struct FootstepIdentity(uint ModelType, uint SoundId);

// Preserve the NPC's native contact events; change only the sound bank selected
// within that event. Never change Character fields or schedule extra footsteps.
internal sealed class MinionFootsteps : IDisposable
{
    internal delegate void Contact(nint actor, int contactId, int flags);
    internal delegate void Audio(
        nint manager, float x, float y, float z, uint surface, uint modelType,
        uint race, uint soundId, uint sex, uint feet, uint environment,
        uint movement, uint body, float volume, float scale, uint category, byte extra);

    [ThreadStatic] private static FootstepIdentity? currentSource;
    private readonly Func<nint, FootstepIdentity?> resolveSource;
    private readonly Hook<Contact> contactHook;
    private readonly Hook<Audio> audioHook;

    internal MinionFootsteps(
        Func<nint, FootstepIdentity?> resolveSource,
        IGameInteropProvider interop, ISigScanner scanner)
    {
        this.resolveSource = resolveSource;
        // Verified against EXE SHA256 5BBC501DD5C7F22FD61A11D08C25356041D878DB7CD83203ADAE393E4DFACC44.
        // PlayFootStepSound -> sound selection; the latter has no Actor argument.
        contactHook = interop.HookFromAddress<Contact>(scanner.ScanText(
            "89 54 24 10 55 56 57 41 54 41 55 41 56 41 57 48 81 EC 00 01 00 00 48 8B 05 ?? ?? ?? ?? 4C 8B F1"),
            ContactDetour);
        Hook<Audio>? createdAudio = null;
        try
        {
            createdAudio = interop.HookFromAddress<Audio>(scanner.ScanText(
                "48 8B C4 53 55 56 48 81 EC A0 01 00 00 0F 29 70 C8 0F 29 78 B8 44 0F 29 40 A8"),
                AudioDetour);
            audioHook = createdAudio;
            audioHook.Enable();
            contactHook.Enable();
        }
        catch
        {
            contactHook.Dispose();
            createdAudio?.Dispose();
            throw;
        }
    }

    private void ContactDetour(nint actor, int contactId, int flags)
    {
        var previous = currentSource;
        try
        {
            // A nested contact for another Actor must not inherit this override.
            currentSource = resolveSource(actor);
            contactHook.Original(actor, contactId, flags);
        }
        finally
        {
            currentSource = previous;
        }
    }

    private void AudioDetour(
        nint manager, float x, float y, float z, uint surface, uint modelType,
        uint race, uint soundId, uint sex, uint feet, uint environment,
        uint movement, uint body, float volume, float scale, uint category, byte extra)
    {
        if (currentSource is { } source)
        {
            modelType = source.ModelType;
            soundId = source.SoundId;
        }

        audioHook.Original(manager, x, y, z, surface, modelType, race, soundId,
            sex, feet, environment, movement, body, volume, scale, category, extra);
    }

    public void Dispose()
    {
        contactHook.Dispose();
        audioHook.Dispose();
    }
}
