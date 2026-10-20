# Installing TatsiaCraft

## For users

1. Download **TatsiaCraft-Setup-&lt;version&gt;.exe** from the [latest release](https://github.com/stuartraymor/TatsiaCraft/releases).
2. Run it. Choose **Install** (or **Update** when a version is already installed).
3. Pick the options you want: a desktop shortcut, opening `.tasiaproj` files with TatsiaCraft, and starting TatsiaCraft when setup finishes.

Setup installs for your Windows account only and doesn't need administrator rights. Everything goes into `%LOCALAPPDATA%\Programs\TatsiaCraft`, including the FFmpeg media tools and their licenses. Requirements: Windows 10 or 11 (x64) with .NET Framework 4.8, which is built into current Windows versions.

- **Updating.** In TatsiaCraft, use **Help > Check for Updates...** TatsiaCraft finds the newest release, downloads its Setup, checks it against the size and SHA-256 published on GitHub, and starts it once the editor closes. You can also run a newer Setup by hand. An update replaces only the files that changed, so updates that don't change the FFmpeg tools skip rewriting them.
- **Repairing.** Run the same Setup again and choose **Repair**. Missing or changed files are restored.
- **Uninstalling.** Use **Settings > Apps > Installed apps > TatsiaCraft > Uninstall**, or run `Uninstall TatsiaCraft.exe` in the install folder. Your projects, narration recordings, favorites and recent-project list are kept.
- **Older side-by-side versions.** Earlier releases (up to 0.2.2) installed each version in its own folder, with a "TatsiaCraft &lt;version&gt;" shortcut. Setup lists any it finds and offers to remove them, along with their shortcuts.
- **Safety.**
  - Setup won't change files while TatsiaCraft is running; it asks you to close it first.
  - Every payload file is checked against the embedded SHA-256 manifest before it's used.
  - Replaced files are backed up and restored if anything fails, so a failed install or update leaves the previous version working.
  - Setup isn't code-signed yet, so Windows SmartScreen may ask you to confirm with **More info > Run anyway**.

What Setup registers for the current user:

- an Apps & Features entry with version, size and uninstall commands;
- a **TatsiaCraft** Start menu shortcut and, if chosen, a desktop shortcut;
- a Run (Win+R) alias, `TatsiaCraft`;
- optionally, the `.tasiaproj` file type, so double-clicking a project opens it.

## Command-line options

`TatsiaCraft-Setup-<version>.exe [options]`

| Option | Effect |
|---|---|
| `/quiet` | Install, update or uninstall without a window. Exit codes: 0 success, 1 failure, 2 bad option, 3 TatsiaCraft is running. Results are appended to `%TEMP%\TatsiaCraft-Setup.log`. |
| `/dir=<folder>` | Install to another folder. |
| `/nodesktop`, `/noassoc` | Skip the desktop shortcut or the `.tasiaproj` association. |
| `/removelegacy` | Remove older side-by-side version folders and their shortcuts. |
| `/launch`, `/nolaunch` | Start, or don't start, TatsiaCraft afterwards. |
| `/noregister` | Copy files only: no Apps & Features entry, shortcuts or file association. |
| `/uninstall` | Uninstall (the same as running `Uninstall TatsiaCraft.exe`). |

To uninstall silently: `"%LOCALAPPDATA%\Programs\TatsiaCraft\Uninstall TatsiaCraft.exe" /uninstall /quiet`.

## For developers

- **Build Setup.** `scripts/build.ps1`, then `scripts/build-setup.ps1 -ToolsPath <compatible-tools-directory>`, produces `dist\TatsiaCraft-Setup-<version>.exe`.
- **What's in the payload.** The editor, LICENSE, README, VERSION, the tools folder (it must contain `ffmpeg.exe`, `ffprobe.exe` and `LICENSE-FFmpeg.txt`), `Uninstall TatsiaCraft.exe` and `install-manifest.json`. The manifest lists the path, size and SHA-256 of every file. The payload is compressed into one embedded resource; Setup and the uninstaller are built from the same `installer/Setup.cs`.
- **Test Setup.** `tests/SetupTest.ps1 -SetupPath <setup.exe>` runs about 300 checks. It uses a unique folder under `artifacts` and a test-only registry root, so it never touches the real installation. It checks:
  - a fresh install, payload sizes and hashes, and registration;
  - opening a project from the command line;
  - refusing to run while the editor is open;
  - repair restoring a changed file without rewriting unchanged ones;
  - removing files no longer in the payload;
  - rollback when a file is locked;
  - removing legacy versions only on request;
  - an unknown option being rejected;
  - uninstall removing files and registration, keeping user files, and deleting the uninstaller itself.
- **Releases.** `scripts/release.ps1` builds and tests Setup, installs the release on the build machine through Setup, smoke-tests that install, and uploads Setup as the main download, alongside the bare editor and the source archive.
- **Old installer.** `scripts/install.ps1` still installs immutable side-by-side versions into `%LOCALAPPDATA%\Programs\TatsiaCraft\<version>` for comparing revisions. Releases don't use it.
