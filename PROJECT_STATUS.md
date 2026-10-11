# Project state

Updated: 2026-10-11.

## Scope and baseline
Unity 6000.6.3f1, URP. Single-player now; multiplayer later. Keep gameplay state, decisions, navigation and presentation separate. Original baseline commit: 9bbd013. Current gameplay scene: Assets/Scenes/RoomSearchPrototype.unity. Earlier Prototype.unity remains available. Work is on feature branches; main/baseline is not automatically merged.

## Implemented systems
- Player movement/look/crouch/sprint, per-participant input action instances, interaction UI and one held item.
- Rooms/temperature, doors/lights, EMF/UV/writing evidence and motion sensor.
- Ghost profile, vision/hearing/memory, anger, probabilistic hunts, chase and room search.
- GhostNavigation owns NavMesh movement; RuntimeNavigationWorld explicitly bakes floor/walls and gives the moving door a carving obstacle. Three connected zones: Bedroom ↔ Corridor ↔ Kitchen. Corridor is not a home ghost room. Roaming stays in a room; room transitions use physical openings. Blocked/stalled transitions fall back to reachable local search; AI does not open doors.
- Alive/Dead state, local participant registry, dead-target filtering, presentation/control disabling, line-of-sight attack, all-dead completion and restart. These are local foundations, not networking or voice transport.
- Latest batch (feature/defensive-items): Dropped/Held/Placed item states, all child colliders disabled while held, R/F placement preview, placed-only writing book/motion sensor. Cross protects in hand and when placed nearby, saves the breaking attack, breaks by profile/anger, repels hunting and supports immune types. Salt container (5 portions) places piles with profile-specific avoidance/scattering/footprints/ignore rules and dynamic Salt NavMesh area updates. Optional cyan ghost debug silhouette can be hidden in Inspector.
- Both gameplay scenes include placement, debug visual, salt reaction and a primitive defensive-item setup. The old Interaction-blocking ProtectionWard is preserved but inactive. Example FootprintGhostProfile and CrossImmuneGhostProfile are supplied alongside existing profiles.

## Validation evidence
User confirmed ordinary ghost movement and blocked-door recovery. Screenshot of local Phasma/Validate Death and Round (Play Mode) shows PASSED; user also confirms real death/restart button works. These reports refer to the previous batch.

Latest batch: cloud C# syntax parsing passed for 59 files; static three-zone geometry clearance, both scenes' local references/component wiring, 148 unique asset GUIDs/input bindings and git diff --check passed. Syntax checks do not resolve Unity APIs or establish compilation. Unity Editor is absent here, so new mechanics, compilation and dynamic salt pathfinding are NOT runtime-verified in the cloud. Phasma/Validate Defensive Items (Play Mode) supplies isolated actual attack and asynchronous NavMesh checks for the local Editor. See Docs/DEFENSIVE_ITEMS_CHECK.md. Prior check guides remain in Docs.

## Ownership and limitations
PickupItem owns placement state; CrossItem owns broken state/protection eligibility; SaltContainer owns portions; SaltPile owns intact/scattered state. PlayerItemPlacement owns local input/preview, GhostAttack requests protection before death, GhostHuntController ends/restricts hunts after repel, GhostSaltReaction owns local salt contacts/footprint presentation, GhostProfile holds configurable rules. Future server authority and replication remain necessary.

No multiplayer, journal or full mission loop. One held item. No voice/noise occlusion, door-opening AI, actual floating locomotion or final item models. UV/EMF evidence rules need further refinement. Salt rebakes are suitable for this small test layout; async latency and repeated placement must be measured in Unity. Square NavMesh modifier bounds conservatively approximate circular salt piles; escape is allowed if salt is poured under the ghost. Inventory/placement validation is local; it must be made authoritative before online play. Primitive visuals are for testing. Cross probability/radius and salt portions are starting balance values, not fixed design requirements.
