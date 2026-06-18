// Streamer.bot Action: Chat_emote_counter
// Trigger: Twitch Chat Message (every message)
// Purpose: Counts emote usages and persists them in the global variable "emoteUsageCounts"

using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

public class CPHInline
{
    public bool Execute()
    {
        CPH.TryGetArg("emoteCount", out int emoteCount);
        if (emoteCount == 0) return true;

        CPH.TryGetArg("emotes", out object emotesObj);
        if (emotesObj == null) return true;

        var emoteList = emotesObj as System.Collections.IList;
        if (emoteList == null || emoteList.Count == 0) return true;

        string json = CPH.GetGlobalVar<string>("emoteUsageCounts", true) ?? "{}";
        var counts = JsonConvert.DeserializeObject<Dictionary<string, int>>(json)
                     ?? new Dictionary<string, int>();

        // Dump emote object properties to log for inspection — remove once confirmed
        if (emoteList.Count > 0 && emoteList[0] != null)
        {
            var props = emoteList[0].GetType().GetProperties();
            var sb = new System.Text.StringBuilder();
            foreach (var p in props)
                sb.Append(p.Name + "=" + p.GetValue(emoteList[0]) + " | ");
            CPH.LogInfo("[EmoteDebug] " + sb.ToString());
        }

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
            if (!counts.ContainsKey(emoteName)) counts[emoteName] = 0;
            counts[emoteName]++;
        }

        CPH.SetGlobalVar("emoteUsageCounts", JsonConvert.SerializeObject(counts), true);
        return true;
    }
}
