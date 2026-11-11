# ORBIT 3.2 — Motion Desktop (Windows source)

This is a real C# Windows Forms + WebView2 application project, updated from the supplied ORBIT 3.1 codebase. It is not a compiled or runtime-verified installer; GitHub Actions builds the Windows executable and installer.

## New in this source release
- First-run name welcome and typed greeting, stored in `%APPDATA%\Orbit\profile.json`.
- Animated ORBIT orb, glass cards, optional collapsed sidebar, Ctrl+K command palette.
- Theme (dark, experimental light, auto), font family fallback, Full/Reduced/Off motion.
- Local-first versioned settings backup in `%APPDATA%\Orbit\state.json` with `.bak` recovery and atomic writes.
- Native Export / Import settings dialogs; reset profile, reset preferences/reminders/presets.
- CPU/RAM live dashboard cards and hide/re-add controls. GPU shows unavailable when Windows reports no metric; no fabricated GPU numbers.
- Native window bounds saved and restored; notes kept separately with a backup file.
- Preserved existing notes, reminders, timers, presets, speech bridge, performance overlay, weather, GitHub updater and install script.

## Build
1. Unzip the project and upload the **contents of the ORBIT_3_2 folder** to your `orbit-windows` GitHub repository's root, replacing the existing source files.
2. In GitHub Actions run `Build ORBIT 3.2 for Windows`, or push to main to trigger it.
3. Download the artifact `ORBIT-3.2-Windows` containing the installer and executable.
4. Run the installer. Your current `%LOCALAPPDATA%\ORBIT` data remains in place.

Alternative on Windows: install .NET 10 SDK, then run `dotnet publish Native/ORBIT.csproj -c Release -r win-x64 --self-contained true -o Publish` from the repository root. WebView2 Runtime is required at runtime.

## Important boundaries
- Speech recognition has NOT been upgraded in this source release; Romanian recognition and voices depend on Windows installed components and may still fail. Voice improvements are planned separately.
- No cloud backend, telemetry upload, AI speech service, guaranteed GPU/FPS/temp sensors or arbitrary process termination.
- Light theme uses a provisional color transform and is not fully art-directed.
- Inter, Poppins and JetBrains Mono are system-font fallbacks, not bundled font files; Segoe is the offline-safe default.
- CPU/RAM widgets are live; GPU uses existing limited Windows metric support. The 1% CPU target is not performance-certified.
- The settings migration preserves existing ORBIT 3.1/2.6-style browser localStorage keys when first opened and then copies settings/reminders/presets into a versioned JSON file; no data is deleted automatically.
- Update service still needs a configured GitHub Releases repository and checksum-matched setup artifacts; an Actions artifact alone is not a public release.
- Reset everything intentionally preserves separately stored notes; export Notes separately before uninstalling or replacing the computer.
