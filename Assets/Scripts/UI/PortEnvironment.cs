// Assets/Scripts/Rendering/PortEnvironment.cs
// Procedurally spawns a realistic top-down port around the puzzle grid.
// No external assets required — uses only primitive meshes + runtime materials.
//
// HOW TO USE:
//   1. Create an empty GameObject in your GameScene, name it "PortEnvironment".
//   2. Add this component.
//   3. Set GridManager reference (or leave null — it auto-finds it).
//   4. Press Play. The port rebuilds itself whenever the grid changes.
//
// The component listens for grid size via GridManager.Instance and places:
//   • Dock plank border  (brown wood)
//   • Bollards           (dark cylinders with a specular cap)
//   • Yellow safety line (thin quads)
//   • Two corner cranes  (L-shaped yellow beams + cable)
//   • Background warehouses (grey boxes, far edge)
//   • Seawater plane     (handled by OceanBackground on the camera)

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PortEnvironment : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Grid")]
    [Tooltip("Leave null — auto-found via GridManager.Instance")]
    public GridManager gridManager;

    [Header("Dock")]
    [SerializeField] float dockWidth         = 0.55f;   // world units — thickness of each side plank
    [SerializeField] float dockPlankSpacing  = 0.9f;    // spacing of plank-seam lines (visual only)
    [SerializeField] float dockInset         = 0.10f;   // gap between dock edge and grid edge

    [Header("Bollards")]
    [SerializeField] float bollardRadius     = 0.14f;
    [SerializeField] float bollardHeight     = 0.30f;
    [SerializeField] int   bollardSegs       = 12;
    [SerializeField] float bollardSpacingFraction = 0.33f; // fraction of dock length between bollards

    [Header("Safety Line")]
    [SerializeField] float safetyLineWidth   = 0.06f;
    [SerializeField] float safetyLineDashLen = 0.45f;
    [SerializeField] float safetyLineGapLen  = 0.30f;

    [Header("Cranes")]
    [SerializeField] float craneArmLength    = 2.2f;
    [SerializeField] float craneMastHeight   = 2.8f;
    [SerializeField] float craneBeamThick    = 0.14f;

    [Header("Warehouses")]
    [SerializeField] int   warehouseCount    = 3;
    [SerializeField] float warehouseDepth    = 1.4f;   // Z extent (into background)

    // ── Colors ────────────────────────────────────────────────────────────────
    static readonly Color COL_WOOD_LIGHT = Hex("a07830");
    static readonly Color COL_WOOD_DARK  = Hex("7a5010");
    static readonly Color COL_BOLLARD    = Hex("1a1005");
    static readonly Color COL_BOLLARD_CAP= Hex("50380a");
    static readonly Color COL_SAFETY     = Hex("e8c830");
    static readonly Color COL_CRANE      = Hex("d4a820");
    static readonly Color COL_WAREHOUSE  = Hex("4a5060");
    static readonly Color COL_WHOUSE_ROOF= Hex("3a3f4a");

    // ── Runtime ───────────────────────────────────────────────────────────────
    readonly List<GameObject> _spawned = new List<GameObject>();
    int _lastGridW = -1, _lastGridH = -1;
    float _lastCellSize = -1f;

    // ── Materials (lazy-created, shared) ─────────────────────────────────────
    Material _matWood, _matBollard, _matBollardCap, _matSafety, _matCrane, _matWarehouse, _matRoof;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start() => TryBuild();

    void Update()
    {
        if (GridManager.Instance == null) return;
        int w  = GridManager.Instance.width;
        int h  = GridManager.Instance.height;
        float c = GridManager.Instance.cellSize;
        if (w != _lastGridW || h != _lastGridH || !Mathf.Approximately(c, _lastCellSize))
            TryBuild();
    }

    void OnDestroy() => ClearAll();

    // ── Build ─────────────────────────────────────────────────────────────────

    void TryBuild()
    {
        if (gridManager == null) gridManager = GridManager.Instance;
        if (gridManager == null) return;

        int   w = gridManager.width;
        int   h = gridManager.height;
        float cs = gridManager.cellSize;
        if (w <= 0 || h <= 0 || cs <= 0f) return;

        ClearAll();

        _lastGridW    = w;
        _lastGridH    = h;
        _lastCellSize = cs;

        EnsureMaterials();

        float worldW = w * cs;
        float worldH = h * cs;

        // Grid starts at world origin — centre = (worldW/2 - cs/2, 0, worldH/2 - cs/2)
        // We mirror GridManager's coordinate convention (X right, Z forward).

        float x0 = -dockInset;
        float z0 = -dockInset;
        float x1 = worldW + dockInset;
        float z1 = worldH + dockInset;

        BuildDockBorder(x0, z0, x1, z1);
        BuildBollards(x0, z0, x1, z1);
        BuildSafetyLine(x0, z0, x1, z1);
        BuildCranes(x0, z0, x1, z1);
        BuildWarehouses(x0, z0, x1, z1);
    }

    // ── Dock border ───────────────────────────────────────────────────────────
    // Four rectangular planks (top, bottom, left, right) flush around the grid.

    void BuildDockBorder(float x0, float z0, float x1, float z1)
    {
        float w = x1 - x0;
        float h = z1 - z0;

        // Top plank (Z+)
        SpawnPlank("Dock_Top",
            new Vector3(x0 + w * 0.5f, 0f, z1 + dockWidth * 0.5f),
            new Vector3(w + dockWidth * 2f, 1f, dockWidth));

        // Bottom plank (Z-)
        SpawnPlank("Dock_Bot",
            new Vector3(x0 + w * 0.5f, 0f, z0 - dockWidth * 0.5f),
            new Vector3(w + dockWidth * 2f, 1f, dockWidth));

        // Left plank (X-)
        SpawnPlank("Dock_Left",
            new Vector3(x0 - dockWidth * 0.5f, 0f, z0 + h * 0.5f),
            new Vector3(dockWidth, 1f, h));

        // Right plank (X+)
        SpawnPlank("Dock_Right",
            new Vector3(x1 + dockWidth * 0.5f, 0f, z0 + h * 0.5f),
            new Vector3(dockWidth, 1f, h));
    }

    void SpawnPlank(string name, Vector3 centre, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform);
        go.transform.position   = centre;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = _matWood;
        DestroyCollider(go);
        _spawned.Add(go);
    }

    // ── Bollards ──────────────────────────────────────────────────────────────

    void BuildBollards(float x0, float z0, float x1, float z1)
    {
        float dockCentreTop  = z1 + dockWidth * 0.5f;
        float dockCentreBot  = z0 - dockWidth * 0.5f;
        float dockCentreLeft = x0 - dockWidth * 0.5f;
        float dockCentreRight= x1 + dockWidth * 0.5f;

        // Along top & bottom edges (spaced in X)
        int countX = Mathf.Max(2, Mathf.FloorToInt((x1 - x0) / (bollardRadius * 2f + (x1-x0) * bollardSpacingFraction)));
        for (int i = 0; i <= countX; i++)
        {
            float t = (float)i / countX;
            float x = Mathf.Lerp(x0 + dockWidth * 0.5f, x1 - dockWidth * 0.5f, t);
            SpawnBollard(new Vector3(x, 0f, dockCentreTop));
            SpawnBollard(new Vector3(x, 0f, dockCentreBot));
        }

        // Along left & right edges (spaced in Z) — skip corners (already placed)
        int countZ = Mathf.Max(2, Mathf.FloorToInt((z1 - z0) / (bollardRadius * 2f + (z1-z0) * bollardSpacingFraction)));
        for (int i = 1; i < countZ; i++) // skip i=0 and i=countZ (corners)
        {
            float t = (float)i / countZ;
            float z = Mathf.Lerp(z0 + dockWidth * 0.5f, z1 - dockWidth * 0.5f, t);
            SpawnBollard(new Vector3(dockCentreLeft,  0f, z));
            SpawnBollard(new Vector3(dockCentreRight, 0f, z));
        }
    }

    void SpawnBollard(Vector3 base_)
    {
        // Body
        var body = SpawnCylinder("Bollard_Body", base_ + Vector3.up * (bollardHeight * 0.5f),
            new Vector3(bollardRadius * 2f, bollardHeight, bollardRadius * 2f), _matBollard);

        // Cap (slightly wider dark disc)
        var cap = SpawnCylinder("Bollard_Cap", base_ + Vector3.up * bollardHeight,
            new Vector3(bollardRadius * 2.4f, bollardHeight * 0.12f, bollardRadius * 2.4f), _matBollardCap);
    }

    GameObject SpawnCylinder(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(transform);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        DestroyCollider(go);
        _spawned.Add(go);
        return go;
    }

    // ── Safety / quayside dashed line ─────────────────────────────────────────

    void BuildSafetyLine(float x0, float z0, float x1, float z1)
    {
        float inset = dockWidth * 0.7f; // line runs just inside the dock edge
        float y     = 0.005f;           // just above the dock planks

        BuildDashedLine(x0, x1, z1 + inset, y, true);
        BuildDashedLine(x0, x1, z0 - inset, y, true);
        BuildDashedLineZ(z0, z1, x0 - inset, y);
        BuildDashedLineZ(z0, z1, x1 + inset, y);
    }

    void BuildDashedLine(float x0, float x1, float z, float y, bool alongX)
    {
        float len = x1 - x0;
        float cursor = x0;
        bool  dash   = true;
        int   idx    = 0;
        while (cursor < x1)
        {
            float segLen = dash ? safetyLineDashLen : safetyLineGapLen;
            segLen = Mathf.Min(segLen, x1 - cursor);
            if (dash)
            {
                float cx = cursor + segLen * 0.5f;
                SpawnQuad($"SafetyLine_{idx}",
                    new Vector3(cx, y, z),
                    new Vector3(segLen, 0.001f, safetyLineWidth),
                    _matSafety);
            }
            cursor += segLen;
            dash = !dash;
            idx++;
        }
    }

    void BuildDashedLineZ(float z0, float z1, float x, float y)
    {
        float cursor = z0;
        bool  dash   = true;
        int   idx    = 0;
        while (cursor < z1)
        {
            float segLen = dash ? safetyLineDashLen : safetyLineGapLen;
            segLen = Mathf.Min(segLen, z1 - cursor);
            if (dash)
            {
                float cz = cursor + segLen * 0.5f;
                SpawnQuad($"SafetyLineZ_{idx}",
                    new Vector3(x, y, cz),
                    new Vector3(safetyLineWidth, 0.001f, segLen),
                    _matSafety);
            }
            cursor += segLen;
            dash = !dash;
            idx++;
        }
    }

    void SpawnQuad(string name, Vector3 centre, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform);
        go.transform.position   = centre;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        DestroyCollider(go);
        _spawned.Add(go);
    }

    // ── Cranes ────────────────────────────────────────────────────────────────
    // Simple L-shaped crane at two corners (top-left, bottom-right in top-down view).

    void BuildCranes(float x0, float z0, float x1, float z1)
    {
        // Corner 1: far left / far back (top-left from top-down)
        SpawnCrane("Crane_TL",
            new Vector3(x0 - dockWidth * 1.5f, 0f, z1 + dockWidth * 1.5f),
            +1f);

        // Corner 2: far right / near front (bottom-right)
        SpawnCrane("Crane_BR",
            new Vector3(x1 + dockWidth * 1.5f, 0f, z0 - dockWidth * 1.5f),
            -1f);
    }

    void SpawnCrane(string name, Vector3 base_, float dir)
    {
        // Vertical mast
        SpawnCubeChild($"{name}_Mast",
            base_ + Vector3.up * craneMastHeight * 0.5f,
            new Vector3(craneBeamThick, craneMastHeight, craneBeamThick),
            _matCrane);

        // Horizontal boom
        float boomOffset = craneArmLength * 0.5f * dir;
        SpawnCubeChild($"{name}_Boom",
            base_ + Vector3.up * craneMastHeight + new Vector3(boomOffset, 0f, 0f),
            new Vector3(craneArmLength, craneBeamThick, craneBeamThick),
            _matCrane);

        // Cable (thin dark line hanging from boom tip)
        float cableLen = craneMastHeight * 0.55f;
        SpawnCubeChild($"{name}_Cable",
            base_ + Vector3.up * (craneMastHeight - cableLen * 0.5f) + new Vector3(boomOffset * 1.8f, 0f, 0f),
            new Vector3(craneBeamThick * 0.3f, cableLen, craneBeamThick * 0.3f),
            _matBollard);
    }

    void SpawnCubeChild(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        DestroyCollider(go);
        _spawned.Add(go);
    }

    // ── Warehouses ────────────────────────────────────────────────────────────
    // Row of flat boxes along the far background edge (high Z).

    void BuildWarehouses(float x0, float z0, float x1, float z1)
    {
        float totalW = x1 - x0;
        float gapW   = totalW / warehouseCount;
        float backZ  = z1 + dockWidth + warehouseDepth * 0.5f + 0.3f;
        float whH    = 0.6f; // how tall (Y) the building appears from top-down

        for (int i = 0; i < warehouseCount; i++)
        {
            float cx = x0 + gapW * (i + 0.5f);
            float ww = gapW * 0.78f;

            // Main body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = $"Warehouse_{i}";
            body.transform.SetParent(transform);
            body.transform.position   = new Vector3(cx, whH * 0.5f, backZ);
            body.transform.localScale = new Vector3(ww, whH, warehouseDepth);
            body.GetComponent<Renderer>().sharedMaterial = _matWarehouse;
            DestroyCollider(body);
            _spawned.Add(body);

            // Roof stripe
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = $"Warehouse_{i}_Roof";
            roof.transform.SetParent(transform);
            roof.transform.position   = new Vector3(cx, whH + 0.02f, backZ);
            roof.transform.localScale = new Vector3(ww, 0.04f, warehouseDepth);
            roof.GetComponent<Renderer>().sharedMaterial = _matRoof;
            DestroyCollider(roof);
            _spawned.Add(roof);
        }
    }

    // ── Materials ─────────────────────────────────────────────────────────────

    void EnsureMaterials()
    {
        _matWood      = MakeMat(COL_WOOD_LIGHT, 0.1f);
        _matBollard   = MakeMat(COL_BOLLARD,    0.05f);
        _matBollardCap= MakeMat(COL_BOLLARD_CAP,0.3f);
        _matSafety    = MakeMat(COL_SAFETY,     0.05f);
        _matCrane     = MakeMat(COL_CRANE,      0.2f);
        _matWarehouse = MakeMat(COL_WAREHOUSE,  0.05f);
        _matRoof      = MakeMat(COL_WHOUSE_ROOF,0.05f);
    }

    Material MakeMat(Color col, float smoothness)
    {
        // Use URP Lit if available, otherwise Standard
        var shader = Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Standard");
        var mat = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave,
            color     = col
        };
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Glossiness", smoothness);
        return mat;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    void ClearAll()
    {
        foreach (var go in _spawned)
            if (go != null) DestroyImmediate(go);
        _spawned.Clear();

        SafeDestroy(_matWood);  SafeDestroy(_matBollard); SafeDestroy(_matBollardCap);
        SafeDestroy(_matSafety); SafeDestroy(_matCrane);  SafeDestroy(_matWarehouse);
        SafeDestroy(_matRoof);

        _lastGridW = _lastGridH = -1;
        _lastCellSize = -1f;
    }

    static void SafeDestroy(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj);
    }

    static void DestroyCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);
    }

    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString("#" + h, out Color c);
        return c;
    }
}