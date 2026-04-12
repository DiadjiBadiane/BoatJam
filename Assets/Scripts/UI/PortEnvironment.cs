// Assets/Scripts/Rendering/PortEnvironment.cs
// Procedurally spawns a rich, realistic top-down port around the puzzle grid.
// Inspired by container port imagery: gantry cranes, stacked containers,
// warehouses, lighthouse, buoys, docked cargo ship, bollards, safety lines.
// No external assets required — uses only primitive meshes + runtime materials.

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
    [SerializeField] float dockWidth         = 0.55f;
    [SerializeField] float dockInset         = 0.10f;

    [Header("Bollards")]
    [SerializeField] float bollardRadius     = 0.14f;
    [SerializeField] float bollardHeight     = 0.30f;
    [SerializeField] float bollardSpacingFraction = 0.33f;

    [Header("Safety Line")]
    [SerializeField] float safetyLineWidth   = 0.06f;
    [SerializeField] float safetyLineDashLen = 0.45f;
    [SerializeField] float safetyLineGapLen  = 0.30f;

    // ── Colors ────────────────────────────────────────────────────────────────
    static readonly Color COL_WOOD_LIGHT  = Hex("a07830");
    static readonly Color COL_WOOD_DARK   = Hex("7a5010");
    static readonly Color COL_BOLLARD     = Hex("1a1005");
    static readonly Color COL_BOLLARD_CAP = Hex("50380a");
    static readonly Color COL_SAFETY      = Hex("e8c830");
    static readonly Color COL_CRANE       = Hex("6b7280");  // steel grey
    static readonly Color COL_CRANE_ACCENT= Hex("ef4444");  // red accent
    static readonly Color COL_WAREHOUSE   = Hex("4a5060");
    static readonly Color COL_WHOUSE_ROOF = Hex("3a3f4a");
    static readonly Color COL_CONTAINER_1 = Hex("e67e22");
    static readonly Color COL_CONTAINER_2 = Hex("2980b9");
    static readonly Color COL_CONTAINER_3 = Hex("27ae60");
    static readonly Color COL_CONTAINER_4 = Hex("c0392b");
    static readonly Color COL_CONTAINER_5 = Hex("f1c40f");
    static readonly Color COL_CONTAINER_6 = Hex("8e44ad");
    static readonly Color COL_LIGHTHOUSE  = Hex("ecf0f1");
    static readonly Color COL_LH_STRIPE   = Hex("e74c3c");
    static readonly Color COL_LH_LIGHT    = Hex("f39c12");
    static readonly Color COL_BUOY_RED    = Hex("e74c3c");
    static readonly Color COL_BUOY_GREEN  = Hex("2ecc71");
    static readonly Color COL_DOCK_SHIP   = Hex("2c3e50");
    static readonly Color COL_SHIP_CABIN  = Hex("ecf0f1");
    static readonly Color COL_SHIP_FUNNEL = Hex("34495e");
    static readonly Color COL_CONCRETE    = Hex("9ca3af");
    static readonly Color COL_ASPHALT     = Hex("6b7280");
    static readonly Color COL_ROAD_LINE   = Hex("fbbf24");

    // ── Runtime ───────────────────────────────────────────────────────────────
    readonly List<GameObject> _spawned = new List<GameObject>();
    int _lastGridW = -1, _lastGridH = -1;
    float _lastCellSize = -1f;

    Material _matWood, _matBollard, _matBollardCap, _matSafety, _matCrane, _matCraneAccent;
    Material _matWarehouse, _matRoof, _matConcrete, _matAsphalt, _matRoadLine;
    Material _matLighthouse, _matLHStripe, _matLHLight;
    Material[] _matContainers;

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

        float x0 = -dockInset;
        float z0 = -dockInset;
        float x1 = worldW + dockInset;
        float z1 = worldH + dockInset;

        BuildDockBorder(x0, z0, x1, z1);
        BuildBollards(x0, z0, x1, z1);
        BuildSafetyLine(x0, z0, x1, z1);
        BuildQuayside(x0, z0, x1, z1);
        BuildGantryCranes(x0, z0, x1, z1);
        BuildContainerYard(x0, z0, x1, z1);
        BuildWarehouses(x0, z0, x1, z1);
        BuildDockedShip(x0, z0, x1, z1);
        BuildLighthouse(x0, z0, x1, z1);
        BuildBuoys(x0, z0, x1, z1);
    }

    // ── Quayside (concrete apron around dock) ─────────────────────────────────

    void BuildQuayside(float x0, float z0, float x1, float z1)
    {
        float w = x1 - x0;
        float h = z1 - z0;
        float apronW = 1.8f; // width of concrete area around dock

        // Top apron (behind grid)
        SpawnCube("Apron_Top", new Vector3(x0 + w * 0.5f, -0.01f, z1 + dockWidth + apronW * 0.5f),
            new Vector3(w + apronW * 2f + dockWidth * 2f, 0.02f, apronW), _matConcrete);

        // Bottom apron
        SpawnCube("Apron_Bot", new Vector3(x0 + w * 0.5f, -0.01f, z0 - dockWidth - apronW * 0.5f),
            new Vector3(w + apronW * 2f + dockWidth * 2f, 0.02f, apronW), _matConcrete);

        // Left apron
        SpawnCube("Apron_Left", new Vector3(x0 - dockWidth - apronW * 0.5f, -0.01f, z0 + h * 0.5f),
            new Vector3(apronW, 0.02f, h + dockWidth * 2f), _matConcrete);

        // Right apron
        SpawnCube("Apron_Right", new Vector3(x1 + dockWidth + apronW * 0.5f, -0.01f, z0 + h * 0.5f),
            new Vector3(apronW, 0.02f, h + dockWidth * 2f), _matConcrete);

        // Road markings on top apron
        float roadZ = z1 + dockWidth + apronW * 0.7f;
        float dashLen = 0.5f, gapLen = 0.4f;
        float cursor = x0 - apronW;
        int idx = 0;
        while (cursor < x1 + apronW)
        {
            SpawnCube($"Road_{idx}", new Vector3(cursor + dashLen * 0.5f, 0.001f, roadZ),
                new Vector3(dashLen, 0.005f, 0.06f), _matRoadLine);
            cursor += dashLen + gapLen;
            idx++;
        }
    }

    // ── Gantry cranes (container port style, like reference image) ────────────

    void BuildGantryCranes(float x0, float z0, float x1, float z1)
    {
        float craneZ = z1 + dockWidth + 0.5f;
        float gridW = x1 - x0;
        int craneCount = gridW > 5f ? 2 : 1;
        float spacing = gridW / (craneCount + 1);

        for (int i = 0; i < craneCount; i++)
        {
            float cx = x0 + spacing * (i + 1);
            BuildSingleGantryCrane($"GantryCrane_{i}", cx, craneZ);
        }

        // One crane on the left side
        float leftX = x0 - dockWidth - 1.2f;
        float leftZ = z0 + (z1 - z0) * 0.4f;
        BuildSingleGantryCrane("GantryCrane_Left", leftX, leftZ, true);
    }

    void BuildSingleGantryCrane(string name, float cx, float cz, bool rotated = false)
    {
        float legH    = 3.5f;
        float legW    = 0.18f;
        float boomLen = 4.0f;
        float boomH   = 0.2f;
        float span    = 1.8f; // distance between legs
        float trolleyOffset = boomLen * 0.3f;

        // Four legs (two front, two back pairs)
        float halfSpan = span * 0.5f;
        float dz = rotated ? 0f : halfSpan;
        float dx = rotated ? halfSpan : 0f;

        Vector3[] legPositions = {
            new Vector3(cx - dx, legH * 0.5f, cz - dz),
            new Vector3(cx + dx, legH * 0.5f, cz + dz),
            new Vector3(cx - dx - (rotated ? 0f : 0.4f), legH * 0.5f, cz - dz - (rotated ? 0.4f : 0f)),
            new Vector3(cx + dx - (rotated ? 0f : 0.4f), legH * 0.5f, cz + dz - (rotated ? 0.4f : 0f)),
        };

        for (int l = 0; l < 4; l++)
            SpawnCube($"{name}_Leg{l}", legPositions[l], new Vector3(legW, legH, legW), _matCrane);

        // Top cross-beam connecting legs
        float beamY = legH;
        Vector3 beamSize = rotated
            ? new Vector3(span + 0.4f, boomH, 0.5f)
            : new Vector3(0.5f, boomH, span + 0.4f);
        SpawnCube($"{name}_Beam", new Vector3(cx, beamY, cz), beamSize, _matCrane);

        // Horizontal boom (extending over water)
        float boomDir = rotated ? -1f : 1f;
        Vector3 boomPos = rotated
            ? new Vector3(cx - boomLen * 0.3f, beamY + boomH, cz)
            : new Vector3(cx, beamY + boomH, cz - boomLen * 0.3f);
        Vector3 boomSize = rotated
            ? new Vector3(boomLen, boomH * 0.5f, 0.15f)
            : new Vector3(0.15f, boomH * 0.5f, boomLen);
        SpawnCube($"{name}_Boom", boomPos, boomSize, _matCrane);

        // Counter-boom (shorter, opposite direction)
        Vector3 counterPos = rotated
            ? new Vector3(cx + boomLen * 0.25f, beamY + boomH, cz)
            : new Vector3(cx, beamY + boomH, cz + boomLen * 0.25f);
        Vector3 counterSize = rotated
            ? new Vector3(boomLen * 0.4f, boomH * 0.5f, 0.12f)
            : new Vector3(0.12f, boomH * 0.5f, boomLen * 0.4f);
        SpawnCube($"{name}_Counter", counterPos, counterSize, _matCrane);

        // Red accent stripe on boom
        Vector3 accentPos = rotated
            ? new Vector3(cx - boomLen * 0.5f, beamY + boomH + 0.02f, cz)
            : new Vector3(cx, beamY + boomH + 0.02f, cz - boomLen * 0.5f);
        Vector3 accentSize = rotated
            ? new Vector3(boomLen * 0.15f, boomH * 0.3f, 0.17f)
            : new Vector3(0.17f, boomH * 0.3f, boomLen * 0.15f);
        SpawnCube($"{name}_Accent", accentPos, accentSize, _matCraneAccent);

        // Cable hanging from trolley
        float cableLen = legH * 0.6f;
        Vector3 cablePos = rotated
            ? new Vector3(cx - trolleyOffset, beamY - cableLen * 0.5f, cz)
            : new Vector3(cx, beamY - cableLen * 0.5f, cz - trolleyOffset);
        SpawnCube($"{name}_Cable", cablePos, new Vector3(0.03f, cableLen, 0.03f), _matCrane);

        // Spreader (hook) at cable end
        Vector3 spreaderPos = rotated
            ? new Vector3(cx - trolleyOffset, beamY - cableLen, cz)
            : new Vector3(cx, beamY - cableLen, cz - trolleyOffset);
        SpawnCube($"{name}_Spreader", spreaderPos, new Vector3(0.4f, 0.06f, 0.2f), _matCraneAccent);

        // Operator cabin
        Vector3 cabinPos = new Vector3(cx, beamY - 0.3f, cz);
        SpawnCube($"{name}_Cabin", cabinPos, new Vector3(0.4f, 0.35f, 0.35f), _matCraneAccent);
    }

    // ── Container yard (stacked containers) ───────────────────────────────────

    void BuildContainerYard(float x0, float z0, float x1, float z1)
    {
        Material[] cMats = _matContainers;
        float cW = 0.45f, cH = 0.22f, cL = 0.9f;

        // Container stacks behind the grid (top side)
        float yardZ = z1 + dockWidth + 2.5f;
        int columns = Mathf.FloorToInt((x1 - x0) / (cL + 0.15f));
        int ci = 0;
        for (int col = 0; col < columns; col++)
        {
            float cx = x0 + 0.5f + col * (cL + 0.15f);
            int stackH = (col % 3 == 0) ? 3 : 2;
            for (int row = 0; row < 2; row++)
            {
                float rz = yardZ + row * (cW + 0.1f);
                for (int s = 0; s < stackH; s++)
                {
                    float cy = s * cH + cH * 0.5f;
                    SpawnCube($"Container_{ci}", new Vector3(cx, cy, rz),
                        new Vector3(cL, cH, cW), cMats[ci % cMats.Length]);
                    ci++;
                }
            }
        }

        // Some containers on the left side too
        float leftX = x0 - dockWidth - 2.2f;
        for (int r = 0; r < 3; r++)
        {
            float rz = z0 + 1.0f + r * (cW + 0.15f);
            int stackH = (r == 1) ? 3 : 2;
            for (int s = 0; s < stackH; s++)
            {
                float cy = s * cH + cH * 0.5f;
                SpawnCube($"ContainerL_{ci}", new Vector3(leftX, cy, rz),
                    new Vector3(cL, cH, cW), cMats[ci % cMats.Length]);
                ci++;
            }
        }
    }

    // ── Docked cargo ship (on the right/bottom side) ──────────────────────────

    void BuildDockedShip(float x0, float z0, float x1, float z1)
    {
        float shipZ = z0 - dockWidth - 2.2f;  // below the grid
        float shipCX = x0 + (x1 - x0) * 0.5f;
        float sL = 4.5f, sW = 0.9f, sH = 0.3f;

        // Hull
        SpawnCube("DockedShip_Hull",
            new Vector3(shipCX, sH * 0.5f, shipZ), new Vector3(sW, sH, sL),
            MakeMat(COL_DOCK_SHIP, 0.1f));

        // Bow
        var bow = SpawnCube("DockedShip_Bow",
            new Vector3(shipCX, sH * 0.35f, shipZ + sL * 0.47f),
            new Vector3(sW * 0.4f, sH * 0.7f, sW * 0.5f), MakeMat(COL_DOCK_SHIP, 0.1f));
        bow.transform.localRotation = Quaternion.Euler(0, 45, 0);

        // Deck
        SpawnCube("DockedShip_Deck",
            new Vector3(shipCX, sH, shipZ), new Vector3(sW * 0.9f, 0.02f, sL * 0.85f),
            MakeMat(COL_CONCRETE, 0.05f));

        // Bridge at stern
        float brH = sH * 1.6f;
        SpawnCube("DockedShip_Bridge",
            new Vector3(shipCX, sH + brH * 0.5f, shipZ - sL * 0.35f),
            new Vector3(sW * 0.6f, brH, sL * 0.12f), MakeMat(COL_SHIP_CABIN, 0.15f));

        // Funnel
        float fH = sH * 2.0f;
        var funnel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        funnel.name = "DockedShip_Funnel";
        funnel.transform.SetParent(transform);
        funnel.transform.position = new Vector3(shipCX, sH + fH * 0.5f, shipZ - sL * 0.28f);
        funnel.transform.localScale = new Vector3(0.15f, fH * 0.5f, 0.15f);
        funnel.GetComponent<Renderer>().sharedMaterial = MakeMat(COL_SHIP_FUNNEL, 0.1f);
        DestroyCollider(funnel);
        _spawned.Add(funnel);

        // Containers on deck
        Material[] cMats = _matContainers;
        float cW = 0.25f, cH = 0.12f, cL = 0.55f;
        int ci = 7; // offset so different colors from yard
        for (int row = 0; row < 4; row++)
        {
            float cz = shipZ + sL * 0.15f - row * (cL * 0.35f);
            for (int s = 0; s < 2; s++)
            {
                SpawnCube($"ShipCont_{ci}",
                    new Vector3(shipCX, sH + 0.02f + s * cH + cH * 0.5f, cz),
                    new Vector3(cW, cH, cL * 0.3f), cMats[ci % cMats.Length]);
                ci++;
            }
        }
    }

    // ── Lighthouse ────────────────────────────────────────────────────────────

    void BuildLighthouse(float x0, float z0, float x1, float z1)
    {
        float lhX = x1 + dockWidth + 3.5f;
        float lhZ = z1 + dockWidth + 2.0f;
        float baseR = 0.3f, lhH = 3.0f;

        // Base platform
        SpawnCube("LH_Platform", new Vector3(lhX, 0.05f, lhZ),
            new Vector3(1.2f, 0.1f, 1.2f), _matConcrete);

        // Tower (tapered — bottom cylinder wider)
        var towerBot = SpawnCylinder("LH_TowerBot", new Vector3(lhX, lhH * 0.25f, lhZ),
            new Vector3(baseR * 2f, lhH * 0.5f, baseR * 2f), _matLighthouse);

        var towerTop = SpawnCylinder("LH_TowerTop", new Vector3(lhX, lhH * 0.65f, lhZ),
            new Vector3(baseR * 1.5f, lhH * 0.3f, baseR * 1.5f), _matLighthouse);

        // Red stripes
        for (int s = 0; s < 3; s++)
        {
            float sy = 0.5f + s * 0.7f;
            SpawnCylinder($"LH_Stripe{s}", new Vector3(lhX, sy, lhZ),
                new Vector3(baseR * 2.05f, 0.08f, baseR * 2.05f), _matLHStripe);
        }

        // Light housing
        SpawnCylinder("LH_Light", new Vector3(lhX, lhH, lhZ),
            new Vector3(baseR * 1.8f, 0.25f, baseR * 1.8f), _matLHLight);

        // Cap
        SpawnCylinder("LH_Cap", new Vector3(lhX, lhH + 0.15f, lhZ),
            new Vector3(baseR * 2.2f, 0.06f, baseR * 2.2f), _matCrane);
    }

    // ── Navigation buoys ──────────────────────────────────────────────────────

    void BuildBuoys(float x0, float z0, float x1, float z1)
    {
        float w = x1 - x0;
        float h = z1 - z0;

        // Red buoy markers (port side)
        Vector3[] redPositions = {
            new Vector3(x0 - dockWidth - 3f, 0.12f, z0 + h * 0.3f),
            new Vector3(x0 - dockWidth - 4f, 0.12f, z1 - h * 0.1f),
        };

        // Green buoy markers (starboard side)
        Vector3[] greenPositions = {
            new Vector3(x1 + dockWidth + 3f, 0.12f, z0 + h * 0.2f),
            new Vector3(x1 + dockWidth + 4.5f, 0.12f, z0 + h * 0.7f),
        };

        for (int i = 0; i < redPositions.Length; i++)
            BuildSingleBuoy($"Buoy_R{i}", redPositions[i], COL_BUOY_RED);
        for (int i = 0; i < greenPositions.Length; i++)
            BuildSingleBuoy($"Buoy_G{i}", greenPositions[i], COL_BUOY_GREEN);
    }

    void BuildSingleBuoy(string name, Vector3 pos, Color col)
    {
        Material mat = MakeMat(col, 0.3f);
        // Body
        SpawnCylinder($"{name}_Body", pos, new Vector3(0.18f, 0.12f, 0.18f), mat);
        // Top light
        var topMat = MakeMat(Color.Lerp(col, Color.white, 0.5f), 0.5f);
        SpawnCylinder($"{name}_Light", pos + Vector3.up * 0.15f, new Vector3(0.08f, 0.06f, 0.08f), topMat);
    }

    // ── Dock border ───────────────────────────────────────────────────────────

    void BuildDockBorder(float x0, float z0, float x1, float z1)
    {
        float w = x1 - x0;
        float h = z1 - z0;

        SpawnPlank("Dock_Top", new Vector3(x0 + w * 0.5f, 0f, z1 + dockWidth * 0.5f),
            new Vector3(w + dockWidth * 2f, 1f, dockWidth));
        SpawnPlank("Dock_Bot", new Vector3(x0 + w * 0.5f, 0f, z0 - dockWidth * 0.5f),
            new Vector3(w + dockWidth * 2f, 1f, dockWidth));
        SpawnPlank("Dock_Left", new Vector3(x0 - dockWidth * 0.5f, 0f, z0 + h * 0.5f),
            new Vector3(dockWidth, 1f, h));
        SpawnPlank("Dock_Right", new Vector3(x1 + dockWidth * 0.5f, 0f, z0 + h * 0.5f),
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
        float dockCentreTop   = z1 + dockWidth * 0.5f;
        float dockCentreBot   = z0 - dockWidth * 0.5f;
        float dockCentreLeft  = x0 - dockWidth * 0.5f;
        float dockCentreRight = x1 + dockWidth * 0.5f;

        int countX = Mathf.Max(2, Mathf.FloorToInt((x1 - x0) / (bollardRadius * 2f + (x1 - x0) * bollardSpacingFraction)));
        for (int i = 0; i <= countX; i++)
        {
            float t = (float)i / countX;
            float x = Mathf.Lerp(x0 + dockWidth * 0.5f, x1 - dockWidth * 0.5f, t);
            SpawnBollard(new Vector3(x, 0f, dockCentreTop));
            SpawnBollard(new Vector3(x, 0f, dockCentreBot));
        }

        int countZ = Mathf.Max(2, Mathf.FloorToInt((z1 - z0) / (bollardRadius * 2f + (z1 - z0) * bollardSpacingFraction)));
        for (int i = 1; i < countZ; i++)
        {
            float t = (float)i / countZ;
            float z = Mathf.Lerp(z0 + dockWidth * 0.5f, z1 - dockWidth * 0.5f, t);
            SpawnBollard(new Vector3(dockCentreLeft,  0f, z));
            SpawnBollard(new Vector3(dockCentreRight, 0f, z));
        }
    }

    void SpawnBollard(Vector3 base_)
    {
        SpawnCylinder("Bollard_Body", base_ + Vector3.up * (bollardHeight * 0.5f),
            new Vector3(bollardRadius * 2f, bollardHeight, bollardRadius * 2f), _matBollard);
        SpawnCylinder("Bollard_Cap", base_ + Vector3.up * bollardHeight,
            new Vector3(bollardRadius * 2.4f, bollardHeight * 0.12f, bollardRadius * 2.4f), _matBollardCap);
    }

    // ── Safety line ───────────────────────────────────────────────────────────

    void BuildSafetyLine(float x0, float z0, float x1, float z1)
    {
        float inset = dockWidth * 0.7f;
        float y = 0.005f;
        BuildDashedLine(x0, x1, z1 + inset, y);
        BuildDashedLine(x0, x1, z0 - inset, y);
        BuildDashedLineZ(z0, z1, x0 - inset, y);
        BuildDashedLineZ(z0, z1, x1 + inset, y);
    }

    void BuildDashedLine(float x0, float x1, float z, float y)
    {
        float cursor = x0; bool dash = true; int idx = 0;
        while (cursor < x1)
        {
            float segLen = Mathf.Min(dash ? safetyLineDashLen : safetyLineGapLen, x1 - cursor);
            if (dash)
                SpawnCube($"SafetyLine_{idx}", new Vector3(cursor + segLen * 0.5f, y, z),
                    new Vector3(segLen, 0.001f, safetyLineWidth), _matSafety);
            cursor += segLen; dash = !dash; idx++;
        }
    }

    void BuildDashedLineZ(float z0, float z1, float x, float y)
    {
        float cursor = z0; bool dash = true; int idx = 0;
        while (cursor < z1)
        {
            float segLen = Mathf.Min(dash ? safetyLineDashLen : safetyLineGapLen, z1 - cursor);
            if (dash)
                SpawnCube($"SafetyLineZ_{idx}", new Vector3(x, y, cursor + segLen * 0.5f),
                    new Vector3(safetyLineWidth, 0.001f, segLen), _matSafety);
            cursor += segLen; dash = !dash; idx++;
        }
    }

    // ── Warehouses ────────────────────────────────────────────────────────────

    void BuildWarehouses(float x0, float z0, float x1, float z1)
    {
        float totalW = x1 - x0;
        int whCount = 3;
        float gapW = totalW / whCount;
        float backZ = z1 + dockWidth + 4.5f;
        float whH = 0.8f;
        float whD = 1.6f;

        for (int i = 0; i < whCount; i++)
        {
            float cx = x0 + gapW * (i + 0.5f);
            float ww = gapW * 0.72f;

            SpawnCube($"Warehouse_{i}", new Vector3(cx, whH * 0.5f, backZ),
                new Vector3(ww, whH, whD), _matWarehouse);
            // Roof
            SpawnCube($"Warehouse_{i}_Roof", new Vector3(cx, whH + 0.02f, backZ),
                new Vector3(ww, 0.04f, whD), _matRoof);
            // Door
            SpawnCube($"Warehouse_{i}_Door", new Vector3(cx, whH * 0.3f, backZ - whD * 0.51f),
                new Vector3(ww * 0.25f, whH * 0.5f, 0.02f), MakeMat(COL_WOOD_DARK, 0.05f));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    GameObject SpawnCube(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        DestroyCollider(go);
        _spawned.Add(go);
        return go;
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

    // ── Materials ─────────────────────────────────────────────────────────────

    void EnsureMaterials()
    {
        _matWood       = MakeMat(COL_WOOD_LIGHT,   0.1f);
        _matBollard    = MakeMat(COL_BOLLARD,       0.05f);
        _matBollardCap = MakeMat(COL_BOLLARD_CAP,   0.3f);
        _matSafety     = MakeMat(COL_SAFETY,        0.05f);
        _matCrane      = MakeMat(COL_CRANE,         0.2f);
        _matCraneAccent= MakeMat(COL_CRANE_ACCENT,  0.15f);
        _matWarehouse  = MakeMat(COL_WAREHOUSE,     0.05f);
        _matRoof       = MakeMat(COL_WHOUSE_ROOF,   0.05f);
        _matConcrete   = MakeMat(COL_CONCRETE,      0.05f);
        _matAsphalt    = MakeMat(COL_ASPHALT,       0.03f);
        _matRoadLine   = MakeMat(COL_ROAD_LINE,     0.05f);
        _matLighthouse = MakeMat(COL_LIGHTHOUSE,    0.15f);
        _matLHStripe   = MakeMat(COL_LH_STRIPE,     0.1f);
        _matLHLight    = MakeMat(COL_LH_LIGHT,      0.6f);

        _matContainers = new Material[] {
            MakeMat(COL_CONTAINER_1, 0.1f), MakeMat(COL_CONTAINER_2, 0.1f),
            MakeMat(COL_CONTAINER_3, 0.1f), MakeMat(COL_CONTAINER_4, 0.1f),
            MakeMat(COL_CONTAINER_5, 0.1f), MakeMat(COL_CONTAINER_6, 0.1f),
        };
    }

    Material MakeMat(Color col, float smoothness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Standard");
        var mat = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave,
            color     = col
        };
        if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor", col);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        return mat;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    void ClearAll()
    {
        foreach (var go in _spawned) if (go != null) SafeDestroy(go);
        _spawned.Clear();

        SafeDestroy(_matWood);  SafeDestroy(_matBollard); SafeDestroy(_matBollardCap);
        SafeDestroy(_matSafety); SafeDestroy(_matCrane); SafeDestroy(_matCraneAccent);
        SafeDestroy(_matWarehouse); SafeDestroy(_matRoof);
        SafeDestroy(_matConcrete); SafeDestroy(_matAsphalt); SafeDestroy(_matRoadLine);
        SafeDestroy(_matLighthouse); SafeDestroy(_matLHStripe); SafeDestroy(_matLHLight);

        if (_matContainers != null)
            foreach (var m in _matContainers) SafeDestroy(m);

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