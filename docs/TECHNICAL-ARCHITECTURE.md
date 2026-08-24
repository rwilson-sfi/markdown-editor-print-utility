# Technical Architecture

- Status: Adopted for implementation
- Date: 2026-08-24
- Target: Windows 11 24H2 or newer, x64 first

## Decision summary

Build a native Windows desktop shell in C# on .NET 10 WPF, host the rich editor and formatted
document surface in Microsoft Edge WebView2, use Milkdown Crepe for Markdown-native WYSIWYG editing,
and use Markdig for the host-side GFM render/print pipeline.

This combines native Windows lifecycle, tray, filesystem, dialogs, and shell integration with a web
editor whose internal model serializes back to Markdown. It is smaller and more Windows-native than
shipping an Electron runtime, while still using the browser-grade editing primitives needed for a
credible WYSIWYG experience.

## Supported platform

- Windows 11 24H2 or newer, x64, for the first supported release.
- .NET 10 LTS, framework-dependent while developing and self-contained for release packaging.
- Evergreen WebView2 Runtime. The app detects a missing runtime and reports a useful setup error.
- Windows 10 is not in the version-one support contract. Current .NET 10 support on Windows 10 is
  limited to specific Enterprise/LTSC editions, so broad Windows 10 support would be misleading.

## Component choices

### Native host: .NET 10 WPF

Responsibilities:

- Application and window lifecycle
- System tray and opt-in sign-in startup
- File dialogs, drag/drop, tabs, and recent files
- Encoding/line-ending detection, atomic saving, and recovery drafts
- Single-instance coordination and command forwarding
- Print-preview window and Windows print dialog
- Reversible per-user Explorer command registration
- Settings and diagnostics without telemetry

WPF is a supported Windows-only .NET desktop framework, fits the installed toolchain, and does not
require a Windows App SDK workload. .NET 10 is an active LTS release.

### Embedded editor: WebView2 + Milkdown Crepe

Responsibilities:

- Rich Markdown editing
- Markdown serialization after rich edits
- Source and split-mode editing
- Formatting toolbar, lists, tasks, tables, links, images, and code blocks
- Keyboard editing and browser-grade accessibility semantics

The editor is bundled into static local assets at build time. The running application makes no CDN
or network request. Milkdown's LaTeX and AI features are disabled because both are outside the
approved version-one scope. WebView2 messaging carries only the current document and editor commands
between the local WPF host and the local editor page.

Pinned editor packages at adoption:

- `@milkdown/crepe` 7.22.1
- `@milkdown/kit` 7.22.1
- Vite 8.2.2 for build-time bundling only

### Markdown renderer: Markdig

Markdig 1.3.2 provides the host-side CommonMark/GFM-compatible parse and HTML render pipeline used
for preview and printing. Rendering is separate from saving: print operations consume a snapshot and
never write the source file.

### Printing: WebView2

The preview window hosts sanitized, locally generated HTML with a print stylesheet. Its Print
button opens `CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System)`. No code path uses silent
printing. PDF output is available through the Windows printer selection rather than a separate PDF
engine in version one.

### Tray: Windows Forms NotifyIcon from WPF

Use the platform `NotifyIcon` through the Windows Forms compatibility surface. No third-party tray
package is required. Closing the editor hides it; Exit performs unsaved-change checks and disposes
the icon.

### Single instance and Explorer commands

- A named mutex chooses the primary process.
- A per-user named pipe forwards explicit commands and file paths from later invocations.
- Planned command line: `--edit <path>`, `--print <path>`, and `--preview <path>`.
- Planned per-user registrations live under the merged per-user classes view for
  `SystemFileAssociations` and are owned by the installer/uninstaller.
- Registry-writing code will be testable in an isolated key, but no live registration occurs without
  the project approval gate.

## Application structure

```text
MarkdownEditor.slnx
src/
  MarkdownEditor.App/          WPF shell, editor bridge, rendering and print UI
  MarkdownEditor.Core/         document, encoding, persistence and command-line domain logic
tests/
  MarkdownEditor.Core.Tests/   deterministic filesystem and document tests
web/
  editor/                      Milkdown source, lockfile and Vite build
docs/                          product and technical design
```

Dependencies point inward: `App` references `Core`; `Core` references no UI framework or third-party
package. The web editor is a local build artifact copied into the application output.

## Document state

Each open document tracks:

- Canonical absolute path, or untitled identity
- Current Markdown text and last-saved text hash
- UTF-8 BOM state and dominant line ending
- Dirty state and external-file fingerprint
- Recovery-draft identity and last recovery timestamp
- Active editor mode

Supported Markdown moves freely between source and rich modes. Before entering rich mode, the host
detects out-of-contract syntax such as Mermaid fences, math, footnotes, and embedded HTML. The user
is warned that rich serialization may normalize it; cancellation leaves the source untouched.

## Persistence and recovery

- Read with strict UTF-8 validation; UTF-8 BOM is recognized and preserved.
- Reject or explicitly confirm non-UTF-8 conversion rather than guessing.
- Preserve the dominant CRLF or LF style on save.
- Write a sibling temporary file, flush it, and atomically replace/move it into place.
- Store recovery drafts and settings below the user's local application-data directory, never beside
  the source document.
- Recovery drafts contain document content and therefore never enter Git or logs.

## Security and privacy

- No telemetry, account, updater, cloud service, or background network request.
- WebView2 navigation is restricted to the mapped local asset origin and generated preview content.
- Disable new-window navigation and route external links through an explicit user action.
- Do not expose arbitrary host objects to JavaScript; use a narrow JSON message contract.
- Sanitize generated HTML and block executable raw HTML in the print pipeline.
- Explorer and startup integration are per-user and reversible.

## Packaging direction

Release packaging remains a later gate. The required outcome is a signed per-user installer without
administrator access, with deterministic uninstall of tray startup and Explorer verbs. Packaging
technology will be selected only after the command-line and registry integration are validated; the
installer must not be allowed to dictate editor architecture.

## Alternatives considered

- **Electron:** capable but rejected for version one because it duplicates Chromium and Node for a
  Windows-only helper when WebView2 is already present.
- **WinUI 3:** capable, but WPF requires fewer workloads and has a mature tray/desktop interop story;
  WebView2 exposes the same underlying `CoreWebView2` API.
- **TOAST UI Editor:** directly supports source/WYSIWYG toggling, but its current package line is much
  older than Milkdown's active 2026 release stream. Milkdown provides a Markdown-native model and an
  actively maintained GFM preset.
- **WPF RichTextBox:** rejected as the primary editor because Markdown semantic round-tripping would
  become a custom document-model project.

## Primary references

- [.NET 10 support policy](https://dotnet.microsoft.com/en-us/platform/support/policy)
- [.NET 10 supported operating systems](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md)
- [WPF documentation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- [WebView2 WPF hosting guidance](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/webview2)
- [WebView2 printing](https://learn.microsoft.com/en-us/microsoft-edge/webview2/how-to/print)
- [Milkdown getting started](https://milkdown.dev/docs/guide/getting-started)
- [Milkdown Crepe API](https://milkdown.dev/docs/api/crepe)
- [Milkdown GFM preset](https://milkdown.dev/docs/api/preset-gfm)
- [Markdig](https://github.com/xoofx/markdig)
- [Windows application and verb registration](https://learn.microsoft.com/en-us/windows/win32/shell/app-registration)
