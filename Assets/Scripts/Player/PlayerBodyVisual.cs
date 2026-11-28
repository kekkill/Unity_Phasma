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

    private readonly struct Limb
    {
        public readonly Transform Upper, Lower, End;
        public bool IsValid => Upper != null && Lower != null && End != null;

        public Limb(Transform upper, Transform lower, Transform end)
        {
            Upper = upper;
            Lower = lower;
            End = end;
        }
    }

    private readonly List<(Transform Bone, Quaternion RestRotation, bool IsIndex)> fingers = new();
    private CharacterController capsule;
    private PlayerCrouch crouch;
    private PlayerLifeState life;
    private PlayerItemHolder holder;
    private Transform holdPoint;
    private Vector3 originalHoldPosition;
    private GameObject instance;
    private PlayableGraph graph;
    private AnimationMixerPlayable poseMixer;
    private Transform eyePoint, hips, deviceSocket;
    private Limb leftArm, rightArm, leftLeg, rightLeg;
    private Quaternion wristRest;
    private Vector3 gaitVelocity;
    private float gaitPhase;
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

        var bones = instance.GetComponentsInChildren<Transform>();
        Transform FindBone(string name) => Array.Find(bones, bone => bone.name == name);
        eyePoint = FindBone("EyePoint");
        hips = FindBone("Hips");
        deviceSocket = FindBone("Socket_Device_R");
        leftArm = new Limb(FindBone("LeftArm"), FindBone("LeftForeArm"), FindBone("LeftHand"));
        rightArm = new Limb(FindBone("RightArm"), FindBone("RightForeArm"), FindBone("RightHand"));
        leftLeg = new Limb(FindBone("LeftUpLeg"), FindBone("LeftLeg"), FindBone("LeftFoot"));
        rightLeg = new Limb(FindBone("RightUpLeg"), FindBone("RightLeg"), FindBone("RightFoot"));
        if (rightArm.IsValid) wristRest = rightArm.End.localRotation;
        foreach (var bone in bones)
        {
            if (bone == rightArm.End || !bone.name.StartsWith("RightHand", StringComparison.Ordinal)) continue;
            fingers.Add((bone, bone.localRotation, bone.name.StartsWith("RightHandIndex", StringComparison.Ordinal)));
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
            ResetGait();
            if (item != null && hiddenItemRenderers == null) HideItemRendering(item);
            return;
        }
        RestoreItemRendering();

        float posture = crouch.VisualAmount;
        poseMixer.SetInputWeight(0, 1f - posture);
        poseMixer.SetInputWeight(1, posture);
        graph.Evaluate(0);
        PositionBody();
        Vector3 armSwing = AnimateGait(posture, out Vector3 gripSway);
        if (!rightArm.IsValid) return;
        if (item != null && holdPoint != null && deviceSocket != null)
        {
            holdPoint.localPosition += gripSway;
            AlignGrip(grip);
        }
        else
        {
            RelaxHand(posture, armSwing);
        }
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

    private Vector3 AnimateGait(float posture, out Vector3 gripSway)
    {
        Vector3 velocity = capsule.enabled && capsule.isGrounded
            ? transform.InverseTransformDirection(capsule.velocity) : Vector3.zero;
        velocity.y = 0f;
        if (velocity.sqrMagnitude < 0.01f) velocity = Vector3.zero;
        gaitVelocity = Vector3.MoveTowards(gaitVelocity, velocity, 24f * Time.deltaTime);
        float speed = gaitVelocity.magnitude;
        gripSway = Vector3.zero;
        if (speed < 0.01f)
        {
            ResetGait();
            return Vector3.zero;
        }

        float running = Mathf.InverseLerp(4f, 6.5f, speed) * (1f - posture);
        float cycleDistance = Mathf.Lerp(2.55f, 1.75f, posture) + 0.5f * running;
        gaitPhase = Mathf.Repeat(gaitPhase + speed * Time.deltaTime / cycleDistance, 1f);
        float strength = Mathf.SmoothStep(0f, 1f, speed / 2f);
        Vector3 direction = gaitVelocity / speed;
        float swing = Mathf.Cos(gaitPhase * 2f * Mathf.PI);
        float dip = 1f - swing * swing;

        if (hips != null && leftLeg.IsValid && rightLeg.IsValid)
        {
            Vector3 leftAnkle = leftLeg.End.position;
            Vector3 rightAnkle = rightLeg.End.position;
            float lowerHips = Mathf.Lerp(0.055f, 0.024f, posture) + dip * 0.018f;
            hips.position -= transform.up * (lowerHips * strength);

            // Side steps stay within the stance width so the feet do not cross.
            float forwardReach = Mathf.Lerp(0.29f, 0.18f, posture) + 0.045f * running;
            Vector3 localReach = new(0.105f * direction.x, 0f, forwardReach * direction.z);
            Vector3 reach = transform.TransformDirection(localReach) * strength;
            float lift = (Mathf.Lerp(0.14f, 0.085f, posture) + 0.045f * running) * strength;
            StepLeg(leftLeg, leftAnkle, gaitPhase, reach, lift);
            StepLeg(rightLeg, rightAnkle, Mathf.Repeat(gaitPhase + 0.5f, 1f), reach, lift);
        }

        float armReach = Mathf.Lerp(0.13f, 0.065f, posture) * strength;
        Vector3 swingDirection = new(0.3f * direction.x, 0.12f, direction.z);
        Vector3 armSwing = transform.TransformDirection(swingDirection) * (armReach * swing);
        if (leftArm.IsValid)
            MoveLimb(leftArm, leftArm.End.position + armSwing, leftArm.Lower.position - leftArm.Upper.position);

        gripSway = new Vector3(0.016f * swing, -0.025f * dip, 0.012f * direction.z * swing) * strength;
        return -armSwing;
    }

    private void StepLeg(Limb leg, Vector3 ankle, float phase, Vector3 reach, float lift)
    {
        // During contact the foot travels back along the stride. During recovery it lifts and swings ahead.
        float recovery = Mathf.Max(0f, 2f * phase - 1f);
        float travel = phase < 0.5f ? 1f - 4f * phase : -Mathf.Cos(recovery * Mathf.PI);
        float elevation = Mathf.Sin(recovery * Mathf.PI);
        Quaternion soleRotation = leg.End.rotation;
        MoveLimb(leg, ankle + reach * travel + transform.up * (elevation * elevation * lift), transform.forward, 0.995f);
        leg.End.rotation = soleRotation;
    }

    private void ResetGait()
    {
        gaitVelocity = Vector3.zero;
        gaitPhase = 0.25f;
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
        Transform hand = rightArm.End;
        Vector3 socketOffset = Quaternion.Inverse(hand.rotation) * (deviceSocket.position - hand.position);
        Quaternion relativeRotation = Quaternion.Inverse(hand.rotation) * deviceSocket.rotation;
        float wristRoll = pose switch { Grip.Flat => -26f, Grip.Controls => -10f, _ => 0f };
        Quaternion wristRotation = holdPoint.rotation * Quaternion.Euler(0f, 0f, wristRoll) * Quaternion.Inverse(relativeRotation);
        MoveLimb(rightArm, holdPoint.position - wristRotation * socketOffset,
            rightArm.Lower.position - rightArm.Upper.position);
        hand.rotation = wristRotation;
        holdPoint.position = deviceSocket.position;

        foreach (var (bone, rest, isIndex) in fingers)
        {
            float curl = pose switch { Grip.Flat => 0.35f, Grip.Controls when isIndex => 0.32f, _ => 0.65f };
            bone.localRotation = Quaternion.Slerp(rest, bone.localRotation, curl);
        }
    }

    private void RelaxHand(float posture, Vector3 armSwing)
    {
        Vector3 shoulder = rightArm.Upper.position;
        float side = Mathf.Sign(Vector3.Dot(shoulder - instance.transform.position, transform.right));
        Vector3 besideBody = new(side * 0.085f, -0.44f, Mathf.Lerp(-0.04f, 0.09f, posture));
        MoveLimb(rightArm, shoulder + transform.TransformVector(besideBody) + armSwing, rightArm.Lower.position - shoulder);
        rightArm.End.localRotation = wristRest;
        foreach (var (bone, rest, _) in fingers)
            bone.localRotation = Quaternion.Slerp(rest, bone.localRotation, 0.16f);
    }

    private void MoveLimb(Limb limb, Vector3 target, Vector3 bendDirection, float extension = 0.98f)
    {
        Vector3 start = limb.Upper.position;
        Vector3 joint = limb.Lower.position;
        float upperLength = Vector3.Distance(start, joint);
        float lowerLength = Vector3.Distance(joint, limb.End.position);
        Vector3 toTarget = target - start;
        if (upperLength < 0.01f || lowerLength < 0.01f || toTarget.sqrMagnitude < 0.0001f) return;

        Vector3 direction = toTarget.normalized;
        float distance = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(upperLength - lowerLength) + 0.005f,
            (upperLength + lowerLength) * extension);
        Vector3 bend = Vector3.ProjectOnPlane(bendDirection, direction);
        if (bend.sqrMagnitude < 0.0001f) bend = Vector3.Cross(direction, transform.up);
        if (bend.sqrMagnitude < 0.0001f) bend = transform.right;
        float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2f * distance);
        Vector3 jointGoal = start + direction * along + bend.normalized * Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
        Vector3 endGoal = start + direction * distance;
        limb.Upper.rotation = Quaternion.FromToRotation(joint - start, jointGoal - start) * limb.Upper.rotation;
        limb.Lower.rotation = Quaternion.FromToRotation(limb.End.position - limb.Lower.position, endGoal - limb.Lower.position) * limb.Lower.rotation;
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
        ResetGait();
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
