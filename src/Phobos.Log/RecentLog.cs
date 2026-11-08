using System;
using System.Collections.Generic;

namespace Phobos.Logging
{
    /// <summary>The last lines every <see cref="PhobosLog"/> wrote in this process, oldest first, for a "copy the recent log" action.</summary>
    public static class RecentLog
    {
        public const int Capacity = 200;

        public readonly struct Line
        {
            public Line(DateTime time, string category, LogLevel level, string text)
            {
                Time = time;
                Category = category;
                Level = level;
                Text = text;
            }

            public DateTime Time { get; }
            public string Category { get; }
            public LogLevel Level { get; }
            public string Text { get; }
        }

        static readonly Line[] ring = new Line[Capacity];
        static int next;
        static int count;

        internal static void Add(string category, LogLevel level, string text)
        {
            lock (ring)
            {
                ring[next] = new Line(DateTime.Now, category, level, text);
                next = (next + 1) % Capacity;
                if (count < Capacity) count++;
            }
        }

        public static List<Line> Snapshot()
        {
            lock (ring)
            {
                var lines = new List<Line>(count);
                int start = (next - count + Capacity) % Capacity;
                for (int i = 0; i < count; i++) lines.Add(ring[(start + i) % Capacity]);
                return lines;
            }
        }

        public static void Clear()
        {
            lock (ring)
            {
                next = 0;
                count = 0;
            }
        }
    }
}
