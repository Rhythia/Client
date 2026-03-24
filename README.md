<p align="center">
  <img src="https://github.com/Rhythia/Client/blob/master/textures/RhythiaSquircle.png?raw=true" alt="Rhythia" width="400"/>
</p>

<h3 align="center">A free-to-play rhythm game where you move your mouse across a 3×3 grid.</h3>

<p align="center">
  <a href="https://discord.gg/rhythia"><img src="https://img.shields.io/discord/1234567890?label=Discord&logo=discord&logoColor=white&color=5865F2" alt="Discord"></a>
  <img src="https://img.shields.io/badge/Godot-4.6-%23478CBF?logo=godot-engine&logoColor=white" alt="Godot 4.6">
</p>

---

> *"Rhyt-Rhythmia"*
> — Edward "BTMC" Ling, 2024

> [!CAUTION]
> **Rhythia is NOT affiliated with CAPO Games, Steam Rhythia, or rhythia.com in any way.** We cannot provide support or assistance for any issues related to those platforms. Please ensure you are downloading the correct client from the links below. We do, however, support any modification of our logos and content in the client source under our [license](LICENSE).

---

## Table of Contents

- [About](#about)
- [Downloading the Game](#downloading-the-game)
  - [Rewrite (Current)](#rewrite-current-maintained-client)
  - [Nightly (Legacy)](#nightly-legacy)
- [Installation](#installation)
  - [Windows](#windows)
  - [Linux](#linux)
  - [Linux Troubleshooting](#linux-troubleshooting)
- [User Folder](#user-folder)
- [Development](#development)
  - [Prerequisites](#prerequisites)
  - [Project Structure](#project-structure)
- [Contributing](#contributing)

---

## About

**Rhythia** (formerly *Sound Space Plus*) is a free and open-source rhythm game built with **Godot 4.6** and **C#**.

---

## Downloading the Game

There are currently two builds of the game available.

> [!IMPORTANT]
> **Do not** clone this repository to play the game. Use the download links below instead.

### Rewrite (Current Maintained Client)

The current client being actively maintained, now in early testing.

> [!NOTE]
> **Recommended** — This is the latest version of the client. Please report any bugs found in **#bug-reports** on our [Discord](https://discord.gg/rhythia) server.

You can view all releases on the [Rewrite releases page](https://github.com/Rhythia/Client/releases).

| Platform | Direct Download |
|---|---|
| **Windows** | [Download](https://github.com/Rhythia/Client/releases/latest/download/windows.zip) |
| **Linux** | [Download](https://github.com/Rhythia/Client/releases/latest/download/linux.zip) |

You can view the Rewrite source code [here](https://github.com/Rhythia/Client).

### Nightly (Legacy)

The legacy client we all know and (somewhat) love. We recommend this client if you're just starting out for now.

> [!WARNING]
> **Nightly is no longer being maintained.** All current development efforts are focused on the Rewrite client above.

You can view all releases on the [Nightly releases page](https://github.com/David20122/sound-space-plus/releases).

| Platform | Direct Download |
|---|---|
| **Windows** | [Download](https://github.com/David20122/sound-space-plus/releases/latest/download/windows.zip) |
| **Linux** | [Download](https://github.com/David20122/sound-space-plus/releases/latest/download/linux.zip) |

You can view Nightly's source code [here](https://github.com/David20122/sound-space-plus).

---

## Installation

### Windows

1. Download `windows.zip` for your chosen client above.
2. Extract the `.zip` file to a folder of your choice.
3. Run `Rhythia.exe`.

> [!WARNING]
> You **must** have the [Visual C++ Redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe) installed to run the game on Windows!

### Linux

#### GUI File Manager

> [!TIP]
> The following packages are recommended: `thunar`, `thunar-archive-plugin`

1. Download `linux.zip` for your chosen client above.
2. Right-click the downloaded file and extract it.
3. Run `Rhythia.x86_64`.

#### Terminal

> [!WARNING]
> The `unzip` package is **required**. Install it using your distribution's package manager if you don't have it.

1. Download `linux.zip` for your chosen client above.
2. Open your terminal and run the following commands:
   ```bash
   # Assuming the file is in the Downloads folder
   cd $HOME/Downloads
   unzip -d Rhythia linux.zip
   ```
3. Move into the extracted folder and run the game:
   ```bash
   cd Rhythia
   ./Rhythia.x86_64
   ```

### Linux Troubleshooting

If your game doesn't run, make the binary executable from within your Rhythia folder:

```bash
sudo chmod +x Rhythia.x86_64
```

> [!NOTE]
> You need to be a superuser in order to run `sudo`.

Still having issues? Visit the **#support** channel in our [Discord](https://discord.gg/rhythia).

---

## User Folder

Your maps, replays, skins, and settings are stored separately from the game installation:

| Platform | Path |
|---|---|
| **Windows** | `%appdata%\Rhythia` |
| **Linux** | `~/.local/share/Rhythia` |

You can also access the user folder from within the game via **Settings → User Folder**.

---

## Development

### Prerequisites

| Tool | Version | Notes |
|---|---|---|
| [Godot Engine](https://godotengine.org/download) | **4.6** | .NET (C#) build required |
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0** | |
| [Git LFS](https://git-lfs.github.com/) | Latest | Required for large binary assets |

### Project Structure

```
├── addons/          # Third-party addons (ffmpeg, etc.)
├── fonts/           # Font assets
├── meshes/          # 3D mesh assets
├── prefabs/         # Reusable scene prefabs (UI elements, etc.)
├── scenes/          # Main game scenes (menu, game, results, etc.)
├── scripts/         # C# source code
│   ├── database/    # Database / persistence layer
│   ├── game/        # Core gameplay (attempts, renderers, mods, judgments)
│   ├── map/         # Map parsing and management
│   ├── multiplayer/ # Multiplayer lobby and player logic
│   ├── scenes/      # Scene-specific scripts
│   ├── shaders/     # Shader code
│   ├── skinning/    # Skin loading and management
│   ├── spaces/      # Space-related logic
│   ├── ui/          # UI components and notifications
│   └── util/        # Utility / helper classes
├── sounds/          # Audio assets
├── textures/        # Texture and image assets
├── themes/          # Godot UI themes
└── user/            # Default user data scaffold
```

---

## Contributing

We ❤️ developers! Contributions are welcome — whether it's bug fixes, features, documentation, or translations.

> [!WARNING]
> **Nightly is no longer actively maintained.** This guide is for contributing to the **Rewrite** client. The contribution process is the same, but you'll be working with the [Rewrite repository](https://github.com/Rhythia/Client).

### Required Software

| Software | Notes |
|---|---|
| [Godot 4.6 Stable (.NET)](https://godotengine.org/download) | The C# / .NET build is required |
| [Git](https://git-scm.com/downloads) | Windows: Git for Windows · Linux: `git` package |
| [GitHub Account](https://github.com/signup) | Needed to fork and open pull requests |

**Optional:** [Visual Studio Code](https://code.visualstudio.com/) with the [godot-tools](https://marketplace.visualstudio.com/items?itemName=geequlim.godot-tools) extension.

### Setting Up Your Workspace

#### 1. Fork the Repository

1. Head to the [Rewrite repository](https://github.com/Rhythia/Client).
2. Press the **Fork** button and create a fork under your account.

#### 2. Clone Your Fork

Open a terminal and clone your fork into a folder of your choice:

```bash
# Navigate to your preferred directory
cd Documents/Rhythia

# Clone your fork (replace YOUR_USERNAME with your GitHub username)
git clone https://github.com/YOUR_USERNAME/Client.git

cd Client

# Fetch large files (textures, audio, etc.)
git lfs fetch --all
git lfs pull
```

#### 3. Open in Godot

1. Open **Godot 4.6 (.NET)**.
2. Click **Import**.
3. Browse to your cloned repository folder and select `project.godot`.
4. Click **Open**.

### Submitting Your Changes

1. **Stage** any new files you created:
   ```bash
   git add ./path/to/your/new/files
   ```

2. **Commit** and **push** your changes:
   ```bash
   git commit -m "Brief description of your change"
   git push origin indev
   ```

3. Go to your forked repository on GitHub — you should see a message like *"This branch is 1 commit ahead of Rhythia/Client:indev"*.

4. Click **Contribute → Open pull request**.

5. Ensure your PR is targeting the **`indev`** branch of the main repository.

6. Review your changes, add a clear title and description, then click **Create pull request**.

7. Wait for maintainers to review your code and provide feedback.

### Guidelines

- Follow existing code style and naming conventions.
- Keep PRs focused — one feature or fix per PR.
- Test your changes locally before submitting.
- For large changes, open an issue first to discuss the approach.

---

## License

Rhythia is licensed under the [GNU Affero General Public License v3.0](LICENSE).
