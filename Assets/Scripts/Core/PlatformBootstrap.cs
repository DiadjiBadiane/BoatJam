using UnityEngine;

public static class PlatformBootstrap
{
    static bool _applied;

    public static void ApplyDefaults()
    {
        if (_applied) return;
        _applied = true;

        // Prefer a deterministic framerate on mobile and web builds.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

#if UNITY_ANDROID || UNITY_IOS
        Screen.orientation = ScreenOrientation.AutoRotation;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = true;
#endif
    }
}
