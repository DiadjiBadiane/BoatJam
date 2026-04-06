using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class GraphicsQualityManager
{
    // Quality indices matching the settings chips: 0=Low, 1=Mid, 2=High
    const string PREFS_KEY = "Setting_graphics";
    const int DEFAULT_LEVEL = 1; // Mid

    public static void Apply()
    {
        int level = PlayerPrefs.GetInt(PREFS_KEY, DEFAULT_LEVEL);
        Apply(level);
    }

    public static void Apply(int level)
    {
        var pipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null)
        {
            Debug.LogWarning("[GraphicsQuality] No URP pipeline asset found.");
            return;
        }

        switch (level)
        {
            case 0: // Low — save battery, minimum visuals
                pipeline.renderScale = 0.6f;
                pipeline.shadowDistance = 15f;
                pipeline.mainLightShadowmapResolution = 512;
                Application.targetFrameRate = 30;
                break;

            case 1: // Mid — balanced (default)
                pipeline.renderScale = 0.8f;
                pipeline.shadowDistance = 30f;
                pipeline.mainLightShadowmapResolution = 1024;
                Application.targetFrameRate = 60;
                break;

            case 2: // High — best visuals
                pipeline.renderScale = 1.0f;
                pipeline.shadowDistance = 50f;
                pipeline.mainLightShadowmapResolution = 2048;
                Application.targetFrameRate = 60;
                break;

            default:
                Debug.LogWarning($"[GraphicsQuality] Unknown level {level}, using Mid.");
                Apply(1);
                return;
        }

        Debug.Log($"[GraphicsQuality] Applied level {level} (renderScale={pipeline.renderScale}, shadows={pipeline.shadowDistance})");
    }

}
