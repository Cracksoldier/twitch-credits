# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A single-file Twitch stream overlay that displays end-of-stream scrolling credits. It is loaded as an OBS browser source with a transparent background.

## How it works

On load, the page opens a WebSocket connection to `ws://localhost:8080` (Streamer.bot running locally). It sends a `GetCredits` request, receives a JSON payload with the session's viewer data, builds the credits DOM, then animates it scrolling upward using the Web Animations API.

### WebSocket message structure

The incoming `credits` message contains these top-level sections:

| Section | Keys |
|---|---|
| `events` | `follows`, `cheers`, `subs`, `reSubs`, `giftSubs`, `giftBombs`, `raided`, `rewardRedemptions`, `goalContributions`, `pyramids` |
| `users` | `editors`, `moderators`, `subscribers`, `vips`, `users` |
| `groups` | custom group names |
| `hypeTrain` | `conductors`, `contributors` |
| `top` | `allBits`, `monthBits`, `weekBits`, `channelRewards` |

The `headers` array in the JS controls display order, German section titles, and which sections deduplicate names via `filterExistingEntries: true`.

## Development

The project uses **Streamer.bot** as the backend. Open the HTML file directly in a browser for layout/style work. To test the full flow, Streamer.bot must be running locally on port 8080.

Scroll speed is derived from content height: `duration = ((container.offsetHeight / window.innerHeight * 100) + 100) * 160` ms.

Fonts (Pirata One, Cinzel) are loaded from Google Fonts — requires internet access at runtime.

## Streamer.bot C# actions

### Emote tracking

The `%emotes%` trigger variable is a `List<Twitch.Common.Models.Emote>` object — not a string. Retrieving it with `TryGetArg<string>` returns the type name, not data. The correct approach is to retrieve it as `object`, cast to `System.Collections.IList`, then use reflection to read the emote name property:

```csharp
CPH.TryGetArg("emotes", out object emotesObj);
var emoteList = emotesObj as System.Collections.IList;
foreach (var emote in emoteList)
{
    PropertyInfo prop = emote.GetType().GetProperty("Name"); // confirmed property name
    string emoteName = prop?.GetValue(emote)?.ToString();
}
```

Each item in the list is one emote occurrence, so four `LUL` in a message = four list entries.

### Global variables

Always use `true` (persisted) as the third parameter of `GetGlobalVar`/`SetGlobalVar`. Using `false` (non-persisted/temporary) stores the variable in memory only — it won't appear in the Streamer.bot Globals UI and is wiped on restart.

### Arg access

Always use `CPH.TryGetArg()` — never access the `args` dictionary directly. Direct access is unsupported in 1.0.x and can crash the code instance.

### System.Linq is unavailable

`System.Linq` is not available in Streamer.bot 1.0.4's C# inline environment — `using System.Linq;` causes a compile error and the action silently fails to run. Use `List<T>.Sort()` with a comparison delegate instead of `OrderByDescending`, and avoid all other LINQ extension methods (`Select`, `Where`, `Take`, etc.).

```csharp
// Instead of .OrderByDescending(kv => kv.Value).Take(5):
var entries = new List<KeyValuePair<string, int>>(counts);
entries.Sort((a, b) => b.Value.CompareTo(a.Value));
for (int i = 0; i < entries.Count && i < 5; i++) { ... }
```

### CPH.AddToCreditsCustom does not exist in 1.0.4

`CPH.AddToCreditsCustom` is not defined on `IInlineInvokeProxy` in 1.0.4 and causes a compile error. The Credits `custom` section also does not get populated via the WebSocket `GetCredits` response. The workaround is to write emote data to a JSON file (`emotes.json` in the project folder) from C# using `System.IO.File.WriteAllText`, and have the overlay fetch it via `fetch('./emotes.json')`.

Always use a relative path (`./emotes.json`) — not an absolute `file:///` URL. Chrome blocks `fetch('file://')` from `file://` pages with a CORS error; a relative path avoids this and works in both Chrome (for dev testing) and OBS's browser source.

### Triggering actions from OBS scene changes

Streamer.bot connects to OBS via WebSocket (`Settings → OBS WebSocket`; built into OBS Studio 28+). Actions can be triggered on scene switches via **OBS → Scene Changed**, filtered to a specific scene name.

The "Add Emotes to Credits" action uses this trigger on the ending screen scene.

**Sub-action order is critical.** The C# execute sub-action must come **before** any Sleep:

```
✓ CORRECT:  [Execute C#] → [Core: Sleep 1500ms (optional)]
✗ WRONG:    [Core: Sleep 1500ms] → [Execute C#]
```

If the Sleep comes first, the browser source refreshes and fetches `emotes.json` (which still has the previous stream's data) before the C# has a chance to write the new file. The overlay HTML includes its own 2000ms delay before the fetch to provide an additional safety margin — the C# write must still happen before that window closes.
