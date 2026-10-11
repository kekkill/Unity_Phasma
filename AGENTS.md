# Project instructions

- Unity 6000.6.3f1, URP. Current test/gameplay scene: Assets/Scenes/RoomSearchPrototype.unity. Earlier layout: Assets/Scenes/Prototype.unity.
- Implement single-player now. Keep responsibilities separated to support multiplayer later; do not introduce networking until requested.
- Design for PC and future phones. Route gameplay input through Input System actions, not direct Keyboard/Mouse polling in gameplay components. Keep input, gameplay state and UI separate so touch controls can drive the same actions. Mobile controls, responsive UI and performance/build checks on actual phones need their own agreed batch; do not claim mobile readiness from desktop tests.
- Read PROJECT_STATUS.md and ROADMAP.md before starting a new feature. Inspect actual repository files rather than treating old code snapshots as current.
- Preserve existing Inspector fields, GUIDs and .meta files. Keep runtime state on components, configuration on ScriptableObject assets.
- Agree feature scope with the user before each medium-sized batch. Explain component ownership, important Inspector references and the gameplay check in plain Russian for the user.
- Keep project state and validation results current. Clearly distinguish source inspection, syntax checks, Unity compilation and gameplay verification.
- Work in the existing checkout. Do not create a worktree unless the user asks.
- Keep the initial prototype commit 9bbd013 as the baseline; work on feature branches.
