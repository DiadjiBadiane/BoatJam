using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class MenuWaterBackground3D : MonoBehaviour
{
    const string RigName = "MenuBackdrop3D";
    const string QuadName = "WaterQuad";
    const int MeshSubdivisionsX = 96;
    const int MeshSubdivisionsY = 54;

    static readonly int PerspectiveProp       = Shader.PropertyToID("_PERSPECTIVE");
    static readonly int OrthographicSizeProp  = Shader.PropertyToID("_OrthographicCamSize");
    static readonly int ReflectionProp        = Shader.PropertyToID("_REFLECTIONS");
    static readonly int WorldSpaceUVProp      = Shader.PropertyToID("_WORLD_SPACE_UV");
    static readonly int TileSizeProp          = Shader.PropertyToID("_TileSize");
    static readonly int WaveSpeedProp = Shader.PropertyToID("_WaveSpeed");
    static readonly int WaveLengthProp = Shader.PropertyToID("_WaveLength");
    static readonly int WaveSteepnessProp = Shader.PropertyToID("_WaveStepness");
    static readonly int NormalSpeedProp = Shader.PropertyToID("_NormalSpeed");
    static readonly int NormalScaleProp = Shader.PropertyToID("_NormalScale");
    static readonly int NormalStrengthProp = Shader.PropertyToID("_NormalStrength");
    static readonly int FoamBlendProp = Shader.PropertyToID("_FoamBlend");
    static readonly int SurfaceFoamSpeedProp = Shader.PropertyToID("_SurfaceFoamSpeed");
    static readonly int SurfaceFoamDirectionProp = Shader.PropertyToID("_SurfaceFoamDirection");
    static readonly int SurfaceFoamTilingProp = Shader.PropertyToID("_SurfaceFoamTiling");
    static readonly int SurfaceFoamDistortionProp = Shader.PropertyToID("_SurfaceFoamDistorsion");
    static readonly int SecondaryFoamBlendProp = Shader.PropertyToID("_SecondaryFoamBlend");
    static readonly int CaveBlendProp = Shader.PropertyToID("_CaveBlend");
    static readonly int BaseColorProp          = Shader.PropertyToID("_BaseColor");
    static readonly int ShallowColorProp       = Shader.PropertyToID("_ShallowColor");
    static readonly int DeepColorProp          = Shader.PropertyToID("_DeepColor");
    static readonly int TopColorProp           = Shader.PropertyToID("_TopColor");
    static readonly int SurfaceFoamColorProp   = Shader.PropertyToID("_SurfaceFoamColor");
    static readonly int SecondaryFoamColorProp = Shader.PropertyToID("_SecondaryFoamColor");
    static readonly int SpecularColorProp      = Shader.PropertyToID("_SpecularColor");
    static readonly int HorizonColorProp       = Shader.PropertyToID("_HorizonColor");
    static readonly int WaveDirectionsProp     = Shader.PropertyToID("_WaveDirections");
    // Extra AureDevGames-only properties
    static readonly int DepthFadeDistanceProp    = Shader.PropertyToID("_DepthFadeDistance");
    static readonly int DepthCompareDistanceProp = Shader.PropertyToID("_DepthCompareDistance");
    static readonly int LightingSmoothnessP      = Shader.PropertyToID("_LightingSmoothness");
    static readonly int LightingHardnessProp     = Shader.PropertyToID("_LightingHardness");
    static readonly int FresnelPowerProp         = Shader.PropertyToID("_FresnelPower");
    static readonly int HorizonDistanceProp      = Shader.PropertyToID("_HorizonDistance");
    static readonly int WaterAlphaProp           = Shader.PropertyToID("_WaterAlpha");

    static Mesh s_BackdropMesh;

    Camera targetCamera;
    Material sourceMaterial;
    Material runtimeMaterial;
    GameObject rigRoot;
    MeshRenderer quadRenderer;

    public bool Configure(Material newSourceMaterial, Camera camera)
    {
        if (newSourceMaterial == null || camera == null)
        {
            Clear();
            return false;
        }

        targetCamera = camera;
        EnsureRig();
        EnsureMaterial(newSourceMaterial);
        ApplyCameraSettings();
        ApplyMenuTuning();
        FitToCamera();
        return true;
    }

    public void Clear()
    {
        quadRenderer = null;
        sourceMaterial = null;
        targetCamera = null;
        DestroyUnityObject(runtimeMaterial);
        runtimeMaterial = null;
        DestroyUnityObject(rigRoot);
        rigRoot = null;
        // Null the cached mesh so UV changes in the source are picked up on next Configure
        s_BackdropMesh = null;
    }

    void LateUpdate()
    {
        if (rigRoot == null || targetCamera == null)
            return;

        FitToCamera();
        AnimateMaterial();
    }

    void OnDisable() => Clear();
    void OnDestroy() => Clear();

    void EnsureRig()
    {
        if (rigRoot != null)
            return;

        rigRoot = new GameObject(RigName);
        rigRoot.transform.SetParent(targetCamera.transform, false);
        rigRoot.transform.localRotation = Quaternion.identity;

        var quad = new GameObject(QuadName);
        quad.transform.SetParent(rigRoot.transform, false);

        var filter = quad.AddComponent<MeshFilter>();
        filter.sharedMesh = GetOrCreateBackdropMesh();

        quadRenderer = quad.AddComponent<MeshRenderer>();
        quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quadRenderer.receiveShadows = false;
        quadRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        quadRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        quadRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
    }

    void EnsureMaterial(Material newSourceMaterial)
    {
        if (sourceMaterial == newSourceMaterial && runtimeMaterial != null)
            return;

        sourceMaterial = newSourceMaterial;
        DestroyUnityObject(runtimeMaterial);
        runtimeMaterial = new Material(sourceMaterial)
        {
            name = sourceMaterial.name + " (Menu Runtime)"
        };

        if (quadRenderer != null)
            quadRenderer.sharedMaterial = runtimeMaterial;
    }

    void ApplyCameraSettings()
    {
        if (targetCamera == null)
            return;

        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = new Color(0.18f, 0.61f, 0.81f, 1f);
    }

    void ApplyMenuTuning()
    {
        if (runtimeMaterial == null || targetCamera == null)
            return;

        SetToggle(PerspectiveProp,  !targetCamera.orthographic, "_PERSPECTIVE");
        SetToggle(ReflectionProp,   false,                     "_REFLECTIONS");
        // World-space UV OFF — the shader is designed for a horizontal ocean surface;
        // on our screen-facing plane XZ world coords are badly skewed → extreme stretch.
        // With it off the shader reads the mesh UVs we've already baked with 3×2 tiling.
        SetToggle(WorldSpaceUVProp, false, "_WORLD_SPACE_UV");

        if (runtimeMaterial.HasProperty(OrthographicSizeProp))
            runtimeMaterial.SetFloat(OrthographicSizeProp, targetCamera.orthographic ? targetCamera.orthographicSize : 0f);

        // TileSize = 1 — tiling is already baked into the mesh UVs (3 wide × 2 tall).
        // Leaving TileSize > 1 would double-scale those baked UVs.
        SetFloat(TileSizeProp, 1f);

        // Gerstner waves — vertex displacement + animated UVs
        SetFloat(WaveSpeedProp,             0.56f);
        SetFloat(WaveLengthProp,            4.6f);
        SetFloat(WaveSteepnessProp,         0.55f);

        // Normal map — creates the shimmering surface chop
        SetFloat(NormalSpeedProp,           0.16f);
        SetFloat(NormalScaleProp,           1.61f);
        SetFloat(NormalStrengthProp,        1.71f);

        // Foam — white crests that appear on wave peaks
        SetFloat(FoamBlendProp,             0.28f);
        SetFloat(SurfaceFoamSpeedProp,      0.12f);
        SetFloat(SurfaceFoamDirectionProp,  0.25f);
        SetFloat(SurfaceFoamTilingProp,     0.53f);
        SetFloat(SurfaceFoamDistortionProp, 0.38f);
        SetFloat(SecondaryFoamBlendProp,    0.18f);

        // Cave/Voronoi — organic cellular depth patterns
        // Small blend gives the organic water-cell look without swamping the surface colour
        SetFloat(CaveBlendProp,             0.22f);

        // ── Colours (HDR values match the WaterUnlit preset) ──────────────────
        SetColor(BaseColorProp,          new Color(0.00f, 0.20f, 1.11f, 1f));
        SetColor(ShallowColorProp,       new Color(0.00f, 0.57f, 0.71f, 1f));
        // DeepColor must not be black — no scene geometry means depth = max everywhere.
        // A rich dark blue keeps the water readable even at full depth saturation.
        SetColor(DeepColorProp,          new Color(0.00f, 0.18f, 0.85f, 1f));
        SetColor(TopColorProp,           new Color(0.00f, 0.18f, 0.56f, 0.45f));
        SetColor(SurfaceFoamColorProp,   new Color(1.71f, 1.71f, 1.71f, 0.28f));
        SetColor(SecondaryFoamColorProp, new Color(0.00f, 0.00f, 0.19f, 0.41f));
        SetColor(SpecularColorProp,      new Color(1f,    1f,    1f,    0.32f));
        SetColor(HorizonColorProp,       new Color(7.38f, 3.66f, 6.66f, 1f));

        SetVector(WaveDirectionsProp, new Vector4(0.05f, 0.28f, 0.58f, 0.84f));

        // ── AureDevGames-specific extras ──────────────────────────────────────
        SetFloat(WaterAlphaProp,              1f);
        SetFloat(DepthFadeDistanceProp,       2.5f);
        SetFloat(DepthCompareDistanceProp,    0.1f);
        SetFloat(FresnelPowerProp,            1.4f);
        SetFloat(HorizonDistanceProp,         78f);
        SetFloat(LightingSmoothnessP,         12.3f);
        SetFloat(LightingHardnessProp,        0.39f);
    }

    void FitToCamera()
    {
        if (rigRoot == null || targetCamera == null)
            return;

        const float planeDistance = 7f;
        const float overscan = 1.15f;

        float distance = Mathf.Max(targetCamera.nearClipPlane + 0.25f, planeDistance);
        Transform quadTransform = rigRoot.transform.GetChild(0);

        rigRoot.transform.position = targetCamera.transform.position;
        rigRoot.transform.rotation = targetCamera.transform.rotation;
        quadTransform.localPosition = new Vector3(0f, 0f, distance);
        quadTransform.localRotation = Quaternion.identity; // flat — no tilt, fills screen evenly

        float height;
        float width;

        if (targetCamera.orthographic)
        {
            height = targetCamera.orthographicSize * 2f;
            width = height * targetCamera.aspect;
        }
        else
        {
            height = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            width = height * targetCamera.aspect;
        }

        quadTransform.localScale = new Vector3(width * overscan, height * overscan, 1f);
    }

    void AnimateMaterial()
    {
        if (runtimeMaterial == null)
            return;

        float t = Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup;
        float d = t * 0.18f; // master drift — keep slow so motion reads clearly

        // ── Wave directions: four independent Gerstner wave trains ───────────
        // Larger oscillation range (±0.13) makes direction changes clearly visible
        SetVector(WaveDirectionsProp, new Vector4(
            Mathf.Repeat(0.05f + Mathf.Sin(d * 0.80f)          * 0.13f, 1f),
            Mathf.Repeat(0.28f + Mathf.Cos(d * 0.62f + 1.40f)  * 0.12f, 1f),
            Mathf.Repeat(0.58f + Mathf.Sin(d * 1.06f + 0.80f)  * 0.14f, 1f),
            Mathf.Repeat(0.84f + Mathf.Cos(d * 0.74f + 2.20f)  * 0.11f, 1f)));

        // ── Wave shape: slow breathing so the sea feels alive ────────────────
        SetFloat(WaveSteepnessProp, 0.52f + Mathf.Sin(d * 1.40f)         * 0.06f);
        SetFloat(WaveLengthProp,    4.40f + Mathf.Sin(d * 0.46f + 0.40f) * 0.55f);
        SetFloat(WaveSpeedProp,     0.54f + Mathf.Sin(d * 0.68f)         * 0.04f);
        // TileSize stays at 1 — actual tiling is encoded in the mesh UVs
        SetFloat(TileSizeProp, 1f);

        // ── Foam: surges in and recedes like real wave crests ─────────────────
        SetFloat(FoamBlendProp,            0.20f + Mathf.Sin(d * 0.90f + 1.10f) * 0.06f);
        SetFloat(SecondaryFoamBlendProp,   0.09f + Mathf.Sin(d * 1.15f + 0.70f) * 0.04f);
        SetFloat(SurfaceFoamDirectionProp, Mathf.Repeat(1.00f + Mathf.Sin(d * 0.33f) * 0.08f, 1f));
        SetFloat(SurfaceFoamSpeedProp,     0.08f + Mathf.Sin(d * 0.83f + 0.30f) * 0.02f);

        // ── Normal shimmer: surface chop varies with the swell ────────────────
        SetFloat(NormalStrengthProp, 1.70f + Mathf.Sin(d * 1.95f) * 0.35f);

        // ── Specular glinting: sunlight catching wave peaks ───────────────────
        float glint = Mathf.Clamp01(
            0.30f + Mathf.Sin(d * 3.10f) * 0.15f
                  + Mathf.Sin(d * 1.62f + 0.90f) * 0.07f);
        SetColor(SpecularColorProp, new Color(1f, 1f, 1f, glint));

        // ── Shallow colour: subtle caustic-light pulse (HDR) ─────────────────
        float caustic = Mathf.Sin(d * 1.22f + 0.50f) * 0.08f;
        SetColor(ShallowColorProp, new Color(
            0.10f,
            Mathf.Clamp(1.34f + caustic, 0.8f, 1.6f),
            Mathf.Clamp(1.58f + caustic * 0.5f, 1.2f, 1.9f),
            1f));
    }

    void SetFloat(int propertyId, float value)
    {
        if (runtimeMaterial != null && runtimeMaterial.HasProperty(propertyId))
            runtimeMaterial.SetFloat(propertyId, value);
    }

    void SetColor(int propertyId, Color value)
    {
        if (runtimeMaterial != null && runtimeMaterial.HasProperty(propertyId))
            runtimeMaterial.SetColor(propertyId, value);
    }

    void SetVector(int propertyId, Vector4 value)
    {
        if (runtimeMaterial != null && runtimeMaterial.HasProperty(propertyId))
            runtimeMaterial.SetVector(propertyId, value);
    }

    void SetToggle(int propertyId, bool enabled, string keyword)
    {
        if (runtimeMaterial == null || !runtimeMaterial.HasProperty(propertyId))
            return;

        runtimeMaterial.SetFloat(propertyId, enabled ? 1f : 0f);

        if (enabled)
            runtimeMaterial.EnableKeyword(keyword);
        else
            runtimeMaterial.DisableKeyword(keyword);
    }

    static Mesh GetOrCreateBackdropMesh()
    {
        if (s_BackdropMesh != null)
            return s_BackdropMesh;

        s_BackdropMesh = new Mesh
        {
            name = "MenuWaterBackdrop"
        };

        int vertsX = MeshSubdivisionsX + 1;
        int vertsY = MeshSubdivisionsY + 1;
        var vertices = new Vector3[vertsX * vertsY];
        var uvs = new Vector2[vertices.Length];
        var normals = new Vector3[vertices.Length];
        var triangles = new int[MeshSubdivisionsX * MeshSubdivisionsY * 6];

        // Bake 3×2 tiling directly into the mesh UVs.
        // This gives ~3 horizontal and ~2 vertical repeats on a 16:9 screen
        // regardless of whether the shader scales local UVs by _TileSize or not.
        const float TilesU = 3f;
        const float TilesV = 2f;

        for (int y = 0; y < vertsY; y++)
        {
            float fy = (float)y / MeshSubdivisionsY;
            for (int x = 0; x < vertsX; x++)
            {
                float fx = (float)x / MeshSubdivisionsX;
                int index = y * vertsX + x;
                vertices[index] = new Vector3(fx - 0.5f, fy - 0.5f, 0f);
                uvs[index] = new Vector2(fx * TilesU, fy * TilesV);
                normals[index] = Vector3.back;
            }
        }

        int triIndex = 0;
        for (int y = 0; y < MeshSubdivisionsY; y++)
        {
            for (int x = 0; x < MeshSubdivisionsX; x++)
            {
                int a = y * vertsX + x;
                int b = a + vertsX;
                int c = a + 1;
                int d = b + 1;

                triangles[triIndex++] = a;
                triangles[triIndex++] = b;
                triangles[triIndex++] = c;
                triangles[triIndex++] = c;
                triangles[triIndex++] = b;
                triangles[triIndex++] = d;
            }
        }

        s_BackdropMesh.vertices = vertices;
        s_BackdropMesh.uv = uvs;
        s_BackdropMesh.normals = normals;
        s_BackdropMesh.triangles = triangles;
        s_BackdropMesh.bounds = new Bounds(Vector3.zero, new Vector3(4f, 4f, 4f));
        return s_BackdropMesh;
    }

    static void DestroyUnityObject(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}
