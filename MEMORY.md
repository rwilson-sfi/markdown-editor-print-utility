# Project Memory

Curated durable knowledge shared across assistants and machines. Do not store credentials or chat transcripts.

## Stable facts

- The product is a lightweight Windows helper utility for managing Markdown files.
- Its defining capabilities are a system-tray presence, rich and source Markdown editing, formatted
  printing, and a Windows Explorer right-click printing command.

## Decisions and rationale

- **2026-08-24 — Classified Shared / Normal.** The utility is reusable tooling rather than a
  Security First deliverable. Keeping it on the non-work spine preserves portability while its
  content remains non-sensitive.
- **2026-08-24 — Design interview will proceed without `/grill-me`.** The skill was unavailable and
  the user chose to continue without it.
- **2026-08-24 — All twelve recommended version-one defaults approved.** The binding product baseline
  is [docs/PRODUCT-DESIGN.md](docs/PRODUCT-DESIGN.md). It establishes semantic Markdown round trips;
  rich/source/split editing; Explorer Edit, Print, and Print Preview commands; preview plus the
  standard print dialog; a configurable light print style; GitHub-style Markdown; tabbed documents;
  explicit saving with crash recovery; opt-in sign-in startup; a signed per-user installer without
  automatic updates; UTF-8 file handling; and a local-only version-one scope with PDF export through
  the Windows print workflow.
- **2026-08-24 — Local repository initialization approved.** The user authorized an independent Git
  repository. The verified initial `main` commit is `204d20c`; ongoing work on this laptop uses
  `host/sfi-p14-2255`. Remote creation and publication were explicitly left deferred. Workspace
  dashboard registration must wait for the GitLab repository because the non-work bootstrap derives
  every dashboard path as a GitLab clone target; registering a nonexistent remote would break clean
  bootstrap runs.
- **2026-08-24 — Technical architecture adopted.** Target Windows 11 24H2+ x64 with a .NET 10 WPF
  host, Evergreen WebView2, locally bundled Milkdown Crepe 7.22.1 for rich/source/split editing, and
  Markdig 1.3.2 plus WebView2's system print UI for formatted printing. The detailed rationale and
  primary references are in [docs/TECHNICAL-ARCHITECTURE.md](docs/TECHNICAL-ARCHITECTURE.md).
- **2026-08-24 — First executable slice verified.** Commit `94dd53e` launches a responsive tray app
  and implements one-document editing, safe UTF-8 persistence, drag/drop, preview/printing, and an
  unsupported-syntax source-mode guard. Eight core tests pass; the local editor bundle builds from
  its lockfile; NuGet and npm high-severity vulnerability checks found none. This is a development
  milestone, not the signed version-one release.
- **2026-08-24 — Folder Explorer, tabs, and single-instance behavior requested and implemented.** The
  user wants a VS Code/Codex-style left folder tree, one shared editor experience across multiple
  file tabs, and exactly one tray process. Commit `9490b66` implements lazy folder navigation,
  per-document tab state, duplicate-path focus, a per-user mutex, and a current-user named pipe that
  forwards later open requests to the primary window. The commit is runtime-accepted: 13 automated
  tests passed, a second process forwarded its file and exited with code zero while exactly one app
  process remained, and visual QA confirmed the Explorer and multi-tab layout.
- **2026-08-24 — Use `C:\SFM\Dev\MarkdownEditor` for Windows development on `SFI-P14-2255`.** The
  user directed that development use `C:\SFM\Dev\`. A dedicated Git worktree preserves the
  governed repository while providing the host-approved build and execution location.
- **2026-08-24 — Complete Hermes built-in catalog and gallery adopted.** Commit `af6ccba` established
  the compact retro-minimal visual system and runtime-accepted its first five palettes. The user then
  requested every Hermes built-in; commit `9cadced` expands the catalog to Nous, GitHub, Catppuccin,
  Everforest, Solarized, Nous Alt, Midnight, Ember, Mono, Slate, and Cyberpunk. Discovery uses a
  four-column color-preview gallery with instant name/mood search, All/Light/Dark filters, keyboard
  navigation, and active-palette marking rather than a long menu. Resources remain local, preference
  persistence is unchanged, and printing remains light. The expanded code and 21-case suite compile,
  but runtime/test reacceptance is pending because host application control denied the fresh unsigned
  assemblies.

- **2026-09-02 — Print-failure report root-caused to startup, not printing.** The user's report that
  printing to Microsoft Print to PDF "errors out or nothing happens" was caused by secondary launches
  crashing before the file opened: an unhandled `UnauthorizedAccessException` connecting to the
  single-instance pipe when the running primary was started in a different security context (one
  2026-08-31 event records "pipe … not owned by the current user", the signature of an elevated
  instance), plus application-control denials of freshly built binaries. No print-path failure exists
  in the event log. Commit `28df687` makes every hand-off failure show an actionable dialog and lets
  the primary listener recover from a foreign-owned pipe name; it is build-verified, with runtime
  acceptance pending host policy.

- **2026-10-19 — First publication approved and completed.** The user approved creating the private
  GitHub repository `rwilson-sfi/markdown-editor-print-utility` (chosen over the
  `security-first-managers` org to preserve the project's Shared, employer-portable classification;
  GitLab remains unreachable from this host). Both branches pushed and verified; `origin` is wired
  with tracking. GitHub secret scanning/push protection are unavailable on this private
  personal-account plan — a recorded deviation; pre-push tree and history scans found no secrets.
  Workspace-dashboard registration remains deferred to a non-work host because the bootstrap derives
  GitLab clone targets.

## Constraints

- Target Windows and behave as a system-tray application.
- Preserve supported Markdown through source/rich-mode round trips.
- Printing must render formatted output and must not modify the source document.
- Explorer integration and startup registration must be reversible.

## Lessons learned

- On this Windows host, MSBuild must invoke `npm.cmd` explicitly. An extensionless `npm` command from
  an MSBuild `Exec` target fails with `Access is denied`, while the `.cmd` launcher succeeds.
- The restricted host blocks the xUnit analyzer DLL in the user NuGet cache. Declaring
  `xunit.analyzers` directly with all assets excluded preserves compilation and test execution while
  avoiding that host-specific load failure.
- At approximately 18:00 ET on 2026-08-24, managed `C:\AI-Workspace` and temp locations denied fresh
  unsigned project and test DLLs with `FileLoadException: Access is denied`. The user-directed
  `C:\SFM\Dev\MarkdownEditor` worktree restored normal build, test, and app execution without
  changing security policy. Use that worktree on this host and do not weaken endpoint controls.
- At approximately 20:24 ET on 2026-08-24, the same application-control denial began affecting fresh
  unsigned app and test assemblies under `C:\SFM\Dev\MarkdownEditor`. Builds still succeed. Do not
  bypass the control; re-run acceptance after authorized release or the signed packaging milestone.
  Reconfirmed 2026-09-02: first execution of a fresh build is denied in both locations, while binaries
  that have previously run keep running, so denial appears to be per-binary. Retries minutes later did
  not clear it that evening.
- Never run the editor elevated. An elevated primary owns the single-instance mutex and pipe in a
  different security context, so every normal launch fails to connect and (before `28df687`) crashed
  silently. Diagnose "the app does nothing" reports first via Application event log IDs 1000/1026,
  which carried the full managed stack for every failure in this incident.

## Items requiring revalidation

- Validate recovery, external-change handling, recent files, relative local images, print
  customization, Explorer verbs, startup registration, packaging, signing, uninstall, and
  physical-printer output in their planned phases.
