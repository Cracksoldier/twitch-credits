// Streamer.bot Action: Add_emote_credit
// Trigger: OBS Scene Changed (ending screen scene)
// Purpose: Reads emoteUsageCounts global, writes top 5 emotes to emotes.json for the overlay to fetch
// Note: emotes.json must be in the same folder as the HTML overlay (resolved via ./ in fetch)

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        int topN = 5;

        string json = CPH.GetGlobalVar<string>("emoteUsageCounts", true) ?? "{}";
        var counts = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);

        if (counts == null || counts.Count == 0)
        {
            File.WriteAllText(@"E:\stream\Credits - Simple Scrolling\emotes.json", "[]");
            return true;
        }

        // Sort descending by count — System.Linq is not available in Streamer.bot 1.0.4
        var entries = new List<KeyValuePair<string, int>>(counts);
        entries.Sort((a, b) => b.Value.CompareTo(a.Value));

        var topEmotes = new List<string>();
        for (int i = 0; i < entries.Count && i < topN; i++)
            topEmotes.Add(string.Format("{0} ({1}x)", entries[i].Key, entries[i].Value));

        File.WriteAllText(@"E:\stream\Credits - Simple Scrolling\emotes.json", JsonConvert.SerializeObject(topEmotes));
        CPH.SetGlobalVar("emoteUsageCounts", "{}", true);
        return true;
    }
}
