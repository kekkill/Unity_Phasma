# Roadmap

1. Current batch filters actor/equipment contacts and gives ghost-opened doors a brief hold. Verify both changes in ordinary Play Mode.
2. Review and adjust the editable [Archive House floor plan](Docs/ARCHIVE_LAYOUT.drawio), then block out the loop and check clearances and navigation around fixed furniture.
3. SoundHidingValidation and its menu stay removed. Do not restore them or add an elaborate replacement without agreement. Explain changed gameplay checks in plain language, distinguish static checks from actual Unity execution. Distance-based sound-location uncertainty and visible-target chase zigzag remain deferred by the user.
4. Phones need their own agreed batch: gameplay touch controls using the same Input System actions; migrate remaining OnGUI HUD/journal; mobile URP/build settings; actual device layouts/performance. Current scalable menu/results Canvas and safe-area support have not been verified on phones and do not establish mobile readiness.
5. Later choose multiplayer transport and implement server authority, stable participant identities, ownership/replication, personal/shared journal policy, dead-player visibility/voice routing and connection handling. Current registries, sound events, hiding knowledge, inventory, mission requests and completion state remain local foundations.

Baseline 9bbd013 remains intact; main is not automatically merged. Earlier local results: death/restart, defensive items (17 checks), journal (33), mission (73), salt contact (30), EMF/hunt activity (62), inventory 52-check run followed by corrected manual slot keys; normal activity was reported working with actual switch EMF screenshots. These belong to their earlier revisions and do not establish current regressions or standalone/mobile/network builds.
