# Implementation Plan

- Status: Active
- Started: 2026-08-24
- Branch: `host/sfi-p14-2255`

## Phase 1 — Executable editing slice

- Create the .NET solution, WPF app, core library, and core tests.
- Bundle a local Milkdown Crepe editor with rich, source, and split modes.
- Establish the narrow WebView2 JSON message contract.
- Implement new/open/save/save-as for one document.
- Implement strict UTF-8/BOM and line-ending preservation with atomic saving.
- Add a basic print-preview window and system print dialog.
- Add tray restore/exit behavior without startup registration.

Exit evidence:

- Solution and web assets build from a clean dependency restore.
- Automated core tests pass.
- Application launches on `SFI-P14-2255` without network runtime dependencies.
- A representative GFM fixture opens, switches modes, saves, and renders for print.

## Phase 2 — Multi-document and resilience

Progress on 2026-08-24: multi-document tabs, lazy folder Explorer, duplicate-path focus, and
single-instance file forwarding are implemented and runtime-accepted from the user-approved
`C:\SFM\Dev\MarkdownEditor` worktree. A Hermes-inspired retro-minimal interface is implemented across
the WPF chrome, editor, and preview chrome. The palette catalog now contains all eleven official
Hermes built-ins and uses a searchable, Light/Dark-filterable, four-column color-preview gallery in
place of the original linear menu. The 21-case automated suite compiles and the full solution builds
with zero warnings or errors. The original five-palette implementation passed runtime acceptance;
expanded-gallery runtime and test execution await release by this host's application-control policy.

- Add tabbed documents, recent files, drag/drop, and duplicate-path focus. **Tabs, drag/drop, and
  duplicate-path focus implemented; recent files remain.**
- Add external-change detection and conflict prompts.
- Add recovery drafts and abnormal-exit restoration.
- Add single-instance mutex and named-pipe command forwarding. **Implemented and runtime-accepted.**
- Add a coherent modern interface and locally persisted screen themes. **Complete eleven-palette
  catalog and searchable gallery implemented; expanded runtime reacceptance pending host policy.**
- Complete keyboard/accessibility review.

Exit evidence:

- Multi-document workflows and conflict paths have automated coverage where deterministic.
- Forced-exit recovery succeeds without modifying source files.
- Secondary invocations forward file commands to the primary process.

## Phase 3 — Shell integration and packaging

- Implement `--edit`, `--print`, and `--preview` command-line behavior.
- Build reversible per-user Explorer verb registration behind an explicit action.
- Validate Windows 11 classic/modern context-menu placement.
- Select and build the signed per-user installer.
- Verify uninstall removes only keys/files owned by the utility.

Exit evidence:

- Registration/unregistration passes isolated-key tests.
- Live registry and sign-in changes occur only after approval.
- Install/uninstall verification accounts for every persisted artifact.

## Phase 4 — Release acceptance

- Run supported Markdown round-trip fixtures.
- Verify print preview, Microsoft Print to PDF, and one physical printer.
- Verify local images, code styles, tables, tasks, headers/footers, margins, and page numbers.
- Verify offline use, missing-WebView2 handling, high DPI, keyboard operation, and contrast.
- Produce the signed release and checksums.

## Cross-cutting rules

- No telemetry or runtime network dependencies.
- No live shell/startup mutation without explicit approval.
- Printing never writes the source file.
- Unsupported Markdown is preserved and never silently discarded.
- Generated assets and dependency caches stay out of Git; sources and lockfiles are tracked.
