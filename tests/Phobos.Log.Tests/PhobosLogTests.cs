using System.Collections.Generic;
using Phobos.Log;
using Xunit;

namespace Phobos.Log.Tests;

sealed class Sink : ILogSink
{
    public readonly List<(string Category, LogLevel Level, string Text)> Lines = new();
    public void Write(string category, LogLevel level, string text) => Lines.Add((category, level, text));
}

sealed class Clock : ILogClock
{
    public long GameTicks { get; set; }
    public double RealSeconds { get; set; }
}

public class PhobosLogTests
{
    static (PhobosLog Log, Sink Sink, Clock Clock) Make(LogBudget? budget = null, bool debug = false)
    {
        var sink = new Sink();
        var clock = new Clock();
        return (new PhobosLog("D'thorga Patches", "Harrier", sink, clock, budget ?? new LogBudget(), () => debug), sink, clock);
    }

    [Fact]
    public void LinesGoUnderTheModAndPartCategory()
    {
        var (log, sink, _) = Make();
        log.Warn("RimPacts' bridge is gone.");
        Assert.Equal(("D'thorga Patches: Harrier", LogLevel.Warning, "RimPacts' bridge is gone."), sink.Lines[0]);
        Assert.Equal("D'thorga Patches", new PhobosLog("D'thorga Patches", null, sink, new Clock(), new LogBudget()).Category);
    }

    [Fact]
    public void DebugLinesOnlyWhileTheHostSaysSo()
    {
        var (quiet, quietSink, _) = Make(debug: false);
        quiet.Debug("hidden");
        Assert.Empty(quietSink.Lines);
        var (loud, loudSink, _) = Make(debug: true);
        loud.Debug("shown");
        Assert.Single(loudSink.Lines);
    }

    static OnceGate gate;

    [Fact]
    public void AOnceGateOpensAgainInANewSession()
    {
        var (log, _, _) = Make();
        gate = default;
        Assert.True(log.Once(ref gate));
        Assert.False(log.Once(ref gate));
        PhobosLog.NewSession();
        Assert.True(log.Once(ref gate));
        Assert.False(log.Once(ref gate));
    }

    [Fact]
    public void WarnOnceKeysByCallSite()
    {
        var (log, sink, _) = Make();
        for (int i = 0; i < 3; i++) log.WarnOnce("same site");
        log.WarnOnce("another site");
        Assert.Equal(2, sink.Lines.Count);
    }

    [Fact]
    public void AThrottleWaitsItsIntervalInGameTicks()
    {
        var (log, _, clock) = Make();
        var throttle = default(Throttle);
        clock.GameTicks = 1000;
        Assert.True(log.Every(ref throttle, 2500));
        clock.GameTicks = 3499;
        Assert.False(log.Every(ref throttle, 2500));
        clock.GameTicks = 3500;
        Assert.True(log.Every(ref throttle, 2500));
        clock.GameTicks = 10;   // another game, loaded since
        Assert.True(log.Every(ref throttle, 2500));
    }

    [Fact]
    public void ABudgetHoldsLinesBackAndSaysHowManyWhenTheWindowTurns()
    {
        var (log, sink, clock) = Make(new LogBudget(windowSeconds: 60, info: 10, warnings: 2, errors: 1));
        for (int i = 0; i < 5; i++) log.Warn("storm " + i);
        Assert.Equal(2, sink.Lines.Count);
        log.Error("first error");
        log.Error("second error");
        Assert.Equal(3, sink.Lines.Count);
        clock.RealSeconds = 61;
        log.Info("after");
        Assert.Equal(5, sink.Lines.Count);
        Assert.Equal(LogLevel.Warning, sink.Lines[3].Level);
        Assert.StartsWith("4 more lines were held back in the last minute", sink.Lines[3].Text);
        Assert.Equal("after", sink.Lines[4].Text);
    }

    [Fact]
    public void TheRecentLogKeepsTheLastLinesOldestFirst()
    {
        RecentLog.Clear();
        var (log, _, _) = Make();
        for (int i = 0; i < RecentLog.Capacity + 5; i++) log.Info("line " + i);
        var lines = RecentLog.Snapshot();
        Assert.Equal(RecentLog.Capacity, lines.Count);
        Assert.Equal("line 5", lines[0].Text);
        Assert.Equal("line " + (RecentLog.Capacity + 4), lines[^1].Text);
    }
}

public class StormTests
{
    const string StormA = "System.NullReferenceException: Object reference not set to an instance of an object.\n[Ref 12B4AFA6]\n"
        + "   at Example.Component.<Drop>b__1(Pawn pawn) in Example.dll:token 0x600007d+0x0\n   at System.Linq.Enumerable.Sum()";
    const string StormB = "System.NullReferenceException: Object reference not set to an instance of an object.\n[Ref 9C0FFEE1]\n"
        + "   at Example.Component.<Drop>b__1(Pawn pawn) in Example.dll:token 0x600007d+0x4\n   at System.Linq.Enumerable.Sum()";
    const string Elsewhere = "System.NullReferenceException: Object reference not set to an instance of an object.\n"
        + "   at Other.Thing.Tick() in Other.dll:token 0x6000001+0x10";

    [Fact]
    public void RepeatsOfOneFaultShareASignatureAndOtherOriginsDoNot()
    {
        Assert.Equal(LogSignature.Of(StormA), LogSignature.Of(StormB));
        Assert.Equal("System.NullReferenceException: Object reference not set to an instance of an object. @ Example.Component.<Drop>b__#(Pawn pawn)",
            LogSignature.Of(StormA));
        Assert.NotEqual(LogSignature.Of(StormA), LogSignature.Of(Elsewhere));
    }

    [Fact]
    public void NumbersAndHexAreBlankedAndLongLinesCut()
    {
        Assert.Equal("Map # at (#, #, #) item #", LogSignature.Of("Map 12 at (135, 0, 208) item 0x1F3A"));
        Assert.Equal(LogSignature.HeadLimit, LogSignature.Of(new string('a', 500)).Length);
        Assert.Equal("(empty)", LogSignature.Of(null));
    }

    [Fact]
    public void TheTallyNamesTheLoudestAndStaysBounded()
    {
        var tally = new StormTally(capacity: 2);
        for (int i = 0; i < 5; i++) tally.Add("storm");
        tally.Add("once");
        tally.Add("third, past the capacity");
        Assert.Equal(new KeyValuePair<string, int>("storm", 5), tally.Top(1)[0]);
        Assert.Equal((7, 1), (tally.Total, tally.Others));
        tally.Clear();
        Assert.Equal(0, tally.Total);
        Assert.Empty(tally.Top(3));
    }

    [Fact]
    public void ThrottleIsDueMatchesItsRule()
    {
        Assert.True(Throttle.IsDue(500, Throttle.Never, 60000));
        Assert.False(Throttle.IsDue(59999, 0, 60000));
        Assert.True(Throttle.IsDue(60000, 0, 60000));
        Assert.True(Throttle.IsDue(1000, 900000, 60000));
    }
}
