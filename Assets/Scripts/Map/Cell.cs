using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell {
    public enum CellType { Left = 0, Right, Top, Bottom, TopLeft, BottomLeft, TopRight, BottomRight, Middle, Empty}

    public int value;
    public CellType cellType;
    public Biome biome;
    public Region region;
    public int backgroundLayer;
    public Cell(int value)
    {
        this.value = value;
        this.backgroundLayer = 0;
    }

    public bool IsFull(int layer)
    {
        return !IsEmpty(layer);
    }

    public bool IsEmpty(int layer)
    {
        return value == 0 || layer < backgroundLayer;
    }

    public int valueGivenLayer(int layer)
    {
        return IsEmpty(layer) ? 0 : 1;
    }
}
