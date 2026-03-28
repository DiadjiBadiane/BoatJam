// Assets/Scripts/Rendering/ResponsiveCameraFitter.cs
using UnityEngine;

/// <summary>
/// Keeps an orthographic camera perfectly framed on the puzzle grid on any device/aspect ratio.
/// Attach to the Main Camera in GameScene.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class ResponsiveCameraFitter : MonoBehaviour
{
    [Header("HUD — Portrait (fraction of screen HEIGHT)")]
    [SerializeField, Range(0f, 0.4f)] float hudTopPortrait    = 0.12f;
    [SerializeField, Range(0f, 0.4f)] float hudBottomPortrait = 0.14f;

    [Header("HUD — Landscape (fraction of screen HEIGHT)")]
    [SerializeField, Range(0f, 0.4f)] float hudTopLandscape    = 0.16f;
    [SerializeField, Range(0f, 0.4f)] float hudBottomLandscape = 0.20f;

    [Header("Padding — extra breathing room around the grid")]
    [SerializeField, Range(0f, 0.5f)] float paddingFraction = 0.10f;

    [Header("Adaptive framing")]
    [Tooltip("Extra fill applied on larger boards (e.g. 8x8) so the grid appears less zoomed out.")]
    [SerializeField, Range(0f, 0.2f)] float largeGridFillBoost = 0.08f;

    [Header("Camera")]
    [SerializeField] bool  forceOrthographic = true;
    [SerializeField] bool  forceTopDown      = false;
    [SerializeField, Range(35f, 89f)] float angledPitch = 65f;
    [SerializeField] float angledYaw = 0f;
    [SerializeField] float cameraHeight      = 15f;

    [Header("Safety limits")]
    [SerializeField] float minOrthoSize = 2f;
    [SerializeField] float maxOrthoSize = 40f;

    Camera     _cam;
    Vector2Int _lastScreen;
    bool       _fitted;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()  { _cam = GetComponent<Camera>(); }

    void OnEnable() { _fitted = false; }

    void LateUpdate()
    {
        if (!_fitted)
        {
            TryFit();
            return;
        }

        // Re-fit when screen size / orientation changes
        var screen = new Vector2Int(Screen.width, Screen.height);
        if (screen != _lastScreen) { _lastScreen = screen; FitNow(); }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void FitNow()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        if (_cam == null) return;

        if (GridManager.Instance == null) return;
        int gridW = GridManager.Instance.width;
        int gridH = GridManager.Instance.height;
        if (gridW <= 0 || gridH <= 0)
        {
            Debug.LogWarning($"[CameraFitter] Skipping — grid not ready ({gridW}x{gridH})");
            return;
        }

        // ── Camera mode ───────────────────────────────────────────────────────
        if (forceOrthographic) _cam.orthographic = true;
        if (!_cam.orthographic) return;
        _cam.transform.rotation = forceTopDown
            ? Quaternion.Euler(90f, 0f, 0f)
            : Quaternion.Euler(angledPitch, angledYaw, 0f);

        float cell   = Mathf.Max(0.01f, GridManager.Instance.cellSize);
        float worldW = gridW * cell;
        float worldH = gridH * cell;

        // Grid centre in world space
        float centreX = worldW * 0.5f - cell * 0.5f;
        float centreZ = worldH * 0.5f - cell * 0.5f;

        // ── Use Screen pixels for aspect — more reliable than _cam.aspect ─────
        float screenW = Screen.width  > 0 ? Screen.width  : 1;
        float screenH = Screen.height > 0 ? Screen.height : 1;
        float aspect  = screenW / screenH;

        // ── HUD fractions by orientation ──────────────────────────────────────
        bool  portrait  = aspect < 1f;
        float hudTop    = portrait ? hudTopPortrait    : hudTopLandscape;
        float hudBottom = portrait ? hudBottomPortrait : hudBottomLandscape;
        float availFrac = Mathf.Max(0.2f, 1f - hudTop - hudBottom);

        // Apply a small automatic zoom-in on larger boards to avoid excessive empty margins.
        float largeGridT = Mathf.InverseLerp(6f, 8f, Mathf.Max(gridW, gridH));
        float effectivePadding = Mathf.Max(0f, paddingFraction - (largeGridFillBoost * largeGridT));
        float pad = 1f + effectivePadding;

        // ── OrthoSize: fit height AND width, take the larger ──────────────────
        float sizeForH  = (worldH * pad) / (2f * availFrac);
        float sizeForW  = (worldW * pad) / (2f * aspect);
        float orthoSize = Mathf.Clamp(Mathf.Max(sizeForH, sizeForW), minOrthoSize, maxOrthoSize);

        _cam.orthographicSize = orthoSize;

        // ── Vertical shift: move grid into the available band ─────────────────
        float shift = (hudBottom - hudTop) * orthoSize;
        Vector3 target = new Vector3(centreX, 0f, centreZ - shift);

        if (forceTopDown)
        {
            _cam.transform.position = new Vector3(target.x, cameraHeight, target.z);
        }
        else
        {
            Vector3 forward = _cam.transform.forward;
            float absY = Mathf.Max(0.001f, Mathf.Abs(forward.y));
            float distance = cameraHeight / absY;
            _cam.transform.position = target - forward * distance;
        }

        _fitted     = true;
        _lastScreen = new Vector2Int(Screen.width, Screen.height);

        Debug.Log($"[CameraFitter] {(portrait ? "PORTRAIT" : "LANDSCAPE")} " +
                  $"screen={Screen.width}x{Screen.height} aspect={aspect:F3} " +
                  $"grid={gridW}x{gridH} cell={cell} worldW={worldW:F2} worldH={worldH:F2} " +
                  $"centre=({centreX:F2},{centreZ:F2}) availFrac={availFrac:F2} " +
                  $"pad={effectivePadding:F3} sizeH={sizeForH:F2} sizeW={sizeForW:F2} ortho={orthoSize:F2} " +
                  $"shift={shift:F2} camPos={_cam.transform.position}");
    }

    void TryFit()
    {
        if (GridManager.Instance == null)    return;
        if (GridManager.Instance.width <= 0) return;
        FitNow();
    }
}