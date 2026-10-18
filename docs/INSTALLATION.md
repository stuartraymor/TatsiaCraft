# Per-user installation

Build the current revision with `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build.ps1`, then run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/install.ps1` from the source checkout. ExecutionPolicy Bypass applies only to this trusted local script process; it does not change the system policy. The installer validates the executable's embedded version against VERSION and installs in `%LOCALAPPDATA%\Programs\TatsiaCraft\<version>`. It creates version-specific desktop and Start menu shortcuts. Run the shortcut to open the installed revision.

Use `-NoShortcuts` to omit shortcuts, `-InstallRoot <directory>` for another installation root, or `-ExecutablePath <file>` for a separately built executable. Existing version directories are always refused, protecting running editors and keeping older revisions available. The installer does not launch, stop, uninstall, or replace applications or modify projects. To install an already installed revision again, choose a different installation root.

Media functionality requires compatible FFmpeg tools. Supply `-ToolsPath <directory>` explicitly to copy an existing compatible tools directory, including its binaries, DLLs and license files. It must contain ffmpeg.exe, ffprobe.exe, ffplay.exe and LICENSE-FFmpeg.txt and must not contain links or junctions. No tools are downloaded or taken from running reference applications. Without this option the application installs, but features requiring FFmpeg may be unavailable. Preserve dependency license files when distributing a packaged build.

Each installation includes LICENSE, README.md, VERSION and `install-manifest.json`. The manifest records the version, UTC installation time, dependency inclusion, and SHA-256 hash and size of each installed payload file. The manifest is inventory, not a signed authenticity guarantee.

Validate without changing user installations or shortcuts:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests/InstallTest.ps1 -ToolsPath <compatible-tools-directory>
```

The test uses a unique directory under the checkout's artifacts folder, verifies every payload hash and size, checks version metadata and overwrite protection, rejects an executable with mismatched version metadata, and removes its own temporary directory. Installation validation does not establish full application feature parity or validate every codec.
