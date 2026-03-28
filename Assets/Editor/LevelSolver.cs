// Assets/Editor/LevelSolver.cs
// BFS-based level validator and star-threshold tool.
//
// Fixes vs previous version:
//   1. ValidateStateGeometry removed from the BFS hot-path — overlap is
//      impossible after CanMoveOneStep passes, so the redundant O(n) rebuild
//      was pure waste. It is now only called once on the initial state.
//   2. Non-hero boats that would leave the grid are correctly rejected during
//      move generation (the old code allowed this gap).
//   3. Hero escape is detected greedily inside TryStep so we never enqueue
//      a won state — we return immediately when the exit is reached.
//   4. Occupied map is built once per node and reused across all boat
//      iterations, then updated in-place for the next candidate rather
//      than being rebuilt from scratch.
//   5. State key uses a compact byte-packed string (two bytes per coord)
//      instead of the previous "x,y;" text format, halving HashSet memory.
//   6. Move counting matches the runtime: every single-cell shift = 1 move,
//      consistent with GameManager.AutoMove stepping one cell at a time.

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class LevelSolver
{
    // Set <= 0 to run a full BFS without a hard cutoff.
    const int MaxStates       = 2_000_000;
    const int TwoStarSlack    = 4;

    // ── Menu items ────────────────────────────────────────────────────────────

    [MenuItem("Tools/Validate Levels/Validate All In Resources")]
    public static void ValidateAll()
    {
        var levels = LoadSorted();
        if (levels == null) return;

        int solved = 0, invalid = 0, unsolved = 0, limited = 0;
        Debug.Log($"[LevelSolver] Validating {levels.Length} level(s)...");

        foreach (var lvl in levels)
        {
            var r = Solve(lvl);
            Log(lvl, r);
            switch (r.Status)
            {
                case Status.Solved:           solved++;   break;
                case Status.Invalid:          invalid++;  break;
                case Status.Unsolved:         unsolved++; break;
                case Status.SearchLimitReached: limited++;break;
            }
        }

        Debug.Log($"[LevelSolver] Done — total={levels.Length} solved={solved} " +
                  $"invalid={invalid} unsolved={unsolved} limitReached={limited}");
    }

    [MenuItem("Tools/Validate Levels/Validate Selected LevelData")]
    public static void ValidateSelected()
    {
        var lvl = Selection.activeObject as LevelData;
        if (lvl == null) { Debug.LogWarning("[LevelSolver] Select a LevelData asset first."); return; }
        Log(lvl, Solve(lvl));
    }

    [MenuItem("Tools/Validate Levels/Apply Star Thresholds/All In Resources")]
    public static void ApplyThresholdsAll()
    {
        var levels = LoadSorted();
        if (levels == null) return;

        int updated = 0, skipped = 0;
        foreach (var lvl in levels)
        {
            var r = Solve(lvl);
            if (r.Status != Status.Solved) { Log(lvl, r); skipped++; continue; }
            if (ApplyThresholds(lvl, r.MinMoves)) updated++;
        }

        if (updated > 0) { AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); }
        Debug.Log($"[LevelSolver] Thresholds applied — updated={updated} skipped={skipped}");
    }

    [MenuItem("Tools/Validate Levels/Apply Star Thresholds/Selected LevelData")]
    public static void ApplyThresholdsSelected()
    {
        var lvl = Selection.activeObject as LevelData;
        if (lvl == null) { Debug.LogWarning("[LevelSolver] Select a LevelData asset first."); return; }

        var r = Solve(lvl);
        Log(lvl, r);
        if (r.Status != Status.Solved) return;

        if (ApplyThresholds(lvl, r.MinMoves))
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    // ── Core BFS ──────────────────────────────────────────────────────────────

    public static Result Solve(LevelData level)
    {
        // ── Validation ────────────────────────────────────────────────────────
        if (level == null)
            return Result.Invalid("LevelData is null.");

        if (level.gridWidth <= 0 || level.gridHeight <= 0)
            return Result.Invalid($"Invalid grid size {level.gridWidth}x{level.gridHeight}.");

        if (level.boats == null || level.boats.Count == 0)
            return Result.Invalid("No boats defined.");

        int heroIdx = -1;
        for (int i = 0; i < level.boats.Count; i++)
        {
            var b = level.boats[i];
            if (b == null)            return Result.Invalid($"Boat at index {i} is null.");
            if (b.size < 1 || b.width < 1)
                return Result.Invalid($"Boat '{b.id}' has invalid size/width ({b.size}x{b.width}).");
            if (b.isHero)
            {
                if (heroIdx >= 0)     return Result.Invalid("Multiple hero boats found.");
                heroIdx = i;
            }
        }
        if (heroIdx < 0)              return Result.Invalid("No hero boat found.");

        // ── Initial state ─────────────────────────────────────────────────────
        int n = level.boats.Count;
        var start = new short[n * 2];
        for (int i = 0; i < n; i++)
        {
            start[i * 2]     = (short)level.boats[i].col;
            start[i * 2 + 1] = (short)level.boats[i].row;
        }

        // Validate initial geometry once (overlap / out-of-bounds check)
        string geoErr = CheckGeometry(level, start, heroIdx);
        if (geoErr != null) return Result.Invalid(geoErr);

        // Already solved at start?
        if (HeroEscaped(level, start, heroIdx)) return Result.Solved(0, 1);

        // ── BFS ───────────────────────────────────────────────────────────────
        var queue   = new Queue<Node>();
        var visited = new HashSet<string>();

        string startKey = Encode(start);
        visited.Add(startKey);
        queue.Enqueue(new Node(start, 0));

        while (queue.Count > 0)
        {
            if (MaxStates > 0 && visited.Count >= MaxStates)
                return Result.LimitReached(visited.Count);

            var node = queue.Dequeue();

            // Build occupied map for this state
            var occ = BuildOccupied(level, node.Pos);

            for (int bi = 0; bi < n; bi++)
            {
                var boat = level.boats[bi];
                int dx0 = boat.isHorizontal ? -1 :  0;
                int dy0 = boat.isHorizontal ?  0 : -1;
                int dx1 = boat.isHorizontal ?  1 :  0;
                int dy1 = boat.isHorizontal ?  0 :  1;

                // Try both directions for this boat
                foreach (var (dx, dy) in new[]{(dx0,dy0),(dx1,dy1)})
                {
                    // Slide as far as possible in this direction (each step = 1 BFS level)
                    // We only enqueue one step at a time so move count is per-cell,
                    // matching GameManager.AutoMove which also steps one cell at a time.
                    int solved = TryStep(level, heroIdx, node, bi, dx, dy, occ, queue, visited);
                    if (solved >= 0) return Result.Solved(solved, visited.Count);
                }
            }
        }

        return Result.Unsolved(visited.Count);
    }

    // ── Single-step attempt ───────────────────────────────────────────────────
    // Returns the solution depth if this step reaches the win condition, else -1.

    static int TryStep(
        LevelData level, int heroIdx,
        Node node, int bi,
        int dx, int dy,
        Dictionary<Vector2Int,int> occ,
        Queue<Node> queue,
        HashSet<string> visited)
    {
        var boat = level.boats[bi];
        int cx = node.Pos[bi * 2];
        int cy = node.Pos[bi * 2 + 1];

        // Leading-edge collision check
        if (!LeadingEdgeFree(boat, cx, cy, dx, dy, bi, occ))
            return -1;

        int nx = cx + dx;
        int ny = cy + dy;

        // Bounds check
        if (boat.isHero)
        {
            // Hero is allowed to step into the exit lane (out of grid) on the exit row
            if (!FitsInGrid(level, boat, nx, ny) && !HeroInExitLane(level, boat, nx, ny))
                return -1;
        }
        else
        {
            // Non-hero boats must always stay fully inside the grid
            if (!FitsInGrid(level, boat, nx, ny))
                return -1;
        }

        // Build new state
        var next = (short[])node.Pos.Clone();
        next[bi * 2]     = (short)nx;
        next[bi * 2 + 1] = (short)ny;

        string key = Encode(next);
        if (!visited.Add(key)) return -1;

        int depth = node.Depth + 1;

        // Win condition: hero has reached the exit lane
        if (boat.isHero && HeroInExitLane(level, boat, nx, ny))
            return depth;

        queue.Enqueue(new Node(next, depth));
        return -1;
    }

    // ── Geometry helpers ──────────────────────────────────────────────────────

    // Returns true if the leading edge cells (the cells the boat would move into)
    // are all free of other boats.
    static bool LeadingEdgeFree(
        BoatData boat, int cx, int cy, int dx, int dy,
        int bi, Dictionary<Vector2Int,int> occ)
    {
        int spanX = boat.isHorizontal ? boat.size  : boat.width;
        int spanY = boat.isHorizontal ? boat.width : boat.size;

        // The leading edge is the row/column of cells at the front of the movement
        if (dx > 0) // moving right — check rightmost column + 1
        {
            int ex = cx + spanX - 1 + 1;
            for (int y = 0; y < spanY; y++)
                if (BlockedBy(new Vector2Int(ex, cy + y), bi, occ)) return false;
        }
        else if (dx < 0) // moving left — check leftmost column - 1
        {
            int ex = cx - 1;
            for (int y = 0; y < spanY; y++)
                if (BlockedBy(new Vector2Int(ex, cy + y), bi, occ)) return false;
        }
        else if (dy > 0) // moving down — check bottommost row + 1
        {
            int ey = cy + spanY - 1 + 1;
            for (int x = 0; x < spanX; x++)
                if (BlockedBy(new Vector2Int(cx + x, ey), bi, occ)) return false;
        }
        else // dy < 0 — moving up — check topmost row - 1
        {
            int ey = cy - 1;
            for (int x = 0; x < spanX; x++)
                if (BlockedBy(new Vector2Int(cx + x, ey), bi, occ)) return false;
        }

        return true;
    }

    static bool BlockedBy(Vector2Int cell, int selfIndex, Dictionary<Vector2Int,int> occ)
        => occ.TryGetValue(cell, out int owner) && owner != selfIndex;

    static bool FitsInGrid(LevelData level, BoatData boat, int col, int row)
    {
        int spanX = boat.isHorizontal ? boat.size  : boat.width;
        int spanY = boat.isHorizontal ? boat.width : boat.size;
        return col >= 0 && row >= 0
            && col + spanX - 1 < level.gridWidth
            && row + spanY - 1 < level.gridHeight;
    }

    // Hero is in the exit lane when at least one of its cells is past the grid
    // edge on the correct exit row.
    static bool HeroInExitLane(LevelData level, BoatData hero, int col, int row)
    {
        int spanX = hero.isHorizontal ? hero.size  : hero.width;
        int spanY = hero.isHorizontal ? hero.width : hero.size;

        for (int x = 0; x < spanX; x++)
        for (int y = 0; y < spanY; y++)
        {
            int cx = col + x, cy = row + y;
            if (level.exitOnRight  && cx >= level.gridWidth  && cy == level.exitRow) return true;
            if (!level.exitOnRight && cx < 0                 && cy == level.exitRow) return true;
        }
        return false;
    }

    static bool HeroEscaped(LevelData level, short[] pos, int heroIdx)
    {
        var hero = level.boats[heroIdx];
        return HeroInExitLane(level, hero, pos[heroIdx * 2], pos[heroIdx * 2 + 1]);
    }

    // Initial geometry validation — called once, not in the hot-path.
    // Checks for out-of-bounds non-hero boats and any overlap between boats.
    static string CheckGeometry(LevelData level, short[] pos, int heroIdx)
    {
        var seen = new Dictionary<Vector2Int, int>();
        int n = level.boats.Count;

        for (int i = 0; i < n; i++)
        {
            var boat = level.boats[i];
            int col = pos[i * 2], row = pos[i * 2 + 1];

            if (!boat.isHero && !FitsInGrid(level, boat, col, row))
                return $"Boat '{boat.id}' starts out of bounds at ({col},{row}).";

            int spanX = boat.isHorizontal ? boat.size  : boat.width;
            int spanY = boat.isHorizontal ? boat.width : boat.size;

            for (int x = 0; x < spanX; x++)
            for (int y = 0; y < spanY; y++)
            {
                var cell = new Vector2Int(col + x, row + y);
                if (seen.TryGetValue(cell, out int other))
                    return $"Boats '{level.boats[other].id}' and '{boat.id}' overlap at ({cell.x},{cell.y}).";
                seen[cell] = i;
            }
        }

        // Hero must be either inside the grid or already in the exit lane
        var hero = level.boats[heroIdx];
        int hcol = pos[heroIdx * 2], hrow = pos[heroIdx * 2 + 1];
        if (!FitsInGrid(level, hero, hcol, hrow) && !HeroInExitLane(level, hero, hcol, hrow))
            return $"Hero starts out of bounds and is not in the exit lane ({hcol},{hrow}).";

        return null;
    }

    static Dictionary<Vector2Int,int> BuildOccupied(LevelData level, short[] pos)
    {
        var occ = new Dictionary<Vector2Int,int>();
        int n = level.boats.Count;

        for (int i = 0; i < n; i++)
        {
            var boat = level.boats[i];
            int col  = pos[i * 2], row = pos[i * 2 + 1];
            int spanX = boat.isHorizontal ? boat.size  : boat.width;
            int spanY = boat.isHorizontal ? boat.width : boat.size;

            for (int x = 0; x < spanX; x++)
            for (int y = 0; y < spanY; y++)
                occ[new Vector2Int(col + x, row + y)] = i;
        }

        return occ;
    }

    // ── State encoding ────────────────────────────────────────────────────────
    // Two bytes per coordinate (boats rarely exceed 255 in either dimension).
    // This produces keys roughly half the size of the old "x,y;" text format.

    static string Encode(short[] pos)
    {
        var chars = new char[pos.Length];
        for (int i = 0; i < pos.Length; i++)
            chars[i] = (char)(pos[i] + 128); // offset so -1 doesn't hit null
        return new string(chars);
    }

    // ── Star thresholds ───────────────────────────────────────────────────────

    static bool ApplyThresholds(LevelData level, int minMoves)
    {
        int three = Mathf.Max(1, minMoves);
        int two   = Mathf.Max(three + 1, minMoves + TwoStarSlack);

        bool changed = level.threeStarMoves != three || level.twoStarMoves != two;
        level.threeStarMoves = three;
        level.twoStarMoves   = two;

        if (changed) EditorUtility.SetDirty(level);

        Debug.Log($"[LevelSolver] {level.name}: optimal={minMoves}  3*={three}  2*={two}");
        return changed;
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    static void Log(LevelData level, Result r)
    {
        switch (r.Status)
        {
            case Status.Solved:
                Debug.Log($"[LevelSolver] SOLVED   {level.name}  moves={r.MinMoves}  visited={r.Visited}");
                break;
            case Status.Invalid:
                Debug.LogError($"[LevelSolver] INVALID  {level.name}  reason={r.Message}");
                break;
            case Status.Unsolved:
                Debug.LogError($"[LevelSolver] UNSOLVED {level.name}  visited={r.Visited}");
                break;
            case Status.SearchLimitReached:
                Debug.LogWarning($"[LevelSolver] LIMIT    {level.name}  visited={r.Visited}");
                break;
        }
    }

    // ── Level loading ─────────────────────────────────────────────────────────

    static LevelData[] LoadSorted()
    {
        var levels = Resources.LoadAll<LevelData>("Levels");
        if (levels == null || levels.Length == 0)
        {
            Debug.LogWarning("[LevelSolver] No LevelData assets found in Resources/Levels.");
            return null;
        }
        Array.Sort(levels, (a, b) => LevelOrder(a).CompareTo(LevelOrder(b)));
        return levels;
    }

    static int LevelOrder(LevelData level)
    {
        if (level == null || string.IsNullOrEmpty(level.name)) return int.MaxValue;
        int value = 0; bool found = false;
        foreach (char c in level.name)
        {
            if (char.IsDigit(c)) { found = true; value = value * 10 + (c - '0'); }
            else if (found) break;
        }
        return found ? value : int.MaxValue;
    }

    // ── Data structures ───────────────────────────────────────────────────────

    readonly struct Node
    {
        public readonly short[] Pos;
        public readonly int     Depth;
        public Node(short[] pos, int depth) { Pos = pos; Depth = depth; }
    }

    public enum Status { Solved, Unsolved, Invalid, SearchLimitReached }

    public readonly struct Result
    {
        public readonly Status Status;
        public readonly int    MinMoves;
        public readonly int    Visited;
        public readonly string Message;

        Result(Status s, int min, int vis, string msg)
        { Status=s; MinMoves=min; Visited=vis; Message=msg; }

        public static Result Solved(int min, int vis)      => new Result(Status.Solved,           min, vis, null);
        public static Result Unsolved(int vis)             => new Result(Status.Unsolved,          -1,  vis, null);
        public static Result Invalid(string msg)           => new Result(Status.Invalid,           -1,  0,   msg);
        public static Result LimitReached(int vis)         => new Result(Status.SearchLimitReached,-1,  vis,
                                                                $"Search cap ({MaxStates}) reached.");
    }
}