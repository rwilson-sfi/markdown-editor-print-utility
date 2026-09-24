---
project: Markdown Editor and Print Utility
created: 2026-08-24
area: Shared
status: Active
sensitivity: Normal
owner: RW
---

# Markdown Editor and Print Utility

## Purpose

Provide a lightweight Windows helper for managing Markdown files: edit them in either a formatted
WYSIWYG view or Markdown source, and print formatted documents without opening a larger development
environment.

## Scope

### Included

- A Windows system-tray application.
- WYSIWYG and Markdown-source editing of the same document.
- Opening, saving, and printing Markdown files.
- A Windows Explorer right-click command for printing Markdown files.
- A tray menu for opening the editor and accessing common commands.
- Formatted print preview and the standard Windows print dialog.
- GitHub-style Markdown as the approved version-one compatibility target.
- Local Mermaid diagram rendering in rich view, print preview, and printed output.

### Excluded

- Cloud storage, collaboration, user accounts, and synchronization.
- Telemetry or analytics.
- A general-purpose word processor or integrated development environment.
- Security First-specific content or integrations.
- Background modification of Markdown files during printing.

## Success criteria

- The utility starts and remains available from the Windows system tray.
- A Markdown file can be edited in rich and source modes without losing supported content.
- A saved Markdown file can be printed as formatted output from the application.
- A `.md` file can invoke formatted printing from Windows Explorer through a reversible context-menu
  integration.
- Installation and removal leave no undocumented startup items, file associations, or registry keys.

## Important locations

- Shared project root: `.`
- Active Windows development/build worktree on `SFI-P14-2255`:
  `C:\SFM\Dev\MarkdownEditor`
- Source repository: independent local Git repository with baseline `main` and current
  `host/sfi-p14-2255`; no remote configured.
- Target platform: Windows; supported versions and packaging remain design decisions.
- Approved product baseline: [docs/PRODUCT-DESIGN.md](docs/PRODUCT-DESIGN.md)
- Technical architecture: [docs/TECHNICAL-ARCHITECTURE.md](docs/TECHNICAL-ARCHITECTURE.md)
- Implementation plan: [docs/IMPLEMENTATION-PLAN.md](docs/IMPLEMENTATION-PLAN.md)

## Current implementation

The current development slice is available on `host/sfi-p14-2255`. It provides one Windows tray
process with a VS Code-style folder Explorer, multi-document tabs, rich/source/split editing,
open/save/save-as, strict UTF-8 and line-ending preservation, drag/drop, a formatted print-preview
window, and the standard Windows system print dialog. A per-user mutex and named pipe forward files
from later launches into the existing window, so an Explorer Open command does not create a second
tray instance. Documents containing known out-of-contract syntax open in source mode so a rich-editor
round trip cannot silently normalize them.

Mermaid fenced blocks are rendered locally with a pinned Mermaid.js dependency and a clean,
Codex-inspired diagram theme. Diagram rendering is available in rich view and formatted print output;
the Markdown source remains the canonical saved representation.

The application chrome now uses a compact, retro-minimal visual system inspired by Hermes Desktop.
The searchable palette gallery exposes all eleven Hermes built-ins—Nous, GitHub, Catppuccin,
Everforest, Solarized, Nous Alt, Midnight, Ember, Mono, Slate, and Cyberpunk—as visual color-preview
cards. Users can search by name or mood, filter Light/Dark palettes, and select without traversing a
long menu. The selected palette persists in the current user's local application profile and applies
to the Windows chrome and rich/source/split editor. Print-preview chrome follows the screen palette
while the printable document remains light for predictable office output.

Crash recovery, external-change detection, recent files, Explorer registration, startup registration,
packaging, and signing remain in later phases.

## Developer quick start

Prerequisites are the .NET 10 SDK, Node.js 24 or newer, npm, and the Evergreen WebView2 Runtime.
On `SFI-P14-2255`, run these commands from `C:\SFM\Dev\MarkdownEditor`, the user-approved
development worktree.

```powershell
dotnet restore .\MarkdownEditor.slnx --configfile .\NuGet.config
dotnet build .\MarkdownEditor.slnx --no-restore --configuration Debug
dotnet test .\MarkdownEditor.slnx --no-build --configuration Debug
& .\src\MarkdownEditor.App\bin\Debug\net10.0-windows\MarkdownEditor.App.exe
```

The .NET build runs the locked web-editor build and copies its generated local assets into the app
output. Dependency caches and generated assets remain untracked.
