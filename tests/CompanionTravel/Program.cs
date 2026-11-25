using MinionMirage;

var kanE = new TravelCompanion(73, (nint)0x1000, DrawEnabled: true);
var merlwyb = new TravelCompanion(72, (nint)0x2000, DrawEnabled: true);
var travel = new CompanionTravel();

void Check(CompanionTravelRecovery? actual, CompanionTravelRecovery? expected, string scenario)
{
    if (actual != expected)
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

void Start(TravelCompanion companion)
{
    travel.Reset();
    Check(travel.Observe(132, false, true, companion, true), null, "ordinary frame");
}

Start(kanE);
Check(travel.Observe(132, true, false, null, false), null, "actor disappears on departure");
Check(travel.Observe(144, true, false, null, false), null, "destination during loading");
Check(travel.Observe(144, false, false, null, false), null, "local player still unavailable");
Check(travel.Observe(144, false, true, null, false), new(73, false), "same variant recreated on arrival");
Check(travel.Observe(144, false, true, null, false), null, "no retry during idle frames");

Start(merlwyb);
Check(travel.Observe(132, true, false, null, false), null, "same-territory transfer starts");
Check(travel.Observe(132, false, true, null, false), new(72, false), "same-territory transfer completes");

Start(kanE);
Check(travel.Observe(144, false, false, null, false), null, "territory change before player returns");
Check(travel.Observe(144, false, true, kanE, true), null, "game already restored the companion");

Start(kanE);
Check(travel.Observe(144, false, false, null, false), null, "territory changes while the player is absent");
Check(travel.Observe(144, false, true, null, false), new(73, false), "missing actor after territory-only signal");

Start(kanE);
travel.Observe(132, true, false, null, false);
Check(travel.Observe(144, false, true, kanE with { DrawEnabled = false }, true),
    new(73, true), "existing hidden companion only needs drawing");

Start(kanE);
travel.Observe(132, true, false, null, false);
Check(travel.Observe(144, false, true, null, true), null, "occupied native companion slot");

Start(kanE);
travel.Observe(132, true, false, null, false);
Check(travel.Observe(144, false, true, merlwyb, true), null, "different companion already present");

Start(kanE);
travel.CancelForAction(new AcceptedCompanionAction(kanE));
travel.Observe(132, false, true, kanE, true);
travel.Observe(132, true, false, null, false);
Check(travel.Observe(144, false, true, null, false), null, "accepted return before departure with old actor still drawn");

Start(kanE);
travel.CancelForAction(new AcceptedCompanionAction(merlwyb));
travel.Observe(132, false, true, merlwyb, true);
travel.Observe(132, true, false, null, false);
Check(travel.Observe(144, false, true, null, false), null, "return uses the actor that replaced the last scanned one");

Start(kanE);
travel.Observe(132, true, false, null, false);
travel.CancelForAction(new AcceptedCompanionAction(kanE, LocalSummonRow: 72));
Check(travel.Observe(144, false, true, null, false), null, "accepted switch cancels pending restoration");
travel.Observe(144, false, true, merlwyb, true);
travel.Observe(144, true, false, null, false);
Check(travel.Observe(132, false, true, null, false), new(72, false), "next transfer uses the newer companion");

Start(kanE);
travel.CancelForAction(new AcceptedCompanionAction(kanE, LocalSummonRow: 73));
travel.Observe(132, false, true, kanE, true);
travel.Observe(132, true, false, null, false);
Check(travel.Observe(144, false, true, null, false), new(73, false), "intentional same-row local summon stays eligible");

Start(kanE);
Check(travel.Observe(132, false, true, null, false), null, "loss outside a transfer causes no recreation");
travel.Observe(132, true, false, null, false);
Check(travel.Observe(144, false, true, null, false), null, "already returned on departure");

Start(kanE);
travel.Observe(132, true, false, null, false);
travel.Reset();
Check(travel.Observe(144, false, true, null, false), null, "logout clears pending restoration");

Console.WriteLine("Companion travel regression tests passed.");
