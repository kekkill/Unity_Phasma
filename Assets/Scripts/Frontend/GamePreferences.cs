using UnityEngine;

public static class GamePreferences
{
    private const string VolumeKey = "phasma.volume.v1", SensitivityKey = "phasma.sensitivity.v1";
    private static bool loaded;
    private static float volume, sensitivity;
    public static float Volume { get { Load(); return volume; } }
    public static float Sensitivity { get { Load(); return sensitivity; } }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => loaded = false;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() { Load(); AudioListener.volume = volume; }
    private static void Load()
    {
        if (loaded) return;
        loaded = true;
        volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, 1f), 0.25f, 3f);
    }
    public static void SetVolume(float value)
    {
        Load(); volume = Mathf.Clamp01(value);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumeKey, volume);
    }
    public static void SetSensitivity(float value)
    {
        Load(); sensitivity = Mathf.Clamp(value, 0.25f, 3f);
        PlayerPrefs.SetFloat(SensitivityKey, sensitivity);
    }
    public static void Save() => PlayerPrefs.Save();
}
