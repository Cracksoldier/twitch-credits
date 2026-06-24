// Streamer.bot Action: Chat_emote_counter
// Trigger: Twitch Chat Message (every message)
// Purpose: Counts emote usages and persists them in the global variable "emoteUsageCounts"

using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

public class CPHInline
{
    private class EmoteEntry
    {
        public int Count { get; set; }
        public string Url { get; set; }
    }

    public bool Execute()
    {
        CPH.TryGetArg("emoteCount", out int emoteCount);
        if (emoteCount == 0) return true;

        CPH.TryGetArg("emotes", out object emotesObj);
        if (emotesObj == null) return true;

        var emoteList = emotesObj as System.Collections.IList;
        if (emoteList == null || emoteList.Count == 0) return true;

        string json = CPH.GetGlobalVar<string>("emoteUsageCounts", true) ?? "{}";
        var counts = JsonConvert.DeserializeObject<Dictionary<string, EmoteEntry>>(json)
                     ?? new Dictionary<string, EmoteEntry>();

        foreach (var emote in emoteList)
        {
            if (emote == null) continue;
            string emoteName = null;
            foreach (string propName in new[] { "Name", "EmoteName", "Text", "Code", "Id" })
            {
                PropertyInfo prop = emote.GetType().GetProperty(propName);
                if (prop != null)
                {
                    emoteName = prop.GetValue(emote)?.ToString();
                    if (!string.IsNullOrEmpty(emoteName)) break;
                }
            }
            if (string.IsNullOrEmpty(emoteName)) continue;
            if (!emoteName.StartsWith("monkdr", StringComparison.OrdinalIgnoreCase)) continue;

            string imageUrl = null;
            PropertyInfo urlProp = emote.GetType().GetProperty("ImageUrl");
            if (urlProp != null)
                imageUrl = urlProp.GetValue(emote)?.ToString();

            if (!counts.ContainsKey(emoteName))
                counts[emoteName] = new EmoteEntry { Count = 0, Url = imageUrl };
            else if (!string.IsNullOrEmpty(imageUrl))
                counts[emoteName].Url = imageUrl;

            counts[emoteName].Count++;
        }

        CPH.SetGlobalVar("emoteUsageCounts", JsonConvert.SerializeObject(counts), true);
        return true;
    }
}
