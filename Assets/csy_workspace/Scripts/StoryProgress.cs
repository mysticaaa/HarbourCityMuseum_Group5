using System;
using System.Collections.Generic;

namespace CsyMuseum
{
    // Pure state: showing or turning a page never claims that it was read.
    public sealed class StoryProgress
    {
        private readonly string[] ids;
        private readonly HashSet<string> read = new HashSet<string>();
        public int Index { get; private set; }
        public int Count => ids.Length;
        public int ReadCount => read.Count;
        public bool CurrentRead => read.Contains(ids[Index]);
        public StoryProgress(string[] chapterIds)
        {
            if (chapterIds == null || chapterIds.Length == 0) throw new ArgumentException("No story chapters");
            ids = (string[])chapterIds.Clone();
            var unique = new HashSet<string>();
            foreach (string id in ids)
                if (string.IsNullOrWhiteSpace(id) || !unique.Add(id)) throw new ArgumentException("Invalid or duplicate story id");
        }
        public void Select(int index) { Index = Math.Max(0, Math.Min(ids.Length - 1, index)); }
        public void MarkRead() { read.Add(ids[Index]); }
        public void Reset() { read.Clear(); Index = 0; }
    }

    [Serializable] public sealed class StoryChapter
    {
        public string id, period, title, body, prompt, sourceLabel, sourceUrl, disclosure;
    }
    [Serializable] public sealed class StoryCatalogue { public StoryChapter[] chapters; }
}
