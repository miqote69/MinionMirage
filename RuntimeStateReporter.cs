using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Plugin.Services;

namespace MinionToNPC;

internal sealed record RuntimeStateSnapshot(
    int SchemaVersion,
    DateTimeOffset ObservedAtUtc,
    string PluginState,
    IReadOnlyList<RuntimeMappingObservation> Mappings,
    LocalPlayerRuntimeObservation? LocalPlayer,
    string SelectionState,
    IReadOnlyList<CompanionRuntimeObservation> Companions,
    TrackedRuntimeObservation? Tracked,
    TrackedRuntimeObservation? FailedActor,
    string LastTransition,
    DateTimeOffset LastTransitionAtUtc,
    string? LastError);

internal sealed record LocalPlayerRuntimeObservation(
    ushort ObjectIndex,
    string GameObjectId,
    string EntityId);

internal sealed record CompanionRuntimeObservation(
    ushort ObjectIndex,
    uint BaseId,
    string Name,
    string GameObjectId,
    string EntityId,
    string OwnerId,
    bool IsValid,
    bool IsOwnedByLocalPlayer,
    string OwnershipEvidence,
    int? ModelCharaId,
    bool DrawObjectPresent,
    string? ModelType);

internal sealed record TrackedRuntimeObservation(
    ushort ObjectIndex,
    string GameObjectId,
    string EntityId,
    uint SourceCompanionRowId,
    string TargetKind,
    uint TargetRowId,
    uint TargetModelCharaRowId,
    string Stage);

internal sealed record RuntimeMappingObservation(
    uint SourceCompanionRowId,
    string SourceName,
    string TargetKind,
    uint TargetRowId,
    uint TargetModelCharaRowId,
    string TargetName,
    bool IsHuman);

internal sealed class RuntimeStateReporter
{
    internal const int CurrentSchemaVersion = 2;
    internal const string FileName = "runtime-state.json";

    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    private static readonly JsonSerializerOptions FingerprintOptions = new(JsonOptions)
    {
        WriteIndented = false,
    };

    private readonly IPluginLog log;
    private string? lastFingerprint;
    private DateTimeOffset lastWriteAtUtc = DateTimeOffset.MinValue;
    private bool failureReported;

    internal RuntimeStateReporter(string pluginConfigDirectory, IPluginLog log)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginConfigDirectory);
        this.log = log ?? throw new ArgumentNullException(nameof(log));

        var directory = Path.GetFullPath(pluginConfigDirectory);
        StateFilePath = Path.Combine(directory, FileName);
    }

    internal string StateFilePath { get; }

    internal void TryWrite(RuntimeStateSnapshot snapshot, bool force = false)
    {
        try
        {
            var fingerprintSnapshot = snapshot with { ObservedAtUtc = default };
            var fingerprint = JsonSerializer.Serialize(fingerprintSnapshot, FingerprintOptions);
            var unchanged = string.Equals(lastFingerprint, fingerprint, StringComparison.Ordinal);
            if (!force
                && unchanged
                && snapshot.ObservedAtUtc - lastWriteAtUtc < TimeSpan.FromSeconds(2))
            {
                return;
            }

            WriteAtomically(snapshot);
            lastFingerprint = fingerprint;
            lastWriteAtUtc = snapshot.ObservedAtUtc;
            failureReported = false;
        }
        catch (Exception exception)
        {
            if (failureReported)
                return;

            failureReported = true;
            log.Error(exception, "MinionToNPC runtime state reporting is unavailable.");
        }
    }

    private void WriteAtomically(RuntimeStateSnapshot snapshot)
    {
        var directory = Path.GetDirectoryName(StateFilePath)
            ?? throw new InvalidOperationException("Runtime state path has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{FileName}.{Guid.NewGuid():N}.tmp");

        try
        {
            var json = JsonSerializer.Serialize(snapshot, JsonOptions) + "\n";
            var bytes = Utf8NoBom.GetBytes(json);
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       FileOptions.WriteThrough))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, StateFilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
