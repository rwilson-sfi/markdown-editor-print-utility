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

## Items requiring revalidation

- Validate multi-document editing, recovery, external-change handling, single-instance forwarding,
  relative local images, print customization, Explorer verbs, startup registration, packaging,
  signing, uninstall, and physical-printer output in their planned phases.
