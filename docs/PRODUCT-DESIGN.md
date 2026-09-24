# Product Design Baseline

- Status: Approved version-one baseline
- Approved: 2026-08-24
- Owner: Richard Wilson
- Classification: Shared / Normal

## Product intent

The Markdown Editor and Print Utility is a small, local-first Windows application for people who
need to manage Markdown documents without opening a development environment. It lives in the system
tray, provides rich and source editing, and prints Markdown as a formatted document from either the
application or Windows Explorer.

The utility must be predictable: it does not silently print, silently rewrite a file, introduce a
cloud dependency, or leave undocumented Windows integration behind after removal.

## Approved version-one behavior

### 1. Source fidelity

- Rich/source round trips preserve the document's meaning and all supported Markdown constructs.
- The application may normalize non-semantic source details such as whitespace, list markers, and
  table-column alignment.
- Unsupported syntax must never be silently discarded. It remains available in source mode and the
  application warns before an operation would normalize or replace it.

### 2. Editing layout

- A toolbar switches the active document between rich and Markdown-source modes.
- An optional split view shows Markdown source and rendered output together.
- All views represent the same in-memory document and share one undoable editing history where the
  selected editor technology permits it.

### 3. Windows Explorer commands

The reversible `.md` and `.markdown` context-menu integration exposes:

- **Edit with Markdown Utility**
- **Print**
- **Print Preview**

The commands pass explicit file paths to the existing application instance when it is running and
start the application when necessary.

### 4. Print workflow

- **Print** renders the formatted document, shows print preview, and then opens the standard Windows
  print dialog.
- No context-menu or tray command prints silently.
- Printing works from the saved file and does not alter its contents or metadata.

### 5. Printed appearance

- The default print style is a clean light theme suitable for office documents.
- Users can configure margins, base font size, page numbers, and optional filename/date headers.
- Headings, tables, lists, blockquotes, images, links, and code blocks receive print-specific styles.
- Screen dark mode does not produce a dark printed page unless a future explicit print theme is
  selected.

### 6. Markdown compatibility

Version one targets GitHub-style Markdown and supports:

- Headings, paragraphs, emphasis, and strong emphasis
- Ordered and unordered lists
- Task lists
- Blockquotes
- Tables
- Strikethrough
- Fenced code blocks with syntax highlighting
- Mermaid fenced diagrams
- Hyperlinks
- Local images

Mermaid diagram rendering was added as an approved post-baseline enhancement. Mathematical notation,
footnotes, and embedded HTML remain outside the rendering contract; their source must still be
preserved.

### 7. Document handling

- One application instance manages multiple documents in tabs.
- Files can be opened through the application, Explorer commands, drag and drop, or the recent-files
  list.
- The application detects when another program changes an open file and asks the user how to proceed.
- Duplicate requests for the same path focus the existing tab rather than opening competing copies.

### 8. Saving and recovery

- Saving is explicit; content is not automatically written back to the source file.
- Closing a modified document prompts the user to save, discard, or cancel.
- Writes use an atomic replacement strategy when the filesystem supports it.
- Recovery drafts protect unsaved work after an abnormal exit and are removed after successful save
  or intentional discard.
- Automatic saving is disabled in version one.

### 9. Tray behavior

- Double-clicking the tray icon restores or opens the editor window.
- Closing the editor window hides it to the tray while documents remain safely managed.
- The tray menu provides open/new, recent files, restore editor, settings, and exit commands.
- Starting at Windows sign-in is available as an opt-in setting and is disabled by default.
- **Exit** performs the normal unsaved-change checks before terminating the process.

### 10. Installation and updates

- Distribution uses a signed, per-user Windows installer that does not require administrator access.
- Installation and removal register and unregister Explorer commands reversibly.
- Version one does not update itself automatically.
- The application records no telemetry or analytics.

### 11. File compatibility

- Primary extensions are `.md` and `.markdown`.
- UTF-8 with or without a byte-order mark is supported.
- Existing line endings are preserved when practical.
- An unusual or lossy encoding is detected and requires confirmation before conversion.

### 12. Version-one boundary

Version one provides local editing, formatted printing, and PDF export through the Windows print
workflow. It does not include cross-file search, templates, cloud synchronization, collaboration,
user accounts, or network services.

### 13. Appearance and themes

- The interface uses a clean retro-minimal visual system inspired by Hermes Desktop, with compact
  controls, crisp borders, restrained depth, and a code-oriented document workspace.
- The complete Hermes built-in palette catalog is available: **Nous**, **GitHub**,
  **Catppuccin**, **Everforest**, **Solarized**, **Nous Alt**, **Midnight**, **Ember**,
  **Mono**, **Slate**, and **Cyberpunk**.
- Palette discovery uses a searchable color-preview gallery with Light/Dark filters and a compact
  multi-column layout. Users must not have to traverse a long linear menu to reach later palettes.
- A palette applies consistently to the Windows chrome, Explorer, tabs, status area, editor toolbar,
  and rich/source/split editing surface. Print-preview chrome follows the selected palette.
- The selected palette persists locally in the current user's Windows profile and requires no
  account, synchronization, web font, or runtime network access.
- Screen themes do not alter the light printable page or the saved Markdown document.

## Primary user flows

### Edit a Markdown file

1. Open the file from Explorer, the tray menu, drag and drop, or recent files.
2. Edit in rich, source, or split view.
3. Switch modes without losing supported document meaning.
4. Save explicitly or respond to the unsaved-change prompt on close.

### Print from Windows Explorer

1. Right-click a `.md` or `.markdown` file and select **Print** or **Print Preview**.
2. The utility renders the saved file using the print stylesheet.
3. Preview the formatted pages.
4. Open the Windows print dialog and choose a physical printer or PDF destination.
5. Complete or cancel printing without modifying the Markdown file.

### Recover unsaved work

1. Restart after an abnormal exit.
2. Review detected recovery drafts and their source paths/timestamps.
3. Restore or discard each draft explicitly.

### Choose an appearance

1. Open the palette gallery in the command bar.
2. Scan the color-preview cards, type a name or mood, or filter by Light/Dark appearance.
3. Choose any built-in palette and continue editing with it applied across the application and editor.
4. Relaunch the utility and retain the selected palette without changing printed output.

## Quality and safety requirements

- Shell and startup integration is per-user, reversible, and testable without administrator access.
- Printing is read-only with respect to the source document.
- Supported Markdown fixtures survive rich/source round trips with semantic equivalence.
- The application remains fully useful offline.
- Local settings, recent-file metadata, and recovery drafts stay in the user's Windows profile.
- No credentials, telemetry identifiers, or document contents leave the computer.
- Keyboard access, focus visibility, readable contrast, and standard Windows scaling are part of the
  version-one acceptance criteria.
- Each screen palette must remain coherent across rich, source, and split editing and preserve a
  light, readable printable page.

## Remaining release design decisions

The product behavior and core application architecture are approved. Release work must still select
or validate:

- Per-user installer technology and code-signing process
- Exact registry mechanism for modern and classic Explorer context menus
- Recovery-draft storage format and retention policy
- Physical-printer output and full supported-Markdown acceptance fixtures

These choices must be tested against the approved behavior, especially Markdown round-trip fidelity,
printing, accessibility, offline operation, and reversible shell integration.
