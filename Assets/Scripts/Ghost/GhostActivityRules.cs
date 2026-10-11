using UnityEngine;

// Profile configuration in; deterministic activity decisions out. No scene/input state.
public static class GhostActivityRules
{
    public static int EmfLevel(GhostProfile profile, float anger, bool hunting)
    {
        if (profile == null) return 1;
        int maximum = profile.HasEmfEvidence ? 5 : 4;
        if (hunting) return maximum;
        anger = Mathf.Clamp(anger, 0f, 100f);
        if (profile.HasEmfEvidence && anger >= profile.EmfLevel5Anger) return 5;
        if (anger >= profile.EmfLevel4Anger) return 4;
        return anger >= profile.EmfLevel3Anger ? 3 : 2;
    }
    public static float InteractionChance(float baseChance, float anger) =>
        Mathf.Lerp(Mathf.Clamp01(baseChance), 1f, Mathf.Clamp01(anger / 100f));
    public static float IdleMultiplier(GhostProfile profile, float anger) => profile == null ? 1f :
        Mathf.Lerp(1f, profile.AngryIdleMultiplier, Mathf.Clamp01(anger / 100f));
}
