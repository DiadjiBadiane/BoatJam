// Assets/Scripts/UI/AmbientHarborTraffic.cs
// Spawns decorative boats that cruise around the harbor outside the puzzle grid.
// Fully procedural — no external assets required.
// Self-bootstrapping: creates its own GameObject at runtime. Zero manual setup.
// Includes: tugboats, cargo ships, sailboats, and fishing boats at varying scales.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class AmbientHarborTraffic : MonoBehaviour
{
    // ── Auto-bootstrap ────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TrySpawnInCurrentScene();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TrySpawnInCurrentScene();

    static void TrySpawnInCurrentScene()
    {
        if (GridManager.Instance == null && Object.FindObjectOfType<GridManager>() == null) return;
        if (Object.FindObjectOfType<AmbientHarborTraffic>() != null) return;

        var go = new GameObject("AmbientHarborTraffic");
        go.AddComponent<AmbientHarborTraffic>();
    }

    // ── Config ────────────────────────────────────────────────────────────────

    GridManager gridManager;

    const int   BOAT_COUNT    = 8;
    const float MIN_SPEED     = 0.3f;
    const float MAX_SPEED     = 0.9f;
    const float LANE_OFFSET   = 2.5f;
    const float LANE_SPACING  = 2.0f;
    const float BOB_AMPLITUDE = 0.04f;
    const float BOB_FREQUENCY = 1.0f;
    const float WAKE_LENGTH   = 1.2f;
    const float WAKE_WIDTH    = 0.2f;

    // ── Boat types ────────────────────────────────────────────────────────────
    enum BoatType { Tugboat, CargoShip, Sailboat, FishingBoat }

    // ── Colors ────────────────────────────────────────────────────────────────
    static readonly Color[] HULL_COLORS = {
        Hex("c0392b"), Hex("2980b9"), Hex("27ae60"), Hex("f39c12"),
        Hex("8e44ad"), Hex("1abc9c"), Hex("d35400"), Hex("2c3e50"),
    };
    static readonly Color COL_CABIN      = Hex("ecf0f1");
    static readonly Color COL_CABIN_2    = Hex("bdc3c7");
    static readonly Color COL_MAST       = Hex("7f8c8d");
    static readonly Color COL_SAIL       = Hex("f5f5f0");
    static readonly Color COL_CONTAINER1 = Hex("e67e22");
    static readonly Color COL_CONTAINER2 = Hex("2980b9");
    static readonly Color COL_CONTAINER3 = Hex("27ae60");
    static readonly Color COL_CONTAINER4 = Hex("c0392b");
    static readonly Color COL_FUNNEL     = Hex("34495e");
    static readonly Color COL_WAKE       = new Color(1f, 1f, 1f, 0.3f);
    static readonly Color COL_DECK       = Hex("95a5a6");
    static readonly Color COL_PORTHOLE   = Hex("2c3e50");
    static readonly Color COL_RAILING    = Hex("bdc3c7");

    // ── Runtime ───────────────────────────────────────────────────────────────
    readonly List<AmbientBoat> _boats = new List<AmbientBoat>();
    readonly List<GameObject>  _allSpawned = new List<GameObject>();
    int   _lastGridW = -1, _lastGridH = -1;
    float _lastCellSize = -1f;

    class AmbientBoat
    {
        public Transform root;
        public Transform wake;
        public Vector3[] waypoints;
        public int       nextWP;
        public float     speed;
        public float     bobPhase;
        public float     baseY;
        public BoatType  type;
    }

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
        AnimateBoats();
    }

    void OnDestroy() => ClearAll();

    // ── Build ─────────────────────────────────────────────────────────────────

    void TryBuild()
    {
        if (gridManager == null) gridManager = GridManager.Instance;
        if (gridManager == null) return;

        int   w  = gridManager.width;
        int   h  = gridManager.height;
        float cs = gridManager.cellSize;
        if (w <= 0 || h <= 0 || cs <= 0f) return;

        ClearAll();
        _lastGridW    = w;
        _lastGridH    = h;
        _lastCellSize = cs;

        float worldW = w * cs;
        float worldH = h * cs;

        for (int i = 0; i < BOAT_COUNT; i++)
        {
            float lane = LANE_OFFSET + LANE_SPACING * (i % 3);
            var route = BuildRoute(worldW, worldH, lane, i);
            SpawnAmbientBoat(route, i);
        }
    }

    Vector3[] BuildRoute(float worldW, float worldH, float lane, int index)
    {
        float half = gridManager.cellSize * 0.5f;
        float margin = 1.5f;
        float x0 = -half - margin - lane;
        float z0 = -half - margin - lane;
        float x1 = worldW - half + margin + lane;
        float z1 = worldH - half + margin + lane;

        int routeType = index % 4;
        switch (routeType)
        {
            case 0:
                return new[] {
                    new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z0),
                    new Vector3(x1, 0f, z1), new Vector3(x0, 0f, z1),
                };
            case 1:
                return new[] {
                    new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1),
                    new Vector3(x1, 0f, z0), new Vector3(x0, 0f, z0),
                };
            case 2:
                return new[] {
                    new Vector3(x0 - 6f, 0f, z0 - LANE_SPACING * 0.5f),
                    new Vector3(x1 + 6f, 0f, z0 - LANE_SPACING * 0.5f),
                };
            case 3:
                return new[] {
                    new Vector3(x1 + 6f, 0f, z1 + LANE_SPACING * 0.5f),
                    new Vector3(x0 - 6f, 0f, z1 + LANE_SPACING * 0.5f),
                };
            default:
                return new[] { new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z0) };
        }
    }

    void SpawnAmbientBoat(Vector3[] route, int index)
    {
        if (route.Length < 2) return;

        var root = new GameObject($"AmbientBoat_{index}");
        root.transform.SetParent(transform);
        _allSpawned.Add(root);

        // Start at random position along route
        float t = Random.value;
        int startSeg = Mathf.FloorToInt(t * route.Length) % route.Length;
        float segT = (t * route.Length) - startSeg;
        int nextIdx = (startSeg + 1) % route.Length;
        Vector3 startPos = Vector3.Lerp(route[startSeg], route[nextIdx], segT);
        root.transform.position = startPos;

        Vector3 dir = (route[nextIdx] - startPos);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            root.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        Color hullColor = HULL_COLORS[index % HULL_COLORS.Length];

        // Assign boat type — cargo ships are bigger, sailboats medium, tugs+fishing smaller
        BoatType type;
        float boatScale;
        if (index % 4 == 0)      { type = BoatType.CargoShip;   boatScale = Random.Range(1.6f, 2.2f); }
        else if (index % 4 == 1) { type = BoatType.Sailboat;    boatScale = Random.Range(0.9f, 1.3f); }
        else if (index % 4 == 2) { type = BoatType.Tugboat;     boatScale = Random.Range(0.7f, 1.0f); }
        else                     { type = BoatType.FishingBoat;  boatScale = Random.Range(0.8f, 1.1f); }

        BuildBoatByType(root.transform, hullColor, boatScale, type, index);

        Transform wakeTr = BuildWake(root.transform, boatScale);

        _boats.Add(new AmbientBoat
        {
            root      = root.transform,
            wake      = wakeTr,
            waypoints = route,
            nextWP    = nextIdx,
            speed     = Random.Range(MIN_SPEED, MAX_SPEED) * (type == BoatType.CargoShip ? 0.6f : 1f),
            bobPhase  = Random.value * Mathf.PI * 2f,
            baseY     = -0.02f,
            type      = type,
        });
    }

    // ── Boat builders ─────────────────────────────────────────────────────────

    void BuildBoatByType(Transform parent, Color hullColor, float s, BoatType type, int variant)
    {
        switch (type)
        {
            case BoatType.CargoShip:   BuildCargoShip(parent, hullColor, s, variant); break;
            case BoatType.Sailboat:    BuildSailboat(parent, hullColor, s, variant);  break;
            case BoatType.Tugboat:     BuildTugboat(parent, hullColor, s, variant);   break;
            case BoatType.FishingBoat: BuildFishingBoat(parent, hullColor, s, variant); break;
        }
    }

    void BuildCargoShip(Transform parent, Color hullColor, float s, int variant)
    {
        float hL = 1.6f * s, hW = 0.5f * s, hH = 0.15f * s;

        // Hull
        Prim(PrimitiveType.Cube, "Hull", parent, Vector3.up * hH * 0.5f,
            new Vector3(hW, hH, hL), hullColor);

        // Bow wedge
        var bow = Prim(PrimitiveType.Cube, "Bow", parent,
            new Vector3(0, hH * 0.4f, hL * 0.48f),
            new Vector3(hW * 0.45f, hH * 0.7f, hW * 0.5f), hullColor);
        bow.transform.localRotation = Quaternion.Euler(0, 45, 0);

        // Deck
        Prim(PrimitiveType.Cube, "Deck", parent,
            new Vector3(0, hH, 0), new Vector3(hW * 0.92f, 0.01f * s, hL * 0.9f), COL_DECK);

        // Bridge (wheelhouse) at stern
        float brH = hH * 1.8f;
        Prim(PrimitiveType.Cube, "Bridge", parent,
            new Vector3(0, hH + brH * 0.5f, -hL * 0.32f),
            new Vector3(hW * 0.7f, brH, hL * 0.18f), COL_CABIN);

        // Bridge windows
        Prim(PrimitiveType.Cube, "Windows", parent,
            new Vector3(0, hH + brH * 0.7f, -hL * 0.32f + hL * 0.09f + 0.01f),
            new Vector3(hW * 0.6f, brH * 0.25f, 0.015f * s), COL_PORTHOLE);

        // Funnel
        float fH = hH * 2.5f;
        Prim(PrimitiveType.Cylinder, "Funnel", parent,
            new Vector3(0, hH + fH * 0.5f, -hL * 0.25f),
            new Vector3(0.08f * s, fH * 0.5f, 0.08f * s), COL_FUNNEL);

        // Funnel stripe
        Prim(PrimitiveType.Cylinder, "FunnelStripe", parent,
            new Vector3(0, hH + fH * 0.8f, -hL * 0.25f),
            new Vector3(0.085f * s, fH * 0.06f, 0.085f * s), hullColor);

        // Containers on deck — stacked colored boxes
        Color[] cColors = { COL_CONTAINER1, COL_CONTAINER2, COL_CONTAINER3, COL_CONTAINER4 };
        float cW = hW * 0.28f, cH = hH * 0.55f, cL = hL * 0.14f;
        int ci = 0;
        for (int row = 0; row < 3; row++)
        {
            for (int stack = 0; stack < 2; stack++)
            {
                float cx = (row - 1) * cW * 1.1f;
                float cz = hL * 0.08f + row * cL * 0.3f;
                float cy = hH + cH * 0.5f + stack * cH;
                Prim(PrimitiveType.Cube, $"Container_{ci}", parent,
                    new Vector3(cx, cy, cz),
                    new Vector3(cW, cH, cL), cColors[ci % cColors.Length]);
                ci++;
            }
        }

        // Portholes along hull
        for (int p = 0; p < 3; p++)
        {
            float pz = hL * (-0.15f + p * 0.15f);
            Prim(PrimitiveType.Cylinder, $"Porthole_L{p}", parent,
                new Vector3(-hW * 0.51f, hH * 0.6f, pz),
                new Vector3(0.04f * s, 0.005f * s, 0.04f * s), COL_PORTHOLE);
            Prim(PrimitiveType.Cylinder, $"Porthole_R{p}", parent,
                new Vector3(hW * 0.51f, hH * 0.6f, pz),
                new Vector3(0.04f * s, 0.005f * s, 0.04f * s), COL_PORTHOLE);
        }
    }

    void BuildSailboat(Transform parent, Color hullColor, float s, int variant)
    {
        float hL = 1.0f * s, hW = 0.25f * s, hH = 0.08f * s;

        // Hull
        Prim(PrimitiveType.Cube, "Hull", parent,
            Vector3.up * hH * 0.5f, new Vector3(hW, hH, hL), hullColor);

        // Bow
        var bow = Prim(PrimitiveType.Cube, "Bow", parent,
            new Vector3(0, hH * 0.4f, hL * 0.48f),
            new Vector3(hW * 0.4f, hH * 0.6f, hW * 0.5f), hullColor);
        bow.transform.localRotation = Quaternion.Euler(0, 45, 0);

        // Mast
        float mastH = hL * 0.8f;
        Prim(PrimitiveType.Cylinder, "Mast", parent,
            new Vector3(0, hH + mastH * 0.5f, hL * 0.05f),
            new Vector3(0.02f * s, mastH * 0.5f, 0.02f * s), COL_MAST);

        // Main sail (tall thin quad)
        float sailH = mastH * 0.75f;
        float sailW = hL * 0.35f;
        Prim(PrimitiveType.Cube, "Sail", parent,
            new Vector3(sailW * 0.3f, hH + mastH * 0.45f, hL * 0.05f),
            new Vector3(sailW, sailH, 0.01f * s), COL_SAIL);

        // Jib sail (small forward triangle — approximated as a cube)
        Prim(PrimitiveType.Cube, "Jib", parent,
            new Vector3(-0.02f * s, hH + mastH * 0.3f, hL * 0.25f),
            new Vector3(sailW * 0.5f, sailH * 0.5f, 0.008f * s), COL_SAIL);

        // Small cabin
        Prim(PrimitiveType.Cube, "Cabin", parent,
            new Vector3(0, hH + hH * 0.4f, -hL * 0.2f),
            new Vector3(hW * 0.6f, hH * 0.8f, hL * 0.2f), COL_CABIN);
    }

    void BuildTugboat(Transform parent, Color hullColor, float s, int variant)
    {
        float hL = 0.7f * s, hW = 0.3f * s, hH = 0.1f * s;

        // Hull (chunky)
        Prim(PrimitiveType.Cube, "Hull", parent,
            Vector3.up * hH * 0.5f, new Vector3(hW, hH, hL), hullColor);

        // Bow
        var bow = Prim(PrimitiveType.Cube, "Bow", parent,
            new Vector3(0, hH * 0.4f, hL * 0.45f),
            new Vector3(hW * 0.5f, hH * 0.7f, hW * 0.5f), hullColor);
        bow.transform.localRotation = Quaternion.Euler(0, 45, 0);

        // Large cabin
        float cabH = hH * 1.5f;
        Prim(PrimitiveType.Cube, "Cabin", parent,
            new Vector3(0, hH + cabH * 0.5f, -hL * 0.05f),
            new Vector3(hW * 0.75f, cabH, hL * 0.35f), COL_CABIN);

        // Railing around deck
        Prim(PrimitiveType.Cube, "Rail_F", parent,
            new Vector3(0, hH + 0.02f * s, hL * 0.35f),
            new Vector3(hW * 0.85f, 0.02f * s, 0.01f * s), COL_RAILING);

        // Big funnel
        float fH = hH * 2.2f;
        Prim(PrimitiveType.Cylinder, "Funnel", parent,
            new Vector3(0, hH + fH * 0.5f, -hL * 0.18f),
            new Vector3(0.07f * s, fH * 0.5f, 0.07f * s), COL_FUNNEL);

        // Tow tire bumpers (cylinders on sides)
        for (int side = -1; side <= 1; side += 2)
        {
            Prim(PrimitiveType.Cylinder, $"Bumper_{(side > 0 ? "R" : "L")}", parent,
                new Vector3(hW * 0.5f * side, hH * 0.5f, hL * 0.1f),
                new Vector3(0.06f * s, 0.015f * s, 0.06f * s), Hex("1a1a1a"));
        }
    }

    void BuildFishingBoat(Transform parent, Color hullColor, float s, int variant)
    {
        float hL = 0.8f * s, hW = 0.22f * s, hH = 0.08f * s;

        // Hull
        Prim(PrimitiveType.Cube, "Hull", parent,
            Vector3.up * hH * 0.5f, new Vector3(hW, hH, hL), hullColor);

        // Bow
        var bow = Prim(PrimitiveType.Cube, "Bow", parent,
            new Vector3(0, hH * 0.4f, hL * 0.46f),
            new Vector3(hW * 0.4f, hH * 0.6f, hW * 0.5f), hullColor);
        bow.transform.localRotation = Quaternion.Euler(0, 45, 0);

        // Small cabin
        Prim(PrimitiveType.Cube, "Cabin", parent,
            new Vector3(0, hH + hH * 0.6f, -hL * 0.15f),
            new Vector3(hW * 0.6f, hH * 1.2f, hL * 0.2f),
            (variant % 2 == 0) ? COL_CABIN : COL_CABIN_2);

        // Fishing boom / outrigger pole
        float poleH = hH * 4f;
        Prim(PrimitiveType.Cylinder, "Pole", parent,
            new Vector3(0, hH + poleH * 0.5f, hL * 0.05f),
            new Vector3(0.015f * s, poleH * 0.5f, 0.015f * s), COL_MAST);

        // Cross beam
        Prim(PrimitiveType.Cube, "CrossBeam", parent,
            new Vector3(0, hH + poleH * 0.85f, hL * 0.05f),
            new Vector3(hW * 1.5f, 0.012f * s, 0.012f * s), COL_MAST);

        // Antenna
        Prim(PrimitiveType.Cylinder, "Antenna", parent,
            new Vector3(0.03f * s, hH + hH * 1.5f, -hL * 0.15f),
            new Vector3(0.008f * s, hH * 1.5f, 0.008f * s), COL_MAST);
    }

    Transform BuildWake(Transform parent, float scale)
    {
        float wl = WAKE_LENGTH * scale;
        float ww = WAKE_WIDTH * scale;

        var wake = Prim(PrimitiveType.Cube, "Wake", parent,
            new Vector3(0f, -0.01f, -0.8f * scale - wl * 0.5f),
            new Vector3(ww, 0.005f, wl), COL_WAKE);

        return wake.transform;
    }

    GameObject Prim(PrimitiveType type, string name, Transform parent,
                    Vector3 localPos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        SetMat(go, color);

        var col = go.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);
        _allSpawned.Add(go);
        return go;
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    void AnimateBoats()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < _boats.Count; i++)
        {
            var b = _boats[i];
            if (b.root == null) continue;

            Vector3 target = b.waypoints[b.nextWP];
            Vector3 pos    = b.root.position;
            Vector3 dir    = target - pos;
            dir.y = 0f;
            float dist = dir.magnitude;

            if (dist < 0.15f)
            {
                b.nextWP = (b.nextWP + 1) % b.waypoints.Length;
                target = b.waypoints[b.nextWP];
                dir = target - pos;
                dir.y = 0f;
                dist = dir.magnitude;
            }

            if (dist > 0.01f)
            {
                Vector3 move = (dir / dist) * b.speed * dt;
                if (move.magnitude > dist) move = dir;
                pos += move;
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                b.root.rotation = Quaternion.Slerp(b.root.rotation, targetRot, dt * 3f);
            }

            float bob = Mathf.Sin(Time.time * BOB_FREQUENCY + b.bobPhase) * BOB_AMPLITUDE;
            pos.y = b.baseY + bob;
            float roll = Mathf.Sin(Time.time * BOB_FREQUENCY * 0.7f + b.bobPhase + 1f) * 2.5f;
            Vector3 euler = b.root.rotation.eulerAngles;
            b.root.rotation = Quaternion.Euler(euler.x, euler.y, roll);
            b.root.position = pos;

            if (b.wake != null)
            {
                var r = b.wake.GetComponent<Renderer>();
                if (r != null && r.material != null)
                {
                    float alpha = 0.18f + 0.12f * Mathf.Sin(Time.time * 2f + b.bobPhase);
                    Color c = r.material.color;
                    c.a = alpha;
                    r.material.color = c;
                }
            }
        }
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    void ClearAll()
    {
        _boats.Clear();
        foreach (var go in _allSpawned)
            if (go != null) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
        _allSpawned.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        _lastGridW = _lastGridH = -1;
        _lastCellSize = -1f;
    }

    // ── Material helper ───────────────────────────────────────────────────────

    static void SetMat(GameObject go, Color col)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var m = new Material(FindShader());
        m.hideFlags = HideFlags.HideAndDontSave;
        bool transparent = col.a < 0.99f;
        if (transparent)
        {
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 0f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            m.SetInt("_ZWrite", 1);
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        if (m.HasProperty("_Color"))     m.SetColor("_Color",     col);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
        if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic",   0f);
        r.material = m;
    }

    static Shader FindShader()
    {
        string[] c = { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Standard", "Unlit/Color" };
        for (int i = 0; i < c.Length; i++) { var s = Shader.Find(c[i]); if (s != null && s.isSupported) return s; }
        return Shader.Find("Unlit/Color");
    }

    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out Color c); return c; }
}
