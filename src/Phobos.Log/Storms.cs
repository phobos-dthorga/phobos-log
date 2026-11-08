using System;
using System.Collections.Generic;
using System.Text;

namespace Phobos.Log
{
    /// <summary>
    /// A message's signature, so a storm of near-identical messages counts as one: its first line, plus, for an exception, the first
    /// frame of its own trace (where it was thrown), with numbers, hexadecimal and RimWorld's "[Ref ...]" tags blanked. Made for
    /// counting inside a game, cheaply; phobos-modkit's logs/signature.py is the thorough one, for reports after the game.
    /// </summary>
    public static class LogSignature
    {
        public const int HeadLimit = 120;
        public const int FrameLimit = 100;

        public static string Of(string? message)
        {
            if (message == null || message.Length == 0) return "(empty)";
            int end = message.IndexOf('\n');
            string head = (end < 0 ? message : message.Substring(0, end)).Trim();
            string frame = FirstFrame(message, end);
            var key = new StringBuilder(head.Length + frame.Length + 3);
            AppendBlanked(key, head, HeadLimit);
            if (frame.Length > 0)
            {
                key.Append(" @ ");
                AppendBlanked(key, frame, FrameLimit);
            }
            return key.ToString();
        }

        /// <summary>The method of the first "at ..." line after the first line, without its " in file" part; "" when there is none.</summary>
        static string FirstFrame(string text, int from)
        {
            while (from >= 0 && from < text.Length)
            {
                int next = text.IndexOf('\n', from + 1);
                string line = (next < 0 ? text.Substring(from + 1) : text.Substring(from + 1, next - from - 1)).Trim();
                if (line.StartsWith("at ", StringComparison.Ordinal))
                {
                    int cut = line.IndexOf(" in ", StringComparison.Ordinal);
                    return (cut < 0 ? line.Substring(3) : line.Substring(3, cut - 3)).Trim();
                }
                from = next;
            }
            return "";
        }

        /// <summary>Appends text cut at limit, each run of digits as one '#', "0x" hexadecimal as '#', and "[Ref ...]" dropped.</summary>
        static void AppendBlanked(StringBuilder key, string text, int limit)
        {
            int written = 0;
            for (int i = 0; i < text.Length && written < limit; i++)
            {
                char c = text[i];
                if (c == '[' && string.CompareOrdinal(text, i, "[Ref ", 0, 5) == 0)
                {
                    int close = text.IndexOf(']', i);
                    if (close > 0)
                    {
                        i = close;
                        continue;
                    }
                }
                if (c == '0' && i + 1 < text.Length && (text[i + 1] == 'x' || text[i + 1] == 'X'))
                {
                    i += 2;
                    while (i < text.Length && Uri.IsHexDigit(text[i])) i++;
                    i--;
                    key.Append('#');
                    written++;
                    continue;
                }
                if (char.IsDigit(c))
                {
                    while (i + 1 < text.Length && char.IsDigit(text[i + 1])) i++;
                    key.Append('#');
                    written++;
                    continue;
                }
                key.Append(c);
                written++;
            }
        }
    }

    /// <summary>A bounded count of signatures; not thread-safe (callers that count on several threads hold a lock of their own).</summary>
    public sealed class StormTally
    {
        public const int DefaultCapacity = 512;

        readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        readonly int capacity;

        public StormTally(int capacity = DefaultCapacity) => this.capacity = capacity;

        public int Total { get; private set; }
        public int Others { get; private set; }

        public void Add(string signature)
        {
            Total++;
            if (counts.TryGetValue(signature, out int count)) counts[signature] = count + 1;
            else if (counts.Count < capacity) counts[signature] = 1;
            else Others++;
        }

        /// <summary>The most frequent signatures, most first.</summary>
        public List<KeyValuePair<string, int>> Top(int howMany)
        {
            var all = new List<KeyValuePair<string, int>>(counts);
            all.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (all.Count > howMany) all.RemoveRange(howMany, all.Count - howMany);
            return all;
        }

        public void Clear()
        {
            counts.Clear();
            Total = 0;
            Others = 0;
        }
    }
}
