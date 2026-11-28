using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

[RequireComponent(typeof(CharacterController), typeof(PlayerCrouch))]
public sealed class PlayerBodyVisual : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;

    private const string ModelResource = "PhasmaInvestigator/Phasma_Investigator";
    private CharacterController capsule;
    private PlayerCrouch crouch;
    private PlayerLifeState life;
    private PlayerItemHolder holder;
    private Transform holdPoint;
    private Vector3 originalHoldPosition;
    private GameObject instance;
    private PlayableGraph graph;
    private AnimationMixerPlayable poseMixer;
    private Transform arm, forearm, hand, deviceSocket;
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
                case "RightArm": arm = bone; break;
                case "RightForeArm": forearm = bone; break;
                case "RightHand": hand = bone; break;
                case "Socket_Device_R": deviceSocket = bone; break;
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
        if (life != null && !life.IsAlive)
        {
            instance.SetActive(false);
            return;
        }

        float posture = crouch.VisualAmount;
        poseMixer.SetInputWeight(0, 1f - posture);
        poseMixer.SetInputWeight(1, posture);
        graph.Evaluate(0);
        instance.transform.localPosition = capsule.center - Vector3.up * (capsule.height * 0.5f) - Vector3.forward * 0.08f;
        if (holdPoint == null) return;
        holdPoint.localPosition = originalHoldPosition;
        if (holder.HasItem && arm != null && forearm != null && hand != null && deviceSocket != null)
            AlignGrip();
    }

    private void AlignGrip()
    {
        Vector3 socketOffset = Quaternion.Inverse(hand.rotation) * (deviceSocket.position - hand.position);
        Quaternion relativeRotation = Quaternion.Inverse(hand.rotation) * deviceSocket.rotation;
        Quaternion wristRotation = holdPoint.rotation * Quaternion.Inverse(relativeRotation);
        Vector3 shoulder = arm.position;
        Vector3 elbow = forearm.position;
        float upperLength = Vector3.Distance(shoulder, elbow);
        float lowerLength = Vector3.Distance(elbow, hand.position);
        Vector3 toWrist = holdPoint.position - wristRotation * socketOffset - shoulder;
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
        hand.rotation = wristRotation;
        holdPoint.position = deviceSocket.position;
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
        instance.SetActive(true);
        SetView(true);
    }

    private void OnDisable()
    {
        if (!graph.IsValid()) return;
        if (instance != null) instance.SetActive(false);
        if (holdPoint != null) holdPoint.localPosition = originalHoldPosition;
        SetView(false);
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
        if (instance != null) Destroy(instance);
    }
}
