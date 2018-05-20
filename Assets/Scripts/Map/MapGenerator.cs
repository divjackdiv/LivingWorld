using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Map = the whole world containing many Area
// Area = A part of this world containing one or more biomes
// Biome = pieces of areas, containing one or more Regions
// Cell = Smallest piece of the world possible

public class MapGenerator : MonoBehaviour {
    #region Inspector vars
    public int m_numberOfMaps;
    public int m_width;
    public int m_height;
    [Range(0.0f, 100f)]
    public float m_initialFillPercentage;
    [Range(0, 10)]
    public int m_smoothness;
    [Range(0, 10)]
    public int m_roomRadius = 1;
    public bool m_useRandomSeed;
    public string m_seed;
    public GameObject m_mapPrefab;
    public Shader m_materialShader;
    public int m_pixelPerCell;
    public int m_backgroundWidthMin;
    public int m_backgroundWidthMax;
    public List<Color> m_backgrounds;
    public List<Biome> m_biomes;
    #endregion

    #region Private vars
    private List<List<Area>> m_masterMap; //Contains all maps
    private List<GameObject> m_masterMapGameObjects; 

    #endregion
    // Use this for initialization
    void Awake () {
        m_masterMap = new List<List<Area>>();
        m_masterMapGameObjects = new List<GameObject>();
    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.R))
            GenerateMap();
	}

    void GenerateMap()
    {
        m_masterMap.Clear();
        foreach(GameObject g in m_masterMapGameObjects)
            Destroy(g);
        if (m_numberOfMaps <= 0)
            return;
        Vector2 numberOfMaps2d = OneToTwoDimension(m_numberOfMaps, 3, 3);
        for (int x = 0; x < numberOfMaps2d.x ; x++)
        {
            m_masterMap.Add(new List<Area>());
            for (int y = 0; y < numberOfMaps2d.y; y++)
            {
                Area area = new Area(m_width, m_height, m_roomRadius, m_biomes);
                GenerateMapSimpleRandom(area.areaMap, (x * ((int)numberOfMaps2d.x))+ y);
                ApplyBordersOnMap(area.areaMap, x, y, m_roomRadius);
                for (int j = 0; j < m_smoothness; j++)
                {
                    ApplyCellularLawsOnMap(area.areaMap);
                }
                CreateMapObject(area, x, y, m_pixelPerCell);
                m_masterMap[x].Add(area);
            }
        }
    }

    void GenerateMapSimpleRandom(Cell[,] map, int seedOffset)
    {
        if (m_useRandomSeed)
        {
            m_seed = System.DateTime.Now.Ticks.ToString() + seedOffset;
        }

        System.Random pseudoRandom = new System.Random(m_seed.GetHashCode());

        for (int x = 0; x < m_width + m_roomRadius * 2; x++)
        {
            for(int y = 0; y < m_height + m_roomRadius * 2; y++)
            {
                map[x, y] =  new Cell(pseudoRandom.NextDouble() * 100.0f >= m_initialFillPercentage ? 0 : 1);
            }
        }
    }

    

    void ApplyCellularLawsOnMap(Cell[,] map)
    {
        for (int x = m_roomRadius; x < m_width + m_roomRadius; x++)
        {
            for (int y = m_roomRadius; y < m_height + m_roomRadius; y++)
            {
                float emptyCellNeighbourRatio = GetEmptyCellNeighbourRatio(map, x, y, m_roomRadius);
                if (emptyCellNeighbourRatio > 0.5f)
                {
                    map[x, y].value = 0;
                }
                else if (emptyCellNeighbourRatio < 0.5f)
                {
                    map[x, y].value = 1;
                }

            }
        }
    }
    void ApplyBordersOnMap(Cell[,] map, int x, int y, int depth)
    {
        if (x > 0)
            ApplyLeftMapBordersOnMap(m_masterMap[x - 1][y].areaMap, map, depth);
        if (x < m_masterMap.Count - 1)
            ApplyRightMapBordersOnMap(m_masterMap[x + 1][y].areaMap, map, depth);
        if (y > 0)
            ApplyBottomMapBordersOnMap(m_masterMap[x][y - 1].areaMap, map, depth);
        if (y < m_masterMap[x].Count - 1)
            ApplyTopMapBordersOnMap(m_masterMap[x][y - 1].areaMap, map, depth);
    }

    void ApplyLeftMapBordersOnMap(Cell[,] neighbourMap, Cell[,] map, int depth)
    {
        for (int d = 0; d < depth; d++)
        {
            for (int y = depth; y < m_height + depth; y++)
            {
                map[depth - 1 - d, y] = neighbourMap[m_width + depth - d - 1, y];
            }
        }
    }

    void ApplyRightMapBordersOnMap(Cell[,] neighbourMap, Cell[,] map, int depth)
    {
        for (int d = 0; d < depth; d++)
        {
            for (int y = depth; y < m_height + depth; y++)
            {
                map[m_width + depth*2 - 1 - d, y] = neighbourMap[depth*2 - d - 1, y];
            }
        }
    }

    void ApplyTopMapBordersOnMap(Cell[,] neighbourMap, Cell[,] map, int depth)
    {
        for (int d = 0; d < depth; d++)
        {
            for (int x = depth; x < m_width + depth; x++)
            {
                map[x, m_height + depth * 2 - 1 - d] = neighbourMap[x, depth * 2 - d - 1];
            }
        }
    }

    void ApplyBottomMapBordersOnMap(Cell[,] neighbourMap, Cell[,] map, int depth)
    {
        for (int d = 0; d < depth; d++)
        {
            for (int x = depth; x < m_width + depth; x++)
            {
                map[x, depth - 1 - d] = neighbourMap[x, m_height + depth - d - 1];
            }
        }
    }

    public void FillBackground(Cell[,] map, int layerWidth, int layer)
    {
        for (int x = 0; x < m_width + m_roomRadius * 2; x++)
        {
            for (int y = 0; y < m_height + m_roomRadius * 2; y++)
            {
                if (map[x, y].value == 0 || map[x,y].backgroundLayer > layer)
                {
                    int nearestBackgroundLayer = -1;
                    for(int i = -layerWidth; i <= layerWidth; i++)
                    {
                        for (int j = -layerWidth; j <=  layerWidth; j++)
                        {
                            if(i != 0 || j != 0)
                            {
                                if ((x + i >= 0 && x + i < m_width + m_roomRadius * 2) && (y + j >= 0 && y + j < m_height + m_roomRadius * 2))
                                {
                                    if(map[x + i, y + j].value == 1 && map[x + i, y + j].backgroundLayer == layer)
                                    {
                                        int dist = Mathf.Max(Mathf.Abs(i), Mathf.Abs(j));
                                        if (map[x + i, y + j].backgroundLayer > nearestBackgroundLayer)
                                        {
                                            nearestBackgroundLayer = map[x + i, y + j].backgroundLayer;
                                        }
                                    }
                                }
                            }                            
                        }
                    }
                    if(nearestBackgroundLayer != -1)
                    {
                        map[x, y].backgroundLayer = nearestBackgroundLayer + 1;
                        map[x, y].value = 1;
                    }
                    else
                    {
                        map[x, y].backgroundLayer = m_backgrounds.Count;
                        map[x, y].value = 1;
                    }
                }
            }
        }        
    }

    float GetEmptyCellNeighbourRatio(Cell[,] map, int x, int y, int radius = 1)
    {
        float filledNeighbours = GetFilledCellNeighbourRatio(map, x, y, radius);
        return (1f - filledNeighbours);
    }
 
    float GetFilledCellNeighbourRatio(Cell[,] map, int x, int y, int radius = 1)
    {
        int filledNeighbours = 0;
        int neighbourCount = 0;
        int i = x - radius >= 0 ? x - radius : radius;
        for (; i <= (x == m_width - 1 + radius * 2 ? x : x + radius); i++)
        {
            int j = y - radius >= 0 ? y - radius : radius;
            for (; j <= (y == m_height - 1 + radius * 2 ? y : y + radius); j++)
            {
                if (i == x && j == y)
                    continue;
                neighbourCount += 1;
                filledNeighbours += map[i, j].value;
            }
        }
        return (filledNeighbours * 1f) / neighbourCount;
    }

    public bool m_useDebug;
    void CreateMapObject(Area area, int x, int y, int pixelSizeOfCell)
    {
        GameObject mapObj = (GameObject)Instantiate(m_mapPrefab);
        mapObj.transform.position = new Vector3(transform.position.x + x, transform.position.y + y, transform.position.z);
        Material mat = new Material(m_materialShader);
        mapObj.GetComponent<Renderer>().material = mat;

        for(int i = 0; i < m_backgrounds.Count; i++) 
            FillBackground(area.areaMap, Random.Range(m_backgroundWidthMin, m_backgroundWidthMax), i);
        area.DetermineAllCellsType();
        area.AssignBiomes();
        area.AssignBiggestRectanglesInArea();
        //area.AddCeilingProps();
        if (m_useDebug)
        {
            // MapToTexture(area, mat);
            MapToTextureWithBorders(area, mat);
        }
        else { 
            area.ApplyBiomesTextures(mat, pixelSizeOfCell, m_backgrounds);
        }
        m_masterMapGameObjects.Add(mapObj);
    }

    Vector2 OneToTwoDimension(int index, int width, int height)
    {
        return new Vector2(((index-1) % width) + 1, ((index-1) / height) + 1);
    }

    void MapToTexture(Area area, Material mat)
    {
        Texture2D tex = new Texture2D(m_width, m_height);
        tex.filterMode = FilterMode.Point;
        for (int x = m_roomRadius; x < m_width + m_roomRadius; x++)
        {
            for (int y = m_roomRadius; y < m_height + m_roomRadius; y++)
            {
                if (area.areaMap[x, y].value == 1)
                    tex.SetPixel(x - m_roomRadius, y - m_roomRadius, Color.black);
                else if(area.areaMap[x, y].value == 0)
                {
                   tex.SetPixel(x - m_roomRadius, y - m_roomRadius, Color.white);
                }
            }
        }
        for(int i = 0; i < area.regions.Count; i++)
        {
            Region currentRegion = area.regions[i];
            for (int j = 0; j < currentRegion.rectangles.Count; j++)
            {
                Rectangle currentRect = currentRegion.rectangles[j];
                for (int x = currentRect.start.x; x <= currentRect.end.x; x++)
                {
                    for (int y = currentRect.start.y; y <= currentRect.end.x; y++)
                    {
                        tex.SetPixel(x , y, Color.blue);
                    }
                }
            }
        }
        mat.mainTexture = tex;
        tex.Apply();
    }

    void MapToTextureWithBorders(Area area, Material mat)
    {
        Texture2D tex = new Texture2D(m_width + m_roomRadius*2, m_height + m_roomRadius*2);
        tex.filterMode = FilterMode.Point;
        for (int x = 0; x < m_width + m_roomRadius * 2; x++)
        {
            for (int y = 0; y < m_height + m_roomRadius * 2; y++)
            {
                if (area.areaMap[x, y].value == 1)
                    tex.SetPixel(x, y, Color.black);
                else if (area.areaMap[x, y].value == 0)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
        }
        for (int i = 0; i < area.regions.Count; i++)
        {
            Region currentRegion = area.regions[i];
            for (int j = 0; j < currentRegion.rectangles.Count; j++)
            {
                Color col = Random.ColorHSV();
                Rectangle currentRect = currentRegion.rectangles[j];
                for (int x = currentRect.start.x; x <= currentRect.end.x; x++)
                {
                    for (int y = currentRect.start.y; y <= currentRect.end.y; y++)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
        }
        mat.mainTexture = tex;
        tex.Apply();
    }
}


/* Depreciated version; adds arbitrary borders around map
void ApplyBordersOnMap(int[,] map)
{
    int floorHeightMinPix = (int)(m_height * m_floorHeightMin) + m_roomRadius;
    int floorHeightMaxPix = (int)(m_height * m_floorHeightMax) + m_roomRadius;
    for (int x = 0; x < m_height + m_roomRadius * 2; x++)
    {
        for (int y = 0; y < m_height + m_roomRadius * 2; y++)
        {
            if (y < m_roomRadius) //if bottom of the map then make it a filled (for world crust)
                map[x, y] = 1; 
            else if (y > m_height + m_roomRadius - 1) //if top of map then make it empty (for sky)
                map[x, y] = 0;
            else if (x < m_roomRadius || x > m_width + m_roomRadius - 1)
            {
                if (y < floorHeightMinPix)  //side Walls
                {
                    map[x, y] = 1;
                }
                else if (y > floorHeightMaxPix) //sky sides
                {
                    map[x, y] = 0;
                }
            }
        }
    }
}
*/
