// Assets/Scripts/Level/LevelLoader.cs
using UnityEngine;
using System.Collections.Generic;

public class LevelLoader : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject heroBoardPrefab;   // Boat_Hero prefab (size 2)
    public GameObject boatSize2Prefab;   // 2x1 boat prefab
    public GameObject boatSize4Prefab;   // 4x1 boat prefab
    public GameObject boatSize2x2Prefab; // 2x2 boat prefab
    public GameObject boatSize4x2Prefab; // 4x2 boat prefab

    [Header("Prefab Variety")]
    [Tooltip("If enabled, boats can be spawned from prefab pools for visual variety.")]
    public bool enablePrefabVariety = true;

    [Tooltip("Use deterministic selection so the same level always gets the same boat variants.")]
    public bool deterministicVariety = true;

    [Tooltip("Seed used when deterministic variety is enabled.")]
    public int varietySeed = 1337;

    [Tooltip("Optional hero prefab variants. If empty, heroBoardPrefab is used.")]
    public GameObject[] heroPrefabPool;

    [Tooltip("Optional 2x1 prefab variants. If empty, boatSize2Prefab is used.")]
    public GameObject[] boatSize2PrefabPool;

    [Tooltip("Optional 4x1 prefab variants. If empty, boatSize4Prefab is used.")]
    public GameObject[] boatSize4PrefabPool;

    [Tooltip("Optional 2x2 prefab variants. If empty, boatSize2x2Prefab is used.")]
    public GameObject[] boatSize2x2PrefabPool;

    [Tooltip("Optional 4x2 prefab variants. If empty, boatSize4x2Prefab is used.")]
    public GameObject[] boatSize4x2PrefabPool;

    [Header("Boat Visual Fit")]
    [Tooltip("If enabled, every spawned boat is auto-fitted to grid size at runtime.")]
    public bool enforceRuntimeBoatFit = true;

    [Tooltip("Scale multiplier for regular (non-hero) boats after grid fitting.")]
    [Range(0.5f, 4.0f)] public float runtimeScaleFactor = 1.15f;

    [Tooltip("Scale multiplier for the hero boat after grid fitting. Increase this if your hero prefab mesh appears too small.")]
    [Range(0.5f, 4.0f)] public float heroScaleFactor = 2.5f;

    [Tooltip("Height used for generated BoxCollider on spawned boats.")]
    public float runtimeColliderHeight = 0.4f;

    [Header("References")]
    [Tooltip("Drag the HarborGrid object here so cellSize stays in sync")]
    public HarborGrid harborGrid;

    private GameObject[] _spawnedBoats;

    void Awake()
    {
        if (harborGrid == null)
            harborGrid = FindObjectOfType<HarborGrid>();
    }

    public void LoadLevel(LevelData levelData)
    {
        ClearLevel();

        // ── Sync grid dimensions ──────────────────────────────────────────────
        GridManager.Instance.width    = levelData.gridWidth;
        GridManager.Instance.height   = levelData.gridHeight;

        // ── CRITICAL: sync cellSize from HarborGrid into GridManager ──────────
        // ResponsiveCameraFitter reads GridManager.cellSize to compute world size.
        // HarborGrid has its own cellSize field — they MUST match.
        if (harborGrid != null)
        {
            GridManager.Instance.cellSize = harborGrid.cellSize;
            harborGrid.RebuildVisuals();
        }

        GridManager.Instance.ClearGrid();
        Debug.Log($"LevelLoader set grid to {GridManager.Instance.width}x{GridManager.Instance.height}" +
                  $" cellSize={GridManager.Instance.cellSize}");

        _spawnedBoats = new GameObject[levelData.boats.Count];

        for (int i = 0; i < levelData.boats.Count; i++)
        {
            BoatData data = levelData.boats[i];

            if (data.size < 1)
            {
                Debug.LogWarning($"Boat '{data.id}' has invalid size {data.size}. Enforcing size = 1.");
                data.size = 1;
            }

            if (data.width < 1)
                data.width = 1;

            GameObject prefab = SelectPrefabForBoat(data);
            if (prefab == null)
            {
                Debug.LogError($"No prefab available for boat '{data.id}' (size={data.size}, isHero={data.isHero}).");
                continue;
            }

            GameObject go     = Instantiate(prefab, Vector3.zero, GetRotation(data));
            go.name = data.id;

            BoatMovement bm = go.GetComponent<BoatMovement>();
            if (bm == null)
            {
                Debug.LogError($"Prefab '{prefab.name}' for boat '{data.id}' is missing BoatMovement. Destroying.");
                Destroy(go);
                continue;
            }

            bm.boatId       = data.id;
            bm.size         = data.size;
            bm.width        = data.width;
            bm.isHorizontal = data.isHorizontal;
            bm.isHero       = data.isHero;

            if (enforceRuntimeBoatFit)
            {
                var fitter = go.GetComponent<BoatMeshFitter>();
                if (fitter == null)
                    fitter = go.AddComponent<BoatMeshFitter>();

                fitter.ApplyFitSettings(bm.size, bm.width, bm.isHero ? heroScaleFactor : runtimeScaleFactor, runtimeColliderHeight);
            }

            Vector2Int startPos = new Vector2Int(data.col, data.row);
            Vector2Int clamped  = GridManager.Instance.GetValidPosition(bm, startPos);
            if (clamped != startPos)
            {
                Debug.LogWarning($"Boat '{data.id}' clamped from ({startPos.x},{startPos.y}) to ({clamped.x},{clamped.y})");
                startPos = clamped;
            }

            bm.InitializePosition(startPos);
            GridManager.Instance.RegisterBoat(bm);

            _spawnedBoats[i] = go;
        }
    }

    public void ClearLevel()
    {
        if (_spawnedBoats == null) return;
        foreach (var b in _spawnedBoats)
            if (b != null) Destroy(b);
        _spawnedBoats = null;
    }

    private Quaternion GetRotation(BoatData data)
        => data.isHorizontal ? Quaternion.identity : Quaternion.Euler(0, 90, 0);

    private GameObject SelectPrefabForBoat(BoatData data)
    {
        if (data.isHero)
        {
            var heroPrefab = PickFromPool(heroPrefabPool, data, 101);
            return heroPrefab != null ? heroPrefab : heroBoardPrefab;
        }

        if (data.size == 4 && data.width == 2)
        {
            var pooled = PickFromPool(boatSize4x2PrefabPool, data, 402);
            if (pooled != null) return pooled;

            if (boatSize4x2Prefab != null)
                return boatSize4x2Prefab;

            if (boatSize4Prefab != null)
            {
                Debug.LogWarning($"Boat '{data.id}' is 4x2 but boatSize4x2Prefab is not set. Falling back to boatSize4Prefab.");
                return boatSize4Prefab;
            }

            Debug.LogWarning($"Boat '{data.id}' is 4x2 but no dedicated prefab is set. Falling back to boatSize2Prefab.");
            return boatSize2Prefab;
        }

        if (data.size == 2 && data.width == 2)
        {
            var pooled = PickFromPool(boatSize2x2PrefabPool, data, 202);
            if (pooled != null) return pooled;

            if (boatSize2x2Prefab != null)
                return boatSize2x2Prefab;

            Debug.LogWarning($"Boat '{data.id}' is 2x2 but boatSize2x2Prefab is not set. Falling back to boatSize2Prefab.");
            return boatSize2Prefab;
        }

        if (data.size == 4)
        {
            var pooled = PickFromPool(boatSize4PrefabPool, data, 401);
            if (pooled != null) return pooled;

            if (boatSize4Prefab != null)
                return boatSize4Prefab;

            Debug.LogWarning($"Boat '{data.id}' is size 4 but boatSize4Prefab is not set. Falling back to boatSize2Prefab.");
        }

        var pooled2 = PickFromPool(boatSize2PrefabPool, data, 201);
        if (pooled2 != null) return pooled2;

        return boatSize2Prefab;
    }

    private GameObject PickFromPool(GameObject[] pool, BoatData data, int salt)
    {
        if (!enablePrefabVariety || pool == null || pool.Length == 0)
            return null;

        var valid = new List<GameObject>(pool.Length);
        for (int i = 0; i < pool.Length; i++)
            if (pool[i] != null) valid.Add(pool[i]);

        if (valid.Count == 0)
            return null;

        int index;
        if (deterministicVariety)
        {
            int h = varietySeed;
            h = unchecked(h * 31 + salt);
            h = unchecked(h * 31 + data.size);
            h = unchecked(h * 31 + data.width);
            h = unchecked(h * 31 + data.col);
            h = unchecked(h * 31 + data.row);
            h = unchecked(h * 31 + (data.isHorizontal ? 1 : 0));
            h = unchecked(h * 31 + (data.id != null ? data.id.GetHashCode() : 0));
            index = (h & int.MaxValue) % valid.Count;
        }
        else
        {
            index = Random.Range(0, valid.Count);
        }

        return valid[index];
    }
}