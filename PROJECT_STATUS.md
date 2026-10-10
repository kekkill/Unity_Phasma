# Project state

Updated: 2026-10-10.

## Scope
Single-player paranormal investigation prototype. Multiplayer is a future goal; current changes should keep movement, decisions, configuration and presentation separate.

Unity: 6000.6.3f1. URP. Gameplay scene: Assets/Scenes/Prototype.unity.
Original baseline: 9bbd013 (Initial prototype — working baseline).

## Existing systems
Player movement/look/crouch/sprint; input abstraction; interaction UI; one held pickup/use/drop item; room temperature; doors/light switches; EMF/UV/writing evidence; motion sensor; ghost vision/hearing/memory; anger and probabilistic hunts; chase/search; action-blocking protection zones.
Previous checkpoint reports gameplay checks for these systems. Those historical results are not verification of the navigation changes.

## Navigation change awaiting Unity verification
- GhostNavigation owns NavMeshAgent, complete-path validation, spawning, roaming and search-point selection.
- GhostActor and GhostHuntController request movement rather than changing transform.position.
- RuntimeNavigationWorld builds a local NavMesh in Awake before ghost initialization. Prototype explicitly references floor and wall colliders; room/item triggers are excluded.
- DoorVisual gets a carving NavMeshObstacle at runtime.
- New components and references are already saved in Prototype.unity. No manual Inspector wiring is required for this scene.
- This is a small-map prototype bake at startup, not a production solution for large maps. New static geometry must be added to the builder's Geometry list. New moving blockers must be assigned separately.

## Validation
Cloud checks passed: C# syntax parsing for all 40 source files, unique asset GUIDs, scene component references, and git diff --check. Syntax parsing does not resolve Unity APIs or replace compilation.

Unity Editor is unavailable in the current cloud machine. Unity import, compilation, pathfinding and gameplay remain unverified. See Docs/NAVIGATION_CHECK.md for the local validation procedure.

## Known remaining limitations
No networking, attack/death, journal or mission completion. One held item. Generic UV and EMF generation needs event-specific rules. Child triggers can remain active on held items. Hearing has no sound occlusion. Search is still radius-based, not room-graph-based. EditorBuildSettings still selects SampleScene; open Prototype explicitly for gameplay checks.
