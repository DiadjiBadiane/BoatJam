// Assets/Scripts/Core/BoatMovement.cs
using System.Collections.Generic;
using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    public static System.Action<BoatMovement> OnAnyBoatMoved;

    [Header("Boat Config")]
    public string boatId;
    public int    size         = 2;
    public int    width        = 1;
    public bool   isHorizontal = true;
    public bool   isHero       = false;

    public int LengthCells => Mathf.Max(1, size);
    public int WidthCells  => Mathf.Max(1, width);
    public int SpanX       => isHorizontal ? LengthCells : WidthCells;
    public int SpanY       => isHorizontal ? WidthCells  : LengthCells;

    [Header("Movement")]
    public float moveSpeed = 8f;

    public Vector2Int GridPosition { get; private set; }
    public bool       IsMoving     { get; private set; }

    private Vector3 _gridOffset;
    private Vector3 _targetWorldPos;
    private bool _completionReported;

    float GetWorldYOffset()
    {
        var fitter = GetComponent<BoatMeshFitter>();
        return fitter != null ? fitter.WorldYOffset : 0f;
    }

    Vector3 GetWorldCenterOffsetXZ()
    {
        var fitter = GetComponent<BoatMeshFitter>();
        return fitter != null ? fitter.WorldCenterOffsetXZ : Vector3.zero;
    }

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

        float cell = GridManager.Instance.cellSize;
        float axisOffset  = ((LengthCells - 1) * 0.5f) * cell;
        float crossOffset = ((WidthCells  - 1) * 0.5f) * cell;
        _gridOffset = isHorizontal
            ? new Vector3(axisOffset, 0f, crossOffset)
            : new Vector3(crossOffset, 0f, axisOffset);

        transform.position = GridManager.Instance.GridToWorld(gridPos) + _gridOffset + Vector3.up * GetWorldYOffset() - GetWorldCenterOffsetXZ();
    }

    // ── Cell queries ──────────────────────────────────────────────────────────

    public List<Vector2Int> GetOccupiedCells()
    {
        var cells = new List<Vector2Int>(LengthCells * WidthCells);
        for (int l = 0; l < LengthCells; l++)
        for (int w = 0; w < WidthCells; w++)
            cells.Add(new Vector2Int(
                GridPosition.x + (isHorizontal ? l : w),
                GridPosition.y + (isHorizontal ? w : l)));
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

        _targetWorldPos = GridManager.Instance.GridToWorld(GridPosition) + _gridOffset + Vector3.up * GetWorldYOffset() - GetWorldCenterOffsetXZ();
        IsMoving = true;
    }

    // ── Auto-advance (path-clear exit) ───────────────────────────────────────

    /// <summary>
    /// Moves the boat one step in dir without firing OnAnyBoatMoved.
    /// Used by GameManager's auto-advance so the move counter is not incremented.
    /// </summary>
    public void AutoMove(Vector2Int dir)
    {
        if (IsMoving) return;

        GridManager.Instance.UnregisterBoat(this);
        GridPosition = GridPosition + dir;
        GridManager.Instance.RegisterBoat(this);

        // Do NOT fire OnAnyBoatMoved — this is a silent, automatic step.

        _targetWorldPos = GridManager.Instance.GridToWorld(GridPosition) + _gridOffset + Vector3.up * GetWorldYOffset() - GetWorldCenterOffsetXZ();
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

        foreach (var cell in GetOccupiedCellsAt(newPos))
        {
            if ( level.exitOnRight && cell.x >= GridManager.Instance.width && cell.y == level.exitRow) return true;
            if (!level.exitOnRight && cell.x < 0                           && cell.y == level.exitRow) return true;
        }
        return false;
    }

    List<Vector2Int> GetOccupiedCellsAt(Vector2Int gridPos)
    {
        var cells = new List<Vector2Int>(LengthCells * WidthCells);
        for (int l = 0; l < LengthCells; l++)
        for (int w = 0; w < WidthCells; w++)
            cells.Add(new Vector2Int(
                gridPos.x + (isHorizontal ? l : w),
                gridPos.y + (isHorizontal ? w : l)));
        return cells;
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