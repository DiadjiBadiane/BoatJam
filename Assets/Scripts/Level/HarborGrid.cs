// Assets/Scripts/Level/HarborGrid.cs
// Fully code-driven — assign only GridManager. No materials needed.

using UnityEngine;

public class HarborGrid : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;

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
        Quad("WaterBacking", transform,
            new Vector3((w-1)*cellSize*.5f, -0.02f, (h-1)*cellSize*.5f),
            new Vector3(w*cellSize+.06f, h*cellSize+.06f), COL_GRID_BG, false);
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
            Quad($"Water_{x}_{y}", transform,
                new Vector3(x*cellSize, -0.01f, y*cellSize),
                Vector3.one*(cellSize-.04f),
                (x+y)%2==0 ? COL_WATER_A : COL_WATER_B, true);
    }

    void DrawGridLines()
    {
        int w = gridManager.width, h = gridManager.height;
        float lw = 0.03f;
        for (int y = 0; y <= h; y++)
            Quad($"GH_{y}", transform,
                new Vector3((w-1)*cellSize*.5f, .001f, y*cellSize-cellSize*.5f),
                new Vector3(w*cellSize, lw), COL_GRID_LINE, false);
        for (int x = 0; x <= w; x++)
            Quad($"GV_{x}", transform,
                new Vector3(x*cellSize-cellSize*.5f, .001f, (h-1)*cellSize*.5f),
                new Vector3(lw, h*cellSize), COL_GRID_LINE, false);
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
            SetMat(bolt, COL_BOLT, false); Destroy(bolt.GetComponent<Collider>());
        }
    }

    static GameObject Quad(string n, Transform p, Vector3 pos, Vector3 scl, Color col, bool glossy)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name=n; go.transform.SetParent(p,false);
        go.transform.position=pos; go.transform.rotation=Quaternion.Euler(90,0,0);
        go.transform.localScale=new Vector3(scl.x,scl.y,1); SetMat(go,col,glossy);
        Destroy(go.GetComponent<Collider>()); return go;
    }

    static GameObject Cube(string n, Transform p, Vector3 pos, Vector3 scl, Color col)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name=n; go.transform.SetParent(p,false);
        go.transform.position=pos; go.transform.localScale=scl;
        SetMat(go,col,false); Destroy(go.GetComponent<Collider>()); return go;
    }

    static void SetMat(GameObject go, Color col, bool glossy)
    {
        var r = go.GetComponent<Renderer>(); if(r==null) return;
        Shader shader = FindSupportedSurfaceShader();
        var m = new Material(shader);

        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        if (m.HasProperty("_Color")) m.SetColor("_Color", col);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", glossy ? 0.65f : 0.10f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", glossy ? 0.65f : 0.10f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);

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