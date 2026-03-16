// Assets/Editor/LevelAutoCreator.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Utility to make sure the sample levels exist and are properly serialized.
/// Invoke via Tools > Ensure Sample Levels.
///
/// Star thresholds per level:
///   threeStarMoves — finish in this many moves or fewer → 3 stars
///   twoStarMoves   — finish in this many moves or fewer → 2 stars (else 1 star)
///
/// Thresholds are set equal to the known optimal solution length (3 stars)
/// and roughly double it (2 stars), so skilled players are rewarded.
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

            ld.gridWidth      = def.gridWidth;
            ld.gridHeight     = def.gridHeight;
            ld.exitRow        = def.exitRow;
            ld.exitOnRight    = def.exitOnRight;
            ld.threeStarMoves = def.threeStarMoves;
            ld.twoStarMoves   = def.twoStarMoves;
            ld.boats          = new List<BoatData>();

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
            // LEVEL 1  —  1 blocker  —  optimal: 1 move
            // =====================================================================
            new LevelDefinition {
                index=1, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                threeStarMoves=1, twoStarMoves=3,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,1, 3, false, false),
                }
            },

            // =====================================================================
            // LEVEL 2  —  2 blockers  —  optimal: 2 moves
            // =====================================================================
            new LevelDefinition {
                index=2, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                threeStarMoves=2, twoStarMoves=5,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 3,2, 2, false, false),
                    new BoatDefinition("boat_2", 3,1, 2, true,  false),
                }
            },

            // =====================================================================
            // LEVEL 3  —  3 blockers  —  optimal: 1-3 moves
            // =====================================================================
            new LevelDefinition {
                index=3, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                threeStarMoves=3, twoStarMoves=6,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false),
                    new BoatDefinition("boat_2", 2,1, 2, true,  false),
                    new BoatDefinition("boat_3", 4,0, 2, false, false),
                }
            },

            // =====================================================================
            // LEVEL 4  —  4 blockers  —  optimal: 2-3 moves
            // =====================================================================
            new LevelDefinition {
                index=4, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                threeStarMoves=3, twoStarMoves=7,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false),
                    new BoatDefinition("boat_2", 2,1, 2, true,  false),
                    new BoatDefinition("boat_3", 4,0, 2, false, false),
                    new BoatDefinition("boat_4", 2,4, 2, true,  false),
                }
            },

            // =====================================================================
            // LEVEL 5  —  5 blockers  —  optimal: 4 moves
            // =====================================================================
            new LevelDefinition {
                index=5, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                threeStarMoves=4, twoStarMoves=8,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false),
                    new BoatDefinition("boat_2", 2,1, 2, true,  false),
                    new BoatDefinition("boat_3", 3,2, 2, false, false),
                    new BoatDefinition("boat_4", 2,4, 2, true,  false),
                    new BoatDefinition("boat_5", 4,0, 2, false, false),
                }
            },

            // =====================================================================
            // LEVEL 6  —  6 blockers  —  optimal: 5 moves
            // =====================================================================
            new LevelDefinition {
                index=6, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                threeStarMoves=5, twoStarMoves=10,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false),
                    new BoatDefinition("boat_2", 2,1, 2, true,  false),
                    new BoatDefinition("boat_3", 3,2, 2, false, false),
                    new BoatDefinition("boat_4", 2,4, 2, true,  false),
                    new BoatDefinition("boat_5", 4,0, 2, false, false),
                    new BoatDefinition("boat_6", 0,4, 2, true,  false),
                }
            },

            // =====================================================================
            // LEVEL 7  —  7 blockers  —  optimal: 6 moves
            // =====================================================================
            new LevelDefinition {
                index=7, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                threeStarMoves=6, twoStarMoves=12,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false),
                    new BoatDefinition("boat_2", 2,1, 2, true,  false),
                    new BoatDefinition("boat_3", 3,2, 2, false, false),
                    new BoatDefinition("boat_4", 2,4, 2, true,  false),
                    new BoatDefinition("boat_5", 4,2, 2, false, false),
                    new BoatDefinition("boat_6", 0,4, 2, true,  false),
                    new BoatDefinition("boat_7", 0,0, 2, false, false),
                }
            },

            // =====================================================================
            // LEVEL 8  —  8 blockers  —  optimal: 7 moves
            // =====================================================================
            new LevelDefinition {
                index=8, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                threeStarMoves=7, twoStarMoves=14,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false),
                    new BoatDefinition("boat_2", 2,1, 2, true,  false),
                    new BoatDefinition("boat_3", 3,2, 2, false, false),
                    new BoatDefinition("boat_4", 2,4, 2, true,  false),
                    new BoatDefinition("boat_5", 4,2, 2, false, false),
                    new BoatDefinition("boat_6", 0,4, 2, true,  false),
                    new BoatDefinition("boat_7", 4,0, 2, false, false),
                    new BoatDefinition("boat_8", 5,2, 2, false, false),
                }
            },

            // =====================================================================
            // LEVEL 9  —  8 blockers  —  optimal: 7 moves
            // =====================================================================
            new LevelDefinition {
                index=9, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                threeStarMoves=7, twoStarMoves=14,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false),
                    new BoatDefinition("boat_2", 2,1, 2, true,  false),
                    new BoatDefinition("boat_3", 3,2, 2, false, false),
                    new BoatDefinition("boat_4", 2,4, 2, true,  false),
                    new BoatDefinition("boat_5", 4,2, 2, false, false),
                    new BoatDefinition("boat_6", 0,4, 2, true,  false),
                    new BoatDefinition("boat_7", 1,0, 2, false, false),
                    new BoatDefinition("boat_8", 5,1, 2, false, false),
                }
            },

            // =====================================================================
            // LEVEL 10  —  8 blockers  —  optimal: 8 moves
            // =====================================================================
            new LevelDefinition {
                index=10, gridWidth=6, gridHeight=6, exitRow=2, exitOnRight=true,
                threeStarMoves=8, twoStarMoves=16,
                boats = new List<BoatDefinition> {
                    new BoatDefinition("hero",   0,2, 2, true,  true),
                    new BoatDefinition("boat_1", 2,2, 2, false, false),
                    new BoatDefinition("boat_2", 2,1, 2, true,  false),
                    new BoatDefinition("boat_3", 3,2, 2, false, false),
                    new BoatDefinition("boat_4", 2,4, 2, true,  false),
                    new BoatDefinition("boat_5", 4,2, 2, false, false),
                    new BoatDefinition("boat_6", 0,4, 2, true,  false),
                    new BoatDefinition("boat_7", 1,0, 2, false, false),
                    new BoatDefinition("boat_8", 5,2, 2, false, false),
                }
            },
        };
    }
}

// =============================================================================
// Helper classes
// =============================================================================

public class LevelDefinition
{
    public int  index;
    public int  gridWidth;
    public int  gridHeight;
    public int  exitRow;
    public bool exitOnRight;
    public int  threeStarMoves = 3;   // optimal move count → 3 stars
    public int  twoStarMoves   = 6;   // acceptable move count → 2 stars
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