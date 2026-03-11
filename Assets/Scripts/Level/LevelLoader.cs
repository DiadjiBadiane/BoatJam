// Assets/Scripts/Level/LevelLoader.cs
using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject heroBoardPrefab;   // Boat_Hero prefab (size 2)
    public GameObject boatSize2Prefab;   // Boat_Small prefab (size 2)

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
            GridManager.Instance.cellSize = harborGrid.cellSize;

        GridManager.Instance.ClearGrid();
        Debug.Log($"LevelLoader set grid to {GridManager.Instance.width}x{GridManager.Instance.height}" +
                  $" cellSize={GridManager.Instance.cellSize}");

        _spawnedBoats = new GameObject[levelData.boats.Count];

        for (int i = 0; i < levelData.boats.Count; i++)
        {
            BoatData data = levelData.boats[i];

            // All boats must be exactly size 2
            if (data.size != 2)
            {
                Debug.LogWarning($"Boat '{data.id}' has size {data.size}. Enforcing size = 2.");
                data.size = 2;
            }

            GameObject prefab = data.isHero ? heroBoardPrefab : boatSize2Prefab;
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
            bm.size         = 2;
            bm.isHorizontal = data.isHorizontal;
            bm.isHero       = data.isHero;

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
}