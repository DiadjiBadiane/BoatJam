// Assets/Scripts/Data/BoatData.cs
using System;
using UnityEngine;

[Serializable]
public class BoatData
{
    public string id;           // unique name, e.g. "hero", "boat_1"
    public int col;             // starting column (X on grid)
    public int row;             // starting row (Z on grid)
    public int size;            // length in cells along movement axis (e.g. 2 or 4)
    public int width;           // width in cells perpendicular to movement axis (e.g. 1 or 2)
    public bool isHorizontal;   // true = slides left/right
    public bool isHero;         // true = this is the boat that must escape
}