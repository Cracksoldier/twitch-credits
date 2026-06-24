// Streamer.bot Action: Add_emote_credit
// Trigger: OBS Scene Changed (ending screen scene)
// Purpose: Reads emoteUsageCounts global, writes top 5 emotes to emotes.json for the overlay to fetch
// Note: emotes.json must be in the same folder as the HTML overlay (resolved via ./ in fetch)
//
// Sub-action order in Streamer.bot matters:
//   CORRECT:  [Execute C# (this file)] → [Core: Sleep 1500ms]
//   WRONG:    [Core: Sleep 1500ms] → [Execute C# (this file)]   ← causes the "previous stream" bug
//
// The browser source loads and fetches emotes.json almost immediately after the scene change.
// If the Sleep runs first, the C# write happens too late and the browser reads stale data.
// The HTML overlay already includes its own 2000ms fetch delay as a safety margin, so the
// Sleep sub-action in Streamer.bot can also be removed entirely if preferred.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class CPHInline
{
    private class EmoteEntry
    {
        public int Count { get; set; }
        public string Url { get; set; }
    }

    private class EmoteOutput
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public string Url { get; set; }
    }

    public bool Execute()
    {
        int topN = 5;
        string path = @"E:\stream\Credits - Simple Scrolling\emotes.json";

        string json = CPH.GetGlobalVar<string>("emoteUsageCounts", true) ?? "{}";
        var counts = JsonConvert.DeserializeObject<Dictionary<string, EmoteEntry>>(json);

        if (counts == null || counts.Count == 0)
        {
            File.WriteAllText(path, "[]");
            return true;
        }

        // Sort descending by count — System.Linq is not available in Streamer.bot 1.0.4
        var entries = new List<KeyValuePair<string, EmoteEntry>>(counts);
        entries.Sort((a, b) => b.Value.Count.CompareTo(a.Value.Count));

        var topEmotes = new List<EmoteOutput>();
        for (int i = 0; i < entries.Count && i < topN; i++)
            topEmotes.Add(new EmoteOutput {
                Name = entries[i].Key,
                Count = entries[i].Value.Count,
                Url = entries[i].Value.Url
            });

        File.WriteAllText(path, JsonConvert.SerializeObject(topEmotes));
        CPH.SetGlobalVar("emoteUsageCounts", "{}", true);
        return true;
    }
}
