# 🛡️ Clan Helper

**Clan Helper** is a convenient portable utility (WPF / .NET 8) designed for quick access and downloading of tools frequently used for clan checks (Everything, SystemInformer, and custom checkers). 

The application is compiled into a single standalone `.exe` file, allowing it to be run instantly without installing any additional components or runtimes.

---

## ✨ Key Features

- **📥 Quick Downloads:** Download the necessary utilities (`.exe` files) directly from the app to any folder on your PC.
- **🌐 Access to Official Sites:** Quick links to the developers' official web pages or the latest GitHub releases.
- **📁 Smart Navigation:** Once a file is downloaded, you can open its destination folder with a single click.
- **🎨 UI Customization:** - 8 built-in color themes.
  - Dynamic background animations (snowflakes, stars, leaves, etc.) that adapt to the selected theme.
  - Option to disable background animations in the settings to save system resources.

---

## 🚀 How to Use

1. Download the latest version of `ClanHelper.exe` from the **[Releases](../../releases/latest)** section.
2. Run the executable file (no installation required).
3. Choose the required tool from the list.
4. Click **"📥 Download"**, select a folder on your PC, and wait for the success message.
5. Click **"📁 Open folder"** to navigate directly to the downloaded file in Windows Explorer.

---

## 🎨 Themes Showcase

Here is a preview of the available built-in themes you can switch between in the settings:

| Default Dark Blue | Orange | Midnight Purple | Ice Gray |
| :---: | :---: | :---: | :---: |
| ![Default Dark Blue](image/deff.png) | ![Orange](image/orange.png) | ![Midnight Purple](image/night.png) | ![Ice Gray](image/blek.png) |

| Red Alert | Purple Neon | Forest | Solar Light |
| :---: | :---: | :---: | :---: |
| ![Red Alert](image/red.png) | ![Purple Neon](image/purpure.png) | ![Forest](image/green.png) | ![Solar Light](image/yellow.png) |

---

## 🛠️ For Developers (Build it yourself)

This project is written in C# using the WPF framework.

**Requirements:**
- .NET 8.0 SDK

**Building a single-file executable:**
To compile the project into a single standalone `.exe` file (which includes all necessary `.dll` libraries inside), open the terminal in the project folder and run the following command:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
