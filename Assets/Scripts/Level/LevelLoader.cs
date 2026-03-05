// Assets/Scripts/Level/LevelLoader.cs
using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject heroBoardPrefab;   // Boat_Hero prefab (size 2)
    public GameObject boatSize2Prefab;   // Boat_Small prefab (size 2)

    private GameObject[] _spawnedBoats;

    public void LoadLevel(LevelData levelData)
    {
        ClearLevel();

        // Configure grid dimensions
        GridManager.Instance.width  = levelData.gridWidth;
        GridManager.Instance.height = levelData.gridHeight;
        GridManager.Instance.ClearGrid(); // wipe occupancy map
        Debug.Log($"LevelLoader set grid to {GridManager.Instance.width}x{GridManager.Instance.height}");

        _spawnedBoats = new GameObject[levelData.boats.Count];

        for (int i = 0; i < levelData.boats.Count; i++)
        {
            BoatData data   = levelData.boats[i];
            
            // All boats must be exactly size 2
            if (data.size != 2)
            {
                Debug.LogWarning($"Boat '{data.id}' has size {data.size}. Enforcing size = 2.");
                data.size = 2;
            }
            
            // Choose prefab based on boat type
            GameObject prefab = data.isHero ? heroBoardPrefab : boatSize2Prefab;

            GameObject go = Instantiate(prefab, Vector3.zero, GetRotation(data));
            go.name = data.id;

            BoatMovement bm = go.GetComponent<BoatMovement>();
            if (bm == null)
            {
                Debug.LogError($"Prefab '{prefab.name}' instantiated for boat '{data.id}' is missing BoatMovement component. Destroying instance.");
                Destroy(go);
                continue;
            }

            bm.boatId       = data.id;
            bm.size         = 2;  // Enforce exactly 2 squares
            bm.isHorizontal = data.isHorizontal;
            bm.isHero       = data.isHero;

            // start position according to level data
            Vector2Int startPos = new Vector2Int(data.col, data.row);
            // clamp to grid bounds in case the asset contains a bad coordinate
            Vector2Int clamped = GridManager.Instance.GetValidPosition(bm, startPos);
            if (clamped != startPos)
            {
                Debug.LogWarning($"Boat '{data.id}' start position ({startPos.x},{startPos.y}) clamped to ({clamped.x},{clamped.y})");
                startPos = clamped;
            }
            
            // final sanity check (should always pass since we clamped)
            if (!GridManager.Instance.IsValidPlacement(bm, startPos))
            {
                Debug.LogError($"Boat '{data.id}' still overflows grid after clamping: {startPos}");
                startPos = GridManager.Instance.GetValidPosition(bm, startPos);
            }

            bm.InitializePosition(startPos);

            // ── Key fix: register each boat immediately after placing it ──
            GridManager.Instance.RegisterBoat(bm);
            // ─────────────────────────────────────────────────────────────

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