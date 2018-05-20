using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Biome
{
    public string m_name;
    public Texture2D m_spriteSheet;
    public List<Sprite> m_floorSprites;
    public List<Sprite> m_ceilingSprites;
    public List<Sprite> m_leftWallSprites;
    public List<Sprite> m_rightWallSprites; // Should just be one corner list for top and one for bottom
    public List<Sprite> m_topLeftWallSprites;
    public List<Sprite> m_topRightWallSprites;
    public List<Sprite> m_bottomLeftWallSprites;
    public List<Sprite> m_bottomRightWallSprites;
    public List<Sprite> m_filledSprites; //back and walls
    public List<Sprite> m_emptySprites;
    public List<BiomeElement> m_ObjectsSprites;
}

[Serializable]
public class BiomeElement
{
    public Texture2D texture;
    [Range(0, 1)]
    public float frequency;
}

public class Region
{
    public List<Rectangle> rectangles;
    public int regionID;
    public Region(int id)
    {
        regionID = id;
        rectangles = new List<Rectangle>();
    }
}

public class Rectangle
{
    public Coord start;
    public Coord end;
    public Rectangle(Coord xy, Coord ij)
    {
        start = xy;
        end = ij;
    }

    public int size()
    {
        return width() * height();
    }
    public int width()
    {
        return (end.x - start.x + 1);
    }
    public int height()
    {
        return (end.y - start.y + 1);
    }
}
