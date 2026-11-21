using System;
using CsyMuseum;

/// <summary>
/// Executable, deterministic checks of proximity visits. These are software
/// checks, not participant evaluation or evidence of headset usability.
/// Compile this file with CSYGuideVisit.cs and run its Main entry point.
/// </summary>
public static class VisitPolicyTests
{
    private static int assertions;

    private static void Require(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private static bool Tick(CSYGuideVisit visit, float distance, double time,
        float height = 0f, bool available = true)
    {
        return visit.Update(available, distance, height, 1.6f, 1.85f, 1.2f, time);
    }

    public static int Main()
    {
        try
        {
            QuickPassAndDwell();
            RadiusHysteresisAndReentry();
            AvailabilityAndHeight();
            ConfigurationBoundaries();
            InvalidDataAndClockReset();
            Console.WriteLine("PASS: CSYGuideVisit software checks (" + assertions + " assertions).");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL: " + error.Message);
            return 1;
        }
    }

    private static void QuickPassAndDwell()
    {
        var visit = new CSYGuideVisit();
        Require(!visit.Ready(0, .6f), "Outside an exhibit must not be ready.");
        Require(Tick(visit, 1f, 10), "Approaching an exhibit should begin a visit.");
        Require(!visit.Ready(10.3, .6f), "Brief pass should not reach the dwell interval.");
        Require(!Tick(visit, 2f, 10.4), "Walking away should end a brief visit.");
        Require(!visit.Ready(11, .6f), "Elapsed time after walking away must not trigger playback.");

        Require(Tick(visit, 1f, 20), "A later approach should begin a fresh visit.");
        Require(visit.EnteredAt == 20, "Dwell starts at the new entry, not at the quick pass.");
        Require(visit.Ready(20.7, .6f), "A sustained approach should become ready.");
        visit.MarkPlayed();
        Require(visit.Played, "Successful playback must be recorded.");
        Require(!visit.Ready(100, .6f), "Remaining near the exhibit must not repeat auto playback.");
        Require(Tick(visit, 1f, 101) && visit.Played, "Ordinary updates must retain the played marker.");
    }

    private static void RadiusHysteresisAndReentry()
    {
        var visit = new CSYGuideVisit();
        Require(!Tick(visit, 1.7f, 0), "An outside visitor in the exit band must not enter.");
        Require(Tick(visit, 1.6f, 1), "Entry radius boundary should be inclusive.");
        Require(Tick(visit, 1.7f, 1.5), "Movement into the hysteresis band should retain the visit.");
        Require(visit.EnteredAt == 1, "Hysteresis must not restart dwell.");
        visit.MarkPlayed();
        Require(Tick(visit, 1.85f, 2) && visit.Played, "Exit boundary should not cause repeated playback.");
        Require(!Tick(visit, 1.86f, 3), "Beyond exit radius should end the visit.");
        Require(!visit.Inside && !visit.Played, "Leaving clears the per-visit playback marker.");
        Require(Tick(visit, 1f, 4), "Returning should create a new visit.");
        Require(visit.Ready(4.7, .6f), "A new visit may play again after dwelling.");
    }

    private static void AvailabilityAndHeight()
    {
        var visit = new CSYGuideVisit();
        Tick(visit, 0f, 1);
        visit.MarkPlayed();
        Require(!Tick(visit, 0f, 2, available: false), "Unavailable museum or station must end the visit.");
        Require(!visit.Played && !visit.Ready(10, 0), "Disabled guide must not become ready.");
        Require(Tick(visit, 0f, 3), "A re-enabled guide can begin another visit.");
        Require(!visit.Ready(3.2, .6f), "Re-enabling must wait for a new dwell interval.");
        Require(!Tick(visit, 0f, 4, height: 1.21f), "Another floor above the vertical band must not trigger.");
        Require(!Tick(visit, 0f, 5, height: -1.21f), "Another floor below the vertical band must not trigger.");
        Require(Tick(visit, 0f, 6, height: -1.2f), "Vertical band uses absolute distance and includes its boundary.");
        visit.Reset();
        Require(!visit.Inside && !visit.Played && visit.EnteredAt == 0, "Reset clears all visit state.");
        visit.MarkPlayed();
        Require(!visit.Played, "A playback marker outside a visit must be ignored.");
    }

    private static void ConfigurationBoundaries()
    {
        var visit = new CSYGuideVisit();
        Require(!visit.Update(true, .01f, 0f, -1f, -2f, -3f, 0),
            "Negative radii clamp to zero rather than creating an unbounded area.");
        Require(visit.Update(true, 0f, 0f, -1f, -2f, -3f, 1),
            "An exact zero-distance match is valid after clamping.");
        Require(visit.Ready(1, -1f), "Negative dwell clamps to zero.");
        Require(!visit.Update(true, 0f, .01f, -1f, -2f, -3f, 2),
            "Negative vertical tolerance clamps to zero.");
        Require(visit.Update(true, 1.5f, 0f, 1.6f, .5f, 1f, 3),
            "A short exit radius must be raised to the enter radius.");
        Require(visit.Update(true, 1.55f, 0f, 1.6f, .5f, 1f, 4),
            "A short configured exit radius must not repeatedly end a valid entry.");
        Require(!visit.Update(true, -.1f, 0f, 1.6f, 1.85f, 1f, 5),
            "Negative horizontal measurements are invalid, not a proximity match.");
    }

    private static void InvalidDataAndClockReset()
    {
        var visit = new CSYGuideVisit();
        float[] invalid = { float.NaN, float.PositiveInfinity, float.NegativeInfinity };
        foreach (float value in invalid)
        {
            Tick(visit, 0f, 1);
            visit.MarkPlayed();
            Require(!visit.Update(true, value, 0, 1, 2, 1, 2), "Reject non-finite horizontal distance.");
            Require(!visit.Played, "Invalid measurement clears stale playback state.");
            Require(!visit.Update(true, 0, value, 1, 2, 1, 2), "Reject non-finite vertical distance.");
            Require(!visit.Update(true, 0, 0, value, 2, 1, 2), "Reject non-finite entry configuration.");
            Require(!visit.Update(true, 0, 0, 1, value, 1, 2), "Reject non-finite exit configuration.");
            Require(!visit.Update(true, 0, 0, 1, 2, value, 2), "Reject non-finite vertical configuration.");
            Tick(visit, 0, 3);
            Require(!visit.Ready(4, value), "Non-finite dwell must never become ready.");
        }

        foreach (double time in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Tick(visit, 0f, 1);
            Require(!visit.Ready(time, .6f), "Non-finite time must never trigger readiness.");
            Require(!visit.Update(true, 0, 0, 1, 2, 1, time), "Non-finite time ends the visit.");
        }

        Tick(visit, 0f, 100);
        Require(!visit.Ready(99, .6f), "A backwards clock must not satisfy dwell.");
        Require(Tick(visit, 0f, 1) && visit.EnteredAt == 1, "Clock reset begins a fresh dwell interval.");
        Require(!visit.Ready(1.1, .6f), "Clock reset must not use the old elapsed interval.");
        Require(visit.Ready(1.7, .6f), "Playback becomes ready normally after a clock reset.");
    }
}
