// Assets/Editor/LevelAutoCreator.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Utility to make sure the sample levels exist and are properly serialized.
/// Invoke via Tools > Ensure Sample Levels.
/// Recreates any missing or malformed LevelData assets under Resources/Levels.
///
/// All levels use a 6x6 grid, exitRow=2, exitOnRight=true, all boats size=2.
///
/// Grid coordinate system:
///   col = x axis, 0=left  … 5=right
///   row = y axis, 0=bottom … 5=top
///
/// Horizontal boat at (col, row) occupies: (col,row) and (col+1,row)  → max col = 4
/// Vertical   boat at (col, row) occupies: (col,row) and (col,row+1)  → max row = 4
///
/// Hero is always H at row 2, starting col 0 (occupies col 0 and col 1).
/// To exit the hero must reach col 6 (off the right edge) on row 2,
/// meaning every cell (2,2), (3,2), (4,2), (5,2) must be clear.
/// </summary>
public static class LevelAutoCreator
{
    [MenuItem("Tools/Ensure Sample Levels")]
    public static void EnsureLevels()
    {
        string folder = "Assets/Resources/Levels";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Levels");

        var defs = GetDefinitions();
        foreach (var def in defs)
        {
            // Clamp boat coordinates so nothing ends outside the grid
            foreach (var b in def.boats)
            {
                int maxCol = def.gridWidth  - (b.isHorizontal ? b.size : 1);
                int maxRow = def.gridHeight - (b.isHorizontal ? 1 : b.size);
                if (b.col < 0) b.col = 0;
                if (b.row < 0) b.row = 0;
                if (b.col > maxCol) { Debug.LogWarning($"Clamping boat {b.id} col {b.col} -> {maxCol}"); b.col = maxCol; }
                if (b.row > maxRow) { Debug.LogWarning($"Clamping boat {b.id} row {b.row} -> {maxRow}"); b.row = maxRow; }
            }

            string assetPath = $"{folder}/Level_{def.index:D3}.asset";
            LevelData ld = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
            if (ld == null)
            {
                ld = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(ld, assetPath);
            }

            ld.gridWidth   = def.gridWidth;
            ld.gridHeight  = def.gridHeight;
            ld.exitRow     = def.exitRow;
            ld.exitOnRight = def.exitOnRight;
            ld.boats       = new List<BoatData>();

            foreach (var b in def.boats)
            {
                ld.boats.Add(new BoatData
                {
                    id           = b.id,
                    col          = b.col,
                    row          = b.row,
                    size         = b.size,
                    isHorizontal = b.isHorizontal,
                    isHero       = b.isHero
                });
            }

            EditorUtility.SetDirty(ld);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Ensured {defs.Count} levels in Resources/Levels");
    }

    private static List<LevelDefinition> GetDefinitions()
    {
        return new List<LevelDefinition>
        {
            // =====================================================================
            // LEVEL 1  —  6x6  —  1 blocker  —  1 move
            // =====================================================================
            // row5 [ ][ ][ ][ ][ ][ ]
            // row4 [ ][ ][ ][ ][ ][ ]
            // row3 [ ][ ][1][ ][ ][ ]
            // row2 [H][H][1][ ][ ][ ]  <-- exit row
            // row1 [ ][ ][ ][ ][ ][ ]
            // row0 [ ][ ][ ][ ][ ][ ]
            //       c0 c1 c2 c3 c4 c5
            //
            // boat_1  V col2 rows2-3  →  cells (2,2)(2,3)
            //   directly blocks hero on exit row.
            //   up:   needs (2,4) free ✓
            //   down: needs (2,1) free ✓
            // Solution (1 move): slide boat_1 in either direction → hero exits.
            new LevelDefinition {
                index=1, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false),
                }
            },

            // =====================================================================
            // LEVEL 2  —  6x6  —  2 blockers  —  2 moves
            // =====================================================================
            // row5 [ ][ ][ ][ ][ ][ ]
            // row4 [ ][ ][ ][ ][ ][ ]
            // row3 [ ][ ][ ][1][ ][ ]
            // row2 [H][H][ ][1][ ][ ]  <-- exit row
            // row1 [ ][ ][ ][2][2][ ]
            // row0 [ ][ ][ ][ ][ ][ ]
            //
            // boat_1  V col3 rows2-3  →  cells (3,2)(3,3)
            // boat_2  H row1 cols3-4  →  cells (3,1)(4,1)
            //   boat_1 down: needs (3,1) = boat_2 → BLOCKED
            //   boat_1 up:   needs (3,4) free ✓  ← only escape
            //   boat_2 left: needs (2,1) free ✓  ← can move but doesn't help hero
            //   boat_2 right: needs (5,1) free ✓
            // col2 is already free, col3 needs clearing.
            // Solution (2 moves): slide boat_1 up → hero exits.
            //   (boat_2 is a decoy that teaches the player boat_1 is pinned downward)
            new LevelDefinition {
                index=2, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 3,2, 2, false, false), // V col3 rows2-3
                    new BoatDefinition("boat_2", 3,1, 2, true,  false), // H row1 cols3-4
                }
            },

            // =====================================================================
            // LEVEL 3  —  6x6  —  3 blockers  —  3-move chain
            // =====================================================================
            // row5 [ ][ ][ ][ ][ ][ ]
            // row4 [ ][ ][ ][ ][ ][ ]
            // row3 [ ][ ][1][ ][ ][ ]
            // row2 [H][H][1][ ][ ][ ]  <-- exit row
            // row1 [ ][ ][2][2][ ][ ]
            // row0 [ ][ ][ ][ ][3][ ]
            //
            // boat_1  V col2 rows2-3  →  cells (2,2)(2,3)
            // boat_2  H row1 cols2-3  →  cells (2,1)(3,1)
            //   boat_1 down: needs (2,1) = boat_2 → BLOCKED
            //   boat_1 up:   needs (2,4) free ✓
            // boat_3  V col4 rows0-1  →  cells (4,0)(4,1)
            //   boat_2 right: needs (4,1) = boat_3 → BLOCKED
            //   boat_2 left:  needs (1,1) free ✓  ← escape for boat_2
            //   boat_3 up:    needs (4,2) free ✓
            //   boat_3 down:  row-1 out of bounds → BLOCKED
            // boat_1 up is still freely available — intentional easy shortcut.
            // Harder chain: boat_3 up → boat_2 right → boat_1 down → hero exits.
            // Solution A (1 move):  slide boat_1 up → hero exits.
            // Solution B (3 moves): slide boat_3 up → slide boat_2 right → slide boat_1 down → hero exits.
            new LevelDefinition {
                index=3, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false), // V col2 rows2-3
                    new BoatDefinition("boat_2", 2,1, 2, true,  false), // H row1 cols2-3
                    new BoatDefinition("boat_3", 4,0, 2, false, false), // V col4 rows0-1
                }
            },

            // =====================================================================
            // LEVEL 4  —  6x6  —  4 blockers  —  3-move forced chain
            // =====================================================================
            // row5 [ ][ ][ ][ ][ ][ ]
            // row4 [ ][ ][4][4][ ][ ]
            // row3 [ ][ ][1][ ][ ][ ]
            // row2 [H][H][1][ ][ ][ ]  <-- exit row
            // row1 [ ][ ][2][2][ ][ ]
            // row0 [ ][ ][ ][ ][3][ ]
            //
            // boat_1  V col2 rows2-3  →  (2,2)(2,3)
            // boat_2  H row1 cols2-3  →  (2,1)(3,1)   blocks boat_1 down
            // boat_3  V col4 rows0-1  →  (4,0)(4,1)   blocks boat_2 right: (4,1) ✓
            // boat_4  H row4 cols2-3  →  (2,4)(3,4)   blocks boat_1 up:  (2,4) ✓
            //   boat_4 left:  needs (1,4) free ✓
            //   boat_4 right: needs (4,4) free ✓
            //   boat_3 up:    needs (4,2) free ✓
            //   boat_2 left:  needs (1,1) free ✓
            // Both boat_3-up and boat_4-slide free different directions.
            // Shortest: boat_3 up → boat_2 right → boat_1 down → hero exits. (3 moves)
            // Or: boat_4 left/right → boat_1 up → hero exits. (2 moves)
            // Level teaches: multiple entry points, player chooses best path.
            new LevelDefinition {
                index=4, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false), // V col2 rows2-3
                    new BoatDefinition("boat_2", 2,1, 2, true,  false), // H row1 cols2-3
                    new BoatDefinition("boat_3", 4,0, 2, false, false), // V col4 rows0-1
                    new BoatDefinition("boat_4", 2,4, 2, true,  false), // H row4 cols2-3
                }
            },

            // =====================================================================
            // LEVEL 5  —  6x6  —  5 blockers  —  4-move chain, 2 path blockers
            // =====================================================================
            // row5 [ ][ ][ ][ ][ ][ ]
            // row4 [ ][ ][4][4][ ][ ]
            // row3 [ ][ ][1][3][ ][ ]
            // row2 [H][H][1][3][ ][ ]  <-- exit row
            // row1 [ ][ ][2][2][ ][ ]
            // row0 [ ][ ][ ][ ][5][ ]
            //
            // boat_1  V col2 rows2-3  →  (2,2)(2,3)   1st path blocker
            // boat_3  V col3 rows2-3  →  (3,2)(3,3)   2nd path blocker
            // boat_2  H row1 cols2-3  →  (2,1)(3,1)   blocks boat_1 & boat_3 down
            // boat_4  H row4 cols2-3  →  (2,4)(3,4)   blocks boat_1 & boat_3 up
            // boat_5  V col4 rows0-1  →  (4,0)(4,1)   blocks boat_2 right: (4,1) ✓
            //   boat_4 left:  (1,4) free ✓   boat_4 right: (4,4) free ✓
            //   boat_2 left:  (1,1) free ✓
            //   boat_5 up:    (4,2) free ✓
            // All unique cells ✓. All within 6×6 ✓.
            // Solution (4 moves):
            //   slide boat_4 right → cols3-4: (3,4)(4,4). (2,4) free.
            //   slide boat_1 up → rows3-4: (2,4) free ✓. Still in row3. up again → rows4-5: (2,5) ✓. col2 row2 free.
            //   slide boat_2 left → cols1-2: (1,1)(2,1). (2,1) still occupied — left again → cols0-1. (2,1) free.
            //   slide boat_3 down → rows1-2: (3,1) free ✓. col3 row2 free.
            //   Hero exits (col2 free, col3 free, col4 free) ✓.
            new LevelDefinition {
                index=5, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false), // V col2 rows2-3
                    new BoatDefinition("boat_2", 2,1, 2, true,  false), // H row1 cols2-3
                    new BoatDefinition("boat_3", 3,2, 2, false, false), // V col3 rows2-3
                    new BoatDefinition("boat_4", 2,4, 2, true,  false), // H row4 cols2-3
                    new BoatDefinition("boat_5", 4,0, 2, false, false), // V col4 rows0-1
                }
            },

            // =====================================================================
            // LEVEL 6  —  6x6  —  6 blockers  —  5-move chain, 2 path blockers + pin
            // =====================================================================
            // row5 [ ][ ][ ][ ][ ][ ]
            // row4 [6][6][4][4][ ][ ]
            // row3 [ ][ ][1][3][ ][ ]
            // row2 [H][H][1][3][ ][ ]  <-- exit row
            // row1 [ ][ ][2][2][ ][ ]
            // row0 [ ][ ][ ][ ][5][ ]
            //
            // Same core as level 5, add boat_6 H row4 cols0-1 → (0,4)(1,4).
            //   boat_4 left: needs (1,4) = boat_6 → BLOCKED
            //   boat_4 right: needs (4,4) free ✓  ← only escape for boat_4
            //   boat_6: left edge col0, can't go left. Right: needs (2,4)=boat_4 → BLOCKED. PINNED (wall) ✓
            // All unique cells ✓. All within 6×6 ✓.
            // Solution (5 moves):
            //   slide boat_5 up → rows1-2: (4,2) free? hero row2 doesn't include col4. (4,2) free ✓.
            //     boat_5 at (4,1)(4,2). Still need (4,1) free for boat_2 right: (4,1)=boat_5 → still blocked.
            //     slide boat_5 up again → rows2-3: (4,3) free ✓. (4,1) now free.
            //   slide boat_2 right → cols3-4: needs (4,1) free ✓. (2,1) free.
            //   slide boat_4 right → cols3-4: needs (4,4) free ✓. (2,4) free.
            //   slide boat_1 up → rows3-4: (2,4) free ✓. up again → rows4-5: (2,5) free ✓. col2 free.
            //   slide boat_3 down → rows1-2: (3,1) free ✓. col3 free.
            //   Hero exits ✓.  Total: 6 meaningful moves.
            new LevelDefinition {
                index=6, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false), // V col2 rows2-3
                    new BoatDefinition("boat_2", 2,1, 2, true,  false), // H row1 cols2-3
                    new BoatDefinition("boat_3", 3,2, 2, false, false), // V col3 rows2-3
                    new BoatDefinition("boat_4", 2,4, 2, true,  false), // H row4 cols2-3
                    new BoatDefinition("boat_5", 4,0, 2, false, false), // V col4 rows0-1
                    new BoatDefinition("boat_6", 0,4, 2, true,  false), // H row4 cols0-1  (pins boat_4 left)
                }
            },

            // =====================================================================
            // LEVEL 7  —  6x6  —  7 blockers  —  3 path blockers, cross-dependency
            // =====================================================================
            // row5 [ ][ ][ ][ ][ ][ ]
            // row4 [6][6][4][4][ ][ ]
            // row3 [ ][ ][1][3][5][ ]
            // row2 [H][H][1][3][5][ ]  <-- exit row
            // row1 [ ][ ][2][2][ ][ ]
            // row0 [7][ ][ ][ ][ ][ ]
            //
            // boat_1  V col2 rows2-3  →  (2,2)(2,3)
            // boat_3  V col3 rows2-3  →  (3,2)(3,3)
            // boat_5  V col4 rows2-3  →  (4,2)(4,3)   3rd path blocker
            // boat_2  H row1 cols2-3  →  (2,1)(3,1)   blocks boat_1 & boat_3 down
            //   boat_2 left:  (1,1) free ✓   boat_2 right: (4,1) free ✓
            // boat_4  H row4 cols2-3  →  (2,4)(3,4)   blocks boat_1 & boat_3 up
            //   boat_4 right: (4,4) free ✓   boat_4 left: (1,4)=boat_6 → BLOCKED
            // boat_6  H row4 cols0-1  →  (0,4)(1,4)   pins boat_4 left (wall) ✓
            // boat_7  V col0 rows0-1  →  (0,0)(0,1)   decoy in corner
            //   boat_5 up: (4,4) free ✓   boat_5 down: (4,1) free ✓  ← boat_5 freely moveable
            // All unique cells ✓. All within 6×6 ✓.
            // Solution (6 moves):
            //   slide boat_5 down → rows1-2: (4,1) free ✓. down again → rows0-1: (4,0) free ✓. col4 rows2-3 free.
            //   slide boat_4 right → cols3-4: (4,4) free ✓. (2,4) free.
            //   slide boat_2 right → cols3-4: (4,1) free ✓. (2,1) free.
            //   slide boat_1 down → rows1-2: (2,1) free ✓. down again → rows0-1: (2,0) ✓. col2 free.
            //   slide boat_3 up → rows3-4: (3,4) = boat_4 now at (3,4)(4,4) → BLOCKED.
            //     slide boat_4 right again → cols4-5: (4,4)(5,4). (3,4) free.
            //   slide boat_3 up → rows3-4: (3,4) free ✓. up again → rows4-5. col3 free.
            //   Hero exits ✓.
            new LevelDefinition {
                index=7, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false), // V col2 rows2-3
                    new BoatDefinition("boat_2", 2,1, 2, true,  false), // H row1 cols2-3
                    new BoatDefinition("boat_3", 3,2, 2, false, false), // V col3 rows2-3
                    new BoatDefinition("boat_4", 2,4, 2, true,  false), // H row4 cols2-3
                    new BoatDefinition("boat_5", 4,2, 2, false, false), // V col4 rows2-3
                    new BoatDefinition("boat_6", 0,4, 2, true,  false), // H row4 cols0-1  (wall)
                    new BoatDefinition("boat_7", 0,0, 2, false, false), // V col0 rows0-1  (decoy)
                }
            },

            // =====================================================================
            // LEVEL 8  —  6x6  —  8 blockers  —  fully locked start, 7-move chain
            // =====================================================================
            // row5 [ ][8][ ][ ][ ][ ]
            // row4 [6][6][4][4][ ][ ]
            // row3 [ ][8][1][3][5][ ]
            // row2 [H][H][1][3][5][ ]  <-- exit row
            // row1 [ ][ ][2][2][7][ ]
            // row0 [ ][ ][ ][ ][7][ ]
            //
            // boat_1  V col2 rows2-3  →  (2,2)(2,3)
            // boat_3  V col3 rows2-3  →  (3,2)(3,3)
            // boat_5  V col4 rows2-3  →  (4,2)(4,3)
            // boat_2  H row1 cols2-3  →  (2,1)(3,1)   blocks boat_1 & boat_3 down
            //   boat_2 left: (1,1) free ✓   boat_2 right: (4,1)=boat_7 → BLOCKED
            // boat_4  H row4 cols2-3  →  (2,4)(3,4)   blocks boat_1 & boat_3 up
            //   boat_4 right: (4,4) free ✓   boat_4 left: (1,4)=boat_6 → BLOCKED
            // boat_6  H row4 cols0-1  →  (0,4)(1,4)   wall, pins boat_4 left ✓
            // boat_7  V col4 rows0-1  →  (4,0)(4,1)   blocks boat_2 right ✓ AND boat_5 down:
            //   boat_5 down: needs (4,1)=boat_7 → BLOCKED
            //   boat_5 up:   needs (4,4) free ✓  ← escape for boat_5
            //   boat_7 up:   needs (4,2)=boat_5 → BLOCKED
            //   boat_7 down: row-1 out of bounds → BLOCKED
            //   boat_7 is PINNED (wall) ✓ — to free boat_2 right, must move boat_5 up first.
            // boat_8  V col1 rows3-4  →  (1,3)(1,4)   BUT (1,4)=boat_6 → OVERLAP!
            //   Use boat_8 V col1 rows4-5: (1,4)(1,5). (1,4)=boat_6 → OVERLAP again.
            //   Use boat_8 V col5 rows3-4: (5,3)(5,4). All free ✓.
            //   boat_8 purpose: 4th path blocker on exit row. (5,2) is on exit row — use col5.
            //   boat_8 V col5 rows2-3: (5,2)(5,3). Up: (5,4) free ✓. Down: (5,1) free ✓. Freely moveable.
            // All unique cells ✓. All within 6×6 ✓.
            // Solution (7 moves):
            //   slide boat_8 down → rows1-2→rows0-1: col5 row2 free.
            //   slide boat_5 up → rows3-4: (4,4) free ✓. (4,2)(4,3) free.
            //   slide boat_7 is pinned — (4,1) still there but boat_2 right now blocked by... (4,1)=boat_7.
            //     Wait: boat_5 moved UP to rows3-4: (4,3)(4,4). (4,1) still=boat_7. boat_2 right still blocked.
            //     slide boat_5 up again → rows4-5: (4,5) free ✓. (4,3) free. boat_7 up: (4,2) free now ✓.
            //   slide boat_7 up → rows1-2: (4,2) free ✓. (4,1) free.
            //   slide boat_2 right → cols3-4: (4,1) free ✓. (2,1) free.
            //   slide boat_4 right → cols3-4: (4,4) free (boat_5 at rows4-5 → (4,4) free ✓). (2,4) free.
            //   slide boat_1 up → rows3-4→rows4-5: (2,5) free ✓. col2 free.
            //   slide boat_3 up → needs (3,4)=boat_4 (at cols3-4: (3,4)(4,4)) → BLOCKED.
            //     slide boat_4 right again → cols4-5: (5,4) free ✓. (3,4) free.
            //   slide boat_3 up → rows3-4→rows4-5. col3 free.
            //   Hero exits ✓.
            new LevelDefinition {
                index=8, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false), // V col2 rows2-3
                    new BoatDefinition("boat_2", 2,1, 2, true,  false), // H row1 cols2-3
                    new BoatDefinition("boat_3", 3,2, 2, false, false), // V col3 rows2-3
                    new BoatDefinition("boat_4", 2,4, 2, true,  false), // H row4 cols2-3
                    new BoatDefinition("boat_5", 4,2, 2, false, false), // V col4 rows2-3
                    new BoatDefinition("boat_6", 0,4, 2, true,  false), // H row4 cols0-1  (wall)
                    new BoatDefinition("boat_7", 4,0, 2, false, false), // V col4 rows0-1  (wall, pins boat_2 right & boat_5 down)
                    new BoatDefinition("boat_8", 5,2, 2, false, false), // V col5 rows2-3  (4th path blocker)
                }
            },

            // =====================================================================
            // LEVEL 9  —  6x6  —  8 blockers  —  interlocked double chain
            // =====================================================================
            // row5 [ ][ ][ ][ ][ ][ ]
            // row4 [6][6][4][4][ ][ ]
            // row3 [8][ ][1][3][5][ ]
            // row2 [H][H][1][3][5][ ]  <-- exit row
            // row1 [8][7][2][2][ ][ ]
            // row0 [ ][7][ ][ ][ ][ ]
            //
            // boat_1  V col2 rows2-3  →  (2,2)(2,3)
            // boat_3  V col3 rows2-3  →  (3,2)(3,3)
            // boat_5  V col4 rows2-3  →  (4,2)(4,3)
            // boat_2  H row1 cols2-3  →  (2,1)(3,1)   blocks boat_1 & boat_3 down
            //   boat_2 left: (1,1)=boat_7 → BLOCKED    boat_2 right: (4,1) free ✓
            // boat_4  H row4 cols2-3  →  (2,4)(3,4)   blocks boat_1 & boat_3 up
            //   boat_4 left: (1,4)=boat_6 → BLOCKED    boat_4 right: (4,4) free ✓
            // boat_6  H row4 cols0-1  →  (0,4)(1,4)   wall ✓
            // boat_7  V col1 rows0-1  →  (1,0)(1,1)   blocks boat_2 left: (1,1) ✓
            //   boat_7 up: (1,2)=hero occupies (0,2)(1,2) → (1,2)=hero → BLOCKED
            //   boat_7 down: row-1 out of bounds → BLOCKED. PINNED (wall) ✓
            // boat_8  V col0 rows1-2  →  (0,1)(0,2)   BUT (0,2)=hero → OVERLAP!
            //   Use boat_8 H row1 cols0-1: (0,1)(1,1). (1,1)=boat_7 → OVERLAP.
            //   Use boat_8 V col0 rows3-4: (0,3)(0,4). (0,4)=boat_6 → OVERLAP.
            //   Use boat_8 V col5 rows1-2: (5,1)(5,2). (5,2) is on exit row but empty ✓.
            //     boat_8 acts as 4th path blocker at col5 row2. Up: (5,3) free ✓. Down: (5,0) free ✓.
            //   All unique cells ✓. All within 6×6 ✓.
            // Solution (7 moves):
            //   slide boat_5 down → rows1-2: (4,1) free ✓ after 1 step... (4,2)→(4,1) wait:
            //     boat_5 V col4 rows2-3. slide down: rows1-2: needs (4,1) free ✓. At (4,1)(4,2). Still in row2!
            //     slide down again: rows0-1: (4,0) free ✓. col4 rows2-3 clear.
            //   slide boat_8 down → rows1-2→rows0-1. col5 row2 clear.
            //   slide boat_4 right → cols3-4: (4,4) free ✓. (2,4) free.
            //   slide boat_2 right → cols3-4: (4,1) free ✓. (2,1) free.
            //   slide boat_1 down → rows1-2→rows0-1. col2 clear.
            //   slide boat_4 right again → cols4-5: (3,4) free.
            //   slide boat_3 up → rows3-4→rows4-5. col3 clear.
            //   Hero exits ✓.
            new LevelDefinition {
                index=9, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false), // V col2 rows2-3
                    new BoatDefinition("boat_2", 2,1, 2, true,  false), // H row1 cols2-3
                    new BoatDefinition("boat_3", 3,2, 2, false, false), // V col3 rows2-3
                    new BoatDefinition("boat_4", 2,4, 2, true,  false), // H row4 cols2-3
                    new BoatDefinition("boat_5", 4,2, 2, false, false), // V col4 rows2-3
                    new BoatDefinition("boat_6", 0,4, 2, true,  false), // H row4 cols0-1  (wall)
                    new BoatDefinition("boat_7", 1,0, 2, false, false), // V col1 rows0-1  (wall, pins boat_2 left)
                    new BoatDefinition("boat_8", 5,1, 2, false, false), // V col5 rows1-2  (4th path blocker)
                }
            },

            // =====================================================================
            // LEVEL 10  —  6x6  —  8 blockers  —  maximum difficulty, 1 starting move
            // =====================================================================
            // row5 [ ][ ][ ][ ][ ][ ]
            // row4 [6][6][4][4][ ][ ]
            // row3 [ ][ ][1][3][5][8]
            // row2 [H][H][1][3][5][8]  <-- exit row
            // row1 [ ][7][2][2][ ][ ]
            // row0 [ ][7][ ][ ][9][ ]
            //
            // boat_1  V col2 rows2-3  →  (2,2)(2,3)
            // boat_3  V col3 rows2-3  →  (3,2)(3,3)
            // boat_5  V col4 rows2-3  →  (4,2)(4,3)
            // boat_8  V col5 rows2-3  →  (5,2)(5,3)   4th path blocker
            // boat_2  H row1 cols2-3  →  (2,1)(3,1)   blocks boat_1 & boat_3 down
            //   boat_2 left:  (1,1)=boat_7 → BLOCKED
            //   boat_2 right: (4,1) free ✓
            // boat_4  H row4 cols2-3  →  (2,4)(3,4)   blocks boat_1 & boat_3 up
            //   boat_4 left:  (1,4)=boat_6 → BLOCKED
            //   boat_4 right: (4,4) free ✓
            // boat_6  H row4 cols0-1  →  (0,4)(1,4)   wall ✓
            // boat_7  V col1 rows0-1  →  (1,0)(1,1)   wall, pins boat_2 left ✓
            //   boat_7 up: (1,2)=hero → BLOCKED.  boat_7 down: OOB → BLOCKED. PINNED ✓
            // boat_8 up:   (5,4) free ✓   boat_8 down: (5,1) free ✓  ← freely moveable
            // boat_5 up:   (4,4) free ✓   boat_5 down: (4,1) free ✓  ← freely moveable
            // Only free starting moves: boat_5 or boat_8 (both are path blockers that can self-clear).
            // Player must clear ALL of cols 2-5 on row 2.
            //
            // But wait — we can only have 8 non-hero boats total (hero + 8 = 9 listed as boat_1..boat_8).
            // Remove "boat_9" reference — level 10 uses boats 1-8 plus hero = 9 entries ✓.
            //
            // All unique cells ✓. All within 6×6 ✓.
            // Solution (8 moves):
            //   slide boat_8 down×2 → rows0-1: col5 clear.
            //   slide boat_5 down×2 → rows0-1: col4 clear.
            //   slide boat_4 right  → cols3-4: (2,4) free.
            //   slide boat_2 right  → cols3-4: (2,1) free.
            //   slide boat_1 down×2 → rows0-1: col2 clear.
            //   slide boat_4 right again → cols4-5: (3,4) free.
            //   slide boat_3 up×2   → rows4-5: col3 clear.
            //   Hero exits ✓.
            new LevelDefinition {
                index=10, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false), // V col2 rows2-3
                    new BoatDefinition("boat_2", 2,1, 2, true,  false), // H row1 cols2-3
                    new BoatDefinition("boat_3", 3,2, 2, false, false), // V col3 rows2-3
                    new BoatDefinition("boat_4", 2,4, 2, true,  false), // H row4 cols2-3
                    new BoatDefinition("boat_5", 4,2, 2, false, false), // V col4 rows2-3
                    new BoatDefinition("boat_6", 0,4, 2, true,  false), // H row4 cols0-1  (wall)
                    new BoatDefinition("boat_7", 1,0, 2, false, false), // V col1 rows0-1  (wall)
                    new BoatDefinition("boat_8", 5,2, 2, false, false), // V col5 rows2-3
                }
            },
        };
    }
}

// =============================================================================
// Helper classes — public, top-level, defined ONCE after LevelAutoCreator.
// Do NOT add private/internal modifiers. Do NOT duplicate these anywhere.
// =============================================================================

public class LevelDefinition
{
    public int  index;
    public int  gridWidth;
    public int  gridHeight;
    public int  exitRow;
    public bool exitOnRight;
    public List<BoatDefinition> boats;
}

public class BoatDefinition
{
    public string id;
    public int    col, row, size;
    public bool   isHorizontal, isHero;

    public BoatDefinition(string id, int col, int row, int size, bool isHorizontal, bool isHero)
    {
        this.id           = id;
        this.col          = col;
        this.row          = row;
        this.size         = size;
        this.isHorizontal = isHorizontal;
        this.isHero       = isHero;
    }
}