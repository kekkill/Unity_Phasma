using System.Collections;
using UnityEngine;

// Archive-only arrival and extraction. GameSession still owns evidence validation and results.
public sealed class ArchiveMissionRoute : MonoBehaviour
{
    [SerializeField] private GameSession session;
    [SerializeField] private PlayerInputReader player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Door entrance;
    [SerializeField] private Collider entranceBarrier;
    [SerializeField] private BoxCollider entryThreshold;
    [SerializeField] private SafeZone workroom;
    [SerializeField] private Transform curb;
    [SerializeField] private AudioClip dispatchReply;

    private const float PickupDelay = 60f;
    private const float ApproachTime = 3f;
    private Transform car;
    private Transform driverArm;
    private Camera introCamera;
    private BoxCollider carBody;
    private bool introPlaying;
    private bool entranceSealed;
    private bool called;
    private bool carReady;
    private bool leaving;
    private float pickupAt;
    private string message;
    private float messageUntil;

    public string PhonePrompt => called ? "Машина уже вызвана" : "[E] Позвонить диспетчеру";

    private void Start()
    {
        BuildCar();
        StartCoroutine(Arrive());
    }

    private void Update()
    {
        if (introPlaying || session.HasEnded) return;
        if (!entranceSealed && !called && entryThreshold.bounds.Contains(player.transform.position))
        {
            entranceSealed = true;
            entrance.SetLocked(this, true);
            entranceBarrier.enabled = true;
        }

        if (!called || carReady || Time.time < pickupAt - ApproachTime) return;
        if (!car.gameObject.activeSelf) car.gameObject.SetActive(true);
        float remaining = Mathf.Max(0f, pickupAt - Time.time);
        car.position = curb.position - curb.forward * (remaining * 4f);
        if (remaining > 0f) return;

        carBody.enabled = true;
        carReady = true;
    }

    public void Call(PlayerLifeState life)
    {
        if (life == null || life.Session != session || !workroom.Contains(life.transform.position) || called)
            return;
        if (!session.TryConfirmExtraction(life, out string reason))
        {
            Notify(reason);
            return;
        }

        called = true;
        pickupAt = Time.time + PickupDelay;
        entranceBarrier.enabled = false;
        entrance.SetLocked(this, false);
        Notify("We'll be there in one minute.", 5f);
        if (dispatchReply != null) AudioSource.PlayClipAtPoint(dispatchReply, life.transform.position, 0.8f);
    }

    public void BoardCar(PlayerLifeState life)
    {
        if (!carReady || leaving || life == null || !life.IsAlive || life.Session != session) return;
        StartCoroutine(Leave(life));
    }

    private IEnumerator Leave(PlayerLifeState life)
    {
        leaving = true;
        introPlaying = true;
        player.SetGameplayBlocked(this, true);
        carBody.enabled = false;
        playerCamera.enabled = false;
        introCamera.enabled = true;
        yield return MoveCar(curb.position, curb.position + curb.forward * 14f, 2.5f);
        introCamera.enabled = false;
        playerCamera.enabled = true;
        introPlaying = false;
        player.SetGameplayBlocked(this, false);
        if (session.TryExtract(life, out string reason))
        {
            car.gameObject.SetActive(false);
            yield break;
        }
        car.position = curb.position;
        carBody.enabled = true;
        leaving = false;
        Notify(reason);
    }

    private IEnumerator Arrive()
    {
        introPlaying = true;
        player.SetGameplayBlocked(this, true);
        playerCamera.enabled = false;
        introCamera.enabled = true;
        yield return MoveCar(curb.position - curb.forward * 8f, curb.position, 2.1f);

        Quaternion forward = introCamera.transform.localRotation;
        Quaternion glance = Quaternion.Euler(0f, -62f, 0f);
        for (float elapsed = 0f; elapsed < 1.25f; elapsed += Time.deltaTime)
        {
            float weight = Mathf.Sin(Mathf.Clamp01(elapsed / 1.25f) * Mathf.PI);
            introCamera.transform.localRotation = Quaternion.Slerp(forward, glance, weight);
            driverArm.localRotation = Quaternion.Euler(-35f * weight, 0f, 58f * weight);
            yield return null;
        }

        var view = introCamera.transform;
        Vector3 passengerSeat = view.localPosition;
        Vector3 seatedPosition = view.position;
        Quaternion seatedRotation = car.rotation;
        for (float elapsed = 0f; elapsed < 0.55f; elapsed += Time.deltaTime)
        {
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / 0.55f);
            view.SetPositionAndRotation(
                Vector3.Lerp(seatedPosition, playerCamera.transform.position, progress),
                Quaternion.Slerp(seatedRotation, playerCamera.transform.rotation, progress));
            yield return null;
        }

        introCamera.enabled = false;
        playerCamera.enabled = true;
        view.SetLocalPositionAndRotation(passengerSeat, Quaternion.identity);
        driverArm.localRotation = Quaternion.identity;
        introPlaying = false;
        player.SetGameplayBlocked(this, false);
        yield return MoveCar(curb.position, curb.position + curb.forward * 14f, 2.5f);
        car.gameObject.SetActive(false);
    }

    private IEnumerator MoveCar(Vector3 from, Vector3 to, float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            car.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        car.position = to;
    }

    private void BuildCar()
    {
        car = new GameObject("Company car").transform;
        car.SetParent(transform);
        car.SetPositionAndRotation(curb.position, curb.rotation);
        Color body = new Color(0.13f, 0.21f, 0.26f);
        Color seat = new Color(0.15f, 0.16f, 0.17f);
        AddBox("Chassis", car, new Vector3(0, 0.48f, 0), new Vector3(1.85f, 0.55f, 4.1f), body);
        AddBox("Bonnet", car, new Vector3(0, 0.81f, 1.15f), new Vector3(1.85f, 0.34f, 1.7f), body);
        AddBox("Roof", car, new Vector3(0, 1.92f, -0.37f), new Vector3(1.85f, 0.12f, 2.0f), body);
        AddBox("Left cabin post", car, new Vector3(-0.85f, 1.36f, 0.62f), new Vector3(0.12f, 1.04f, 0.12f), body);
        AddBox("Right cabin post", car, new Vector3(0.85f, 1.36f, 0.62f), new Vector3(0.12f, 1.04f, 0.12f), body);
        AddBox("Seat backs", car, new Vector3(0, 1.17f, -0.82f), new Vector3(1.53f, 0.78f, 0.25f), seat);
        AddBox("Driver coat", car, new Vector3(-0.48f, 1.15f, -0.38f), new Vector3(0.42f, 0.59f, 0.32f), seat);
        AddBox("Driver mask", car, new Vector3(-0.48f, 1.59f, -0.22f), new Vector3(0.24f, 0.28f, 0.24f), new Color(0.77f, 0.76f, 0.69f));
        driverArm = new GameObject("Driver gesture").transform;
        driverArm.SetParent(car, false);
        driverArm.localPosition = new Vector3(-0.25f, 1.28f, -0.25f);
        AddBox("Sleeve", driverArm, new Vector3(0, -0.13f, 0.2f), new Vector3(0.15f, 0.32f, 0.15f), seat);
        AddBox("Glove", driverArm, new Vector3(0, -0.23f, 0.24f), new Vector3(0.14f, 0.13f, 0.13f), body);
        AddBox("Thumb", driverArm, new Vector3(0.04f, -0.13f, 0.24f), new Vector3(0.05f, 0.17f, 0.055f), body);
        for (int side = -1; side <= 1; side += 2)
        for (int axle = -1; axle <= 1; axle += 2)
        {
            var wheel = RuntimeVisuals.Primitive(PrimitiveType.Cylinder, car,
                new Vector3(side * 0.93f, 0.35f, axle * 1.3f), new Vector3(0.65f, 0.14f, 0.65f), seat);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        var view = new GameObject("Passenger view").transform;
        view.SetParent(car, false);
        view.localPosition = new Vector3(0.46f, 1.57f, -0.46f);
        introCamera = view.gameObject.AddComponent<Camera>();
        introCamera.enabled = false;
        introCamera.nearClipPlane = 0.05f;
        introCamera.clearFlags = playerCamera.clearFlags;
        introCamera.backgroundColor = playerCamera.backgroundColor;
        introCamera.farClipPlane = 90f;
        carBody = car.gameObject.AddComponent<BoxCollider>();
        carBody.center = new Vector3(0f, 0.92f, 0f);
        carBody.size = new Vector3(1.9f, 1.84f, 4.1f);
        carBody.enabled = false;
        car.gameObject.AddComponent<ArchiveRouteInteraction>().BindCar(this);
    }

    private static void AddBox(string name, Transform root, Vector3 at, Vector3 size, Color color)
    {
        RuntimeVisuals.Primitive(PrimitiveType.Cube, root, at, size, color).name = name;
    }

    private void Notify(string text, float duration = 4f)
    {
        message = text;
        messageUntil = Time.time + duration;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (player != null) player.SetGameplayBlocked(this, false);
        if (introCamera != null) introCamera.enabled = false;
        if (introPlaying && playerCamera != null) playerCamera.enabled = true;
    }

    private void OnGUI()
    {
        if (introPlaying || session.HasEnded) return;
        string objective = carReady ? "Машина у обочины. Подойдите и нажмите E." :
            called ? $"Машина прибудет через {Mathf.CeilToInt(Mathf.Max(0f, pickupAt - Time.time))} с." :
            entranceSealed ? "Соберите улики, выберите тип (J) и позвоните из рабочей комнаты." :
            "Войдите в дом через парадную дверь.";
        GUI.Box(new Rect(15f, 15f, 496f, 34f), objective);
        if (Time.time < messageUntil)
            GUI.Box(new Rect((Screen.width - 540f) / 2f, Screen.height - 105f, 540f, 35f), message);
    }
}
