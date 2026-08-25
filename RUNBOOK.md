# Project Runbook

Only validated, repeatable procedures belong here.

## Standard assistant session

### Startup

1. Follow the root and project `AGENTS.md` files.
2. Read the required project state and relevant workspace policies.
3. Inspect live filesystem and repository state.
4. Provide the context receipt from `../policies/HANDOFF.md` before nontrivial edits.

### Closeout

1. Verify the result.
2. Update the daily worklog and `STATUS.md`.
3. Curate `MEMORY.md` and this runbook only when warranted.
4. Provide the handoff receipt from `../policies/HANDOFF.md`.

## Procedure template

### Purpose

Describe the outcome.

### Preconditions

- Required access, state, and dependencies.

### Steps

1. Perform the first action.

### Expected result

Describe observable success.

### Verification

Describe how to prove the procedure succeeded.

### Recovery or rollback

Describe the safe recovery path.

## Validated project procedures

### Restore, build, test, and run the development app

#### Purpose

Produce and verify a runnable development build with the bundled local editor surface.

#### Preconditions

- Windows 11 x64 with the .NET 10 SDK, Node.js 24 or newer, npm, and Evergreen WebView2 Runtime.
- On `SFI-P14-2255`, run from the user-approved development worktree at
  `C:\SFM\Dev\MarkdownEditor`.
- No installation, registry, startup, or file-association change is performed by this procedure.

#### Steps

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
dotnet restore .\MarkdownEditor.slnx --configfile .\NuGet.config
dotnet build .\MarkdownEditor.slnx --no-restore --configuration Debug
dotnet test .\MarkdownEditor.slnx --no-build --configuration Debug
& .\src\MarkdownEditor.App\bin\Debug\net10.0-windows\MarkdownEditor.App.exe
```

#### Expected result

The build completes without .NET warnings or errors, all core tests pass, and the app opens with its
tray icon and local rich editor. Closing the window leaves the process in the tray; Exit ends it.

#### Verification

- `src/MarkdownEditor.App/bin/Debug/net10.0-windows/EditorAssets/index.html` exists.
- The test output reports 16 passing tests or more.
- The launched process is responsive and starts WebView2 child processes.

#### Recovery or rollback

Delete only the generated `bin/`, `obj/`, `web/editor/dist/`, and `web/editor/node_modules/`
directories if a clean dependency rebuild is needed. They are ignored and regenerated from tracked
project files and lockfiles. Do not delete source, project records, or the package lock.

If Windows reports `FileLoadException: Access is denied` under `C:\AI-Workspace` or a temp location,
stop that launch and preserve the exact evidence. On `SFI-P14-2255`, perform the build and runtime
checks from `C:\SFM\Dev\MarkdownEditor`, which is the user-approved development worktree and has
been validated successfully. Do not disable or bypass application-control or endpoint-security
policy.

### Theme visual acceptance

#### Purpose

Verify that every screen palette is coherent across the native Windows chrome and embedded editor,
persists across launches, and does not alter printable output.

#### Preconditions

- Complete the validated development build procedure from `C:\SFM\Dev\MarkdownEditor`.
- Use a disposable or saved test document with representative headings, lists, code, and tables.

#### Steps

1. Start the development app and open the theme selector in the command bar.
2. Use the gallery search and All/Light/Dark filters to visit every palette in the current catalog.
3. For each palette, inspect the command bar, Explorer, tabs, status area, editor toolbar, and rich,
   source, and split modes.
4. Open print preview and verify that the preview chrome follows the screen palette while the
   printable page stays light.
5. Select a non-default palette, exit the app, relaunch it, and confirm that the selection persists.
6. Return to the intended palette and exit the exact development app process after acceptance.

#### Expected result

Every catalog palette remains legible and visually coherent across native and embedded surfaces. The
selected palette survives relaunch, no runtime network resource is needed, and the printable page is
not recolored.

#### Verification

- The gallery exposes the exact palette IDs in `ThemeCatalog.All`, with no duplicate IDs.
- Search finds palettes by name or mood, and Light/Dark filters produce the expected subsets.
- Each palette updates the editor toolbar and editing surface as well as the native application
  chrome.
- Rich, source, and split modes remain usable in every palette.
- Print preview retains a white document page.
- Relaunch restores the last selected palette from the local application profile.

#### Recovery or rollback

Select Nous from the palette gallery. If the preferences file is unavailable or invalid, the app falls
back to Nous without blocking startup. Do not delete document files while troubleshooting optional
appearance settings.

The original five-palette procedure was fully validated on 2026-08-24. The expanded eleven-palette
gallery compiled successfully, but this host denied fresh unsigned assembly execution before its
runtime reacceptance; repeat the full procedure after an authorized policy release or signed build.

No installation, shell-registration, or release procedure has been validated yet.
