// Assets/Scripts/Core/GridManager.cs
using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int width  = 6;
    public int height = 6;
    public float cellSize = 1f;

    // Maps grid position -> boat occupying it
    private Dictionary<Vector2Int, BoatMovement> _occupied
        = new Dictionary<Vector2Int, BoatMovement>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Registration ──────────────────────────────────────────────────────────

    /// <summary>Stamp all cells of a boat into the grid. Call once after spawning.</summary>
    public void RegisterBoat(BoatMovement boat)
    {
        foreach (var cell in boat.GetOccupiedCells())
            _occupied[cell] = boat;
    }

    /// <summary>Remove all cells of a boat from the grid. Call before moving it.</summary>
    public void UnregisterBoat(BoatMovement boat)
    {
        foreach (var cell in boat.GetOccupiedCells())
            _occupied.Remove(cell);
    }

    /// <summary>Wipe everything — call on level load before spawning boats.</summary>
    public void ClearGrid() => _occupied.Clear();

    // ── Movement validation ───────────────────────────────────────────────────

    /// <summary>
    /// Returns TRUE if the boat can move exactly 1 step in dir without colliding with another boat.
    /// The boat is excluded from its own collision check.
    /// Out-of-bounds checks are handled by IsValidPlacement().
    /// </summary>
    public bool CanMove(BoatMovement boat, Vector2Int dir)
    {
        // Temporarily lift the boat off the grid so it doesn't block itself
        UnregisterBoat(boat);

        bool ok = true;
        foreach (var cell in GetLeadingEdge(boat, dir, 1))
        {
            // Another boat is there
            if (_occupied.ContainsKey(cell))
            {
                ok = false;
                break;
            }
        }

        // Restore the boat BEFORE returning
        RegisterBoat(boat);
        return ok;
    }

    // ── Hero path check ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns TRUE if every cell between the hero boat's leading edge and the exit boundary
    /// is empty — i.e. the hero can slide all the way out without any obstacle.
    /// </summary>
    public bool IsHeroPathClear(BoatMovement hero)
    {
        LevelData level = GameManager.Instance?.CurrentLevel;
        if (level == null) return false;

        // Temporarily remove the hero so it doesn't block itself
        UnregisterBoat(hero);

        bool clear = true;

        if (level.exitOnRight)
        {
            // Hero moves right: check every column from (rightmost cell + 1) to (width - 1)
            int heroRight = int.MinValue;
            foreach (var cell in hero.GetOccupiedCells())
                if (cell.x > heroRight) heroRight = cell.x;

            for (int x = heroRight + 1; x < width; x++)
            {
                var check = new Vector2Int(x, level.exitRow);
                if (_occupied.ContainsKey(check)) { clear = false; break; }
            }
        }
        else
        {
            // Hero moves left: check every column from (leftmost cell - 1) down to 0
            int heroLeft = int.MaxValue;
            foreach (var cell in hero.GetOccupiedCells())
                if (cell.x < heroLeft) heroLeft = cell.x;

            for (int x = heroLeft - 1; x >= 0; x--)
            {
                var check = new Vector2Int(x, level.exitRow);
                if (_occupied.ContainsKey(check)) { clear = false; break; }
            }
        }

        RegisterBoat(hero);
        return clear;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public bool InBounds(Vector2Int cell)
        => cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;

    /// <summary>
    /// Checks if a boat at a given grid position would fit entirely within the grid without overflow.
    /// Does NOT check for collisions with other boats.
    /// </summary>
    public bool IsValidPlacement(BoatMovement boat, Vector2Int gridPos)
    {
        // Check each cell the boat would occupy
        for (int i = 0; i < boat.size; i++)
        {
            Vector2Int cell = new Vector2Int(
                gridPos.x + (boat.isHorizontal ? i : 0),
                gridPos.y + (boat.isHorizontal ? 0 : i));

            if (!InBounds(cell)) return false;  // Overflow detected
        }
        return true;
    }

    /// <summary>
    /// Adjusts a boat position so it fits within grid bounds.
    /// Used as a fallback when initialization position would overflow.
    /// </summary>
    public Vector2Int GetValidPosition(BoatMovement boat, Vector2Int gridPos)
    {
        Vector2Int adjusted = gridPos;

        if (boat.isHorizontal)
        {
            // Clamp so boat doesn't overflow right edge
            int maxX = width - boat.size;
            adjusted.x = Mathf.Clamp(adjusted.x, 0, maxX);
        }
        else
        {
            // Clamp so boat doesn't overflow top edge
            int maxY = height - boat.size;
            adjusted.y = Mathf.Clamp(adjusted.y, 0, maxY);
        }

        return adjusted;
    }

    /// <summary>
    /// Returns the cells the boat's leading face would occupy after <paramref name="steps"/> steps.
    /// For a horizontal size-2 boat moving right by 1 that's just the single cell to the right of its tail.
    /// For a vertical size-3 boat moving up by 1 that's the single cell above its top.
    /// </summary>
    private List<Vector2Int> GetLeadingEdge(BoatMovement boat, Vector2Int dir, int steps)
    {
        var edge = new List<Vector2Int>();
        var occupied = boat.GetOccupiedCells();

        if (dir.x > 0)        // right
        {
            int maxX = int.MinValue;
            foreach (var c in occupied) if (c.x > maxX) maxX = c.x;
            foreach (var c in occupied) if (c.x == maxX) edge.Add(new Vector2Int(c.x + steps, c.y));
        }
        else if (dir.x < 0)   // left
        {
            int minX = int.MaxValue;
            foreach (var c in occupied) if (c.x < minX) minX = c.x;
            foreach (var c in occupied) if (c.x == minX) edge.Add(new Vector2Int(c.x - steps, c.y));
        }
        else if (dir.y > 0)   // up
        {
            int maxY = int.MinValue;
            foreach (var c in occupied) if (c.y > maxY) maxY = c.y;
            foreach (var c in occupied) if (c.y == maxY) edge.Add(new Vector2Int(c.x, c.y + steps));
        }
        else                  // down
        {
            int minY = int.MaxValue;
            foreach (var c in occupied) if (c.y < minY) minY = c.y;
            foreach (var c in occupied) if (c.y == minY) edge.Add(new Vector2Int(c.x, c.y - steps));
        }

        return edge;
    }

    private bool IsHeroExit(Vector2Int cell, BoatMovement boat)
    {
        if (!boat.isHero) return false;
        LevelData level = GameManager.Instance.CurrentLevel;
        if (level == null) return false;
        if (level.exitOnRight  && cell.x >= width && cell.y == level.exitRow) return true;
        if (!level.exitOnRight && cell.x < 0      && cell.y == level.exitRow) return true;
        return false;
    }

    /// <summary>Is every cell of the hero boat outside the grid boundary?</summary>
    public bool HasHeroEscaped(BoatMovement hero)
    {
        LevelData level = GameManager.Instance.CurrentLevel;
        if (level == null) return false;
        foreach (var cell in hero.GetOccupiedCells())
        {
            if ( level.exitOnRight && cell.x < width)  return false;
            if (!level.exitOnRight && cell.x >= 0)     return false;
        }
        return true;
    }

    // ── Coordinate helpers ────────────────────────────────────────────────────

    public Vector3 GridToWorld(Vector2Int gridPos)
        => new Vector3(gridPos.x * cellSize, 0f, gridPos.y * cellSize);

    public Vector2Int WorldToGrid(Vector3 worldPos)
        => new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.z / cellSize));
}