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
- Source repository: independent local Git repository with baseline `main` and current
  `host/sfi-p14-2255`; no remote configured.
- Target platform: Windows; supported versions and packaging remain design decisions.
- Approved product baseline: [docs/PRODUCT-DESIGN.md](docs/PRODUCT-DESIGN.md)
- Technical architecture: [docs/TECHNICAL-ARCHITECTURE.md](docs/TECHNICAL-ARCHITECTURE.md)
- Implementation plan: [docs/IMPLEMENTATION-PLAN.md](docs/IMPLEMENTATION-PLAN.md)

## Current implementation

The first executable slice is available on `host/sfi-p14-2255`. It provides a Windows tray process,
one-document rich/source/split editing, open/save/save-as, strict UTF-8 and line-ending preservation,
drag/drop, a formatted print-preview window, and the standard Windows system print dialog. Documents
containing known out-of-contract syntax open in source mode so a rich-editor round trip cannot
silently normalize them.

Tabs, crash recovery, external-change detection, single-instance forwarding, Explorer registration,
startup registration, packaging, and signing remain in later phases.

## Developer quick start

Prerequisites are the .NET 10 SDK, Node.js 24 or newer, npm, and the Evergreen WebView2 Runtime.

```powershell
dotnet restore .\MarkdownEditor.slnx --configfile .\NuGet.config
dotnet build .\MarkdownEditor.slnx --no-restore --configuration Debug
dotnet test .\MarkdownEditor.slnx --no-build --configuration Debug
& .\src\MarkdownEditor.App\bin\Debug\net10.0-windows\MarkdownEditor.App.exe
```

The .NET build runs the locked web-editor build and copies its generated local assets into the app
output. Dependency caches and generated assets remain untracked.
