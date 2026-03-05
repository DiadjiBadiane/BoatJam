// Assets/Scripts/Data/LevelData.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_001", menuName = "BoatJam/Level Data")]
public class LevelData : ScriptableObject
{
    public int gridWidth = 6;
    public int gridHeight = 6;

    [Tooltip("Which row the exit is on (same row as hero boat)")]
    public int exitRow = 2;

    [Tooltip("Exit is on the right side (col = gridWidth)")]
    public bool exitOnRight = true;

    public List<BoatData> boats = new List<BoatData>();
}