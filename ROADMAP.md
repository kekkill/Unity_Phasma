# Roadmap

1. MissionValidation passed locally (user screenshot: 73 checks); user reports all checks/gameplay work and shows successful extraction with the correct ghost choice. Resolve any reported regressions using Docs/MISSION_LOOP_CHECK.md.
2. Record local results and fix any observed regressions, then agree the next medium-sized batch.
3. Agree distinct evidence combinations and refine instrument/evidence conditions (including future EMF-5). All three current types still share four evidence flags and differ in salt/cross behavior.
4. Expand single-player gameplay after agreeing scope: more usable inventory, objectives, evidence feedback, hiding/noise mechanics, map furnishing and hunt balance. Economy/progression and final audiovisual presentation are separate later decisions.
5. Later, choose multiplayer transport and implement server authority, ownership, replication, personal/shared journal policy, dead-player visibility/voice routing and connection handling. Mission phase/results are shared local state, while life/notes/choice/input remain per participant. No networking yet.

Navigation/door recovery, death/restart and defensive items were confirmed locally by the user (defensive menu: 17 checks plus manual gameplay). JournalValidation passed locally (33 checks); the user tentatively reports gameplay works. MissionValidation passed locally (73 checks), with successful extraction shown and manual gameplay reported working. Standalone builds and multiplayer remain unverified. Baseline 9bbd013 remains intact; feature branches are not automatically merged into main.
