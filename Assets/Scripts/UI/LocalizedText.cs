using TMPro;
using UnityEngine;

/// <summary>
/// Attach to any TextMeshProUGUI to auto-update its text when the language changes.
/// Set <see cref="locKey"/> in code or Inspector. The component subscribes to
/// <see cref="LocalizationManager.OnLanguageChanged"/> and refreshes automatically.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Localization key, e.g. 'ui.settings.music'")]
    public string locKey;

    TextMeshProUGUI _tmp;

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += Refresh;
        if (!string.IsNullOrEmpty(locKey)) Refresh();
    }

    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= Refresh;
    }

    /// <summary>Set the key and immediately refresh the displayed text.</summary>
    public void SetKey(string key)
    {
        locKey = key;
        Refresh();
    }

    public void Refresh()
    {
        if (_tmp == null) _tmp = GetComponent<TextMeshProUGUI>();
        if (string.IsNullOrEmpty(locKey)) return;
        _tmp.text = LocalizationManager.T(locKey);
    }
}
