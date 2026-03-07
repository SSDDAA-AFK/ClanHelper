# 🛡️ Clan Helper

**Clan Helper** is a portable utility (WPF / .NET 8) for quick downloading and launching of tools commonly used for clan checks (Everything, SystemInformer, and custom checkers).

The application is distributed as a **single standalone `.exe`**, so it runs instantly without installation or additional runtimes.

---

# ✨ Features

- **▶️ Direct Launch** — run downloaded tools instantly from the app.
- **📥 Smart Downloads** — download `.exe` tools to any folder with duplicate protection.
- **📊 Advanced Download Manager**
  - Shows **download speed (MB/s)** and **ETA**
  - Smooth progress bar during downloads
- **📁 Quick File Access** — "Open folder" highlights the downloaded `.exe` in Explorer.
- **💾 Auto Configuration Save** — settings stored in  
  `Documents/CHelper/config.json`.
- **🌐 Official Links** — quick access to developer websites or GitHub releases.

---

# 🆕 New in v1.3

### 🧩 Mini Mode
Switch the app to a **compact widget view (180x180)** to keep it on the desktop without taking much space.

### 🔊 Startup Sounds
A **random greeting sound** plays when the app launches.  
This feature can be disabled in settings.

### ✨ Icon Animation
While the startup sound plays, the **title bar icon softly pulses**.

---

# 🎨 UI Customization

- **8 built-in themes**
- Smooth UI transitions
- Custom title bar
- Animated particles (snow, stars, etc.)
- Typewriter text effects
- Focus Mode (dim background when settings open)

### ⚡ Eco Mode
Disables animations, particles, and transitions for **better performance on low-end PCs**.

---

# ⚙️ Performance Improvements

- Particle system moved from  
`DispatcherTimer` → `CompositionTarget.Rendering`
- Provides **smoother animations (~60 FPS)**.

---

# 🛠 Improvements & Fixes

- More reliable config system (`Documents/CHelper/config.json`)
- Automatic cleanup of corrupted or unfinished **sound/video files**
- Fixed animation overlap when switching tabs quickly

---

# 🚀 How to Use

1. Download **ClanHelper.exe** from **Releases**.
2. Run the file (no installation required).
3. Select a tool.
4. Click **📥 Download** and choose a folder.
5. Use **▶ Launch** to run or **📁 Open folder** to locate the file.

---

# 🎨 Themes

| Default | Orange | Midnight Purple | Ice Gray |
| :---: | :---: | :---: | :---: |
| ![](image/deff.png) | ![](image/orange.png) | ![](image/night.png) | ![](image/blek.png) |

| Red Alert | Purple Neon | Forest | Solar Light |
| :---: | :---: | :---: | :---: |
| ![](image/red.png) | ![](image/purpure.png) | ![](image/green.png) | ![](image/yellow.png) |

---

# 🛠 Build (for developers)

Requirements:
- **.NET 8 SDK**

Build a single executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true \
-p:PublishSingleFile=true \
-p:IncludeNativeLibrariesForSelfExtract=true
```

Developer: SSDDAA-AFK
