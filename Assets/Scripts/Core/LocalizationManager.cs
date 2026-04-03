using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton localization manager.
/// Loads JSON translation files from Resources/Localization/.
/// Broadcasts OnLanguageChanged when the language switches.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    /// <summary>Fires whenever the active language changes. Listeners should refresh their text.</summary>
    public static event Action OnLanguageChanged;

    static readonly string[] SUPPORTED = { "en", "fr", "es" };
    const string PREFS_KEY = "Setting_language";
    const string RES_PATH  = "Localization/";

    Dictionary<string, string> _strings = new Dictionary<string, string>();
    string _currentLang;

    public string CurrentLanguage => _currentLang;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("LocalizationManager");
        go.AddComponent<LocalizationManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        int idx = PlayerPrefs.GetInt(PREFS_KEY, 0);
        _currentLang = idx >= 0 && idx < SUPPORTED.Length ? SUPPORTED[idx] : "en";
        LoadLanguage(_currentLang);
    }

    /// <summary>
    /// Switch to a language by code ("en", "fr", "es").
    /// </summary>
    public void SetLanguage(string langCode)
    {
        langCode = langCode.ToLower();
        if (langCode == _currentLang) return;
        _currentLang = langCode;
        LoadLanguage(langCode);
        OnLanguageChanged?.Invoke();
    }

    /// <summary>
    /// Switch to a language by chip index (0=EN, 1=FR, 2=ES).
    /// </summary>
    public void SetLanguageByIndex(int index)
    {
        if (index >= 0 && index < SUPPORTED.Length)
            SetLanguage(SUPPORTED[index]);
    }

    /// <summary>
    /// Look up a localized string by key.  Returns the key itself if not found.
    /// </summary>
    public string Get(string key)
    {
        if (_strings.TryGetValue(key, out var val)) return val;
        return key;
    }

    /// <summary>Shorthand static accessor: LocalizationManager.T("key")</summary>
    public static string T(string key)
    {
        if (Instance != null) return Instance.Get(key);
        return key;
    }

    void LoadLanguage(string langCode)
    {
        _strings.Clear();
        var asset = Resources.Load<TextAsset>(RES_PATH + langCode);
        if (asset == null)
        {
            Debug.LogWarning($"[Localization] Missing file: Resources/{RES_PATH}{langCode}.json — falling back to key names.");
            return;
        }

        var wrapper = JsonUtility.FromJson<TranslationFile>(asset.text);
        if (wrapper?.entries == null) return;

        foreach (var e in wrapper.entries)
        {
            if (!string.IsNullOrEmpty(e.key))
                _strings[e.key] = e.value;
        }
        Debug.Log($"[Localization] Loaded {_strings.Count} strings for '{langCode}'.");
    }

    [Serializable]
    class TranslationFile
    {
        public TranslationEntry[] entries;
    }

    [Serializable]
    class TranslationEntry
    {
        public string key;
        public string value;
    }
}
