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

    [Header("Camera")]
    [SerializeField] bool  forceOrthographic = true;
    [SerializeField] bool  forceTopDown      = true;
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
        if (forceTopDown) _cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

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

        float pad = 1f + paddingFraction;

        // ── OrthoSize: fit height AND width, take the larger ──────────────────
        float sizeForH  = (worldH * pad) / (2f * availFrac);
        float sizeForW  = (worldW * pad) / (2f * aspect);
        float orthoSize = Mathf.Clamp(Mathf.Max(sizeForH, sizeForW), minOrthoSize, maxOrthoSize);

        _cam.orthographicSize = orthoSize;

        // ── Vertical shift: move grid into the available band ─────────────────
        float shift = (hudBottom - hudTop) * orthoSize;
        _cam.transform.position = new Vector3(centreX, cameraHeight, centreZ - shift);

        _fitted     = true;
        _lastScreen = new Vector2Int(Screen.width, Screen.height);

        Debug.Log($"[CameraFitter] {(portrait ? "PORTRAIT" : "LANDSCAPE")} " +
                  $"screen={Screen.width}x{Screen.height} aspect={aspect:F3} " +
                  $"grid={gridW}x{gridH} cell={cell} worldW={worldW:F2} worldH={worldH:F2} " +
                  $"centre=({centreX:F2},{centreZ:F2}) availFrac={availFrac:F2} " +
                  $"sizeH={sizeForH:F2} sizeW={sizeForW:F2} ortho={orthoSize:F2} " +
                  $"shift={shift:F2} camPos={_cam.transform.position}");
    }

    void TryFit()
    {
        if (GridManager.Instance == null)    return;
        if (GridManager.Instance.width <= 0) return;
        FitNow();
    }
}