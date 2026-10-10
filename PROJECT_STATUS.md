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
No networking, attack/death, journal or mission completion. One held item. Generic UV and EMF generation needs event-specific rules. Child triggers can remain active on held items. Hearing has no sound occlusion. The old radius-search helper remains available; the latest hunt search now uses room connections. EditorBuildSettings now selects RoomSearchPrototype; open that scene for current checks.

## Local test report and navigation fix
The user reported automatic Error Pause and a stationary ghost. Console points to NullReferenceException inside NavMeshAgent.CalculatePath. Move NavMeshPath construction from the field initializer to Awake, and recreate it if absent before CalculatePath. The fix is awaiting local Unity validation; the stack screenshot does not establish why the path reference was absent. During HuntActive, constant anger/hunt chance is intentional.

## Three-zone layout and room search (latest increment)
New scene: Assets/Scenes/RoomSearchPrototype.unity, selected as the enabled build scene. Two 6 × 6 m rooms and a 4 × 6 m corridor, perimeter walls and two 2 m openings. Bedroom ↔ Corridor ↔ Kitchen, reciprocal Inspector links. Corridor cannot be the home ghost room. Original Prototype layout remains available.

GhostRoomSearch chooses local goals and adjacent rooms based on the last observed room recorded by GhostPerception. Hidden player position is not consulted during search. Roaming paths are constrained to the home room; return after hunts and search transitions can cross room boundaries through the physical openings.

The user reports the previous CalculatePath fix appears to work. Current room-search batch has NOT been run in Unity. A Play Mode validation menu is supplied for six complete room-center paths, reciprocal room links, containment and partition blocking. Static geometry clearance checks passed for open connections and a blocked bedroom opening. See Docs/ROOM_SEARCH_CHECK.md.

Limits still include unimplemented door-opening-for-navigation, attack/death, multiplayer, voice/noise occlusion and mission loop.

## Roaming regression report
The user reports hunting movement works but ordinary roaming does not; screenshot shows Idle with no active hunt. Arrival previously trusted remainingDistance, which can be stale just after SetPath. Arrival now requires physical horizontal proximity to the stored destination; missing-path failure has a short two-frame grace. This is a diagnosis to validate, not a confirmed runtime fix. Runtime Last Move Result and Requested Destination are exposed. Phasma/Validate Roaming (Play Mode) tests actual confined navigation/arrival using temporary agents without modifying the real actor. Neither this test nor the fix has been run in the cloud.

## Blocked-door search recovery
User confirms ordinary movement now works, but reports search stalls after closing the door. Code review found that a failed transition retried the unreachable destination room without returning to local search. Search now falls back to the physically occupied room, postpones that blocked neighbor, and handles an accepted path that stops making progress. Visible/heard but unreachable targets also fall back to accessible-room search without resetting it every frame. Door-opening AI is still not implemented. This recovery has not been run in Unity; local validation is required.

After a hunt, if the home room is unreachable, GhostActor can roam its current room and retry returning on later decisions; the favorite/home room and its temperature are not reassigned.

## User validation update
User reports blocked-door recovery now works. Unity 6000.6.3f1 emitted CS0618 for FindObjectsSortMode and the sorted FindObjectsByType overload in the two Editor validation tools. They now use FindObjectsByType<Room>() as recommended by that compiler. This API update awaits local compilation; no warnings were suppressed.

## Life state and all-dead round completion (latest batch)
Branch feature/player-death-round includes the preceding layout fixes. PlayerLifeState owns an idempotent Alive→Dead transition. GameSession registers participants, exposes living targets, and ends once after the final participant dies; empty rosters do not lose. PlayerDeathPresentation disables local controls/collider/UI and player renderers/audio while dropping held equipment. GhostPerception ignores deceased participants, removes their memory and selects detectable survivors. GhostAttack only runs from the hunt update, checks range, solid line of sight and Attack restrictions. RoundEndScreen provides scene restart. Both gameplay scenes are wired.

DeathRoundValidation provides a Play Mode test with isolated temporary participants, obstacles and a ghost perception fixture. No actual multiplayer or voice transport was added. All-dead checks and presence are currently local; future authority, replication and voice filtering remain required. Current batch is awaiting Unity compilation and gameplay checks. See Docs/DEATH_ROUND_CHECK.md.

PlayerInputReader now instantiates an action asset per participant, preventing death/disable from disabling another player's shared action map. Local-owner activation will still be required when networking is introduced.
