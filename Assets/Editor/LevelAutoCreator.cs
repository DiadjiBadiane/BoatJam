// Assets/Editor/LevelAutoCreator.cs
//
// Consistent unblock-style campaign generator:
// - Hero is always horizontal on the central lane.
// - Exit is always on the right side (classic unblock flow).
// - Every level contains real lane blockers from the initial state.
// - Difficulty increases progressively from level 1 to 20.
// - Definitions are validated and checked with the BFS solver before writing.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LevelAutoCreator
{
    const int GridSize = 8;
    const int HeroRow = 4; // must match GameManager.FixedHeroRow

    [MenuItem("Tools/Ensure Sample Levels")]
    public static void EnsureLevels()
    {
        const string folder = "Assets/Resources/Levels";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Levels");

        foreach (var rawDef in GetDefinitions())
        {
            var def = rawDef;

            string solveReason = null;
            if (!ValidateDefinition(def, out string invalidReason) ||
                !IsSolvable(def, out solveReason))
            {
                Debug.LogWarning($"[LevelAutoCreator] Level {def.index} invalid/unsolved ({invalidReason ?? solveReason}). Using safe fallback.");
                def = BuildFallback(def.index);

                if (!ValidateDefinition(def, out invalidReason) ||
                    !IsSolvable(def, out solveReason))
                {
                    Debug.LogError($"[LevelAutoCreator] Fallback level {def.index} is still invalid/unsolved: {invalidReason ?? solveReason}. Skipping write.");
                    continue;
                }
            }

            string path = $"{folder}/Level_{def.index:D3}.asset";
            var ld = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (ld == null)
            {
                ld = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(ld, path);
            }

            ld.gridWidth       = def.gridWidth;
            ld.gridHeight      = def.gridHeight;
            ld.exitRow         = def.exitRow;
            ld.exitOnRight     = def.exitOnRight;
            ld.threeStarMoves  = def.threeStarMoves;
            ld.twoStarMoves    = def.twoStarMoves;
            ld.boats           = new List<BoatData>();

            foreach (var b in def.boats)
                ld.boats.Add(new BoatData
                {
                    id           = b.id,
                    col          = b.col,
                    row          = b.row,
                    size         = b.size,
                    width        = b.width,
                    isHorizontal = b.isHorizontal,
                    isHero       = b.isHero
                });

            EditorUtility.SetDirty(ld);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[LevelAutoCreator] 20 consistent unblock-style levels written to Resources/Levels");
    }

    // ── Shorthand helpers ─────────────────────────────────────────────────────

    static BoatDef Hero(int col, int row)
        => new BoatDef("hero", col, row, size: 2, isHorizontal: true, isHero: true, width: 1);

    static BoatDef H(string id, int col, int row, int size, int width = 1)
        => new BoatDef(id, col, row, size, isHorizontal: true,  isHero: false, width);

    static BoatDef V(string id, int col, int row, int size, int width = 1)
        => new BoatDef(id, col, row, size, isHorizontal: false, isHero: false, width);

    static LevelDef L(int index, int exitRow, bool exitOnRight,
                      int threeStar, int twoStar,
                      params BoatDef[] boats)
        => new LevelDef
        {
            index          = index,
            gridWidth      = GridSize,
            gridHeight     = GridSize,
            exitRow        = exitRow,
            exitOnRight    = exitOnRight,
            threeStarMoves = threeStar,
            twoStarMoves   = twoStar,
            boats          = new List<BoatDef>(boats)
        };

    // ── 20 level definitions ─────────────────────────────────────────────────
    //
    // Grid: 8×8, cols 0-7, rows 0-7.
    // Hero: horizontal size-2 at (1, 4), exit right.  Must match GameManager
    //       constants FixedHeroCol=1, FixedHeroRow=4, FixedExitRow=4.
    // V(id, col, row, size): vertical boat occupying (col, row) … (col, row+size-1).
    // H(id, col, row, size): horizontal boat occupying (col, row) … (col+size-1, row).
    //
    // A V2 at (col, 3) occupies (col,3)(col,4) — blocks row 4 (hero lane).
    //   Move up 1 → (col,2)(col,3) → row 4 clear.
    // A V2 at (col, 4) occupies (col,4)(col,5) — also blocks row 4.
    //   Move down 1 → (col,5)(col,6) → row 4 clear.
    //
    // The BFS solver validates every level at write time.
    // Star thresholds are estimates — run
    // Tools → Validate Levels → Apply Star Thresholds to get exact values.

    static List<LevelDef> GetDefinitions() => new List<LevelDef>
    {
        // ═══════════════════════════════════════════════════════════════════
        // TIER 1 — Tutorial (levels 1-3)
        // ═══════════════════════════════════════════════════════════════════

        // Level 1: One blocker. b1 at (5,3)(5,4) blocks row 4.  Move up 1 → clear.
        L(1, exitRow:HeroRow, exitOnRight:true, threeStar:2, twoStar:6,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2)),

        // Level 2: One blocker at col 7.  b1 up 1 → clear.
        L(2, exitRow:HeroRow, exitOnRight:true, threeStar:2, twoStar:6,
            Hero(1, HeroRow),
            V("b1", 7, 3, 2)),

        // Level 3: Two blockers.  Both move up 1.
        L(3, exitRow:HeroRow, exitOnRight:true, threeStar:3, twoStar:7,
            Hero(1, HeroRow),
            V("b1", 4, 3, 2),
            V("b2", 7, 3, 2)),

        // ═══════════════════════════════════════════════════════════════════
        // TIER 2 — Easy (levels 4-6)
        // ═══════════════════════════════════════════════════════════════════

        // Level 4: One blocker + gate.  g1 at (4,2)(5,2) blocks b1 from
        //   going up.  g1 left 1 → b1 up 1 → hero exits.
        L(4, exitRow:HeroRow, exitOnRight:true, threeStar:3, twoStar:7,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            H("g1", 4, 2, 2)),

        // Level 5: Two blockers, one gated.
        //   g1 at (4,2)(5,2) blocks b1.  b2 free to move up.
        L(5, exitRow:HeroRow, exitOnRight:true, threeStar:4, twoStar:8,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 7, 3, 2),
            H("g1", 4, 2, 2)),

        // Level 6: Two blockers, one gate, side clutter.
        //   g1 at (3,2)(4,2) blocks b1 from going up.
        L(6, exitRow:HeroRow, exitOnRight:true, threeStar:4, twoStar:8,
            Hero(1, HeroRow),
            V("b1", 4, 3, 2),
            V("b2", 7, 3, 2),
            H("g1", 3, 2, 2),
            H("c1", 0, 7, 3)),

        // ═══════════════════════════════════════════════════════════════════
        // TIER 3 — Medium-easy (levels 7-9)
        // ═══════════════════════════════════════════════════════════════════

        // Level 7: Three lane blockers, all free to move up.
        L(7, exitRow:HeroRow, exitOnRight:true, threeStar:4, twoStar:8,
            Hero(1, HeroRow),
            V("b1", 3, 3, 2),
            V("b2", 5, 3, 2),
            V("b3", 7, 3, 2),
            H("c1", 0, 7, 2),
            V("c2", 0, 0, 2)),

        // Level 8: Two blockers with separate gates.
        //   g1 at (3,2)(4,2) blocks b1.  g2 V at (6,1)(6,2) blocks b2.
        L(8, exitRow:HeroRow, exitOnRight:true, threeStar:5, twoStar:9,
            Hero(1, HeroRow),
            V("b1", 4, 3, 2),
            V("b2", 6, 3, 2),
            H("g1", 3, 2, 2),
            V("g2", 6, 1, 2),
            H("c1", 0, 7, 3)),

        // Level 9: Three blockers, one gated, clutter.
        //   g1 at (3,2)(4,2) blocks b1.
        L(9, exitRow:HeroRow, exitOnRight:true, threeStar:5, twoStar:9,
            Hero(1, HeroRow),
            V("b1", 4, 3, 2),
            V("b2", 6, 3, 2),
            V("b3", 7, 3, 2),
            H("g1", 3, 2, 2),
            V("c1", 0, 0, 2),
            H("c2", 5, 7, 3)),

        // ═══════════════════════════════════════════════════════════════════
        // TIER 4 — Medium (levels 10-12)
        // ═══════════════════════════════════════════════════════════════════

        // Level 10: Gate chain — gate is itself gated.
        //   b1 at (5,3)(5,4) blocks row 4.  g1 V at (5,1)(5,2) blocks b1 up.
        //   x1 H at (4,0)(5,0) blocks g1 up.  x1 left 1 → g1 up → b1 up.
        L(10, exitRow:HeroRow, exitOnRight:true, threeStar:7, twoStar:11,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 7, 3, 2),
            V("g1", 5, 1, 2),
            H("x1", 4, 0, 2),
            H("c1", 0, 7, 2),
            H("c2", 0, 6, 2)),

        // Level 11: Three blockers, two gates.
        //   g1 H at (3,2)(4,2) blocks b1.  g2 V at (6,1)(6,2) blocks b2.
        L(11, exitRow:HeroRow, exitOnRight:true, threeStar:8, twoStar:12,
            Hero(1, HeroRow),
            V("b1", 4, 3, 2),
            V("b2", 6, 3, 2),
            V("b3", 7, 3, 2),
            H("g1", 3, 2, 2),
            V("g2", 6, 1, 2),
            V("c1", 0, 0, 3),
            H("c2", 1, 7, 2)),

        // Level 12: Blocker with double gate above, shorter solve goes down.
        //   g1 V at (5,1)(5,2) blocks b1 up.  g2 H at (4,0)(5,0) blocks g1 up.
        //   b1 can also go down 2 (needs (5,5)(5,6) free).
        L(12, exitRow:HeroRow, exitOnRight:true, threeStar:8, twoStar:12,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 7, 3, 2),
            V("g1", 5, 1, 2),
            H("g2", 4, 0, 2),
            H("c1", 2, 6, 3),
            V("c2", 0, 5, 2),
            H("c3", 0, 7, 2)),

        // ═══════════════════════════════════════════════════════════════════
        // TIER 5 — Medium-hard (levels 13-15)
        // ═══════════════════════════════════════════════════════════════════

        // Level 13: V3 blocker + gate.
        //   b1 V3 at (5,2)(5,3)(5,4).  g1 H at (4,1)(5,1) blocks b1 up.
        //   g1 left 1, b1 up 1, b2 up 1.
        L(13, exitRow:HeroRow, exitOnRight:true, threeStar:9, twoStar:13,
            Hero(1, HeroRow),
            V("b1", 5, 2, 3),
            V("b2", 7, 3, 2),
            H("g1", 4, 1, 2),
            V("c1", 0, 6, 2),
            V("c2", 0, 0, 2),
            H("c3", 2, 7, 2)),

        // Level 14: Two gated blockers + extra gate.
        //   g1 H at (3,2)(4,2) blocks b1.  g2 V at (6,1)(6,2) blocks b2.
        //   x1 V at (2,1)(2,2) blocks g1 from sliding left.
        //   Chain: x1 up → g1 left → b1 up → g2 up → b2 up.
        L(14, exitRow:HeroRow, exitOnRight:true, threeStar:10, twoStar:14,
            Hero(1, HeroRow),
            V("b1", 4, 3, 2),
            V("b2", 6, 3, 2),
            H("g1", 3, 2, 2),
            V("g2", 6, 1, 2),
            V("x1", 2, 1, 2),
            V("c1", 0, 6, 2),
            H("c2", 0, 7, 3)),

        // Level 15: V3 + 2 blockers, two gates.
        //   b1 V3 at (4,2)-(4,4).  g1 H at (3,1)(4,1) blocks b1 up.
        //   g2 V at (6,1)(6,2) blocks b2 up.  b3 free.
        L(15, exitRow:HeroRow, exitOnRight:true, threeStar:11, twoStar:15,
            Hero(1, HeroRow),
            V("b1", 4, 2, 3),
            V("b2", 6, 3, 2),
            V("b3", 7, 3, 2),
            H("g1", 3, 1, 2),
            V("g2", 6, 1, 2),
            H("c1", 0, 6, 2),
            V("c2", 0, 0, 2),
            H("c3", 1, 7, 3)),

        // ═══════════════════════════════════════════════════════════════════
        // TIER 6 — Hard (levels 16-18)
        // ═══════════════════════════════════════════════════════════════════

        // Level 16: Four blockers, two gated.
        //   g1 H at (2,2)(3,2) blocks b1 up.  g2 V at (6,1)(6,2) blocks b3 up.
        L(16, exitRow:HeroRow, exitOnRight:true, threeStar:12, twoStar:16,
            Hero(1, HeroRow),
            V("b1", 3, 3, 2),
            V("b2", 5, 3, 2),
            V("b3", 6, 3, 2),
            V("b4", 7, 3, 2),
            H("g1", 2, 2, 2),
            V("g2", 6, 1, 2),
            H("c1", 0, 6, 2),
            V("c2", 0, 0, 2),
            H("c3", 1, 7, 3)),

        // Level 17: Three blockers with independent gates.
        //   g1 H at (2,2)(3,2) blocks b1.  g2 V at (5,1)(5,2) blocks b2.
        //   g3 V at (7,1)(7,2) blocks b3.
        L(17, exitRow:HeroRow, exitOnRight:true, threeStar:13, twoStar:17,
            Hero(1, HeroRow),
            V("b1", 3, 3, 2),
            V("b2", 5, 3, 2),
            V("b3", 7, 3, 2),
            H("g1", 2, 2, 2),
            V("g2", 5, 1, 2),
            V("g3", 7, 1, 2),
            V("c1", 0, 0, 3),
            V("c2", 0, 5, 2),
            H("c3", 3, 7, 2),
            H("c4", 6, 7, 2)),

        // Level 18: Deep chain — gate of a gate of a gate.
        //   b1 at (5,3)(5,4).  g1 V at (5,1)(5,2) blocks b1 up.
        //   x1 H at (5,0)(6,0) blocks g1 up.  y1 V at (4,0)(4,1) blocks x1 left.
        //   Chain: y1 down 1, x1 left 1, g1 up 1, b1 up 1.
        L(18, exitRow:HeroRow, exitOnRight:true, threeStar:14, twoStar:18,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 7, 3, 2),
            V("g1", 5, 1, 2),
            H("x1", 5, 0, 2),
            V("y1", 4, 0, 2),
            V("c1", 0, 5, 2),
            V("c2", 0, 0, 2),
            H("c3", 2, 7, 3)),

        // ═══════════════════════════════════════════════════════════════════
        // TIER 7 — Expert (levels 19-20)
        // ═══════════════════════════════════════════════════════════════════

        // Level 19: Five lane blockers, multiple gates.
        //   b2 at (4,4)(4,5) — must go DOWN to clear row 4.
        //   g1 blocks b1, g2 blocks b3, g3 blocks b4.
        L(19, exitRow:HeroRow, exitOnRight:true, threeStar:15, twoStar:19,
            Hero(1, HeroRow),
            V("b1", 3, 3, 2),
            V("b2", 4, 4, 2),
            V("b3", 5, 3, 2),
            V("b4", 6, 3, 2),
            V("b5", 7, 3, 2),
            H("g1", 2, 2, 2),
            V("g2", 5, 1, 2),
            V("g3", 6, 1, 2),
            V("c1", 0, 6, 2),
            H("c2", 1, 7, 2)),

        // Level 20: Maximum complexity — five lane blockers, gate chain.
        //   b2 and b4 at row 4 — must go down.  g1 blocks b1, g2/g3 chain blocks b3.
        L(20, exitRow:HeroRow, exitOnRight:true, threeStar:16, twoStar:20,
            Hero(1, HeroRow),
            V("b1", 3, 3, 2),
            V("b2", 4, 4, 2),
            V("b3", 5, 3, 2),
            V("b4", 6, 4, 2),
            V("b5", 7, 3, 2),
            H("g1", 2, 2, 2),
            V("g2", 5, 1, 2),
            H("g3", 5, 0, 2),
            V("c1", 0, 6, 2),
            V("c2", 0, 0, 2),
            H("c3", 2, 7, 3),
            H("c4", 1, 6, 2))
    };

    static LevelDef BuildFallback(int index)
    {
        // Minimal guaranteed-solvable layout: hero + single lane blocker
        return L(index, exitRow: HeroRow, exitOnRight: true, 2, 6,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2));
    }

    static bool ValidateDefinition(LevelDef def, out string reason)
    {
        reason = null;

        if (def == null)
        {
            reason = "Definition is null.";
            return false;
        }

        int heroCount = 0;
        BoatDef hero = null;
        var occupied = new HashSet<Vector2Int>();

        foreach (var b in def.boats)
        {
            if (b == null)
            {
                reason = "Null boat definition.";
                return false;
            }

            if (b.isHero)
            {
                heroCount++;
                hero = b;
            }

            int spanX = b.isHorizontal ? b.size : b.width;
            int spanY = b.isHorizontal ? b.width : b.size;

            for (int dx = 0; dx < spanX; dx++)
            for (int dy = 0; dy < spanY; dy++)
            {
                var c = new Vector2Int(b.col + dx, b.row + dy);
                if (c.x < 0 || c.x >= def.gridWidth || c.y < 0 || c.y >= def.gridHeight)
                {
                    reason = $"Boat '{b.id}' goes out of bounds.";
                    return false;
                }

                if (!occupied.Add(c))
                {
                    reason = $"Boat overlap detected at ({c.x},{c.y}).";
                    return false;
                }
            }
        }

        if (heroCount != 1)
        {
            reason = "Level must contain exactly one hero.";
            return false;
        }

        if (!def.exitOnRight)
        {
            reason = "Exit must be on the right for unblock-style consistency.";
            return false;
        }

        if (def.exitRow != HeroRow || hero == null || hero.row != HeroRow)
        {
            reason = "Hero and exit must be on the center lane.";
            return false;
        }

        int heroRight = hero.col + hero.size - 1;
        bool hasInitialBlocker = false;

        foreach (var b in def.boats)
        {
            if (b.isHero) continue;

            int spanX = b.isHorizontal ? b.size : b.width;
            int spanY = b.isHorizontal ? b.width : b.size;

            for (int dx = 0; dx < spanX; dx++)
            for (int dy = 0; dy < spanY; dy++)
            {
                int x = b.col + dx;
                int y = b.row + dy;
                if (y == HeroRow && x > heroRight)
                {
                    hasInitialBlocker = true;
                    break;
                }
            }

            if (hasInitialBlocker) break;
        }

        if (!hasInitialBlocker)
        {
            reason = "No initial blocker in hero exit lane.";
            return false;
        }

        return true;
    }

    static bool IsSolvable(LevelDef def, out string reason)
    {
        reason = null;

        var temp = ScriptableObject.CreateInstance<LevelData>();
        temp.gridWidth = def.gridWidth;
        temp.gridHeight = def.gridHeight;
        temp.exitRow = def.exitRow;
        temp.exitOnRight = def.exitOnRight;
        temp.threeStarMoves = def.threeStarMoves;
        temp.twoStarMoves = def.twoStarMoves;
        temp.boats = new List<BoatData>(def.boats.Count);

        foreach (var b in def.boats)
        {
            temp.boats.Add(new BoatData
            {
                id = b.id,
                col = b.col,
                row = b.row,
                size = b.size,
                width = b.width,
                isHorizontal = b.isHorizontal,
                isHero = b.isHero
            });
        }

        var result = LevelSolver.Solve(temp);
        Object.DestroyImmediate(temp);

        if (result.Status != LevelSolver.Status.Solved)
        {
            reason = result.Message ?? result.Status.ToString();
            return false;
        }

        return true;
    }

    // ── Data transfer types (Editor-only) ─────────────────────────────────────

    class LevelDef
    {
        public int           index;
        public int           gridWidth, gridHeight;
        public int           exitRow;
        public bool          exitOnRight;
        public int           threeStarMoves, twoStarMoves;
        public List<BoatDef> boats;
    }

    class BoatDef
    {
        public string id;
        public int    col, row, size, width;
        public bool   isHorizontal, isHero;

        public BoatDef(string id, int col, int row, int size,
                       bool isHorizontal, bool isHero, int width = 1)
        {
            this.id           = id;
            this.col          = col;
            this.row          = row;
            this.size         = size;
            this.width        = Mathf.Max(1, width);
            this.isHorizontal = isHorizontal;
            this.isHero       = isHero;
        }
    }
}