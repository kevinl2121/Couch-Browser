# Couch Browser

A web browser built to be driven entirely by an Xbox controller — no mouse, no keyboard,
no Steam Input remapping. Made for watching, searching and browsing from the couch, as a
non-Steam game tile in Steam Big Picture.

Built on WebView2 (the Chromium engine that ships with Windows).

## Features

- **Full controller navigation** — D-pad jumps between links and buttons on the page
  (not just a mouse-emulated cursor, though the right stick gives you a free cursor too)
- **Private by default** — new tabs save nothing (no history, cookies or form data);
  flip a tab to normal mode when you actually want to stay logged in somewhere
- **On-screen keyboard** built for a controller, with letters/symbols layouts
- **Tabs**, zoom, and a Menu overlay for everything else
- Everything scales for a TV viewed from a couch

## Requirements

- Windows 10/11, 64-bit
- An Xbox (XInput-compatible) controller
- To **build**: nothing extra — `build.ps1` installs the .NET 8 SDK for you if it's missing
- To **run** the built exe: nothing — it's a self-contained single file, no .NET install needed

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Output: `dist\CouchBrowser.exe`. Copy that one file anywhere; it runs standalone.

## Optional: ad blocker

Couch Browser can load unpacked Chromium extensions from
`%LOCALAPPDATA%\CouchBrowser\Extensions\<name>\`. To add uBlock Origin Lite:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\get-ublock-lite.ps1
```

Rerun it anytime to update. Only Manifest V3 extensions work.

## Add to Steam

1. Steam → Games → Add a Non-Steam Game → Browse → `dist\CouchBrowser.exe`
2. Properties → Controller → **Disable Steam Input**
3. Optional: launch option `--normal` to start in normal (logged-in) mode

## Controls

| Button | Browsing | Keyboard open |
|---|---|---|
| D-pad | Jump between links/buttons | Move between keys |
| A | Click (opens keyboard on text boxes) | Type key |
| B (tap) | Back / exit fullscreen video | Close keyboard |
| B (hold ~0.7s) | Close tab | |
| X | Address bar | Delete |
| Y | Type into selected text box | Space |
| LB / RB | Previous / next tab | LB = Shift |
| LT / RT | Page up / down | |
| Left stick | Smooth scroll | |
| Right stick | Free cursor | |
| Menu | Tabs, mode switch, zoom, exit | Go / Enter |
| View | Google home | |
| View + Menu (hold ~0.8s) | Exit Couch Browser (works on any screen) | |

## Project layout

- `src/BrowserForm.cs`: main window, tabs, layout
- `src/BrowserTab.cs`: one tab (WebView2, private or normal)
- `src/Input/`: controller reading and button mappings
- `src/Web/`: page interaction and the injected `nav.js`
- `src/UI/`: top bar, keyboard, menu, theme

Normal-mode data is stored in `%LOCALAPPDATA%\CouchBrowser\Data`. Private tabs store nothing.