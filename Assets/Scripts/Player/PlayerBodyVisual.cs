using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

[RequireComponent(typeof(CharacterController), typeof(PlayerCrouch))]
public sealed class PlayerBodyVisual : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;

    private const string ModelResource = "PhasmaInvestigator/Phasma_Investigator";
    private const float EyeClearance = 0.025f;

    private enum Grip { Round, Controls, Flat }

    private readonly List<(Transform Bone, Quaternion Rest, bool IsIndex)> fingers = new();
    private CharacterController capsule;
    private PlayerCrouch crouch;
    private PlayerLifeState life;
    private PlayerItemHolder holder;
    private Transform holdPoint;
    private Vector3 originalHoldPosition;
    private GameObject instance;
    private PlayableGraph graph;
    private AnimationMixerPlayable poseMixer;
    private Transform eyePoint, arm, forearm, hand, deviceSocket;
    private Quaternion wristRest;
    private PickupItem displayedItem;
    private Grip grip;
    private Renderer[] hiddenItemRenderers;
    private bool[] originalForceRenderingOff;
    private int originalCameraMask;
    private float originalNearClip;
    private int headLayer;

    private void Start()
    {
        if (viewCamera == null) return;
        var model = Resources.Load<GameObject>(ModelResource);
        AnimationClip standing = null;
        AnimationClip crouching = null;
        foreach (var clip in Resources.LoadAll<AnimationClip>(ModelResource))
        {
            if (clip.name.StartsWith("__preview__", StringComparison.Ordinal)) continue;
            if (clip.name.EndsWith("Stand_LookDown", StringComparison.Ordinal)) standing = clip;
            if (clip.name.EndsWith("Crouch_LookDown", StringComparison.Ordinal)) crouching = clip;
        }
        if (model == null || standing == null || crouching == null)
        {
            Debug.LogWarning("PlayerBodyVisual: investigator model or body poses are missing.", this);
            return;
        }

        capsule = GetComponent<CharacterController>();
        crouch = GetComponent<PlayerCrouch>();
        life = GetComponent<PlayerLifeState>();
        holder = GetComponent<PlayerItemHolder>();
        holdPoint = holder != null ? holder.HoldPoint : null;
        if (holdPoint != null) originalHoldPosition = holdPoint.localPosition;

        instance = Instantiate(model, transform, false);
        instance.name = "Phasma Investigator";
        headLayer = LayerMask.NameToLayer("PlayerHead");
        foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (renderer.name != "Head") continue;
            if (headLayer >= 0) renderer.gameObject.layer = headLayer;
            else renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        foreach (var bone in instance.GetComponentsInChildren<Transform>())
        {
            switch (bone.name)
            {
                case "EyePoint": eyePoint = bone; break;
                case "RightArm": arm = bone; break;
                case "RightForeArm": forearm = bone; break;
                case "RightHand": hand = bone; wristRest = bone.localRotation; break;
                case "Socket_Device_R": deviceSocket = bone; break;
                default:
                    if (bone.name.StartsWith("RightHand", StringComparison.Ordinal))
                        fingers.Add((bone, bone.localRotation, bone.name.StartsWith("RightHandIndex", StringComparison.Ordinal)));
                    break;
            }
        }

        if (!instance.TryGetComponent(out Animator animator)) animator = instance.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        graph = PlayableGraph.Create("Investigator body poses");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        poseMixer = AnimationMixerPlayable.Create(graph, 2);
        AddPose(standing, 0);
        AddPose(crouching, 1);
        AnimationPlayableOutput.Create(graph, "Investigator body", animator).SetSourcePlayable(poseMixer);
        graph.Play();

        originalCameraMask = viewCamera.cullingMask;
        originalNearClip = viewCamera.nearClipPlane;
        SetView(true);
        LateUpdate();
    }

    private void AddPose(AnimationClip clip, int index)
    {
        var playable = AnimationClipPlayable.Create(graph, clip);
        playable.SetSpeed(0);
        playable.SetTime(0);
        graph.Connect(playable, 0, poseMixer, index);
    }

    private void LateUpdate()
    {
        if (!graph.IsValid()) return;
        bool visible = viewCamera.isActiveAndEnabled && (life == null || life.IsAlive);
        if (instance.activeSelf != visible) instance.SetActive(visible);

        PickupItem item = holder != null ? holder.CurrentItem : null;
        if (item != displayedItem)
        {
            RestoreItemRendering();
            displayedItem = item;
            if (item != null) grip = ResolveGrip(item);
        }
        if (holdPoint != null) holdPoint.localPosition = originalHoldPosition;
        if (!visible)
        {
            if (item != null && hiddenItemRenderers == null) HideItemRendering(item);
            return;
        }
        RestoreItemRendering();

        float posture = crouch.VisualAmount;
        poseMixer.SetInputWeight(0, 1f - posture);
        poseMixer.SetInputWeight(1, posture);
        graph.Evaluate(0);
        PositionBody();
        if (arm == null || forearm == null || hand == null) return;
        if (item != null && holdPoint != null && deviceSocket != null)
            AlignGrip(grip);
        else
            RelaxHand(posture);
    }

    private void PositionBody()
    {
        Transform body = instance.transform;
        Vector3 feet = capsule.center - Vector3.up * (capsule.height * 0.5f);
        body.localPosition = feet;
        if (eyePoint == null) return;

        // The crouch pose leans forward. Move the mesh under the camera and keep its feet on the floor.
        float eyeZ = transform.InverseTransformPoint(eyePoint.position).z;
        float cameraZ = transform.InverseTransformPoint(viewCamera.transform.position).z;
        body.localPosition += Vector3.forward * (cameraZ - eyeZ - EyeClearance);
    }

    private static Grip ResolveGrip(PickupItem item)
    {
        if (item.TryGetComponent<FlashlightItem>(out _) || item.TryGetComponent<UVFlashlightItem>(out _) ||
            item.TryGetComponent<SaltContainer>(out _) || item.TryGetComponent<CrossItem>(out _)) return Grip.Round;
        if (item.TryGetComponent<GhostWritingBook>(out _) || item.TryGetComponent<MotionSensorItem>(out _)) return Grip.Flat;
        return Grip.Controls;
    }

    private void AlignGrip(Grip pose)
    {
        Vector3 socketOffset = Quaternion.Inverse(hand.rotation) * (deviceSocket.position - hand.position);
        Quaternion relativeRotation = Quaternion.Inverse(hand.rotation) * deviceSocket.rotation;
        float wristRoll = pose switch { Grip.Flat => -26f, Grip.Controls => -10f, _ => 0f };
        Quaternion wristRotation = holdPoint.rotation * Quaternion.Euler(0f, 0f, wristRoll) * Quaternion.Inverse(relativeRotation);
        MoveWrist(holdPoint.position - wristRotation * socketOffset);
        hand.rotation = wristRotation;
        holdPoint.position = deviceSocket.position;

        foreach (var (bone, rest, isIndex) in fingers)
        {
            float curl = pose switch { Grip.Flat => 0.35f, Grip.Controls when isIndex => 0.32f, _ => 0.65f };
            bone.localRotation = Quaternion.Slerp(rest, bone.localRotation, curl);
        }
    }

    private void RelaxHand(float posture)
    {
        float side = Mathf.Sign(Vector3.Dot(arm.position - instance.transform.position, transform.right));
        Vector3 besideBody = new(side * 0.085f, -0.44f, Mathf.Lerp(-0.04f, 0.09f, posture));
        MoveWrist(arm.position + transform.TransformVector(besideBody));
        hand.localRotation = wristRest;
        foreach (var (bone, rest, _) in fingers)
            bone.localRotation = Quaternion.Slerp(rest, bone.localRotation, 0.16f);
    }

    private void MoveWrist(Vector3 target)
    {
        Vector3 shoulder = arm.position;
        Vector3 elbow = forearm.position;
        float upperLength = Vector3.Distance(shoulder, elbow);
        float lowerLength = Vector3.Distance(elbow, hand.position);
        Vector3 toWrist = target - shoulder;
        if (upperLength < 0.01f || lowerLength < 0.01f || toWrist.sqrMagnitude < 0.0001f) return;

        Vector3 direction = toWrist.normalized;
        float distance = Mathf.Clamp(toWrist.magnitude, Mathf.Abs(upperLength - lowerLength) + 0.005f,
            (upperLength + lowerLength) * 0.98f);
        Vector3 bend = Vector3.ProjectOnPlane(elbow - shoulder, direction).normalized;
        if (bend.sqrMagnitude < 0.01f) bend = Vector3.ProjectOnPlane(transform.right, direction).normalized;
        float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2f * distance);
        Vector3 elbowGoal = shoulder + direction * along + bend * Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
        Vector3 wristGoal = shoulder + direction * distance;
        arm.rotation = Quaternion.FromToRotation(elbow - shoulder, elbowGoal - shoulder) * arm.rotation;
        forearm.rotation = Quaternion.FromToRotation(hand.position - forearm.position, wristGoal - forearm.position) * forearm.rotation;
    }

    private void HideItemRendering(PickupItem item)
    {
        hiddenItemRenderers = item.GetComponentsInChildren<Renderer>(true);
        originalForceRenderingOff = new bool[hiddenItemRenderers.Length];
        for (int i = 0; i < hiddenItemRenderers.Length; i++)
        {
            originalForceRenderingOff[i] = hiddenItemRenderers[i].forceRenderingOff;
            hiddenItemRenderers[i].forceRenderingOff = true;
        }
    }

    private void RestoreItemRendering()
    {
        if (hiddenItemRenderers == null) return;
        for (int i = 0; i < hiddenItemRenderers.Length; i++)
            if (hiddenItemRenderers[i] != null)
                hiddenItemRenderers[i].forceRenderingOff = originalForceRenderingOff[i];
        hiddenItemRenderers = null;
        originalForceRenderingOff = null;
    }

    private void SetView(bool show)
    {
        if (viewCamera == null) return;
        viewCamera.cullingMask = show && headLayer >= 0 ? originalCameraMask & ~(1 << headLayer) : originalCameraMask;
        viewCamera.nearClipPlane = show ? Mathf.Min(originalNearClip, 0.06f) : originalNearClip;
    }

    private void OnEnable()
    {
        if (!graph.IsValid()) return;
        SetView(true);
    }

    private void OnDisable()
    {
        if (!graph.IsValid()) return;
        if (instance != null) instance.SetActive(false);
        RestoreItemRendering();
        if (holdPoint != null) holdPoint.localPosition = originalHoldPosition;
        SetView(false);
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
        if (instance != null) Destroy(instance);
    }
}
