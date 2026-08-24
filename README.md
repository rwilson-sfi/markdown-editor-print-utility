---
project: Markdown Editor and Print Utility
created: 2026-08-24
area: Shared
status: Planning
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
- Source repository: independent local Git repository on `main`; no remote configured.
- Target platform: Windows; supported versions and packaging remain design decisions.
- Approved product baseline: [docs/PRODUCT-DESIGN.md](docs/PRODUCT-DESIGN.md)
