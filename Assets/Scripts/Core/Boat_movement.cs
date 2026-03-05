// Assets/Scripts/Core/BoatMovement.cs
using System.Collections.Generic;
using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    [Header("Boat Config")]
    public string boatId;
    public int    size         = 2;  // Always exactly 2 squares
    public bool   isHorizontal = true;
    public bool   isHero       = false;

    [Header("Movement")]
    public float moveSpeed = 8f;

    public Vector2Int GridPosition { get; private set; }
    public bool       IsMoving     { get; private set; }

    // offset applied to world position based on boat pivot / size
    private Vector3 _gridOffset;
    private Vector3 _targetWorldPos;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>Set grid position and snap world transform. Does NOT register on grid.</summary>
    public void InitializePosition(Vector2Int gridPos)
    {
        // Validate that this boat won't overflow the grid
        if (!GridManager.Instance.IsValidPlacement(this, gridPos))
        {
            Debug.LogError($"Boat '{boatId}' at ({gridPos.x}, {gridPos.y}) would overflow the grid! Using adjusted position.");
            gridPos = GridManager.Instance.GetValidPosition(this, gridPos);
        }

        GridPosition = gridPos;

        // compute offset to center the boat across its occupied cells
        float halfCell = GridManager.Instance.cellSize * 0.5f;
        if (isHorizontal)
            _gridOffset = new Vector3(halfCell, 0f, 0f);
        else
            _gridOffset = new Vector3(0f, 0f, halfCell);

        transform.position = GridManager.Instance.GridToWorld(gridPos) + _gridOffset;
    }

    // ── Cell queries ──────────────────────────────────────────────────────────

    public List<Vector2Int> GetOccupiedCells()
    {
        var cells = new List<Vector2Int>(size);
        for (int i = 0; i < size; i++)
        {
            cells.Add(new Vector2Int(
                GridPosition.x + (isHorizontal ? i : 0),
                GridPosition.y + (isHorizontal ? 0 : i)));
        }
        return cells;
    }

    // ── Input / movement ──────────────────────────────────────────────────────

    public void TryMove(Vector2Int dir)
    {
        if (IsMoving) return;

        // Block wrong axis
        if (dir.x != 0 && !isHorizontal) return;
        if (dir.y != 0 &&  isHorizontal) return;

        Vector2Int newPos = GridPosition + dir;

        // Check collisions with other boats
        if (!GridManager.Instance.CanMove(this, dir)) return;  // Would collide with another boat

        // Check bounds (hero boats can exit, regular boats cannot)
        if (!isHero && !GridManager.Instance.IsValidPlacement(this, newPos)) return;  // Would overflow
        if (isHero && !CanHeroMove(newPos)) return;  // Hero-specific movement check

        // ── Key fix: unregister OLD cells, update position, register NEW cells ──
        GridManager.Instance.UnregisterBoat(this);
        GridPosition = newPos;
        GridManager.Instance.RegisterBoat(this);
        // ────────────────────────────────────────────────────────────────────────

        _targetWorldPos = GridManager.Instance.GridToWorld(GridPosition) + _gridOffset;
        IsMoving = true;
    }

    /// <summary>
    /// Special movement validation for hero boats that can exit the grid.
    /// Non-hero boats returning false for positions that would take them out of normal bounds.
    /// </summary>
    private bool CanHeroMove(Vector2Int newPos)
    {
        LevelData level = GameManager.Instance.CurrentLevel;
        if (level == null) return false;

        // Check if boat is trying to exit
        if (GridManager.Instance.IsValidPlacement(this, newPos))
            return true;  // Normal grid position

        // Check if this is a valid exit
        for (int i = 0; i < size; i++)
        {
            Vector2Int cell = new Vector2Int(
                newPos.x + (isHorizontal ? i : 0),
                newPos.y + (isHorizontal ? 0 : i));

            if (level.exitOnRight && cell.x >= GridManager.Instance.width && cell.y == level.exitRow)
                return true;
            if (!level.exitOnRight && cell.x < 0 && cell.y == level.exitRow)
                return true;
        }

        return false;  // Out of bounds but not a valid exit
    }

    // ── Update / animation ────────────────────────────────────────────────────

    void Update()
    {
        if (!IsMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position, _targetWorldPos, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _targetWorldPos) < 0.001f)
        {
            transform.position = _targetWorldPos;
            IsMoving = false;

            if (isHero && GridManager.Instance.HasHeroEscaped(this))
                GameManager.Instance.OnLevelComplete();
        }
    }
}