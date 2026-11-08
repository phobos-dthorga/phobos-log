namespace Phobos.Log
{
    /// <summary>How serious a line is. Hosts map these to their own levels.</summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// Where a host writes a line: the game's own log, under a category of its own where the host has categories (RimWorld 1.7's
    /// engine), or with the category as a prefix where it has none (BepInEx, Lua). The core never formats for a host.
    /// </summary>
    public interface ILogSink
    {
        void Write(string category, LogLevel level, string text);
    }

    /// <summary>The host's clocks: game time for throttles ("once an in-game hour"), real time for budgets ("per ten minutes").</summary>
    public interface ILogClock
    {
        /// <summary>The game's tick count, or a negative number when no game is running.</summary>
        long GameTicks { get; }

        /// <summary>Seconds of real time, from any fixed start, never going backwards.</summary>
        double RealSeconds { get; }
    }
}
