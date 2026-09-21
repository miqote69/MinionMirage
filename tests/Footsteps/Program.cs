using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using MinionMirage;

var passed = 0;
void Check(bool result, string name)
{
    if (!result) throw new Exception(name);
    Console.WriteLine($"PASS {name}");
    passed++;
}

var native = new NativeBoundary();
var enabled = true;
var configuration = new Configuration();
using var routing = new MinionFootsteps(
    actor => enabled && configuration.UseMinionFootsteps && actor == 123
        ? new FootstepIdentity(3, 3419) : null, native, native);
void Emit(uint type = 1, uint sound = 8841) => native.Audio!.Detour(
    999, 1.5f, 2.5f, 3.5f, 4, type, 6, sound, 1, 205, 0, 2, 361, 1f, 0.5f, 7, 0);
var calls = new List<(uint Type, uint Sound, string Others)>();
native.OnAudio = (manager, x, y, z, surface, type, race, sound, sex, feet,
    environment, movement, body, volume, scale, category, extra) =>
    calls.Add((type, sound, $"{manager}/{x}/{y}/{z}/{surface}/{race}/{sex}/{feet}/{environment}/{movement}/{body}/{volume}/{scale}/{category}/{extra}"));
Emit();
var baseline = calls.Single(); calls.Clear();
var contacts = new List<(nint Actor, int Id, int Flags)>();
native.OnContact = (actor, id, flags) => { contacts.Add((actor, id, flags)); Emit(); };
native.Contact!.Detour(123, 17, 0x821);
Check(contacts.SequenceEqual(new[] { ((nint)123, 17, 0x821) }), "contact ID/flags and count preserved");
Check(calls.Single() == (3u, 3419u, baseline.Others), "only source sound bank/type overridden");
calls.Clear();
native.Contact.Detour(456, 18, 0);
Check(calls.Single() == baseline, "other Actor unchanged");
calls.Clear(); enabled = false;
native.Contact.Detour(123, 18, 0);
Check(calls.Single() == baseline, "mapping OFF unchanged");
enabled = true; calls.Clear();
configuration.UseMinionFootsteps = false;
native.Contact.Detour(123, 18, 0);
Check(calls.Single() == baseline, "NPC option preserves NPC footstep selection");
calls.Clear();
configuration.UseMinionFootsteps = true;
native.Contact.Detour(123, 18, 0);
Check(calls.Single() == (3u, 3419u, baseline.Others), "Minion option applies on next contact without recreation");
Check(System.Text.Json.JsonSerializer.Deserialize<Configuration>("{}")!.UseMinionFootsteps,
    "existing config without new property retains Minion default");
foreach (var option in new[] { true, false })
{
    var stored = System.Text.Json.JsonSerializer.Serialize(new Configuration { UseMinionFootsteps = option });
    Check(System.Text.Json.JsonSerializer.Deserialize<Configuration>(stored)!.UseMinionFootsteps == option,
        $"footstep option round-trips: {option}");
}
calls.Clear(); Emit();
Check(calls.Single() == baseline, "outside contact unchanged");
calls.Clear();
native.OnContact = (actor, id, flags) =>
{
    Emit();
    if (actor == 123) { native.Contact.Detour(456, id, flags); Emit(); }
};
native.Contact.Detour(123, 19, 0);
Check(calls.Select(x => x.Sound).SequenceEqual(new uint[] { 3419, 8841, 3419 }), "nested other Actor isolated; outer restored");
calls.Clear();
native.OnContact = (_, _, _) => throw new InvalidOperationException("native test failure");
try { native.Contact.Detour(123, 20, 0); } catch (InvalidOperationException) { }
Emit(); Check(calls.Single() == baseline, "exception releases event scope");
calls.Clear();
native.OnContact = (_, _, _) =>
{
    Task.Run(() => Emit()).GetAwaiter().GetResult();
    Emit();
};
native.Contact.Detour(123, 21, 0);
Check(calls.Select(x => x.Sound).SequenceEqual(new uint[] { 8841, 3419 }), "other thread does not inherit scope");
calls.Clear();
native.OnContact = (_, _, _) => Emit();
for (var i = 0; i < 8; i++) native.Contact.Detour(123, i, 0);
Check(calls.Count == 8 && calls.All(x => x.Sound == 3419), "one native audio call per supplied contact; no extra timer");
routing.Dispose();
Check(native.Contact.Disposed && native.Audio!.Disposed, "both hooks disposed");
foreach (var fail in new[] { "audio_scan", "audio_enable", "contact_enable" })
{
    var failure = new NativeBoundary { Failure = fail };
    try { using var unexpected = new MinionFootsteps(_ => null, failure, failure); throw new Exception("expected failure"); }
    catch (InvalidOperationException) { }
    Check(failure.Contact!.Disposed && (failure.Audio is null || failure.Audio.Disposed), $"partial hooks disposed: {fail}");
}
Console.WriteLine($"{passed} checks passed; native ABI/gameplay/listening remain separate.");

sealed class NativeBoundary : IGameInteropProvider, ISigScanner
{
    public Hook<MinionFootsteps.Contact>? Contact;
    public Hook<MinionFootsteps.Audio>? Audio;
    public MinionFootsteps.Contact OnContact = (_, _, _) => { };
    public MinionFootsteps.Audio OnAudio = (_, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _) => { };
    public string? Failure;
    public nint ScanText(string signature)
    {
        if (signature.StartsWith("48") && Failure == "audio_scan") throw new InvalidOperationException("scan failed");
        return signature.StartsWith("89") ? 1 : 2;
    }
    public Hook<T> HookFromAddress<T>(nint address, T detour) where T : Delegate
    {
        if (typeof(T) == typeof(MinionFootsteps.Contact))
        {
            Contact = new((actor, id, flags) => OnContact(actor, id, flags),
                (MinionFootsteps.Contact)(object)detour, Failure == "contact_enable");
            return (Hook<T>)(object)Contact;
        }
        Audio = new((m, x, y, z, s, t, r, a, sex, f, e, move, b, v, size, c, extra) =>
            OnAudio(m, x, y, z, s, t, r, a, sex, f, e, move, b, v, size, c, extra),
            (MinionFootsteps.Audio)(object)detour, Failure == "audio_enable");
        return (Hook<T>)(object)Audio;
    }
}
