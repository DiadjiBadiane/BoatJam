// Assets/Scripts/Data/LevelData.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_000", menuName = "Leave my Boat/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Grid")]
    public int  gridWidth  = 6;
    public int  gridHeight = 6;

    [Header("Exit")]
    public int  exitRow    = 2;
    public bool exitOnRight = true;

    [Header("Boats")]
    public List<BoatData> boats = new List<BoatData>();

    [Header("Star Thresholds")]
    [Tooltip("Moves needed to earn 3 stars (best).")]
    public int threeStarMoves = 3;
    [Tooltip("Moves needed to earn 2 stars. Above this gives 1 star.")]
    public int twoStarMoves   = 6;

    /// <summary>
    /// Returns 1, 2, or 3 stars based on how many moves the player used.
    /// </summary>
    public int CalculateStars(int moveCount)
    {
        if (moveCount <= threeStarMoves) return 3;
        if (moveCount <= twoStarMoves)   return 2;
        return 1;
    }
}
