// Assets/Scripts/Core/BoatMovement.cs
using System.Collections.Generic;
using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    public static System.Action<BoatMovement> OnAnyBoatMoved;

    [Header("Boat Config")]
    public string boatId;
    public int    size         = 2;
    public bool   isHorizontal = true;
    public bool   isHero       = false;

    [Header("Movement")]
    public float moveSpeed = 8f;

    public Vector2Int GridPosition { get; private set; }
    public bool       IsMoving     { get; private set; }

    private Vector3 _gridOffset;
    private Vector3 _targetWorldPos;
    private bool _completionReported;

    // ── Initialisation ────────────────────────────────────────────────────────

    public void InitializePosition(Vector2Int gridPos)
    {
        _completionReported = false;

        if (!GridManager.Instance.IsValidPlacement(this, gridPos))
        {
            Debug.LogError($"Boat '{boatId}' at ({gridPos.x},{gridPos.y}) overflows grid — adjusting.");
            gridPos = GridManager.Instance.GetValidPosition(this, gridPos);
        }

        GridPosition = gridPos;

        float halfCell = GridManager.Instance.cellSize * 0.5f;
        _gridOffset = isHorizontal
            ? new Vector3(halfCell, 0f, 0f)
            : new Vector3(0f, 0f, halfCell);

        transform.position = GridManager.Instance.GridToWorld(gridPos) + _gridOffset;
    }

    // ── Cell queries ──────────────────────────────────────────────────────────

    public List<Vector2Int> GetOccupiedCells()
    {
        var cells = new List<Vector2Int>(size);
        for (int i = 0; i < size; i++)
            cells.Add(new Vector2Int(
                GridPosition.x + (isHorizontal ? i : 0),
                GridPosition.y + (isHorizontal ? 0 : i)));
        return cells;
    }

    // ── Input / movement ──────────────────────────────────────────────────────

    public void TryMove(Vector2Int dir)
    {
        if (IsMoving) return;

        if (dir.x != 0 && !isHorizontal) return;
        if (dir.y != 0 &&  isHorizontal) return;

        Vector2Int newPos = GridPosition + dir;

        if (!GridManager.Instance.CanMove(this, dir))                        return;
        if (!isHero && !GridManager.Instance.IsValidPlacement(this, newPos)) return;
        if ( isHero && !CanHeroMove(newPos))                                  return;

        // ── Snapshot the board BEFORE changing any state ──────────────────────
        UIManager.Instance?.CapturePreMoveSnapshot();
        // ─────────────────────────────────────────────────────────────────────

        GridManager.Instance.UnregisterBoat(this);
        GridPosition = newPos;
        GridManager.Instance.RegisterBoat(this);

        OnAnyBoatMoved?.Invoke(this);

        _targetWorldPos = GridManager.Instance.GridToWorld(GridPosition) + _gridOffset;
        IsMoving = true;
    }

    // ── Undo restore ──────────────────────────────────────────────────────────

    /// <summary>
    /// Called by UIManager.UndoMove() after it has:
    ///   1. Set transform.position back to the saved world position.
    ///   2. Set GridPosition via ForceGridPosition() below.
    ///   3. Called GridManager.ClearGrid() and re-registered ALL boats.
    /// This method only needs to stop any in-flight animation.
    /// </summary>
    public void OnUndoRestored()
    {
        IsMoving = false;
        // _targetWorldPos is stale after undo, reset it so Update() does nothing harmful.
        _targetWorldPos = transform.position;
    }

    /// <summary>
    /// Directly sets GridPosition without touching the GridManager.
    /// UIManager calls this as part of the full-grid-resync undo sequence.
    /// </summary>
    public void ForceGridPosition(Vector2Int gridPos)
    {
        GridPosition = gridPos;
        _completionReported = false;
    }

    // ── Hero exit validation ──────────────────────────────────────────────────

    private bool CanHeroMove(Vector2Int newPos)
    {
        LevelData level = GameManager.Instance.CurrentLevel;
        if (level == null) return false;

        if (GridManager.Instance.IsValidPlacement(this, newPos)) return true;

        for (int i = 0; i < size; i++)
        {
            Vector2Int cell = new Vector2Int(
                newPos.x + (isHorizontal ? i : 0),
                newPos.y + (isHorizontal ? 0 : i));

            if ( level.exitOnRight && cell.x >= GridManager.Instance.width && cell.y == level.exitRow) return true;
            if (!level.exitOnRight && cell.x < 0                           && cell.y == level.exitRow) return true;
        }
        return false;
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

            if (isHero && !_completionReported && (GridManager.Instance.HasHeroEscaped(this) || IsHeroEnteringExitLane()))
            {
                _completionReported = true;
                GameManager.Instance?.OnLevelComplete();
            }
        }
    }

    bool IsHeroEnteringExitLane()
    {
        LevelData level = GameManager.Instance?.CurrentLevel;
        if (level == null) return false;

        foreach (var cell in GetOccupiedCells())
        {
            if (level.exitOnRight && cell.x >= GridManager.Instance.width && cell.y == level.exitRow)
                return true;
            if (!level.exitOnRight && cell.x < 0 && cell.y == level.exitRow)
                return true;
        }

        return false;
    }
}