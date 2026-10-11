# Roadmap

1. Verify feature/evidence-journal in Unity using Docs/JOURNAL_CHECK.md: JournalValidation, actual UI clicks/scrolling, J/Esc, placement/input/crouch interactions, death/restart and defensive-item regressions.
2. Record user results, fix observed regressions, then agree the next medium-sized batch.
3. Agree distinct ghost evidence combinations and refine evidence conditions/instrument presentation, including any future EMF-5 signature.
4. Extend the journal into a complete single-player mission loop: explicit mission finish, evaluate final choice, results and retry.
5. Later, choose multiplayer transport and implement server authority, ownership, replication, personal/shared journal policy, dead-player visibility/voice routing and connection handling.

Navigation, blocked-door recovery, death/round completion and defensive items were confirmed locally by the user (defensive-item menu: PASSED, 17 checks, plus manual gameplay). The journal is implemented and cloud static checks passed; local Unity compilation, journal menu and UI/regression checks remain pending. Standalone builds and multiplayer remain unverified.
