// Assets/Scripts/Core/AudioManager.cs
using UnityEngine;

/// <summary>
/// Singleton audio manager.
/// Persists across scenes (DontDestroyOnLoad).
/// Reads volume from PlayerPrefs (set by MainMenuManager settings panel).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;
    public AudioClip moveClip;
    public AudioClip winClip;

    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY   = "SFXVolume";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ApplyVolumes();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void PlayMenuMusic()  => SwapMusic(menuMusic);
    public void PlayGameMusic()  => SwapMusic(gameMusic);

    public void PlayMove() => sfxSource?.PlayOneShot(moveClip);
    public void PlayWin()  => sfxSource?.PlayOneShot(winClip);

    public void ApplyVolumes()
    {
        if (musicSource != null) musicSource.volume = PlayerPrefs.GetFloat(MUSIC_KEY, 0.8f);
        if (sfxSource   != null) sfxSource.volume   = PlayerPrefs.GetFloat(SFX_KEY,   1f);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    void SwapMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }
}
