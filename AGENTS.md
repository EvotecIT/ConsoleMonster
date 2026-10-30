# Repository guidance

Resolve shared procedures against `CODEX_HOME` (default `~/.codex`). Before adding
or changing tests, read `skills/test-audit/SKILL.md#authoring-gate` and use
`skills/contract-focused-tests/SKILL.md` for the proof boundary. Deliberate test
pruning follows `skills/test-audit/CAMPAIGN.md`; coverage collected for that work
is local evidence rather than a CI gate. Golden and visual-baseline deletions
require Claude's review.

Website work also follows [Website/AGENTS.md](Website/AGENTS.md).
