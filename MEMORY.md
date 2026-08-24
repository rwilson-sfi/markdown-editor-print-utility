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

## Constraints

- Target Windows and behave as a system-tray application.
- Preserve supported Markdown through source/rich-mode round trips.
- Printing must render formatted output and must not modify the source document.
- Explorer integration and startup registration must be reversible.

## Lessons learned

- None yet.

## Items requiring revalidation

- Confirm supported Windows versions and validate the runtime, editor, renderer, printing, installer,
  shell-integration, and recovery-storage components during technical design.
