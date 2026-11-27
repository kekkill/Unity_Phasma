using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;

// The skinned body is presentation only. The NavMesh agent owns placement and travel.
[RequireComponent(typeof(GhostNavigation), typeof(GhostHuntController), typeof(GhostDebugVisual))]
public sealed class GhostModelVisual : MonoBehaviour
{
    [SerializeField] private Material skin;
    [SerializeField] private Material clothes;
    [SerializeField] private Material shoes;
    [SerializeField] private Material eyes;
    [SerializeField] private Material hair;

    private const string ModelPath = "PhasmaGhost/PhasmaGhost";
    private const string ClipPath = "PhasmaGhost/Animations/PhasmaGhost@";
    private const float WalkClipSpeed = 0.99f / 1.55f;
    private const float ChaseClipSpeed = 1.56f / 0.90f;

    private GameObject instance;
    private NavMeshAgent agent;
    private GhostHuntController hunt;
    private GhostDebugVisual fallback;
    private bool fallbackWasEnabled;
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private AnimationClipPlayable walk;
    private AnimationClipPlayable chase;
    private float walkRate = 1f;
    private float chaseRate = 1f;

    private void Start()
    {
        var model = Resources.Load<GameObject>(ModelPath);
        var idleClip = LoadClip("Idle");
        var walkClip = LoadClip("Walk_InPlace");
        var chaseClip = LoadClip("Chase_InPlace");
        if (model == null || idleClip == null || walkClip == null || chaseClip == null)
        {
            Debug.LogWarning("GhostModelVisual: model or locomotion clip missing.", this);
            return;
        }

        agent = GetComponent<NavMeshAgent>();
        hunt = GetComponent<GhostHuntController>();
        fallback = GetComponent<GhostDebugVisual>();
        fallbackWasEnabled = fallback.enabled;
        instance = Instantiate(model, transform, false);
        instance.name = "Phasma Ghost";
        instance.transform.localPosition = Vector3.down * GetComponent<GhostNavigation>().StandingOffset;
        ApplyMaterials();

        if (!instance.TryGetComponent(out Animator animator))
            animator = instance.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

        graph = PlayableGraph.Create("Phasma Ghost Locomotion");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        mixer = AnimationMixerPlayable.Create(graph, 3);
        Mix(idleClip, 0);
        walk = Mix(walkClip, 1);
        chase = Mix(chaseClip, 2);
        mixer.SetInputWeight(0, 1f);
        var output = AnimationPlayableOutput.Create(graph, "Ghost bones", animator);
        output.SetSourcePlayable(mixer);
        ShowModel();
    }

    private static AnimationClip LoadClip(string name)
    {
        foreach (var clip in Resources.LoadAll<AnimationClip>(ClipPath + name))
            if (!clip.name.StartsWith("__preview__", System.StringComparison.Ordinal)) return clip;
        return null;
    }

    private AnimationClipPlayable Mix(AnimationClip clip, int slot)
    {
        var playable = AnimationClipPlayable.Create(graph, clip);
        graph.Connect(playable, 0, mixer, slot);
        return playable;
    }

    private void Update()
    {
        if (!graph.IsValid()) return;
        Vector3 velocity = agent.enabled && agent.isOnNavMesh ? agent.velocity : Vector3.zero;
        velocity.y = 0f;
        float speed = velocity.magnitude;
        int activeSlot = 0;
        if (speed >= 0.08f) activeSlot = hunt.IsHunting ? 2 : 1;
        float blend = 1f - Mathf.Exp(-10f * Time.deltaTime);
        for (int slot = 0; slot < mixer.GetInputCount(); slot++)
        {
            float targetWeight = slot == activeSlot ? 1f : 0f;
            mixer.SetInputWeight(slot, Mathf.Lerp(mixer.GetInputWeight(slot), targetWeight, blend));
        }

        walkRate = Mathf.Lerp(walkRate, Mathf.Clamp(speed / WalkClipSpeed, 0.2f, 3.2f), blend);
        chaseRate = Mathf.Lerp(chaseRate, Mathf.Clamp(speed / ChaseClipSpeed, 0.2f, 3.2f), blend);
        walk.SetSpeed(walkRate);
        chase.SetSpeed(chaseRate);
    }

    private void ApplyMaterials()
    {
        foreach (var piece in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Material material = piece.name switch
            {
                "Skin" => skin,
                "Jacket_and_Jeans" => clothes,
                "Trail_Shoes" => shoes,
                "Eyes" => eyes,
                "Hair_Cards" => hair,
                _ => null
            };
            if (material != null) piece.sharedMaterial = material;
        }
    }

    private void ShowModel()
    {
        fallback.enabled = false;
        instance.SetActive(true);
        graph.Play();
    }

    private void OnEnable()
    {
        if (graph.IsValid()) ShowModel();
    }

    private void OnDisable()
    {
        if (!graph.IsValid()) return;
        graph.Stop();
        instance.SetActive(false);
        if (fallback != null) fallback.enabled = fallbackWasEnabled;
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
        if (instance != null) Destroy(instance);
    }
}
