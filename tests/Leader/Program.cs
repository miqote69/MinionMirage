using MinionMirage;
using System.Text.Json;

void Check(bool value, string name)
{
    if (!value) throw new Exception(name);
    Console.WriteLine($"PASS {name}");
}

var configuration = new Configuration();
Check(configuration.SelectedLeaderCompanionRowId == 73, "existing users retain Kan-E-Senna selection");
Check(PrototypeContract.DisplayMappings.Count(m => m.SourceCompanionRowId is 72 or 73 or 74) == 1,
    "one Leader card in both views");
Check(PrototypeContract.LeaderCandidates.Select(m => m.SourceCompanionRowId).SequenceEqual(new uint[] {73, 72, 74}),
    "three distinct leader choices");
foreach (var (source, target) in new[] {(73u, 1026816u), (72u, 1002441u), (74u, 1003786u)})
{
    configuration.SelectedLeaderCompanionRowId = source;
    Check(configuration.ResolveCompanionAction(71) == source, $"leader button resolves {source}");
    Check(PrototypeContract.GetSelectedMapping(source, configuration.SelectedTargetRowIds).TargetRowId == target,
        $"spawned row {source} resolves NPC {target}");
    Check(Configuration.GetLeaderTransition(source, source, false) == source, "retained ID without object summons");
    Check(Configuration.GetLeaderTransition(source, source, true) == 0, "same active selection dismisses");
    Check(Configuration.GetLeaderTransition(source, 294, true) == source, "different active minion is replaced");
    Check(configuration.ResolveCompanionAction(294) == 294, "other minion unchanged");
    Check(JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(configuration))!.SelectedLeaderCompanionRowId == source,
        "selection survives save/load");
}
configuration.DisabledCompanionRowIds.Add(73);
Check(new uint[] {72, 73, 74}.All(id => !configuration.IsMappingEnabled(id)), "existing Kan-E-Senna OFF controls group");
Check(configuration.ResolveCompanionAction(71) == 71, "OFF restores native Leader action");
Check(configuration.IsMappingEnabled(294), "group toggle does not affect other minions");
Check(configuration.UseLocalLeaderTransition(0), "owned Leader uses selected local transition");
Check(!configuration.UseLocalLeaderTransition(1325), "experiment OFF does not bypass area status");
configuration.ExperimentalEnableNormalCompanionSummon = true;
Check(configuration.UseLocalLeaderTransition(1325), "experiment ON permits area status");
Check(!configuration.UseLocalLeaderTransition(573), "unowned native Leader remains unchanged");
Check(!configuration.UseLocalLeaderTransition(579), "other native eligibility result remains unchanged");

namespace Dalamud.Configuration
{
    public interface IPluginConfiguration { int Version { get; set; } }
}
