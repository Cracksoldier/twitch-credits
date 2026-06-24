# Twitch Scrolling Credits Overlay

An end-of-stream scrolling credits overlay for OBS, powered by [Streamer.bot](https://streamer.bot). Displays followers, subscribers, moderators, VIPs, raiders, cheers, and the top used channel emotes — all in a single self-contained HTML file with a transparent background.

## Features

- Scrolling credits with animated scroll speed scaled to content length
- Pulls live session data (follows, subs, cheers, raids, etc.) from Streamer.bot via WebSocket
- Tracks channel emote usage across the stream and shows the top 5 with images and counts
- German section titles (easily customizable in the `headers` array)
- Deduplicates viewer names across sections (moderators won't appear again under subscribers, etc.)
- Transparent background — works directly as an OBS browser source

## Requirements

- [OBS Studio](https://obsproject.com/) 28+ (built-in WebSocket server)
- [Streamer.bot](https://streamer.bot/) 1.0.4+
- Internet access at stream time (Google Fonts: Pirata One, Cinzel)

## Files

| File | Purpose |
|---|---|
| `Scrolling Credits for SB.html` | OBS browser source overlay |
| `Action1_Chat_emote_counter.cs` | Streamer.bot C# action — counts emote usage per chat message |
| `Action2_Add_emote_credit.cs` | Streamer.bot C# action — writes top 5 emotes to `emotes.json` on scene change |

## Setup

### 1. OBS browser source

Add `Scrolling Credits for SB.html` as a browser source in OBS. Set the resolution to match your canvas (e.g. 1920×1080) and enable **"Refresh browser when scene becomes active"**.

### 2. Streamer.bot — Action 1: Chat emote counter

1. Create a new action called `Chat_emote_counter`
2. Add trigger: **Twitch → Chat Message** (fires on every message)
3. Add sub-action: **Execute C# Code** → paste the contents of `Action1_Chat_emote_counter.cs`

This action counts every `monkdr*` emote used in chat and persists the totals (plus image URLs) in the global variable `emoteUsageCounts`.

### 3. Streamer.bot — Action 2: Add emote credit

1. Create a new action called `Add_emote_credit`
2. Add trigger: **OBS → Scene Changed** → filter to your ending screen scene name
3. Add sub-action: **Execute C# Code** → paste the contents of `Action2_Add_emote_credit.cs`
4. Optionally add a **Core → Sleep (1500 ms)** sub-action **after** the C# sub-action

> **Sub-action order is critical:** the C# execute must come **before** any Sleep. If Sleep runs first, the browser source will have already fetched the stale `emotes.json` from the previous stream before the new one is written.

Update the file path in `Action2_Add_emote_credit.cs` to point to the folder where your HTML overlay lives:

```csharp
string path = @"E:\stream\Credits - Simple Scrolling\emotes.json";
```

### 4. Streamer.bot WebSocket

Make sure Streamer.bot's WebSocket server is running on `ws://localhost:8080` (the default). The overlay connects to this on load to fetch credits data.

## Customization

### Section titles and order

Edit the `headers` array near the top of the `<script>` block in the HTML file. Each entry controls one section:

```js
{ section: "events", key: "follows", title: "Neue Follower" }
```

Change `title` for a different display name, reorder entries to change scroll order, or remove entries to hide sections entirely.

### Emote filter

Action 1 only counts emotes whose names start with `monkdr` (case-insensitive). To track different emotes, change the prefix check in `Action1_Chat_emote_counter.cs`:

```csharp
if (!emoteName.StartsWith("monkdr", StringComparison.OrdinalIgnoreCase)) continue;
```

### Top N emotes

Change `int topN = 5;` in `Action2_Add_emote_credit.cs` to show more or fewer emotes.

### Scroll speed

Speed is derived from content height:

```js
const duration = ((container.offsetHeight / window.innerHeight * 100) + 100) * 160;
```

Increase the multiplier (`160`) to scroll slower, decrease it to scroll faster.

## How it works

```
Stream running
    └── Every chat message
            └── Action 1: count monkdr* emotes → save to global "emoteUsageCounts"

Scene switches to ending screen
    └── Action 2: read emoteUsageCounts → write top 5 to emotes.json
            └── OBS reloads browser source
                    └── Overlay connects to Streamer.bot WebSocket
                            └── Receives GetCredits response (viewers, subs, follows, …)
                            └── After 2s delay: fetches emotes.json
                            └── Builds and animates the credits DOM
```
