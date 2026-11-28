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

    private enum Grip { Flashlight, Round, Controls, Flat }

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
    private Quaternion? leftPalmBasis, rightPalmBasis;
    private Quaternion leftWristRest, rightWristRest;
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
            // The imported clip bounds do not cover all wrist positions reached by runtime IK.
            if (renderer.name == "Hands_and_Neck") renderer.updateWhenOffscreen = true;
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
        leftPalmBasis = ReadPalmBasis(leftArm, "Left", FindBone);
        rightPalmBasis = ReadPalmBasis(rightArm, "Right", FindBone);
        if (leftPalmBasis.HasValue) leftWristRest = leftArm.End.localRotation;
        if (rightPalmBasis.HasValue) rightWristRest = rightArm.End.localRotation;
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
        RelaxArm(leftArm, leftPalmBasis, leftWristRest, posture, armSwing);
        if (item != null && holdPoint != null && deviceSocket != null && rightPalmBasis.HasValue)
        {
            float upwardPitch = Mathf.Max(0f, Vector3.Dot(viewCamera.transform.forward, transform.up));
            float inward = 0.02f + 0.09f * Mathf.InverseLerp(0.12f, 1f, upwardPitch);
            float raise = 0.115f + upwardPitch * (0.07f + 0.12f * (1f - upwardPitch));
            holdPoint.localPosition += gripSway + new Vector3(-inward, raise, 0f);
            AlignGrip(grip, rightPalmBasis.Value);
        }
        else
        {
            RelaxArm(rightArm, rightPalmBasis, rightWristRest, posture, -armSwing);
            foreach (var (bone, rest, _) in fingers)
                bone.localRotation = Quaternion.Slerp(rest, bone.localRotation, 0.1f);
        }
    }

    private static Quaternion? ReadPalmBasis(Limb limb, string side, Func<string, Transform> findBone)
    {
        Transform middle = findBone(side + "HandMiddle1");
        Transform index = findBone(side + "HandIndex1");
        Transform pinky = findBone(side + "HandPinky1");
        Transform thumb = findBone(side + "HandThumb2");
        if (!limb.IsValid || middle == null || index == null || pinky == null || thumb == null) return null;

        Transform wrist = limb.End;
        Vector3 knuckles = middle.position - wrist.position;
        Vector3 backOfHand = Vector3.Cross(knuckles, index.position - pinky.position);
        if (Vector3.Dot(backOfHand, thumb.position - wrist.position) > 0f) backOfHand = -backOfHand;
        return Quaternion.Inverse(wrist.rotation) * Quaternion.LookRotation(knuckles, backOfHand);
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
        gaitVelocity = Vector3.MoveTowards(gaitVelocity, velocity, 18f * Time.deltaTime);
        float speed = gaitVelocity.magnitude;
        gripSway = Vector3.zero;
        if (speed < 0.01f)
        {
            ResetGait();
            return Vector3.zero;
        }

        float running = Mathf.InverseLerp(4f, 6.5f, speed) * (1f - posture);
        Vector3 direction = gaitVelocity / speed;
        float cycleDistance = Mathf.Lerp(2.55f, 1.85f, posture) + 0.5f * running - Mathf.Abs(direction.x) * 0.25f;
        gaitPhase = Mathf.Repeat(gaitPhase + speed * Time.deltaTime / cycleDistance, 1f);
        float strength = Mathf.SmoothStep(0f, 1f, speed / 2f);
        float swing = Mathf.Cos(gaitPhase * 2f * Mathf.PI);
        float dip = 1f - swing * swing;

        if (hips != null && leftLeg.IsValid && rightLeg.IsValid)
        {
            // Side steps stay within the stance width so the feet do not cross.
            float forwardReach = 0.18f + 0.09f * running;
            Vector3 localReach = new(0.105f * direction.x, 0f, forwardReach * direction.z);
            Vector3 reach = transform.TransformDirection(localReach) * strength;
            float lift = (Mathf.Lerp(0.045f, 0.037f, posture) + 0.045f * running) * strength;
            float toeRoll = Mathf.Lerp(12f, 7f, posture) * direction.z * strength;
            float upright = (1f - posture) * strength;
            Vector3 leftAnkle = NeutralAnkle(leftLeg, upright) + StepOffset(gaitPhase, reach, lift, out float leftRoll);
            Vector3 rightAnkle = NeutralAnkle(rightLeg, upright) + StepOffset(Mathf.Repeat(gaitPhase + 0.5f, 1f), reach, lift, out float rightRoll);

            // Lower the hips only as far as needed to keep the step reachable.
            hips.position += transform.up * (0.012f * upright);
            float reachDrop = Mathf.Max(RequiredHipDrop(leftLeg, leftAnkle), RequiredHipDrop(rightLeg, rightAnkle));
            float crouchDip = posture * (0.018f + dip * 0.012f) * strength;
            hips.position -= transform.up * Mathf.Max(reachDrop, crouchDip);
            StepLeg(leftLeg, leftAnkle, toeRoll * leftRoll);
            StepLeg(rightLeg, rightAnkle, toeRoll * rightRoll);
        }

        float armReach = Mathf.Lerp(0.075f, 0.033f, posture) * strength;
        Vector3 swingDirection = new(0.25f * direction.x, 0.08f, direction.z);
        gripSway = new Vector3(0.012f * swing, -0.014f * dip, 0.009f * direction.z * swing) * strength;
        return transform.TransformDirection(swingDirection) * (armReach * swing);
    }

    private Vector3 NeutralAnkle(Limb leg, float upright)
    {
        Vector3 ankle = leg.End.position;
        float aheadOfHip = Vector3.Dot(ankle - leg.Upper.position, transform.forward);
        return ankle - transform.forward * (0.6f * upright * aheadOfHip);
    }

    private Vector3 StepOffset(float phase, Vector3 reach, float lift, out float roll)
    {
        // Match both halves at zero endpoint speed, with most of the counter-travel during support.
        float contact = Mathf.Min(1f, phase * 2f);
        float step = contact - Mathf.Sin(contact * 2f * Mathf.PI) / (2f * Mathf.PI);
        float recovery = Mathf.Max(0f, 2f * phase - 1f);
        float recoveryCosine = Mathf.Cos(recovery * Mathf.PI);
        float travel = phase < 0.5f ? 1f - 2f * step : -recoveryCosine;
        float elevation = Mathf.Sin(recovery * Mathf.PI);
        float lifted = elevation * elevation;
        roll = lifted * recoveryCosine;
        return reach * travel + transform.up * (lifted * lift);
    }

    private float RequiredHipDrop(Limb leg, Vector3 ankle)
    {
        Vector3 fromHip = ankle - leg.Upper.position;
        float down = -Vector3.Dot(fromHip, transform.up);
        float horizontal = Mathf.Max(0f, fromHip.sqrMagnitude - down * down);
        float reach = (Vector3.Distance(leg.Upper.position, leg.Lower.position) +
            Vector3.Distance(leg.Lower.position, leg.End.position)) * 0.995f;
        return Mathf.Max(0f, down - Mathf.Sqrt(Mathf.Max(0f, reach * reach - horizontal)));
    }

    private void StepLeg(Limb leg, Vector3 ankle, float toeRoll)
    {
        Quaternion soleRotation = Quaternion.AngleAxis(toeRoll, transform.right) * leg.End.rotation;
        Vector3 kneeDirection = transform.forward + Outward(leg) * 0.18f;
        MoveLimb(leg, ankle, kneeDirection, 0.995f);
        leg.End.rotation = soleRotation;
    }

    private void ResetGait()
    {
        gaitVelocity = Vector3.zero;
        gaitPhase = 0.25f;
    }

    private static Grip ResolveGrip(PickupItem item)
    {
        if (item.TryGetComponent<FlashlightItem>(out _) || item.TryGetComponent<UVFlashlightItem>(out _)) return Grip.Flashlight;
        if (item.TryGetComponent<SaltContainer>(out _) || item.TryGetComponent<CrossItem>(out _)) return Grip.Round;
        if (item.TryGetComponent<GhostWritingBook>(out _) || item.TryGetComponent<MotionSensorItem>(out _)) return Grip.Flat;
        return Grip.Controls;
    }

    private void AlignGrip(Grip pose, Quaternion palmBasis)
    {
        Transform hand = rightArm.End;
        Vector3 socketOffset = Quaternion.Inverse(hand.rotation) * (deviceSocket.position - hand.position);
        float tilt = pose == Grip.Flat ? 0.26f : 0.55f;
        Vector3 knuckles = (holdPoint.forward - tilt * holdPoint.up).normalized;
        Vector3 backOfHand = holdPoint.up + holdPoint.right * (pose is Grip.Round or Grip.Flashlight ? 0.3f : 0.14f);
        Quaternion wristRotation = Quaternion.LookRotation(knuckles, backOfHand) * Quaternion.Inverse(palmBasis);
        float liftFromPalm = pose switch { Grip.Flashlight => 0.008f, Grip.Flat => 0.022f, _ => 0.032f };
        Vector3 itemOffset = holdPoint.forward * 0.055f + holdPoint.up * liftFromPalm;
        MoveLimb(rightArm, holdPoint.position - wristRotation * socketOffset - itemOffset, ElbowDirection(rightArm));
        SetWrist(rightArm, palmBasis, rightWristRest, knuckles, backOfHand);
        holdPoint.position = deviceSocket.position + itemOffset;

        foreach (var (bone, rest, isIndex) in fingers)
        {
            float curl = pose switch { Grip.Flat => 0.35f, Grip.Controls when isIndex => 0.32f, _ => 0.65f };
            bone.localRotation = Quaternion.Slerp(rest, bone.localRotation, curl);
        }
    }

    private void RelaxArm(Limb arm, Quaternion? palmBasis, Quaternion wristRest, float posture, Vector3 swing)
    {
        if (!palmBasis.HasValue) return;
        Vector3 outside = Outward(arm);
        Vector3 wristOffset = outside * 0.06f - transform.up * Mathf.Lerp(0.47f, 0.33f, posture)
            + transform.forward * Mathf.Lerp(0.10f, 0.33f, posture);
        MoveLimb(arm, arm.Upper.position + wristOffset + swing, ElbowDirection(arm));
        Vector3 knuckles = arm.End.position - arm.Lower.position;
        SetWrist(arm, palmBasis.Value, wristRest, knuckles, outside + transform.up * 0.18f);
    }

    private static void SetWrist(Limb arm, Quaternion palmBasis, Quaternion wristRest, Vector3 knuckles, Vector3 backOfHand)
    {
        Vector3 forearm = (arm.End.position - arm.Lower.position).normalized;
        Vector3 direction = Vector3.RotateTowards(forearm, knuckles.normalized, 22f * Mathf.Deg2Rad, 0f);
        Quaternion wrist = Quaternion.LookRotation(direction, backOfHand) * Quaternion.Inverse(palmBasis);

        // Carry the palm's roll along the forearm while keeping both elbow and wrist positions fixed.
        Vector3 localForearm = Quaternion.Inverse(arm.Lower.rotation) * forearm;
        Quaternion neutralWristForearm = wrist * Quaternion.Inverse(wristRest);
        Quaternion alignment = Quaternion.FromToRotation(neutralWristForearm * localForearm, forearm);
        arm.Lower.rotation = alignment * neutralWristForearm;
        arm.End.rotation = wrist;
    }

    private Vector3 Outward(Limb limb) => transform.right * Mathf.Sign(Vector3.Dot(limb.Upper.position - transform.position, transform.right));

    private Vector3 ElbowDirection(Limb arm) => Outward(arm) * 0.55f - transform.up - transform.forward * 0.25f;

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
