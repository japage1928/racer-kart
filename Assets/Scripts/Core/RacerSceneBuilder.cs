using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RacerSceneBuilder : MonoBehaviour
{
    private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();

    public RacerSceneBuildResult BuildScene()
    {
        EnsureEventSystem();
        EnsureLight();

        var worldRoot = new GameObject("Racer_Main");
        var trackRoot = new GameObject("Track");
        trackRoot.transform.SetParent(worldRoot.transform);

        var centerRadii = new Vector2(30f, 20f);
        const float roadHalfWidth = 5f;
        var innerRadii = centerRadii - new Vector2(roadHalfWidth, roadHalfWidth);
        var outerRadii = centerRadii + new Vector2(roadHalfWidth, roadHalfWidth);

        BuildTrackMeshes(trackRoot.transform, centerRadii, innerRadii, outerRadii);
        var checkpointPositions = BuildCheckpoints(trackRoot.transform, centerRadii);
        BuildFinishLine(trackRoot.transform, centerRadii, roadHalfWidth);
        BuildBoostPads(trackRoot.transform, centerRadii);
        BuildCoins(trackRoot.transform, centerRadii);

        var kart = BuildKart(worldRoot.transform, centerRadii, innerRadii, outerRadii);
        var lapTracker = kart.GetComponent<RacerLapTracker>();
        lapTracker.SetCheckpointCount(checkpointPositions.Count);

        var checkpointObjects = trackRoot.GetComponentsInChildren<RacerCheckpoint>();
        for (var i = 0; i < checkpointObjects.Length; i++)
        {
            checkpointObjects[i].SetIndex(i);
        }

        var mainCamera = BuildMainCamera(worldRoot.transform, kart.transform);
        var minimapCamera = BuildMinimapCamera(worldRoot.transform, kart.transform);
        var hud = BuildHud(worldRoot.transform, minimapCamera.targetTexture);

        mainCamera.depth = 0;
        minimapCamera.depth = 1;

        return new RacerSceneBuildResult
        {
            playerKart = kart,
            lapTracker = lapTracker,
            hud = hud
        };
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<StandaloneInputModule>();
    }

    private void EnsureLight()
    {
        if (FindFirstObjectByType<Light>() != null)
        {
            return;
        }

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(1f, 0.96f, 0.88f);
        lightGo.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
    }

    private void BuildTrackMeshes(Transform parent, Vector2 centerRadii, Vector2 innerRadii, Vector2 outerRadii)
    {
        var grass = GameObject.CreatePrimitive(PrimitiveType.Plane);
        grass.name = "Grass";
        grass.transform.SetParent(parent);
        grass.transform.position = Vector3.zero;
        grass.transform.localScale = new Vector3(14f, 1f, 14f);
        grass.GetComponent<Renderer>().sharedMaterial = GetMaterial("grass", new Color(0.3f, 0.65f, 0.28f));

        const int segments = 40;
        for (var i = 0; i < segments; i++)
        {
            var t0 = (Mathf.PI * 2f * i) / segments;
            var t1 = (Mathf.PI * 2f * (i + 1)) / segments;

            var p0 = new Vector3(Mathf.Cos(t0) * centerRadii.x, 0f, Mathf.Sin(t0) * centerRadii.y);
            var p1 = new Vector3(Mathf.Cos(t1) * centerRadii.x, 0f, Mathf.Sin(t1) * centerRadii.y);
            var mid = (p0 + p1) * 0.5f;
            var dir = (p1 - p0).normalized;
            var len = Vector3.Distance(p0, p1) + 1.2f;

            var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = $"Road_{i:00}";
            road.transform.SetParent(parent);
            road.transform.position = new Vector3(mid.x, 0.06f, mid.z);
            road.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            road.transform.localScale = new Vector3(10f, 0.12f, len);
            road.GetComponent<Renderer>().sharedMaterial = GetMaterial("road", new Color(0.6f, 0.42f, 0.2f));

            BuildWallSegment(parent, i, t0, t1, innerRadii, "InnerWall");
            BuildWallSegment(parent, i, t0, t1, outerRadii, "OuterWall");
        }
    }

    private void BuildWallSegment(Transform parent, int index, float t0, float t1, Vector2 radii, string namePrefix)
    {
        var p0 = new Vector3(Mathf.Cos(t0) * radii.x, 0f, Mathf.Sin(t0) * radii.y);
        var p1 = new Vector3(Mathf.Cos(t1) * radii.x, 0f, Mathf.Sin(t1) * radii.y);
        var mid = (p0 + p1) * 0.5f;
        var dir = (p1 - p0).normalized;
        var len = Vector3.Distance(p0, p1) + 0.2f;

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = $"{namePrefix}_{index:00}";
        wall.transform.SetParent(parent);
        wall.transform.position = new Vector3(mid.x, 0.65f, mid.z);
        wall.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        wall.transform.localScale = new Vector3(1f, 1.3f, len);
        wall.GetComponent<Renderer>().sharedMaterial = GetMaterial("wall", new Color(0.48f, 0.36f, 0.23f));
        wall.AddComponent<WallSurface>();
    }

    private List<Vector3> BuildCheckpoints(Transform parent, Vector2 centerRadii)
    {
        var result = new List<Vector3>();
        const int count = 8;
        for (var i = 0; i < count; i++)
        {
            var t = (Mathf.PI * 2f * i) / count;
            var pos = new Vector3(Mathf.Cos(t) * centerRadii.x, 0.6f, Mathf.Sin(t) * centerRadii.y);
            result.Add(pos);

            var cp = new GameObject($"Checkpoint_{i:00}");
            cp.transform.SetParent(parent);
            cp.transform.position = pos;
            cp.transform.rotation = Quaternion.Euler(0f, Mathf.Rad2Deg * t + 90f, 0f);

            var box = cp.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(12f, 3f, 1.8f);

            cp.AddComponent<RacerCheckpoint>();
        }

        return result;
    }

    private void BuildFinishLine(Transform parent, Vector2 centerRadii, float roadHalfWidth)
    {
        var finishRoot = new GameObject("FinishLine");
        finishRoot.transform.SetParent(parent);
        finishRoot.transform.position = new Vector3(centerRadii.x, 0.5f, 0f);
        finishRoot.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        var trigger = finishRoot.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3((roadHalfWidth * 2f) + 2f, 3f, 1.4f);

        finishRoot.AddComponent<RacerFinishLine>();

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "FinishVisual";
        visual.transform.SetParent(finishRoot.transform);
        visual.transform.localPosition = new Vector3(0f, -0.44f, 0f);
        visual.transform.localScale = new Vector3((roadHalfWidth * 2f) + 1f, 0.06f, 1.2f);
        visual.GetComponent<Renderer>().sharedMaterial = GetMaterial("finish", new Color(0.93f, 0.93f, 0.93f));
        Destroy(visual.GetComponent<Collider>());
    }

    private void BuildBoostPads(Transform parent, Vector2 centerRadii)
    {
        var padAngles = new[] { 0.65f, 2.5f, 4.3f };
        for (var i = 0; i < padAngles.Length; i++)
        {
            var t = padAngles[i];
            var pos = new Vector3(Mathf.Cos(t) * centerRadii.x, 0.1f, Mathf.Sin(t) * centerRadii.y);
            var tangent = new Vector3(-Mathf.Sin(t), 0f, Mathf.Cos(t)).normalized;

            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = $"BoostPad_{i:00}";
            pad.transform.SetParent(parent);
            pad.transform.position = pos;
            pad.transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
            pad.transform.localScale = new Vector3(4.2f, 0.12f, 2.2f);
            pad.GetComponent<Renderer>().sharedMaterial = GetMaterial("boost", new Color(0.1f, 0.55f, 0.95f));

            var col = pad.GetComponent<BoxCollider>();
            col.isTrigger = true;
            pad.AddComponent<BoostPad>();
        }
    }

    private void BuildCoins(Transform parent, Vector2 centerRadii)
    {
        var root = new GameObject("Coins");
        root.transform.SetParent(parent);

        const int coinCount = 20;
        for (var i = 0; i < coinCount; i++)
        {
            var t = (Mathf.PI * 2f * i) / coinCount;
            var radialOffset = Mathf.Sin(t * 4f) * 1.4f;
            var x = Mathf.Cos(t) * (centerRadii.x + radialOffset);
            var z = Mathf.Sin(t) * (centerRadii.y + radialOffset);

            var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = $"Coin_{i:00}";
            coin.transform.SetParent(root.transform);
            coin.transform.position = new Vector3(x, 0.9f, z);
            coin.transform.localScale = new Vector3(0.4f, 0.08f, 0.4f);
            coin.GetComponent<Renderer>().sharedMaterial = GetMaterial("coin", new Color(0.95f, 0.75f, 0.12f));

            var col = coin.GetComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.height = 1.4f;
            col.radius = 0.55f;
            coin.AddComponent<CoinPickup>();
        }
    }

    private KartController BuildKart(Transform parent, Vector2 centerRadii, Vector2 innerRadii, Vector2 outerRadii)
    {
        var kart = new GameObject("PlayerKart");
        kart.transform.SetParent(parent);
        kart.transform.position = new Vector3(centerRadii.x, 0.65f, -2f);
        kart.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        var bodyCollider = kart.AddComponent<BoxCollider>();
        bodyCollider.size = new Vector3(1.35f, 0.68f, 2f);
        bodyCollider.center = new Vector3(0f, 0.45f, 0f);

        var rb = kart.AddComponent<Rigidbody>();
        rb.mass = 1.6f;
        rb.linearDamping = 0.15f;
        rb.angularDamping = 2.8f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        kart.AddComponent<HybridKartInput>();
        var controller = kart.AddComponent<KartController>();
        controller.ConfigureOffRoadEllipse(Vector2.zero, innerRadii, outerRadii);
        kart.AddComponent<RacerLapTracker>();

        BuildKartVisuals(kart.transform, controller);
        return controller;
    }

    private void BuildKartVisuals(Transform kartRoot, KartController controller)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(kartRoot);
        body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        body.transform.localScale = new Vector3(1.4f, 0.45f, 2.1f);
        body.GetComponent<Renderer>().sharedMaterial = GetMaterial("kartBody", new Color(0.82f, 0.2f, 0.1f));
        Destroy(body.GetComponent<Collider>());

        var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        nose.transform.SetParent(kartRoot);
        nose.transform.localPosition = new Vector3(0f, 0.62f, 0.75f);
        nose.transform.localScale = new Vector3(0.9f, 0.25f, 0.7f);
        nose.GetComponent<Renderer>().sharedMaterial = GetMaterial("kartTrim", new Color(0.1f, 0.22f, 0.58f));
        Destroy(nose.GetComponent<Collider>());

        var wheelOffsets = new[]
        {
            new Vector3(-0.68f, 0.28f, 0.75f),
            new Vector3(0.68f, 0.28f, 0.75f),
            new Vector3(-0.68f, 0.28f, -0.75f),
            new Vector3(0.68f, 0.28f, -0.75f)
        };

        foreach (var offset in wheelOffsets)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.transform.SetParent(kartRoot);
            wheel.transform.localPosition = offset;
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(0.34f, 0.12f, 0.34f);
            wheel.GetComponent<Renderer>().sharedMaterial = GetMaterial("wheel", new Color(0.14f, 0.14f, 0.14f));
            Destroy(wheel.GetComponent<Collider>());
        }

        var trailHolder = new GameObject("BoostTrail");
        trailHolder.transform.SetParent(kartRoot);
        trailHolder.transform.localPosition = new Vector3(0f, 0.28f, -1.15f);
        var trail = trailHolder.AddComponent<TrailRenderer>();
        trail.time = 0.2f;
        trail.startWidth = 0.35f;
        trail.endWidth = 0.02f;
        trail.material = GetMaterial("trail", new Color(0.2f, 0.6f, 1f));
        trail.emitting = false;
        controller.SetBoostTrail(trail);

        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "MinimapMarker";
        marker.transform.SetParent(kartRoot);
        marker.transform.localPosition = new Vector3(0f, 3.5f, 0f);
        marker.transform.localScale = Vector3.one * 0.9f;
        marker.GetComponent<Renderer>().sharedMaterial = GetMaterial("marker", new Color(1f, 0.15f, 0.05f));
        Destroy(marker.GetComponent<Collider>());
    }

    private Camera BuildMainCamera(Transform parent, Transform target)
    {
        var cameraGo = new GameObject("Main Camera");
        cameraGo.transform.SetParent(parent);
        cameraGo.tag = "MainCamera";

        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 62f;

        var listener = cameraGo.AddComponent<AudioListener>();
        listener.enabled = true;

        var follow = cameraGo.AddComponent<KartCameraFollow>();
        follow.SetTarget(target);
        cameraGo.transform.position = target.position + new Vector3(0f, 6f, -8f);
        return camera;
    }

    private Camera BuildMinimapCamera(Transform parent, Transform target)
    {
        var minimapGo = new GameObject("MinimapCamera");
        minimapGo.transform.SetParent(parent);
        minimapGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var cam = minimapGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 32f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.22f, 0.16f);
        cam.cullingMask = ~0;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 120f;

        var rt = new RenderTexture(256, 256, 16);
        rt.name = "MinimapRT";
        cam.targetTexture = rt;

        var follow = minimapGo.AddComponent<MinimapCameraFollow>();
        follow.SetTarget(target);
        return cam;
    }

    private RacerHUD BuildHud(Transform parent, RenderTexture minimapTexture)
    {
        var canvasGo = new GameObject("RacerCanvas");
        canvasGo.transform.SetParent(parent);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var timer = CreateText("Timer", canvasGo.transform, font, 44, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(360f, 60f), "00:00:000");
        var coins = CreateText("Coins", canvasGo.transform, font, 38, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(260f, 55f), "Coins 0");
        var lap = CreateText("Lap", canvasGo.transform, font, 36, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-290f, -20f), new Vector2(250f, 55f), "Lap 1/3");
        var countdown = CreateText("Countdown", canvasGo.transform, font, 140, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(700f, 180f), "3");
        countdown.gameObject.SetActive(false);

        var minimapRaw = CreateRawImage("Minimap", canvasGo.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -74f), new Vector2(250f, 250f));
        minimapRaw.texture = minimapTexture;

        var finishPanel = new GameObject("FinishPanel");
        finishPanel.transform.SetParent(canvasGo.transform, false);
        var finishRect = finishPanel.AddComponent<RectTransform>();
        finishRect.anchorMin = new Vector2(0.5f, 0.5f);
        finishRect.anchorMax = new Vector2(0.5f, 0.5f);
        finishRect.sizeDelta = new Vector2(480f, 330f);
        finishRect.anchoredPosition = Vector2.zero;
        var finishImage = finishPanel.AddComponent<Image>();
        finishImage.color = new Color(0f, 0f, 0f, 0.76f);
        finishPanel.SetActive(false);

        var finishTitle = CreateText("FinishTitle", finishPanel.transform, font, 48, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(420f, 60f), "Finished!");
        finishTitle.color = new Color(0.98f, 0.98f, 0.98f);

        var finishSummary = CreateText("FinishSummary", finishPanel.transform, font, 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0f, -12f), new Vector2(420f, 170f), "");

        var buttonGo = new GameObject("ReturnButton");
        buttonGo.transform.SetParent(finishPanel.transform, false);
        var buttonRect = buttonGo.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(220f, 64f);
        buttonRect.anchoredPosition = new Vector2(0f, 42f);

        var buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.12f, 0.42f, 0.88f);
        var button = buttonGo.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.transition = Selectable.Transition.ColorTint;

        CreateText("ReturnText", buttonGo.transform, font, 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 64f), "Return");
        if (Application.platform == RuntimePlatform.WebGLPlayer || Application.isMobilePlatform)
        {
            BuildBrowserControls(canvasGo.transform, font);
        }

        var hud = canvasGo.AddComponent<RacerHUD>();
        hud.Initialize(timer, coins, lap, countdown, finishPanel, finishSummary, button, minimapRaw);
        hud.SetMinimapTexture(minimapTexture);
        return hud;
    }

    private Text CreateText(
        string name,
        Transform parent,
        Font font,
        int fontSize,
        TextAnchor alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        string value)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = value;
        return text;
    }

    private RawImage CreateRawImage(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        var img = go.AddComponent<RawImage>();
        img.color = Color.white;
        return img;
    }

    private Material GetMaterial(string key, Color color)
    {
        if (_materials.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        _materials[key] = mat;
        return mat;
    }

    private void BuildBrowserControls(Transform canvasRoot, Font font)
    {
        var controlsRoot = new GameObject("BrowserControls");
        controlsRoot.transform.SetParent(canvasRoot, false);
        var rootRect = controlsRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var left = CreateControlButton("Left", controlsRoot.transform, font, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(95f, 95f), new Vector2(92f, 92f), "<");
        left.Configure(BrowserHoldButton.ControlType.Steer, -1f);

        var right = CreateControlButton("Right", controlsRoot.transform, font, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(198f, 95f), new Vector2(92f, 92f), ">");
        right.Configure(BrowserHoldButton.ControlType.Steer, 1f);

        var accel = CreateControlButton("Accel", controlsRoot.transform, font, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-108f, 150f), new Vector2(104f, 104f), "W");
        accel.Configure(BrowserHoldButton.ControlType.Throttle, 1f);

        var brake = CreateControlButton("Brake", controlsRoot.transform, font, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-230f, 95f), new Vector2(92f, 92f), "S");
        brake.Configure(BrowserHoldButton.ControlType.Throttle, -1f);

        var drift = CreateControlButton("Drift", controlsRoot.transform, font, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-108f, 40f), new Vector2(104f, 74f), "Drift");
        drift.Configure(BrowserHoldButton.ControlType.Drift);

        var handbrake = CreateControlButton("Handbrake", controlsRoot.transform, font, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-230f, -5f), new Vector2(92f, 74f), "Brake");
        handbrake.Configure(BrowserHoldButton.ControlType.Handbrake);
    }

    private BrowserHoldButton CreateControlButton(
        string name,
        Transform parent,
        Font font,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = go.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.35f);
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = image;

        var hold = go.AddComponent<BrowserHoldButton>();
        CreateText($"{name}_Label", go.transform, font, 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, label);
        return hold;
    }
}
