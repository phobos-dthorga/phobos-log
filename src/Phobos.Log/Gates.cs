namespace Phobos.Log
{
    /// <summary>
    /// "Only once" for one call site: keep it in a static field beside the call and ask <see cref="PhobosLog.Once"/>. It opens again
    /// in each new session (a new game or a load), so a fault met in one colony is still reported in the next. A struct with one int:
    /// the closed path is a compare and allocates nothing.
    /// </summary>
    public struct OnceGate
    {
        int session;

        internal bool TryPass(int current)
        {
            if (session == current) return false;
            session = current;
            return true;
        }
    }

    /// <summary>
    /// "At most once per so many ticks" for one call site (or one key a caller keeps): a static field beside the call, asked through
    /// <see cref="PhobosLog.Every"/>. A last tick later than now (another game was loaded) counts as never, so a throttle does not
    /// stay shut after a load.
    /// </summary>
    public struct Throttle
    {
        public const long Never = -1;

        long last;
        bool started;

        public static bool IsDue(long now, long lastTick, long intervalTicks) =>
            lastTick < 0 || now < lastTick || now - lastTick >= intervalTicks;

        internal bool TryPass(long now, long intervalTicks)
        {
            if (started && !IsDue(now, last, intervalTicks)) return false;
            started = true;
            last = now;
            return true;
        }
    }
}
