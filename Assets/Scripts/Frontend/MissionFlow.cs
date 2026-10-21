using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Local scene flow only. Network mission authority can consume the launch snapshot later.
public static class MissionFlow
{
    public static MissionLaunchRequest CurrentRequest { get; private set; }
    public static bool OpenPreparation { get; private set; }
    public static bool IsLoading { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { CurrentRequest = null; OpenPreparation = false; IsLoading = false; }
    public static bool TryStart(MissionLaunchRequest request, out string reason)
    {
        if (request == null || request.Difficulty == null) { reason = "Выберите сложность."; return false; }
        var previous = CurrentRequest;
        CurrentRequest = request;
        if (TryLoad(request.SceneName, out reason)) return true;
        CurrentRequest = previous;
        return false;
    }
    public static bool TryRetry(out string reason) => TryLoad(SceneManager.GetActiveScene().name, out reason);
    public static bool TryReturnToPreparation(out string reason)
    {
        bool previous = OpenPreparation;
        OpenPreparation = true;
        if (TryLoad("MainMenu", out reason)) return true;
        OpenPreparation = previous;
        return false;
    }
    private static bool TryLoad(string scene, out string reason)
    {
        if (IsLoading) { reason = "Миссия уже загружается."; return false; }
        if (string.IsNullOrEmpty(scene) || !Application.CanStreamedLevelBeLoaded(scene))
        { reason = "Сцена недоступна: " + scene; return false; }
        try
        {
            IsLoading = true;
            AsyncOperation loading = SceneManager.LoadSceneAsync(scene);
            if (loading == null) throw new InvalidOperationException("Scene loading did not start.");
            loading.completed += _ => IsLoading = false;
            reason = null; return true;
        }
        catch (Exception exception)
        {
            IsLoading = false;
            reason = "Не удалось загрузить сцену: " + exception.Message;
            return false;
        }
    }
}
