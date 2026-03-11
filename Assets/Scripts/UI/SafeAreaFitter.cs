using UnityEngine;

/// <summary>
/// Keeps a RectTransform inside the device safe area (notches, rounded corners, home indicators).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform _rect;
    Rect _lastSafeArea;
    Vector2Int _lastScreen;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        ApplySafeArea(force: true);
    }

    void Update()
    {
        ApplySafeArea(force: false);
    }

    void ApplySafeArea(bool force)
    {
        var safe = Screen.safeArea;
        var size = new Vector2Int(Screen.width, Screen.height);

        if (!force && safe == _lastSafeArea && size == _lastScreen)
            return;

        _lastSafeArea = safe;
        _lastScreen = size;

        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        var min = safe.position;
        var max = safe.position + safe.size;

        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        _rect.anchorMin = min;
        _rect.anchorMax = max;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }
}
