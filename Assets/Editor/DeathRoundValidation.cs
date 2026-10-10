#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DeathRoundValidation
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [MenuItem("Phasma/Validate Death and Round (Play Mode)")]
    public static void Validate()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode first.");
            return;
        }
        GameObject root = new GameObject("Temporary death validation");
        try
        {
            GameSession session = root.AddComponent<GameSession>();
            session.CheckAllPlayersDead();
            Check(!session.HasEnded, "An empty roster must not end the round.");
            GameObject first = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            first.name = "Validation player 1";
            first.transform.SetParent(root.transform);
            first.transform.position = new Vector3(1000f, 1000f, 1000f);
            PlayerLifeState life1 = first.AddComponent<PlayerLifeState>();
            life1.BindSession(session);
            GhostPerceptionTarget target1 = first.AddComponent<GhostPerceptionTarget>();
            first.AddComponent<PlayerDeathPresentation>();
            AudioSource audio = first.AddComponent<AudioSource>();
            GameObject second = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            second.name = "Validation player 2";
            second.transform.SetParent(root.transform);
            second.transform.position = new Vector3(1000f, 1000f, 1002f);
            PlayerLifeState life2 = second.AddComponent<PlayerLifeState>();
            life2.BindSession(session);
            GhostPerceptionTarget target2 = second.AddComponent<GhostPerceptionTarget>();
            Check(session.LivingTargets().Count() == 2, "Both live participants must be targetable.");

            GameObject attacker = new GameObject("Validation attacker");
            attacker.transform.SetParent(root.transform);
            attacker.transform.position = new Vector3(1000f, 1000f, 999f);
            GhostAttack attack = attacker.AddComponent<GhostAttack>();
            GhostActionRestrictions restrictions = attacker.GetComponent<GhostActionRestrictions>();
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(root.transform);
            wall.transform.position = new Vector3(1000f, 1000f, 999.5f);
            wall.transform.localScale = new Vector3(2f, 3f, 0.1f);
            Physics.SyncTransforms();
            Check(!attack.TryAttack(target1) && life1.IsAlive, "Wall must block attack.");
            wall.SetActive(false);
            Physics.SyncTransforms();
            restrictions.BlockAction(GhostActionType.Attack);
            Check(!attack.TryAttack(target1) && life1.IsAlive, "Attack restriction must protect player.");
            restrictions.UnblockAction(GhostActionType.Attack);
            int deaths = 0;
            life1.Died += _ => deaths++;
            Check(attack.TryAttack(target1), "Unblocked nearby attack must kill.");
            Check(!target1.IsAlive && target1.NoiseLevel == 0f, "Dead player must be undetectable and silent.");
            Check(!first.GetComponent<Renderer>().enabled && !audio.enabled, "Dead representation/audio must be hidden.");
            Check(!session.HasEnded && life2.IsAlive, "One survivor must keep the round running.");
            Check(session.LivingTargets().Count() == 1, "Only the survivor remains targetable.");
            first.GetComponent<Collider>().enabled = false;
            GhostController controller = attacker.AddComponent<GhostController>();
            var controllerData = new SerializedObject(controller);
            controllerData.FindProperty("ghostProfile").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GhostProfile>("Assets/GhostProfiles/RoomSearchTestProfile.asset");
            controllerData.ApplyModifiedPropertiesWithoutUndo();
            GhostPerception perception = attacker.AddComponent<GhostPerception>();
            var perceptionData = new SerializedObject(perception);
            perceptionData.FindProperty("session").objectReferenceValue = session;
            perceptionData.FindProperty("target").objectReferenceValue = target1;
            perceptionData.FindProperty("hasKnownTargetPosition").boolValue = true;
            perceptionData.ApplyModifiedPropertiesWithoutUndo();
            Physics.SyncTransforms();
            perception.RefreshTarget();
            Check(perception.Target == target2, "Ghost must select a visible survivor instead of the dead target.");
            Check(!life1.TryDie() && deaths == 1, "Repeated death must be idempotent.");
            int endings = 0;
            session.AllPlayersDead += () => endings++;
            Check(life2.TryDie(), "Second player must die.");
            session.CheckAllPlayersDead();
            Check(session.HasEnded && endings == 1, "Last death must end the round exactly once.");
            perception.RefreshTarget();
            Check(perception.Target == null && !perception.HasKnownTargetPosition,
                "Ghost must clear the final dead target and its memory.");
            Debug.Log("Death/round validation PASSED: wall and protection, presentation, living targets, two participants, idempotent death and round completion. This is a local simulation, not a multiplayer test.");
        }
        catch (Exception error)
        {
            Debug.LogError("Death/round validation FAILED: " + error.Message);
        }
        finally
        {
            UnityEngine.Object.Destroy(root);
        }
    }
}
#endif
