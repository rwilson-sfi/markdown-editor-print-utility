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
- Run from the project root.
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
- The test output reports eight passing tests or more.
- The launched process is responsive and starts WebView2 child processes.

#### Recovery or rollback

Delete only the generated `bin/`, `obj/`, `web/editor/dist/`, and `web/editor/node_modules/`
directories if a clean dependency rebuild is needed. They are ignored and regenerated from tracked
project files and lockfiles. Do not delete source, project records, or the package lock.

No installation, shell-registration, or release procedure has been validated yet.
