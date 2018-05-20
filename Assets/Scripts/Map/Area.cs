using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Area {    
    public Cell[,] areaMap;
    public int width;
    public int height;
    public int roomRadius;
    public List<Biome> biomes;
    public List<Region> regions;

    public Area(int w, int h, int roomR, List<Biome> biomeList)
    {
        areaMap = new Cell[w + roomR * 2, h + roomR * 2];
        biomes = biomeList;
        width = w;
        height = h;
        roomRadius = roomR;
    }


    public void ApplyBiomesTextures(Material mat, int pixelPerCell, List<Color> backgroundTints)
    {
        Texture2D tex = new Texture2D(width * pixelPerCell, height * pixelPerCell);
        tex.filterMode = FilterMode.Point;

        Sprite currentSprite;
        Biome currentBiome;
        Cell currentCell;

        for (int x = roomRadius; x < width + roomRadius; x++)
        {
            for (int y = roomRadius; y < height + roomRadius; y++)
            {
                currentCell = areaMap[x, y];
                currentBiome = currentCell.biome;
                if (currentBiome != null)
                    currentSprite = GetCorrespondingSprite(currentCell.cellType, currentBiome);
                else
                    currentSprite = GetCorrespondingSprite(currentCell.cellType, biomes[0]);
                Color tint = Color.white;
                if(currentCell.backgroundLayer > 0)
                    tint = backgroundTints[currentCell.backgroundLayer - 1];
                DrawTextureOnCell(tex, currentSprite, pixelPerCell, x, y, tint);
            }
        }

        Color pixelValue;
        for (int i = 0; i < regions.Count; i++)
        {
            Region currentRegion = regions[i];
            for (int j = 0; j < currentRegion.rectangles.Count; j++)
            {
                Rectangle currentRect = currentRegion.rectangles[j];
                currentBiome = areaMap[currentRect.start.x, currentRect.start.y].biome;
                int textureIndex = GetRandomObjectInRect(currentBiome, currentRect, pixelPerCell);
                if (textureIndex >= 0)
                {
                    Texture2D currentTexture = currentBiome.m_ObjectsSprites[textureIndex].texture;
                    int spritePosX = 0;
                    for (int x = (currentRect.start.x-1) * pixelPerCell; x <= (currentRect.end.x) * pixelPerCell; x++)
                    {
                        int spritePosY = 0;
                        for (int y = (currentRect.start.y-1) * pixelPerCell; y <= (currentRect.end.y) * pixelPerCell; y++)
                        {
                            if(spritePosX < currentTexture.width && spritePosY < currentTexture.height)
                            {
                                pixelValue = currentTexture.GetPixel(spritePosX, spritePosY);
                                Color originalPix = tex.GetPixel(x, y);
                                tex.SetPixel(x, y, (originalPix * (1-pixelValue.a) + (pixelValue * pixelValue.a)));
                                spritePosY++;
                            }                          
                        }
                        spritePosX++;
                    }
                }
            }
        }
        mat.mainTexture = tex;
        tex.Apply();
    }

    public void DrawTextureOnCell(Texture2D tex, Sprite currentSprite, int pixelPerCell, int x, int y, Color tint)
    {
        Color pixelValue;
        for (int i = 0; i < pixelPerCell; i++)
        {
            for (int j = 0; j < pixelPerCell; j++)
            {
                int pixelX = (x - roomRadius) * pixelPerCell + i;
                int pixelY = (y - roomRadius) * pixelPerCell + j;

                if (currentSprite != null)
                {
                    int spritePosX = (int)currentSprite.rect.position.x;
                    int spritePosY = (int)currentSprite.rect.position.y;
                    pixelValue = biomes[0].m_spriteSheet.GetPixel(i + spritePosX, j + spritePosY) * tint;
                }
                else
                {
                    pixelValue = Color.clear;
                }
                tex.SetPixel(pixelX, pixelY, pixelValue);
            }
        }
    }

   

    public Sprite GetFilledBackgroundTile(Biome currentBiome, int x, int y)
    {
        Cell.CellType ct = GetCellType(areaMap[x, y], new Coord(x, y));
        Sprite sprite = GetCorrespondingSprite(ct, currentBiome);
        return sprite;
    }


    public int GetRandomObjectInRect(Biome currentBiome, Rectangle currentRect, int pixelPerCell)
    {
        int pickedTextureIndex = -1;
        for (int i = 0; i < currentBiome.m_ObjectsSprites.Count; i++) {
            BiomeElement currentElement = currentBiome.m_ObjectsSprites[i];
            if(currentRect.width() * pixelPerCell >= currentElement.texture.width && currentRect.height() * pixelPerCell >= currentElement.texture.height)
            {
                if (currentElement.frequency > UnityEngine.Random.value)
                {
                    pickedTextureIndex = i;
                    break;
                }
            }
        }
        return pickedTextureIndex;

    }
    public void DetermineAllCellsType()
    {
        for (int x = 0; x < width + roomRadius*2; x++)
        {
            for (int y = 0; y < height + roomRadius*2; y++)
            {
                areaMap[x, y].cellType = GetCellType(areaMap[x,y], new Coord(x, y));
            }
        }
    }

    public Cell.CellType GetCellType(Cell cell, Coord cellPos)
    {
        Cell.CellType cellType;
        if(cell.IsEmpty(cell.backgroundLayer))
            cellType = Cell.CellType.Empty;
        else if (!IsTopEdge(cellPos) && areaMap[cellPos.x, cellPos.y + 1].IsEmpty(cell.backgroundLayer)) // cell above is empty
        {
            if (!IsLeftEdge(cellPos) && areaMap[cellPos.x - 1, cellPos.y].IsEmpty(cell.backgroundLayer))
                cellType = Cell.CellType.BottomLeft;
            else if (!IsRightEdge(cellPos) && areaMap[cellPos.x + 1, cellPos.y].IsEmpty(cell.backgroundLayer))
                cellType = Cell.CellType.BottomRight;
            else
                cellType = Cell.CellType.Bottom;
        }
        else if (!IsBottomEdge(cellPos) && areaMap[cellPos.x, cellPos.y - 1].IsEmpty(cell.backgroundLayer))  // cell under is empty
        {
            if (!IsLeftEdge(cellPos) && areaMap[cellPos.x - 1, cellPos.y].IsEmpty(cell.backgroundLayer))
                cellType = Cell.CellType.TopLeft;
            else if (!IsRightEdge(cellPos) && areaMap[cellPos.x + 1, cellPos.y].IsEmpty(cell.backgroundLayer))
                cellType = Cell.CellType.TopRight;
            else
                cellType = Cell.CellType.Top;
        }
        else if (!IsLeftEdge(cellPos) && areaMap[cellPos.x - 1, cellPos.y].IsEmpty(cell.backgroundLayer))
            cellType = Cell.CellType.Left;
        else if (!IsRightEdge(cellPos) && areaMap[cellPos.x + 1, cellPos.y].IsEmpty(cell.backgroundLayer))
            cellType = Cell.CellType.Right;
        else
        {
            cellType = Cell.CellType.Middle;
        }
        return cellType;
    }


    public Sprite GetCorrespondingSprite(Cell.CellType cellType, Biome biome)
    {
        Sprite currentSprite;
        List<Sprite> sprites = new List<Sprite>();
        switch (cellType)
        {
            case Cell.CellType.Bottom:
                sprites = biome.m_floorSprites;
                break;
            case Cell.CellType.Top:
                sprites = biome.m_ceilingSprites;
                break;
            case Cell.CellType.Left:
                sprites = biome.m_leftWallSprites;
                break;
            case Cell.CellType.Right:
                sprites = biome.m_rightWallSprites;
                break;
            case Cell.CellType.TopLeft:
                sprites = biome.m_topLeftWallSprites;
                break;
            case Cell.CellType.TopRight:
                sprites = biome.m_topRightWallSprites;
                break;
            case Cell.CellType.BottomLeft:
                sprites = biome.m_bottomLeftWallSprites;
                break;
            case Cell.CellType.BottomRight:
                sprites = biome.m_bottomRightWallSprites;
                break;
            case Cell.CellType.Middle:
                sprites = biome.m_filledSprites;
                break;
            default:
                sprites = null;
                break;
        }
        if (sprites == null)
            return null;
        currentSprite = sprites[UnityEngine.Random.Range(0, sprites.Count)];
        return currentSprite;
    }


    public void AssignBiggestRectanglesInArea()
    {
        List<int> histHeights = new List<int>();
        for (int y = 0; y < height + roomRadius * 2; y++)
        {
            for (int x = 0; x < width + roomRadius*2; x++)
            {
                if (areaMap[x, y].IsEmpty(0) && (y == 0 || (areaMap[x, y - 1].cellType == Cell.CellType.Bottom && areaMap[x, y - 1].IsFull(0))))
                {
                    int histHeight = 1;
                    for (int h = 1; h < height + roomRadius * 2 - y; h++)
                    {
                        if (areaMap[x, y + h].IsEmpty(0))
                            histHeight++;
                        else
                            break;
                    }
                    histHeights.Add(histHeight);
                    
                }
                else if(histHeights.Count > 0)
                {
                    Rectangle rect = FindLargestRectInHist(histHeights);
                    //0  5  0 
                    rect.start.x = x - (histHeights.Count - 2) + rect.start.x;
                    rect.start.y += y;
                    rect.end.x = x - (histHeights.Count - 2) + rect.end.x;
                    rect.end.y += y;
                    regions[0].rectangles.Add(rect);

                    histHeights = new List<int>();
                }
                if(areaMap[x, y].IsEmpty(0) && x == width+ (roomRadius*2) -1 && histHeights.Count > 0)
                {
                    Rectangle rect = FindLargestRectInHist(histHeights);
                    rect.start.x = x - (histHeights.Count - 2)  + 1 + rect.start.x;
                    rect.start.y += y;
                    rect.end.x = x - (histHeights.Count - 2) + 1 + rect.end.x;
                    rect.end.y += y;
                    regions[0].rectangles.Add(rect);
                    histHeights = new List<int>();
                }
            }
        }
        


    }

    Rectangle FindLargestRectInHist(List<int> histHeights)
    {
        int max = 0;
        histHeights.Insert(0, -1);
        histHeights.Add(-1);
        Stack<int> stack = new Stack<int>();
        stack.Push(0);
        Coord start = new Coord(0, 0);
        Coord end = new Coord(0, 0);
        for (int x = 0; x < histHeights.Count; x++)
        {
            int peek = stack.Peek();

            while (histHeights[x] < histHeights[peek == -1 ? histHeights.Count - 1 : peek]) {                 
                int pop = stack.Pop();
                int h = histHeights[pop == -1 ? histHeights.Count - 1 : pop];
                peek = stack.Peek();
                int m = h * (x - peek - 1);              
                if(m > max)
                {
                    start.x = peek;
                    end.x =  x - 1;
                    end.y = h;
                    max = m;
                }
            }
            stack.Push(x);
        }
        end.x = end.x > 0 ? end.x - 1 : 0;
        end.y = end.y > 0 ? end.y - 1 : 0;
        return new Rectangle(start, end);
    }

    public void AssignBiomes()
    {
        AssignBiomes(0);
        AssignBiomes(1);
    }

    void AssignBiomes(int cellValue)
    {
        Queue<Coord> toVisit = new Queue<Coord>();

        if (this.regions == null)
            this.regions = new List<Region>();
        Region currentRegion = new Region(0);
        this.regions.Add(currentRegion);

        int[,] visited = new int[width + roomRadius * 2, height + roomRadius * 2];
        for (int x = 0; x < width + roomRadius*2; x++)
        {
            for (int y = 0; y < height + roomRadius*2; y++)
            {
                Cell currentCell = areaMap[x, y];
                if (visited[x, y] == 0 && currentCell.biome == null && currentCell.valueGivenLayer(0) == cellValue)
                {
                    int biomeNumber = UnityEngine.Random.Range(0, biomes.Count);
                    Biome currentBiome = biomes[biomeNumber];
                    Coord currentCoord = new Coord(x , y);
                    visited[currentCoord.x, currentCoord.y] = 1;
                    toVisit.Enqueue(currentCoord);
                    while (toVisit.Count > 0)
                    {
                        currentCoord = toVisit.Dequeue();
                        currentCell = areaMap[currentCoord.x, currentCoord.y];
                        currentCell.biome = currentBiome;
                        currentCell.region = currentRegion;
                        for (int i = currentCoord.x - 1; i <= currentCoord.x + 1; i++)
                        {
                            for (int j = currentCoord.y - 1; j <= currentCoord.y + 1; j++)
                            {
                                if (!IsExtremety(new Coord(i, j)))
                                {
                                    if (i == currentCoord.x || j == currentCoord.y)
                                    {
                                        if (visited[i, j] == 0 && areaMap[i, j].value == cellValue)
                                        {
                                            visited[i, j] = 1;

                                            toVisit.Enqueue(new Coord(i, j));
                                        }
                                    }
                                }  
                            }
                        }
                    }
                }
            }
        }
    }


    bool IsLeftEdge(Coord cellPos)
    {
        return (cellPos.x <= roomRadius - 1);
    }

    bool IsRightEdge(Coord cellPos)
    {
        return (cellPos.x >= width + roomRadius);
    }

    bool IsTopEdge(Coord cellPos)
    {
        return (cellPos.y >= height + roomRadius);
    }

    bool IsBottomEdge(Coord cellPos)
    {
        return (cellPos.y <= roomRadius - 1);
    }

    bool IsExtremety(Coord cellPos)
    {
        return IsLeftEdge(cellPos) || IsRightEdge(cellPos) || IsTopEdge(cellPos) || IsBottomEdge(cellPos);
    }
}

[Serializable]
public class Coord
{
    public int x;
    public int y;
    public Coord(int i, int j)
    {
        x = i;
        y = j;
    }

    public int Distance(Coord from)
    {
        return (Mathf.Abs(x - from.x) + Mathf.Abs(y - from.y));
    }
}