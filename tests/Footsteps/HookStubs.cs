// Offline native-boundary doubles. The production detours run unchanged.
namespace Dalamud.Configuration
{
    public interface IPluginConfiguration { int Version { get; set; } }
}

namespace Dalamud.Hooking
{
    public sealed class Hook<T>(T original, T detour, bool failEnable) : IDisposable where T : Delegate
    {
        public T Original { get; } = original;
        public T Detour { get; } = detour;
        public bool Disposed { get; private set; }
        public void Enable() { if (failEnable) throw new InvalidOperationException("enable failed"); }
        public void Dispose() => Disposed = true;
    }
}

namespace Dalamud.Plugin.Services
{
    public interface IGameInteropProvider
    {
        Dalamud.Hooking.Hook<T> HookFromAddress<T>(nint address, T detour) where T : Delegate;
    }
    public interface ISigScanner { nint ScanText(string signature); }
}
