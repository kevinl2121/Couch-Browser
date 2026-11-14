# Couch Browser

A controller-first, private-by-default web browser for Steam Big Picture on Windows.
Built on WebView2 (Microsoft's Chromium engine). Reads Xbox controllers directly, with its own on-screen keyboard.

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Output: `dist\CouchBrowser.exe`. The script installs the .NET 8 SDK if it's missing.

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