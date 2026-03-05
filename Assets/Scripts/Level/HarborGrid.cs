// Assets/Scripts/Level/HarborGrid.cs
using UnityEngine;

public class HarborGrid : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public Material    dockMaterial;   // brown/wood
    public Material    waterMaterial;  // blue water

    [Header("Settings")]
    public float cellSize        = 1f;
    public float borderThickness = 0.15f;

    void Start()
    {
        DrawGrid();
        DrawBorder();
    }

    void DrawGrid()
    {
        int w = gridManager.width;
        int h = gridManager.height;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.name             = $"Tile_{x}_{y}";
                tile.transform.parent = transform;
                tile.transform.position   = new Vector3(x * cellSize, -0.01f, y * cellSize);
                tile.transform.rotation   = Quaternion.Euler(90, 0, 0);
                tile.transform.localScale = Vector3.one * (cellSize - 0.05f);
                tile.GetComponent<Renderer>().material = waterMaterial;
                Destroy(tile.GetComponent<Collider>());
            }
        }
    }

    void DrawBorder()
    {
        int   w       = gridManager.width;
        int   h       = gridManager.height;
        float totalW  = w * cellSize;
        float totalH  = h * cellSize;
        float half    = cellSize * 0.5f;   // ← was hardcoded 0.5f — now scales with cellSize

        // Bottom wall
        CreateBorderSegment("Border_Bottom",
            new Vector3(totalW / 2f - half, 0.1f, -half),
            new Vector3(totalW + borderThickness, 0.3f, borderThickness));

        // Top wall
        CreateBorderSegment("Border_Top",
            new Vector3(totalW / 2f - half, 0.1f, totalH - half),
            new Vector3(totalW + borderThickness, 0.3f, borderThickness));

        // Left wall
        CreateBorderSegment("Border_Left",
            new Vector3(-half, 0.1f, totalH / 2f - half),
            new Vector3(borderThickness, 0.3f, totalH));

        // Right wall — split around the exit gap
        LevelData level = GameManager.Instance.CurrentLevel;

        if (level == null || !level.exitOnRight)
        {
            // No right-side exit — draw full wall
            CreateBorderSegment("Border_Right",
                new Vector3(totalW - half, 0.1f, totalH / 2f - half),
                new Vector3(borderThickness, 0.3f, totalH));
            return;
        }

        float exitZ   = level.exitRow * cellSize;
        float gapSize = 2f * cellSize;   // hero boat is size 2

        // Bottom part of right wall (below gap)
        float bottomH = exitZ - half;    // ← was exitZ - 0.5f
        if (bottomH > 0f)
            CreateBorderSegment("Border_Right_Bottom",
                new Vector3(totalW - half, 0.1f, bottomH / 2f),
                new Vector3(borderThickness, 0.3f, bottomH));

        // Top part of right wall (above gap)
        float topStart = exitZ + gapSize + half;   // ← was + 0.5f
        float topH     = totalH - topStart;
        if (topH > 0f)
            CreateBorderSegment("Border_Right_Top",
                new Vector3(totalW - half, 0.1f, topStart + topH / 2f - half),
                new Vector3(borderThickness, 0.3f, topH));
    }

    void CreateBorderSegment(string segName, Vector3 position, Vector3 scale)
    {
        GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seg.name             = segName;
        seg.transform.parent = transform;
        seg.transform.position   = position;
        seg.transform.localScale = scale;
        seg.GetComponent<Renderer>().material = dockMaterial;
        Destroy(seg.GetComponent<Collider>());
    }
}