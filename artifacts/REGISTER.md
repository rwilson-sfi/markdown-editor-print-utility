# Artifact Register

Every produced deliverable stored under `artifacts/` gets one row. The register records provenance;
the artifact itself is the payload. A file with no row here has no provenance and cannot be vouched
for.

Add the row at the closeout that produced the artifact, not later. `Provenance` should answer "where
did this come from and who has seen it" — the generating script or session, who it was issued to, and
any supersession. `Tracker key` is the Jira issue or Confluence page when one exists, otherwise `—`.

Sensitive material requires the owner's prior approval before it enters the repository at all. Where a
project defines a restricted tier, it lives under `artifacts/restricted/`, is git-excluded, and is
recorded here with its tier marked — never silently omitted. Do not create that tier speculatively;
add it only when the project's `AGENTS.md` defines it.

| Filename | Date | Author | Provenance | Tracker key |
|---|---|---|---|---|
