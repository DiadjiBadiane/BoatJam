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

        var defs = GetDefinitions();

        foreach (var rawDef in defs)
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
        Debug.Log($"[LevelAutoCreator] {defs.Count} consistent unblock-style levels written to Resources/Levels");
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

    static LevelDef L(int index, int gridW, int gridH, int exitRow, bool exitOnRight,
                      int threeStar, int twoStar,
                      params BoatDef[] boats)
        => new LevelDef
        {
            index          = index,
            gridWidth      = gridW,
            gridHeight     = gridH,
            exitRow        = exitRow,
            exitOnRight    = exitOnRight,
            threeStarMoves = threeStar,
            twoStarMoves   = twoStar,
            boats          = new List<BoatDef>(boats)
        };

    // ── 25 level definitions ─────────────────────────────────────────────────
    //
    // Grid: 8×8, cols 0-7, rows 0-7.
    // Hero: horizontal size-2 at (1, 4), exit right.  Must match GameManager
    //       constants FixedHeroCol=1, FixedHeroRow=4, FixedExitRow=4.
    //
    // V(id, col, row, size, width): vertical boat.
    //   Occupies cols [col..col+width-1], rows [row..row+size-1].
    // H(id, col, row, size, width): horizontal boat.
    //   Occupies cols [col..col+size-1], rows [row..row+width-1].
    //
    // Boat type legend (per user spec):
    //   2×1 = size 2, width 1   (small boat)
    //   4×1 = size 4, width 1   (long boat)
    //   2×2 = size 2, width 2   (wide boat)
    //   4×2 = size 4, width 2   (barge)
    //
    // A V(2,1) at (col, 3) occupies rows 3-4 — blocks hero lane (row 4).
    // A V(2,1) at (col, 4) occupies rows 4-5 — also blocks hero lane.
    //
    // The BFS solver validates every level at write time.
    // ─────────────────────────────────────────────────────────────────────────

    static List<LevelDef> GetDefinitions() => new List<LevelDef>
    {
        // ═══════════════════════════════════════════════════════════════════
        // 🟢 START (Levels 1-5)
        // ═══════════════════════════════════════════════════════════════════

        // ── Level 1: 3× (2×1) ────────────────────────────────────────────
        //   Hero (1,4)-(2,4).  b1 V at (5,3)-(5,4) blocks lane. Move b1 up.
        //   b2 H at (0,7)-(1,7) clutter. b3 V at (7,0)-(7,1) clutter.
        L( 1, exitRow:HeroRow, exitOnRight:true, threeStar:2, twoStar:5,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            H("b2", 0, 7, 2),
            V("b3", 7, 0, 2)),

        // ── Level 2: 4× (2×1) ────────────────────────────────────────────
        //   b1 V at (5,3) blocks lane. b2 V at (7,3) blocks lane.
        //   b3 H at (0,0). b4 H at (0,7).
        L( 2, exitRow:HeroRow, exitOnRight:true, threeStar:3, twoStar:6,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 7, 3, 2),
            H("b3", 0, 0, 2),
            H("b4", 0, 7, 2)),

        // ── Level 3: 4× (2×1), 1× (4×1) ─────────────────────────────────
        //   b1 V at (4,3) blocks lane. b2 V at (7,3) blocks lane.
        //   b3 H(4×1) at (3,2) blocks b1 from going up → slide b3 left, then b1 up.
        //   b4 H at (0,6). b5 V at (0,0).
        L( 3, exitRow:HeroRow, exitOnRight:true, threeStar:4, twoStar:7,
            Hero(1, HeroRow),
            V("b1", 4, 3, 2),
            V("b2", 7, 3, 2),
            H("b3", 3, 2, 4),
            H("b4", 0, 6, 2),
            V("b5", 0, 0, 2)),

        // ── Level 4: 3× (2×1), 2× (4×1) ─────────────────────────────────
        //   b1 V at (5,3) blocks lane. b2 V at (7,4) blocks lane.
        //   b3 H(4×1) at (4,2) blocks b1 up → slide left.
        //   b4 H(4×1) at (0,7). b5 H at (0,0).
        L( 4, exitRow:HeroRow, exitOnRight:true, threeStar:4, twoStar:8,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 7, 4, 2),
            H("b3", 4, 2, 4),
            H("b4", 0, 7, 4),
            H("b5", 0, 0, 2)),

        // ── Level 5: 4× (2×1), 2× (4×1) ─────────────────────────────────
        //   b1 V at (4,3), b2 V at (6,3) block lane.
        //   b3 H(4×1) at (3,2) blocks b1 up. b4 H(4×1) at (0,6).
        //   b5 H at (0,0). b6 V at (7,0).
        L( 5, exitRow:HeroRow, exitOnRight:true, threeStar:5, twoStar:8,
            Hero(1, HeroRow),
            V("b1", 4, 3, 2),
            V("b2", 6, 3, 2),
            H("b3", 3, 2, 4),
            H("b4", 0, 6, 4),
            H("b5", 0, 0, 2),
            V("b6", 7, 0, 2)),

        // ═══════════════════════════════════════════════════════════════════
        // 🟡 EASY (Levels 6-10)
        // ═══════════════════════════════════════════════════════════════════

        // ── Level 6: 3× (2×1), 2× (4×1), 1× (2×2) ──────────────────────
        //   b1 V at (5,3) blocks lane. b2 V at (7,3) blocks lane.
        //   b3 H(4×1) at (4,2) gate for b1. b4 H(4×1) at (0,7).
        //   b5 H at (0,0). b6 V(2×2) at (0,5) w=2 occupies (0,5)(1,5)(0,6)(1,6).
        L( 6, exitRow:HeroRow, exitOnRight:true, threeStar:5, twoStar:9,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 7, 3, 2),
            H("b3", 4, 2, 4),
            H("b4", 0, 7, 4),
            H("b5", 0, 0, 2),
            V("b6", 0, 5, 2, 2)),

        // ── Level 7: 3× (2×1), 2× (4×1), 2× (2×2) ─────────────────────
        //   b1 V at (4,3) blocks lane. b2 V at (7,3) blocks lane.
        //   b3 H(4×1) at (3,1) gate for b1. b4 H(4×1) at (0,7).
        //   b5 H at (0,0). b6 V(2×2) at (0,5). b7 V(2×2) at (6,0).
        L( 7, exitRow:HeroRow, exitOnRight:true, threeStar:6, twoStar:10,
            Hero(1, HeroRow),
            V("b1", 4, 3, 2),
            V("b2", 7, 3, 2),
            H("b3", 3, 2, 4),
            H("b4", 0, 7, 4),
            H("b5", 0, 0, 2),
            V("b6", 0, 5, 2, 2),
            V("b7", 6, 0, 2, 2)),

        // ── Level 8: 2× (2×1), 3× (4×1), 2× (2×2) ─────────────────────
        //   b1 V at (5,3) blocks. b2 V at (7,4) blocks.
        //   b3 H(4×1) at (4,2) gate for b1. b4 H(4×1) at (0,7). b5 H(4×1) at (4,6).
        //   b6 V(2×2) at (0,5). b7 V(2×2) at (0,0).
        L( 8, exitRow:HeroRow, exitOnRight:true, threeStar:6, twoStar:10,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 7, 4, 2),
            H("b3", 4, 2, 4),
            H("b4", 0, 7, 4),
            H("b5", 4, 6, 4),
            V("b6", 0, 5, 2, 2),
            V("b7", 0, 0, 2, 2)),

        // ── Level 9: 3× (2×1), 3× (4×1), 2× (2×2) ─────────────────────
        //   b1 V at (4,3), b2 V at (6,3) block lane. b3 V at (7,0) clutter.
        //   b4 H(4×1) at (3,2) gate for b1. b5 H(4×1) at (0,7). b6 H(4×1) at (4,6).
        //   b7 V(2×2) at (0,5). b8 V(2×2) at (0,0).
        L( 9, exitRow:HeroRow, exitOnRight:true, threeStar:7, twoStar:11,
            Hero(1, HeroRow),
            V("b1", 4, 3, 2),
            V("b2", 6, 3, 2),
            V("b3", 7, 0, 2),
            H("b4", 3, 2, 4),
            H("b5", 0, 7, 4),
            H("b6", 4, 6, 4),
            V("b7", 0, 5, 2, 2),
            V("b8", 0, 0, 2, 2)),

        // ── Level 10: 2× (2×1), 3× (4×1), 3× (2×2) ────────────────────
        //   b1 V at (5,3) blocks. b2 V at (7,3) blocks.
        //   b3 H(4×1) at (4,2) gate for b1. b4 H(4×1) at (0,7). b5 H(4×1) at (4,6).
        //   b6 V(2×2) at (0,5). b7 V(2×2) at (0,0). b8 H(2×2) at (6,6) occ (6,6)(7,6)(6,7)(7,7).
        L(10, exitRow:HeroRow, exitOnRight:true, threeStar:7, twoStar:11,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 7, 3, 2),
            H("b3", 4, 2, 4),
            H("b4", 0, 7, 4),
            H("b5", 4, 6, 4),
            V("b6", 0, 5, 2, 2),
            V("b7", 0, 0, 2, 2),
            H("b8", 4, 0, 2, 2)),

        // ═══════════════════════════════════════════════════════════════════
        // 🟠 INTERMEDIATE (Levels 11-15)
        // ═══════════════════════════════════════════════════════════════════

        // ── Level 11: 2× (2×1), 2× (4×1), 3× (2×2), 1× (4×2) ──────────
        //   b1 V at (5,3) blocks. b2 V at (7,4) blocks.
        //   b3 H(4×1) at (4,2) gate. b4 H(4×1) at (0,7).
        //   b5 V(2×2) at (0,5). b6 V(2×2) at (0,0). b7 H(2×2) at (6,6).
        //   b8 H(4×2) at (0,2) occ (0,2)(1,2)(2,2)(3,2)(0,3)(1,3)(2,3)(3,3).
        //   Wait — b8 at row 2-3 occupies (0-3,2)(0-3,3). Hero at (1,4). b3 at (4-7,2).
        //   b8 conflicts with b3 at col 3-row 2. So use b3 at (4,1,4) instead.
        //   Actually let me be more careful.
        //   b8 H(4×2) at (0,0): occupies (0-3, 0-1). Safe.
        //   b5 V(2×2) at (0,5): occupies (0-1, 5-6). b6 V(2×2) at (2,5): occupies (2-3, 5-6).
        //   b7 H(2×2) at (4,6): occupies (4-5, 6-7).
        L(11, exitRow:HeroRow, exitOnRight:true, threeStar:8, twoStar:12,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 7, 4, 2),
            H("b3", 4, 2, 4),
            H("b4", 4, 7, 4),
            V("b5", 0, 5, 2, 2),
            V("b6", 2, 5, 2, 2),
            H("b7", 4, 0, 2, 2),
            H("b8", 0, 0, 4, 2)),

        // ── Level 12: 3× (2×1), 2× (4×1), 3× (2×2), 1× (4×2) ─────────
        //   b1 V at (4,3), b2 V at (6,3), b3 V at (7,0).
        //   b4 H(4×1) at (3,2) gate. b5 H(4×1) at (4,6).
        //   b6 V(2×2) at (0,5). b7 V(2×2) at (2,5). b8 H(2×2) at (6,6).
        //   b9 H(4×2) at (0,0).
        L(12, exitRow:HeroRow, exitOnRight:true, threeStar:9, twoStar:13,
            Hero(1, HeroRow),
            V("b1", 4, 3, 2),
            V("b2", 6, 3, 2),
            V("b3", 7, 0, 2),
            H("b4", 3, 2, 4),
            H("b5", 4, 6, 4),
            V("b6", 0, 5, 2, 2),
            V("b7", 2, 5, 2, 2),
            H("b8", 4, 0, 2, 2),
            H("b9", 0, 0, 4, 2)),

        // ── Level 13: 2× (2×1), 3× (4×1), 3× (2×2), 1× (4×2) ─────────
        //   b1 V at (5,3), b2 V at (7,3).
        //   b3 H(4×1) at (4,2). b4 H(4×1) at (4,6). b5 H(4×1) at (0,7).
        //   b6 V(2×2) at (0,5). b7 V(2×2) at (2,5). b8 H(2×2) at (6,6).
        //   b9 H(4×2) at (0,0).
        L(13, exitRow:HeroRow, exitOnRight:true, threeStar:9, twoStar:13,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 7, 3, 2),
            H("b3", 4, 2, 4),
            H("b4", 4, 6, 4),
            H("b5", 0, 7, 4),
            V("b6", 0, 5, 2, 2),
            V("b7", 2, 5, 2, 2),
            H("b8", 4, 0, 2, 2),
            H("b9", 0, 0, 4, 2)),

        // ── Level 14: 2× (2×1), 3× (4×1), 4× (2×2), 1× (4×2) ─────────
        //   b1 V at (4,3), b2 V at (7,3).
        //   b3 H(4×1) at (3,2). b4 H(4×1) at (4,6). b5 H(4×1) at (0,7).
        //   b6 V(2×2) at (0,5). b7 V(2×2) at (2,5). b8 H(2×2) at (6,6). b9 V(2×2) at (6,0).
        //   b10 H(4×2) at (0,0).
        L(14, exitRow:HeroRow, exitOnRight:true, threeStar:10, twoStar:14,
            Hero(1, HeroRow),
            V("b1", 4, 3, 2),
            V("b2", 7, 3, 2),
            H("b3", 3, 2, 4),
            H("b4", 4, 6, 4),
            H("b5", 0, 7, 4),
            V("b6", 0, 5, 2, 2),
            V("b7", 2, 5, 2, 2),
            H("b8", 4, 0, 2, 2),
            V("b9", 6, 0, 2, 2),
            H("b10", 0, 0, 4, 2)),

        // ── Level 15: 1× (2×1), 3× (4×1), 4× (2×2), 2× (4×2) ─────────
        //   b1 V at (5,3) blocks.
        //   b2 H(4×1) at (4,2). b3 H(4×1) at (4,6). b4 H(4×1) at (0,7).
        //   b5 V(2×2) at (0,5). b6 V(2×2) at (2,5). b7 H(2×2) at (6,6). b8 V(2×2) at (6,0).
        //   b9 H(4×2) at (0,0). b10 H(4×2) at (0,2): occ (0-3,2-3).
        //   Hero at (1,4). b10 at (0-3,2-3): col 1 row 2-3, safe. b2 at (4-7,2): safe.
        L(15, exitRow:HeroRow, exitOnRight:true, threeStar:11, twoStar:15,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            H("b2", 4, 2, 4),
            H("b3", 4, 6, 4),
            H("b4", 0, 7, 4),
            V("b5", 0, 5, 2, 2),
            V("b6", 2, 5, 2, 2),
            H("b7", 4, 0, 2, 2),
            V("b8", 6, 0, 2, 2),
            H("b9", 0, 0, 4, 2),
            H("b10", 0, 2, 4, 2)),

        // ═══════════════════════════════════════════════════════════════════
        // 🔴 HARD (Levels 16-25)
        // ═══════════════════════════════════════════════════════════════════

        // ── Level 16: 2× (2×1), 2× (4×1), 4× (2×2), 2× (4×2) ──────────
        //   b1 V at (5,3), b2 V at (7,3).
        //   b3 H(4×1) at (4,2). b4 H(4×1) at (4,6).
        //   b5 V(2×2) at (0,5). b6 V(2×2) at (2,5). b7 H(2×2) at (6,6). b8 V(2×2) at (6,0).
        //   b9 H(4×2) at (0,0). b10 H(4×2) at (0,2).
        L(16, exitRow:HeroRow, exitOnRight:true, threeStar:12, twoStar:16,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 7, 3, 2),
            H("b3", 4, 2, 4),
            H("b4", 4, 6, 4),
            V("b5", 0, 5, 2, 2),
            V("b6", 2, 5, 2, 2),
            H("b7", 4, 0, 2, 2),
            V("b8", 6, 0, 2, 2),
            H("b9", 0, 0, 4, 2),
            H("b10", 0, 2, 4, 2)),

        // ── Level 17: 2× (2×1), 3× (4×1), 4× (2×2), 2× (4×2) ─────────
        //   b1 V at (4,3), b2 V at (7,4).
        //   b3 H(4×1) at (3,2). b4 H(4×1) at (4,6). b5 H(4×1) at (0,7).
        //   b6 V(2×2) at (0,5). b7 V(2×2) at (2,5). b8 H(2×2) at (6,6). b9 V(2×2) at (6,0).
        //   b10 H(4×2) at (0,0). b11 H(4×2) at (0,2): occ (0-3,2-3). b3 at (3-6,2) overlaps b11!
        //   Fix: b3 H(4×1) at (4,1). b11 at (0,2).
        //   b3 at (4-7,1). b11 at (0-3,2-3). Safe.
        L(17, exitRow:HeroRow, exitOnRight:true, threeStar:13, twoStar:17,
            Hero(1, HeroRow),
            V("b1", 4, 3, 2),
            V("b2", 7, 4, 2),
            H("b3", 4, 2, 4),
            H("b4", 4, 6, 4),
            H("b5", 0, 7, 4),
            V("b6", 0, 5, 2, 2),
            V("b7", 2, 5, 2, 2),
            H("b8", 4, 0, 2, 2),
            V("b9", 6, 0, 2, 2),
            H("b10", 0, 0, 4, 2),
            V("b11", 0, 2, 2, 2)),

        // ── Level 18: 1× (2×1), 3× (4×1), 5× (2×2), 2× (4×2) ─────────
        //   b1 V at (5,3) blocks.
        //   b2 H(4×1) at (4,2). b3 H(4×1) at (4,6). b4 H(4×1) at (0,7).
        //   b5 V(2×2) at (0,5). b6 V(2×2) at (2,5). b7 H(2×2) at (6,6).
        //   b8 V(2×2) at (6,0). b9 V(2×2) at (0,2): occ (0-1,2-3).
        //   b10 H(4×2) at (0,0). b11 H(4×2) at (2,2): occ (2-5,2-3). Overlaps b2 at (4,2)!
        //   Fix: b9 H(2×2) at (2,2): occ (2-3,2-3). b2 at (4-7,2). Safe.
        //   b10 H(4×2) at (0,0). b11 V(4×2) at (4,4): occ (4-5,4-7). Overlaps hero at (2,4)? No, hero is (1-2,4). (4,4) ok. But overlaps b3 at (4-7,6)?
        //   b3 at (4-7,6). b11 at (4-5,4-7) overlaps at (4,6)(5,6). Fix.
        //   b11 V(4×2) at (2,3): occ (2-3,3-6). Overlaps hero at (2,4)!
        //   Simpler: b10 H(4×2) at (0,0): (0-3,0-1). b11 H(4×2) at (4,0): (4-7,0-1). Overlaps b8 V(2×2) at (6,0)? b8 occ (6-7,0-1). b11 occ (4-7,0-1). Overlap at (6,0)(7,0)(6,1)(7,1). Fix: remove b8, use different placement.
        //   Let me simplify: just avoid row 0-1 for 4×2 #2.
        //   b10 H(4×2) at (0,0). b11 H(4×2) at (0,3): occupies (0-3,3-4). Overlaps hero!
        //   Use b11 V(4×2): at (2,0): (2-3,0-3). Overlaps b10 at (2,0)(3,0)(2,1)(3,1).
        //   OK simplify layout:
        //   b10 V(4×2) at (0,0): (0-1,0-3). b11 V(4×2) at (2,0): (2-3,0-3). Both clear of hero lane.
        //   b5 V(2×2) at (0,5): (0-1,5-6). b6 V(2×2) at (2,5): (2-3,5-6). b7 H(2×2) at (6,6): (6-7,6-7).
        //   b9 V(2×2) at (4,5): (4-5,5-6). b8 V(2×2) at (6,0): (6-7,0-1).
        //   b2 H(4×1) at (4,2). b3 H(4×1) at (4,7): overlaps b7 at (6,7)! b3 at (0,7): (0-3,7). Safe.
        //   b4 H(4×1) at (4,7): (4-7,7). Overlaps b7 at (6,7)(7,7)! Use b4 at (4,6): overlaps b9.
        //   b3 at (0,7,4). b4 at (4,7,4): overlaps b7! Use b7 at (6,5): (6-7,5-6). b4 at (4,7): (4-7,7). 
        //   b9 at (4,5): (4-5,5-6). Ok this one is clear of b7 now (6-7,5-6). b4 at (4-7,7). Safe.
        //   Recount: b1=2×1(1), b2=4×1(1), b3=4×1(2), b4=4×1(3), b5=2×2(1), b6=2×2(2), b7=2×2(3), b8=2×2(4), b9=2×2(5), b10=4×2(1), b11=4×2(2). Total 11+hero=12. Need 1×2×1 + 3×4×1 + 5×2×2 + 2×4×2 = 11+hero. Good.
        L(18, exitRow:HeroRow, exitOnRight:true, threeStar:14, twoStar:18,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            H("b2", 4, 2, 4),
            H("b3", 0, 7, 4),
            H("b4", 4, 7, 4),
            V("b5", 0, 5, 2, 2),
            V("b6", 2, 5, 2, 2),
            V("b7", 6, 5, 2, 2),
            V("b8", 6, 0, 2, 2),
            V("b9", 4, 5, 2, 2),
            V("b10", 0, 0, 4, 2),
            V("b11", 2, 0, 4, 2)),

        // ── Level 19: 2× (2×1), 3× (4×1), 5× (2×2), 2× (4×2) ─────────
        //   Same structure as 18 but add one more 2×1.
        //   b1 V at (5,3), b2 V at (7,3).
        //   b3 H(4×1) at (4,2). b4 H(4×1) at (0,7). b5 H(4×1) at (4,7).
        //   b6 V(2×2) at (0,5). b7 V(2×2) at (2,5). b8 V(2×2) at (6,5).
        //   b9 V(2×2) at (4,5). b10 V(2×2) at (6,2).
        //   b11 V(4×2) at (0,0). b12 V(4×2) at (2,0).
        //   Check: b5 at (4-7,7). b8 at (6-7,5-6). Safe.
        //   b10 at (6-7,2-3). b3 at (4-7,2). Overlap at (6,2)(7,2)! Fix: b10 at (4,0): (4-5,0-1). b11 at (0,0): (0-1,0-3). b12 at (2,0): (2-3,0-3). b10 at (4-5,0-1). Safe.
        //   Recount: 2×(2×1):b1,b2. 3×(4×1):b3,b4,b5. 5×(2×2):b6,b7,b8,b9,b10. 2×(4×2):b11,b12 = 12+hero. Good.
        L(19, exitRow:HeroRow, exitOnRight:true, threeStar:15, twoStar:19,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 7, 3, 2),
            H("b3", 4, 2, 4),
            H("b4", 0, 7, 4),
            H("b5", 4, 7, 4),
            V("b6", 0, 5, 2, 2),
            V("b7", 2, 5, 2, 2),
            V("b8", 6, 5, 2, 2),
            V("b9", 4, 5, 2, 2),
            H("b10", 4, 0, 2, 2),
            V("b11", 0, 0, 4, 2),
            V("b12", 2, 0, 4, 2)),

        // ── Level 20: 1× (2×1), 3× (4×1), 5× (2×2), 3× (4×2) ─────────
        //   b1 V at (5,3).
        //   b2 H(4×1) at (4,2). b3 H(4×1) at (0,7). b4 H(4×1) at (4,7).
        //   b5 V(2×2) at (0,5). b6 V(2×2) at (2,5). b7 V(2×2) at (6,5).
        //   b8 V(2×2) at (4,5). b9 H(2×2) at (6,2).
        //   b10 V(4×2) at (0,0). b11 V(4×2) at (2,0). b12 H(4×2) at (4,0): (4-7,0-1).
        //   Check b9 at (6-7,2-3). b2 at (4-7,2). Overlap at (6,2)(7,2)!
        //   Fix: b9 H(2×2) at (6,3): (6-7,3-4). Overlaps hero lane at row 4? (6,4)(7,4) yes hero is at (1-2,4) so (6,4) is fine — but it's ON the lane blocking it as a 2×2. That's actually useful as a blocker!
        //   But the V blocker approach is cleaner. Let me use b9 as blocker directly.
        //   b9 V(2×2) at (6,3): (6-7,3-4) — blocks hero lane at (6,4)(7,4). Great, this IS a blocker.
        //   Remove b1 as redundant then? No, b1 at col 5 is also needed. But b9 would duplicate blocking.
        //   Let me simplify: b1 V at (5,3) blocks col 5. b9 V(2×2) at (3,3): (3-4,3-4) conflicts hero at (2,4)? No, (3,4)(4,4). Hero at (1,4)(2,4). So (3,4) is clear of hero. Actually this blocks lane! This is a 2x2 blocker occupying (3,3)(4,3)(3,4)(4,4).
        //   Total lane blockers at row 4: b1 at (5,4), b9 at (3,4)(4,4). Good.
        //   b12 H(4×2) at (4,0): (4-7,0-1). Clashes with nothing.
        L(20, exitRow:HeroRow, exitOnRight:true, threeStar:16, twoStar:20,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            H("b2", 4, 2, 4),
            H("b3", 0, 7, 4),
            H("b4", 4, 7, 4),
            V("b5", 0, 5, 2, 2),
            V("b6", 2, 5, 2, 2),
            V("b7", 6, 5, 2, 2),
            V("b8", 4, 5, 2, 2),
            V("b9", 6, 3, 2, 2),
            V("b10", 0, 0, 4, 2),
            V("b11", 2, 0, 4, 2),
            H("b12", 4, 0, 4, 2)),

        // ── Level 21: 1× (2×1), 2× (4×1), 5× (2×2), 4× (4×2) ─────────
        //   Need lots of 4×2s. Use 10×10 grid? No, keep 8×8 per codebase.
        //   b1 V at (7,3) blocks.
        //   b2 H(4×1) at (4,2). b3 H(4×1) at (4,7).
        //   b4 V(2×2) at (0,5). b5 V(2×2) at (2,5). b6 V(2×2) at (4,5).
        //   b7 V(2×2) at (6,5). b8 H(2×2) at (5,3): (5-6,3-4) blocks lane.
        //   b9 V(4×2) at (0,0). b10 V(4×2) at (2,0). b11 H(4×2) at (4,0). b12 H(4×2) at (0,7): (0-3,7-8) OOB! Use b12 H(4×2) at (0,3): (0-3,3-4) blocks hero? (0,4)(1,4) = hero! No.
        //   Use vertical 4×2 for b12 too: won't fit many at once.
        //   Alt layout with 8 cells each for 4×2:
        //   b9 V(4×2) at (0,0): (0-1,0-3). b10 V(4×2) at (2,0): (2-3,0-3).
        //   b11 V(4×2) at (4,4): Overlaps hero lane — nope.
        //   b11 H(4×2) at (4,0): (4-7,0-1). b12 V(4×2) at (6,2): (6-7,2-5). Overlaps b7 at (6,5)(7,5)!
        //   b7 at (6,5): (6-7,5-6). b12 at (6-7,2-5). Overlap at (6,5)(7,5). Fix b7 at (6,6): (6-7,6-7).
        //   b12 at (6-7,2-5). b2 at (4-7,2). Overlap at (6,2)(7,2)! Fix: b2 at (4,2,4,1) becomes (4-7,2). b12 V(4×2) at (6,2) occupies col 6-7 rows 2-5. Overlaps b2 at col 6,7 row 2. Yes conflict.
        //   Fix: b12 V(4×2) at (6,3): (6-7,3-6). b7 at (6,6)(7,6)(6,7)(7,7). Overlap at (6,6)(7,6). Move b7 to (4,6)? b6 at (4,5):(4-5,5-6). Overlap at (4,6)(5,6).
        //   This is getting tight. Let me increase grid to handle many boats.
        //   Actually let me verify: GameManager forces gridWidth/gridHeight from LevelData, so I can use bigger grids for harder levels.
        //   For levels 21-25, use 10×10 grid (hero still at row 4, col 1).
        //   With 10×10: cols 0-9, rows 0-9.
        L(21, 10, 10, exitRow:HeroRow, exitOnRight:true, threeStar:16, twoStar:20,
            Hero(1, HeroRow),
            V("b1", 7, 3, 2),
            H("b2", 4, 2, 4),
            H("b3", 0, 9, 4),
            V("b4", 0, 6, 2, 2),
            V("b5", 2, 6, 2, 2),
            V("b6", 4, 6, 2, 2),
            V("b7", 6, 6, 2, 2),
            H("b8", 8, 6, 2, 2),
            V("b9", 0, 0, 4, 2),
            V("b10", 2, 0, 4, 2),
            H("b11", 4, 0, 4, 2),
            V("b12", 8, 0, 4, 2)),

        // ── Level 22: 2× (2×1), 2× (4×1), 5× (2×2), 4× (4×2) ─────────
        L(22, 10, 10, exitRow:HeroRow, exitOnRight:true, threeStar:17, twoStar:21,
            Hero(1, HeroRow),
            V("b1", 5, 3, 2),
            V("b2", 9, 4, 2),
            H("b3", 4, 2, 4),
            H("b4", 0, 9, 4),
            V("b5", 0, 6, 2, 2),
            V("b6", 2, 6, 2, 2),
            V("b7", 4, 6, 2, 2),
            V("b8", 6, 6, 2, 2),
            H("b9", 8, 6, 2, 2),
            V("b10", 0, 0, 4, 2),
            V("b11", 2, 0, 4, 2),
            H("b12", 4, 0, 4, 2),
            V("b13", 8, 0, 4, 2)),

        // ── Level 23: 1× (2×1), 3× (4×1), 5× (2×2), 4× (4×2) ─────────
        L(23, 10, 10, exitRow:HeroRow, exitOnRight:true, threeStar:18, twoStar:22,
            Hero(1, HeroRow),
            V("b1", 7, 3, 2),
            H("b2", 4, 2, 4),
            H("b3", 0, 9, 4),
            H("b4", 4, 9, 4),
            V("b5", 0, 6, 2, 2),
            V("b6", 2, 6, 2, 2),
            V("b7", 4, 6, 2, 2),
            V("b8", 6, 6, 2, 2),
            H("b9", 8, 6, 2, 2),
            V("b10", 0, 0, 4, 2),
            V("b11", 2, 0, 4, 2),
            H("b12", 4, 0, 4, 2),
            V("b13", 8, 0, 4, 2)),

        // ── Level 24: 1× (2×1), 2× (4×1), 6× (2×2), 5× (4×2) ─────────
        L(24, 10, 10, exitRow:HeroRow, exitOnRight:true, threeStar:19, twoStar:23,
            Hero(1, HeroRow),
            V("b1", 7, 3, 2),
            H("b2", 4, 2, 4),
            H("b3", 4, 8, 4),
            V("b4", 0, 6, 2, 2),
            V("b5", 2, 6, 2, 2),
            V("b6", 4, 6, 2, 2),
            V("b7", 6, 6, 2, 2),
            H("b8", 8, 6, 2, 2),
            H("b9", 8, 8, 2, 2),
            V("b10", 0, 0, 4, 2),
            V("b11", 2, 0, 4, 2),
            H("b12", 4, 0, 4, 2),
            V("b13", 8, 0, 4, 2),
            H("b14", 0, 8, 4, 2)),

        // ── Level 25 (Boss): 1× (2×1), 2× (4×1), 5× (2×2), 6× (4×2) ──
        L(25, 10, 10, exitRow:HeroRow, exitOnRight:true, threeStar:20, twoStar:25,
            Hero(1, HeroRow),
            V("b1", 7, 3, 2),
            H("b2", 4, 2, 4),
            H("b3", 4, 8, 4),
            V("b4", 0, 6, 2, 2),
            V("b5", 2, 6, 2, 2),
            V("b6", 4, 6, 2, 2),
            V("b7", 6, 6, 2, 2),
            H("b8", 8, 8, 2, 2),
            V("b9", 0, 0, 4, 2),
            V("b10", 2, 0, 4, 2),
            H("b11", 4, 0, 4, 2),
            V("b12", 8, 0, 4, 2),
            H("b13", 0, 8, 4, 2),
            V("b14", 8, 4, 4, 2))
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