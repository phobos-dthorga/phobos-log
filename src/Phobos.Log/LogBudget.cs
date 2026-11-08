using System;

namespace Phobos.Log
{
    /// <summary>
    /// How many lines one mod may write per window of real time, by level, so a fault of ours can never become the storm that fills a
    /// game's log (RimWorld stops every mod's logging at 10,000 messages a session). Past a level's allowance, lines are counted, not
    /// written, and the first line after the window turns says how many were held back. Shared by every part of one mod.
    /// Agent defaults (phobos-rimworld's logging record): ten minutes; 400 Debug and Info, 200 Warning, 50 Error.
    /// </summary>
    public sealed class LogBudget
    {
        public const double DefaultWindowSeconds = 600;
        public const int DefaultInfo = 400;
        public const int DefaultWarnings = 200;
        public const int DefaultErrors = 50;

        readonly object gate = new object();
        readonly double windowSeconds;
        readonly int[] allowance;
        readonly int[] written = new int[4];
        int heldBack;
        double windowStart = double.NaN;

        public LogBudget(double windowSeconds = DefaultWindowSeconds, int info = DefaultInfo, int warnings = DefaultWarnings, int errors = DefaultErrors)
        {
            if (windowSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(windowSeconds));
            this.windowSeconds = windowSeconds;
            allowance = new[] { info, info, warnings, errors };
        }

        /// <summary>The window in words, for the held-back line: "minute", "ten minutes", "90 seconds".</summary>
        public string WindowText
        {
            get
            {
                if (windowSeconds == 60) return "minute";
                if (windowSeconds % 60 == 0)
                {
                    int minutes = (int)(windowSeconds / 60);
                    return minutes == 10 ? "ten minutes" : minutes + " minutes";
                }
                return windowSeconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " seconds";
            }
        }

        /// <summary>Lines held back in the current window, for a status line.</summary>
        public int HeldBack
        {
            get { lock (gate) return heldBack; }
        }

        /// <summary>
        /// Whether a line at this level may be written now. When a new window begins after lines were held back, heldBackLastWindow
        /// says how many, so the caller can write one line about it first; otherwise it is 0.
        /// </summary>
        public bool TryAdmit(LogLevel level, double now, out int heldBackLastWindow)
        {
            lock (gate)
            {
                heldBackLastWindow = 0;
                if (double.IsNaN(windowStart) || now < windowStart || now - windowStart >= windowSeconds)
                {
                    heldBackLastWindow = heldBack;
                    heldBack = 0;
                    Array.Clear(written, 0, written.Length);
                    windowStart = now;
                }
                int index = (int)level;
                if (written[index] >= allowance[index])
                {
                    heldBack++;
                    return false;
                }
                written[index]++;
                return true;
            }
        }
    }
}
