using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Phobos.Log
{
    /// <summary>
    /// One mod's (or one part's) log. Lines go to the host's sink under the mod's category ("D'thorga Patches: Harrier"), within the
    /// mod's <see cref="LogBudget"/>, and into <see cref="RecentLog"/>. The gates come first so a suppressed line costs a compare and
    /// builds no string:
    /// <code>
    /// static OnceGate missing;
    /// if (Log.Once(ref missing)) Log.Warn("RimPacts' bridge is gone, so its patch is left as it is.");
    /// static Throttle kept;
    /// if (Log.Every(ref kept, GenDate.TicksPerHour)) Log.Info("Kept the map of " + map + " open.");
    /// </code>
    /// <see cref="WarnOnce"/> and <see cref="ErrorOnce"/> key by their caller's file and line, for cold paths. Debug lines are written
    /// only while the host says debug is on (RimWorld: dev mode), so a player can switch them on for a report. Thread-safe.
    /// </summary>
    public sealed class PhobosLog
    {
        static int session = 1;

        readonly ILogSink sink;
        readonly ILogClock clock;
        readonly LogBudget budget;
        readonly Func<bool>? debugOn;
        readonly HashSet<string> onceKeys = new HashSet<string>();
        int onceKeysSession = 1;

        public PhobosLog(string mod, string? part, ILogSink sink, ILogClock clock, LogBudget budget, Func<bool>? debugOn = null)
        {
            Mod = mod ?? throw new ArgumentNullException(nameof(mod));
            Part = part;
            Category = part == null || part.Length == 0 ? mod : mod + ": " + part;
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.budget = budget ?? throw new ArgumentNullException(nameof(budget));
            this.debugOn = debugOn;
        }

        public string Mod { get; }
        public string? Part { get; }

        /// <summary>The category the host writes under: the mod, or "mod: part".</summary>
        public string Category { get; }

        public bool DebugEnabled => debugOn != null && debugOn();

        /// <summary>A new game or a load: every <see cref="OnceGate"/> and once-key opens again. Hosts call it; the core never guesses.</summary>
        public static void NewSession() => System.Threading.Interlocked.Increment(ref session);

        internal static int Session => System.Threading.Volatile.Read(ref session);

        public bool Once(ref OnceGate gate) => gate.TryPass(Session);

        public bool Every(ref Throttle throttle, long intervalTicks) => throttle.TryPass(clock.GameTicks, intervalTicks);

        public void Debug(string text)
        {
            if (DebugEnabled) Write(LogLevel.Debug, text);
        }

        public void Info(string text) => Write(LogLevel.Info, text);

        public void Warn(string text) => Write(LogLevel.Warning, text);

        public void Error(string text) => Write(LogLevel.Error, text);

        public void Error(string text, Exception exception) => Write(LogLevel.Error, text + " " + exception);

        public void WarnOnce(string text, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (FirstTime(file, line)) Write(LogLevel.Warning, text);
        }

        public void ErrorOnce(string text, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (FirstTime(file, line)) Write(LogLevel.Error, text);
        }

        bool FirstTime(string file, int line)
        {
            lock (onceKeys)
            {
                int now = Session;
                if (onceKeysSession != now)
                {
                    onceKeys.Clear();
                    onceKeysSession = now;
                }
                return onceKeys.Add(file + ":" + line);
            }
        }

        void Write(LogLevel level, string text)
        {
            if (!budget.TryAdmit(level, clock.RealSeconds, out int heldBack)) return;
            if (heldBack > 0)
            {
                string note = heldBack + " more line" + (heldBack == 1 ? " was" : "s were") + " held back in the last " + budget.WindowText
                    + ", past " + Mod + "'s log budget (so a repeating fault of ours cannot fill the game's log).";
                sink.Write(Category, LogLevel.Warning, note);
                RecentLog.Add(Category, LogLevel.Warning, note);
            }
            sink.Write(Category, level, text);
            RecentLog.Add(Category, level, text);
        }
    }
}
