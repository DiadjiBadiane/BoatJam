// Assets/Scripts/Level/HarborGrid.cs
// Fully code-driven — assign GridManager and optionally a stylized water texture.

using UnityEngine;

public class HarborGrid : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

    [Header("Stylized Water")]
    public Texture2D stylizedWaterTexture;

    [Header("Tweak (optional)")]
    public float cellSize        = 1f;
    public float borderThickness = 0.18f;
    public float borderHeight    = 0.28f;
    public float gapCellCount    = 2f;

    static readonly Color COL_WATER_A   = Hex("2aa8e8");
    static readonly Color COL_WATER_B   = Hex("1f86d2");
    static readonly Color COL_GRID_BG   = Hex("1b76b8");
    static readonly Color COL_GRID_LINE = new Color(0.80f, 0.94f, 1f, 0.30f);
    static readonly Color COL_DOCK_RIM  = Hex("2a6d9a");
    static readonly Color COL_DOCK_WALL = Hex("235f88");
    static readonly Color COL_BOLT      = Hex("7fc8f2");
    static readonly Color COL_WATER_BACKING = Hex("0d2f4d");

    void Start()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        DrawWaterFloor();
        DrawGridLines();
        DrawDockBorder();
        DrawCornerBolts();
    }

    // Called by Unity when you hit Reset in the Inspector — clears stale fields
    void Reset()
    {
        gridManager = FindObjectOfType<GridManager>();
    }

    void DrawWaterFloor()
    {
        int w = gridManager.width, h = gridManager.height;
        float tileH = 0.06f;
        float backingY = -tileH * 0.5f - 0.025f;

        // Backing plate
        Cube("WaterBacking", transform,
            new Vector3((w-1)*cellSize*.5f, backingY, (h-1)*cellSize*.5f),
            new Vector3(w*cellSize+.08f, tileH, h*cellSize+.08f), COL_WATER_BACKING);

        if (stylizedWaterTexture != null)
        {
            DrawStylizedWaterSurface(w, h);
            return;
        }

        float floorY = -tileH * 0.5f;

        // Per-cell water tiles
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
            Cube($"Water_{x}_{y}", transform,
                new Vector3(x*cellSize, floorY, y*cellSize),
                new Vector3(cellSize-.04f, tileH, cellSize-.04f),
                (x+y)%2==0 ? COL_WATER_A : COL_WATER_B);
    }

    void DrawStylizedWaterSurface(int w, int h)
    {
        float backdropSize = Mathf.Max(w, h) * cellSize + 24f;
        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "StylizedWaterSurface";
        water.transform.SetParent(transform, false);
        water.transform.position = new Vector3((w - 1) * cellSize * 0.5f, -0.013f, (h - 1) * cellSize * 0.5f);
        water.transform.localScale = new Vector3(backdropSize / 10f, 1f, backdropSize / 10f);

        var renderer = water.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = BuildStylizedWaterMaterial(w, h);

        var animator = water.AddComponent<AnimatedMaterialOffset>();
        animator.scrollSpeed = new Vector2(0.02f, -0.008f);

        Destroy(water.GetComponent<Collider>());
    }

    Material BuildStylizedWaterMaterial(int w, int h)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Texture")
                  ?? Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Standard");

        var material = new Material(shader);
        stylizedWaterTexture.wrapMode = TextureWrapMode.Repeat;

        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", stylizedWaterTexture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", stylizedWaterTexture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.53f, 0.89f, 0.98f, 1f));
        if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.53f, 0.89f, 0.98f, 1f));
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);

        Vector2 tiling = new Vector2(Mathf.Max(8f, w * 1.35f), Mathf.Max(8f, h * 1.35f));
        if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", tiling);
        if (material.HasProperty("_MainTex")) material.SetTextureScale("_MainTex", tiling);

        return material;
    }

    void DrawGridLines()
    {
        int w = gridManager.width, h = gridManager.height;
        float lw = 0.03f, lh = 0.02f;
        float lineY = -lh * 0.5f + 0.001f;  // just above water tiles, below boats
        for (int y = 0; y <= h; y++)
            Cube($"GH_{y}", transform,
                new Vector3((w-1)*cellSize*.5f, lineY, y*cellSize-cellSize*.5f),
                new Vector3(w*cellSize, lh, lw), COL_GRID_LINE);
        for (int x = 0; x <= w; x++)
            Cube($"GV_{x}", transform,
                new Vector3(x*cellSize-cellSize*.5f, lineY, (h-1)*cellSize*.5f),
                new Vector3(lw, lh, h*cellSize), COL_GRID_LINE);
    }

    void DrawDockBorder()
    {
        int w = gridManager.width, h = gridManager.height;
        float tw = w*cellSize, th = h*cellSize, half = cellSize*.5f;
        float bt = borderThickness, bh = borderHeight, rim = bt*1.5f;

        Cube("Dock_Rim",    transform, new Vector3(tw*.5f-half, bh*.25f, th*.5f-half), new Vector3(tw+rim*2, bh*.5f, th+rim*2), COL_DOCK_RIM);
        Cube("Dock_Bottom", transform, new Vector3(tw*.5f-half, bh*.5f, -half),        new Vector3(tw+bt*2,  bh, bt),           COL_DOCK_WALL);
        Cube("Dock_Top",    transform, new Vector3(tw*.5f-half, bh*.5f, th-half),      new Vector3(tw+bt*2,  bh, bt),           COL_DOCK_WALL);
        Cube("Dock_Left",   transform, new Vector3(-half,        bh*.5f, th*.5f-half), new Vector3(bt, bh, th),                 COL_DOCK_WALL);

        LevelData level = GameManager.Instance?.CurrentLevel;
        if (level == null || !level.exitOnRight)
        { Cube("Dock_Right", transform, new Vector3(tw-half, bh*.5f, th*.5f-half), new Vector3(bt,bh,th), COL_DOCK_WALL); return; }

        float ez = level.exitRow*cellSize, gap = gapCellCount*cellSize;
        float botH = ez-half;
        if (botH > 0f) Cube("Dock_R_Bot", transform, new Vector3(tw-half,bh*.5f,botH*.5f),              new Vector3(bt,bh,botH), COL_DOCK_WALL);
        float ts = ez+gap+half, topH = th-ts;
        if (topH > 0f) Cube("Dock_R_Top", transform, new Vector3(tw-half,bh*.5f,ts+topH*.5f-half),      new Vector3(bt,bh,topH), COL_DOCK_WALL);
    }

    void DrawCornerBolts()
    {
        int w = gridManager.width, h = gridManager.height;
        float tw = w*cellSize, th = h*cellSize, half = cellSize*.5f;
        float bh = borderHeight, r = borderThickness*.4f;
        foreach (var pos in new[]{ new Vector3(-half,bh,-half), new Vector3(tw-half,bh,-half),
                                   new Vector3(-half,bh,th-half), new Vector3(tw-half,bh,th-half) })
        {
            var bolt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bolt.name = "Bolt"; bolt.transform.SetParent(transform,false);
            bolt.transform.position = pos; bolt.transform.localScale = new Vector3(r,.05f,r);
            SetMat(bolt, COL_BOLT); Destroy(bolt.GetComponent<Collider>());
        }
    }

    static GameObject Cube(string n, Transform p, Vector3 pos, Vector3 scl, Color col)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name=n; go.transform.SetParent(p,false);
        go.transform.position=pos; go.transform.localScale=scl;
        SetMat(go,col); Destroy(go.GetComponent<Collider>()); return go;
    }

    static void SetMat(GameObject go, Color col)
    {
        var r = go.GetComponent<Renderer>(); if (r == null) return;
        var m = new Material(FindSupportedSurfaceShader());

        // Force opaque in URP (new Material() defaults to Transparent)
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        m.SetInt("_ZWrite",   1);
        m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;

        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        if (m.HasProperty("_Color"))     m.SetColor("_Color",     col);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic",   0f);

        r.material = m;
    }

    static Shader FindSupportedSurfaceShader()
    {
        string[] candidates =
        {
            "Unlit/Color",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Standard",
            "Sprites/Default"
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            var s = Shader.Find(candidates[i]);
            if (s != null && s.isSupported) return s;
        }

        return Shader.Find("Unlit/Color");
    }

    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#"+h, out Color c); return c; }
}
