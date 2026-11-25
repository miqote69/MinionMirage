namespace MinionMirage;

internal readonly record struct TravelCompanion(uint RowId, nint Address, bool DrawEnabled)
{
    internal bool IsSameActor(TravelCompanion? other)
        => other is { } actor && RowId == actor.RowId && Address == actor.Address;
}

internal sealed record AcceptedCompanionAction(TravelCompanion? Previous, uint? LocalSummonRow = null);

internal readonly record struct CompanionTravelRecovery(uint RowId, bool RedrawExisting);

internal sealed class CompanionTravel
{
    private uint territory;
    private bool traveling;
    private uint? visibleRow;
    private uint? departureRow;
    private TravelCompanion? outgoing;

    internal void CancelForAction(AcceptedCompanionAction action)
    {
        departureRow = null;
        visibleRow = null;
        outgoing = action.Previous?.RowId == action.LocalSummonRow ? null : action.Previous;
    }

    internal CompanionTravelRecovery? Observe(
        uint territoryId,
        bool betweenAreas,
        bool sceneReady,
        TravelCompanion? companion,
        bool hasCompanion)
    {
        var changedTerritory = territoryId != 0 && territory != 0 && territoryId != territory;
        if (territoryId != 0)
            territory = territoryId;

        if (!traveling && (betweenAreas || changedTerritory))
        {
            departureRow = visibleRow;
            traveling = true;
        }

        if (betweenAreas || !sceneReady)
            return null;

        if (outgoing is { } previous && !previous.IsSameActor(companion))
            outgoing = null;

        visibleRow = companion is { DrawEnabled: true, RowId: > 0 } visible && outgoing is null
            ? visible.RowId
            : null;

        if (!traveling)
            return null;

        traveling = false;
        var row = departureRow;
        departureRow = null;
        if (row is not { } rowId)
            return null;

        if (companion is { DrawEnabled: false } current && current.RowId == rowId)
            return new CompanionTravelRecovery(rowId, RedrawExisting: true);

        return hasCompanion ? null : new CompanionTravelRecovery(rowId, RedrawExisting: false);
    }

    internal void Reset()
    {
        territory = 0;
        traveling = false;
        visibleRow = null;
        departureRow = null;
        outgoing = null;
    }
}
