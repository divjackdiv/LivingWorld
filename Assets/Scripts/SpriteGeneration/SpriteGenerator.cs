using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteGenerator : MonoBehaviour {

    public enum PixelType { Empty = 0, Border = 1, Solid, Undecided, ColorGroup, SolidColor1, SolidColor2, SolidColor3, SolidColor4, SolidColor5}
    public enum GenerationTechnique { Flood = 0 }
    public GenerationTechnique m_generationTechnique;
    public GameObject m_objPrefab;
    public Texture2D m_generateFrom;
    public Texture2D m_generateFrom2;
    public Material m_materialShader;
    public string m_seed;
    public float m_scale = 1f;
    public bool m_useRandomSeed;
    public bool m_mirrorX;
    public bool m_colored;

    [Header("Shape")]
    [Range(0.0f, 100f)]
    public float m_fillRatio;
    [Range(0.0f, 100f)]
    public float m_borderRatio;
    [Range(0, 10)]
    public int m_smooth;

    [Header("Color Flood")]
    [Range(0, 5)]
    public int m_numberOfColors;
    public Coord m_colorFloodStartPoint;
    public int m_colorSmooth;
    [Range(0.0f, 100f)]
    public float m_colorFillRatio;
    public float m_colorFillMultiplier = 1;
    
    [Header("Palette Colors")]
    public Vector2 m_hueDifferenceMinMax = new Vector2(0.02f, 0.08f);
    public Vector2 m_hueMinMax = new Vector2(0f,1f);
    public Vector2 m_saturationMinMax = new Vector2(0f, 1f);
    public Vector2 m_luminanceMinMax = new Vector2(0f, 1f);
    public int m_minPixelPerColor;
    public int m_colorFloodStartingPoints;
    [Range(0, 1)]
    public float m_colorNoise;

    [Header("Light and Shadow")]
    public float m_lightStrenght = 0.9f;
    public int m_shadowSize;
    public float m_shadowStep;
    public float m_minimumShadow;
    public Coord m_lightPos;
    public Vector2 m_lightRadius;

    private GameObject m_currentSprite;
    private int m_numberOfAreas;
    private System.Random m_pseudoRandom;
    private Anchor m_parentAnchor;
    private List<Anchor> m_childAnchors;
    // Use this for initialization
    void Start() {
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.S))
        {
            CleanUp();
            GenerateSprite();
        }
    }

    public GameObject GenerateSprite()
    {
        GameObject spriteObj = Instantiate(m_objPrefab) as GameObject;
        spriteObj.transform.position = transform.position;
        Texture2D tex = new Texture2D(m_generateFrom.width, m_generateFrom.height);
        tex.filterMode = FilterMode.Point;
        if (m_useRandomSeed)
        {
            m_seed = System.DateTime.Now.Ticks.ToString();
        }

        m_pseudoRandom = new System.Random(m_seed.GetHashCode());
        Pixel[,] pixels = new Pixel[m_generateFrom.width, m_generateFrom.height];
        if (m_generationTechnique == GenerationTechnique.Flood)
        {
            FloodGeneration(pixels);
            if (m_colored)
                ApplyToTextureColor(pixels, tex);
            else
                ApplyToTexture(pixels, tex);
        }

        Material mat = new Material(m_materialShader);
        spriteObj.GetComponent<Renderer>().material = mat;
        spriteObj.GetComponent<SpriteRenderer>().sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        tex.Apply();
        spriteObj.transform.localScale *= m_scale;
        m_currentSprite = spriteObj;
        return spriteObj;
    }
    
    void FloodGeneration(Pixel[,] pixels)
    {
        List<Coord> ColorGroups = new List<Coord>();
        List<Coord> Borders = new List<Coord>();
        List<Color> Colors = new List<Color>();
        m_childAnchors = new List<Anchor>();
        for (int i = 0; i < m_generateFrom.width; i++)
        {
            for (int j = 0; j < m_generateFrom.height; j++)
            {
                Color currentCol = m_generateFrom.GetPixel(i, j);
                PixelType pixType;
                Color maxPix = m_generateFrom2.GetPixel(i, j);
                int area;
                if (maxPix.g > 0) //Anchor
                {
                    pixType = PixelType.Solid;
                    Anchor anchor = new Anchor(new Coord(i, j), (int)(maxPix.g * 256), m_generateFrom.width, m_generateFrom.height);
                    m_childAnchors.Add(anchor);
                    if (m_parentAnchor == null || m_parentAnchor.value < anchor.value)
                    {
                        m_parentAnchor = anchor;
                    }
                    area = 0;
                }
                else if (maxPix == Color.blue) //Border
                {
                    pixType = PixelType.Border;
                    area = 0;
                    Borders.Add(new Coord(i, j));
                }
                else if (maxPix == Color.red)
                {
                    if (currentCol == Color.red)
                    {
                        pixType = PixelType.Solid;
                        area = 0;
                    }
                    else
                    {
                        pixType = PixelType.Undecided;
                        if (Colors.Contains(maxPix))
                        {
                            area = Colors.IndexOf(maxPix) + 1;
                        }
                        else
                        {
                            Colors.Add(maxPix);
                            area = Colors.Count + 1;
                        }
                    }
                }
                else if (maxPix.a == 0)
                {
                    pixType = PixelType.Empty;
                    area = -1;
                }
                else
                {
                    if (currentCol == Color.red)
                    {
                        pixType = PixelType.Undecided;
                    }
                    else if (currentCol.a != 0 && currentCol != Color.blue)
                    {
                        ColorGroups.Add(new Coord(i, j));
                        pixType = PixelType.ColorGroup;
                    }
                    else
                        pixType = PixelType.Undecided;
                    if (Colors.Contains(maxPix))
                    {
                        area = Colors.IndexOf(maxPix) + 1;
                    }
                    else
                    {
                        Colors.Add(maxPix);
                        area = Colors.Count + 1;
                    }
                }
                pixels[i, j] = new Pixel(pixType, area);
            }
        }
        m_childAnchors.Remove(m_parentAnchor);
        m_numberOfAreas = Colors.Count;
        List<Coord> coords;
        coords = Flood(pixels, ColorGroups, m_fillRatio);
        Smooth(pixels, coords, PixelType.ColorGroup, PixelType.Solid, 3, -1);
        coords = FloodOneDirection(pixels, Borders, m_borderRatio, false, PixelType.Border);
        ThinOuter(pixels, coords);
        ThinInner(pixels, coords);
        ReplacePixelType(pixels, new Pixel(PixelType.Undecided, -1), new Pixel(PixelType.Solid, 1));
        List<Coord> startPoints = new List<Coord>();
        float colorFillRatio = m_colorFillRatio;
        PixelType targetType = (PixelType)((int)PixelType.SolidColor1);
        startPoints = GetPointsNearBorder(pixels, m_colorFloodStartPoint, m_colorFloodStartingPoints);
        startPoints = Flood(pixels, startPoints, colorFillRatio, false, targetType, PixelType.Solid, PixelType.Solid, m_colorFloodStartingPoints);
        for (int j = 0; j < m_colorSmooth; j++)
            Smooth(pixels, startPoints, targetType, PixelType.Solid, 1, -1);
        for (int i = 1; i < m_numberOfColors - 1; i++)
        {
            targetType = (PixelType)((int)(PixelType.SolidColor1) + i);
            startPoints = Flood(pixels, startPoints, colorFillRatio, false, targetType, PixelType.Solid, PixelType.Solid, m_minPixelPerColor);
            for (int j = 0; j < m_colorSmooth; j++)
                Smooth(pixels, startPoints, targetType, PixelType.Solid, 1, -1);
            colorFillRatio *= m_colorFillMultiplier;
        }
        ReplacePixelType(pixels, new Pixel(PixelType.Solid, 1), new Pixel((PixelType)((int)(PixelType.SolidColor1) + m_numberOfColors - 1), 1));
        if (m_mirrorX)
            MirrorX(pixels);

    }


    List<Coord> Flood(Pixel[,] pixels, List<Coord> startPoints, float ratio, bool onlySameArea = true, PixelType targetType = PixelType.ColorGroup, PixelType compareType = PixelType.Undecided, PixelType fallbackType = PixelType.Solid, int min = 0)
    {
        List<Coord> FloodedCoords = new List<Coord>();
        for (int i = 0; i < startPoints.Count; i++)
            FloodedCoords.Add(startPoints[i]);
        List<Coord> copy = startPoints;
        int floodedCount = 0;
        //now flood
        while (copy.Count > 0)
        {
            Coord pix = copy[0];
            int i = pix.x;
            int j = pix.y;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if ((x != 0 && y != 0) || (x == 0 && y == 0))
                        continue;
                    if (i + x < 0 || i + x >= pixels.GetLength(0) || j + y < 0 || j + y >= pixels.GetLength(1))
                        continue;
                    if (pixels[i + x, j + y].type == compareType && (onlySameArea == false || pixels[i + x, j + y].area == pixels[i, j].area))
                    {
                        PixelType newPixelType;
                        if (floodedCount < min)
                            newPixelType = targetType;
                        else
                            newPixelType = m_pseudoRandom.NextDouble() * 100.0f > ratio ? fallbackType : targetType;
                        pixels[i + x, j + y].type = newPixelType;
                        if (newPixelType == targetType)
                        {
                            pixels[i + x, j + y].area = pixels[i, j].area;
                            copy.Add(new Coord(i + x, j + y));
                            floodedCount++;
                        }
                        FloodedCoords.Add(new Coord(i + x, j + y));
                    }
                }
            }
            copy.Remove(pix);
        }
        return FloodedCoords;
    }

    //flood only one neighbour direction
    List<Coord> FloodOneDirection(Pixel[,] pixels, List<Coord> startPoints, float ratio, bool onlySameArea = true, PixelType targetType = PixelType.ColorGroup, PixelType compareType = PixelType.Undecided, PixelType fallbackType = PixelType.Solid)
    {
        List<Coord> FloodedCoords = new List<Coord>();
        List<Coord[]> copy = new List<Coord[]>();
        for (int i = 0; i < startPoints.Count; i++)
        {
            FloodedCoords.Add(startPoints[i]);
            copy.Add(new Coord[2]);
            copy[i][0] = startPoints[i];
        }

        //now flood
        while (copy.Count > 0)
        {
            Coord[] firstElement = copy[0];
            Coord pix = firstElement[0];
            Coord previousPix = firstElement[1];
            Color currentCol = m_generateFrom2.GetPixel(pix.x, pix.y);
            int i = pix.x;
            int j = pix.y;
            int x = -1;
            int maxX = 1;
            if (previousPix != null)
            {
                x = pix.x - previousPix.x;
                maxX = x;
            }
            for (; x <= maxX; x++)
            {
                int y = -1;
                int maxY = 1;
                if (previousPix != null)
                {
                    y = pix.y - previousPix.y;
                    maxY = y;
                }
                for (; y <= maxY; y++)
                {
                    if ((x != 0 && y != 0) || (x == 0 && y == 0))
                        continue;
                    if (i + x < 0 || i + x >= pixels.GetLength(0) || j + y < 0 || j + y >= pixels.GetLength(1))
                        continue;
                    if (pixels[i + x, j + y].type == compareType && (onlySameArea == false || pixels[i + x, j + y].area == pixels[i, j].area))
                    {
                        PixelType newPixelType = m_pseudoRandom.NextDouble() * 100.0f > ratio ? fallbackType : targetType;
                        pixels[i + x, j + y].type = newPixelType;
                        if (newPixelType == targetType)
                        {
                            pixels[i + x, j + y].area = pixels[i, j].area;

                            Coord[] coords = new Coord[2];
                            coords[0] = new Coord(i + x, j + y);
                            coords[1] = pix;
                            copy.Add(coords);
                        }
                        FloodedCoords.Add(new Coord(i + x, j + y));
                    }
                    else if(pixels[i + x, j + y].type == targetType && (onlySameArea == false || pixels[i + x, j + y].area == pixels[i, j].area) && previousPix != null)
                    {//Special case, to ensure that border do not intersect
                        pixels[i, j].type = fallbackType;
                        FloodedCoords.Remove(pix);
                    }
                }
            }
            copy.Remove(firstElement);
        }
        return FloodedCoords;
    }
    void Smooth(Pixel[,] pixels, List<Coord> ToSmooth, PixelType targetType = PixelType.ColorGroup, PixelType fallbackType = PixelType.Solid, int upSmooth = 3, int downSmooth = 1)
    {
        //smooth
        for (int s = 0; s < m_smooth; s++)
        {
            for (int index = 0; index < ToSmooth.Count; index++)
            {
                Coord pix = ToSmooth[index];
                int i = pix.x;
                int j = pix.y;
                Color col = m_generateFrom2.GetPixel(i, j);
                int count = 0;
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if ((x != 0 && y != 0) || (x== 0 && y == 0))
                            continue;
                        if (i + x < 0 || i + x >= pixels.GetLength(0) || j + y < 0 || j + y >= pixels.GetLength(1))
                            continue;
                        if (pixels[i + x, j + y].type == targetType && pixels[i + x, j + y].area == pixels[i, j].area)
                        {
                            Color neighbourCol = m_generateFrom2.GetPixel(i + x, j + y);
                            if (col == neighbourCol)
                                count++;
                        }
                    }
                }
                if (count >= upSmooth)
                {
                    pixels[i, j].type = targetType;
                }
                else if (count <= downSmooth)
                {
                    pixels[i, j].type = fallbackType;
                }
            }
        }
    }

    List<Coord> ThinOuter(Pixel[,] pixels, List<Coord> ToThin, PixelType targetType = PixelType.Border, PixelType fallbackType = PixelType.Empty)
    {
        List<Coord> innerBorder = new List<Coord>();
        for (int index = 0; index < ToThin.Count; index++)
        {
            Coord pix = ToThin[index];
            int i = pix.x;
            int j = pix.y;
            bool isInnerBorder = false;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y== 0)
                        continue;
                    if (i + x < 0 || i + x >= pixels.GetLength(0) || j + y < 0 || j + y >= pixels.GetLength(1))
                        continue;
                    if (pixels[i + x, j + y].type != targetType && pixels[i + x, j + y].type != fallbackType)
                    {
                        isInnerBorder = true;
                    }
                }
            }
            if (isInnerBorder == false)
                pixels[i, j].type = fallbackType;
            else
                innerBorder.Add(pix);
        }
        return innerBorder;
    }

    List<Coord> ThinInner(Pixel[,] pixels, List<Coord> ToThin, PixelType targetType = PixelType.Border, PixelType fallbackType = PixelType.Solid, PixelType compareType = PixelType.Empty)
    {
        List<Coord> outerBorder = new List<Coord>();
        for (int index = 0; index < ToThin.Count; index++)
        {
            Coord pix = ToThin[index];
            int i = pix.x;
            int j = pix.y;
            if (pixels[i, j].type == fallbackType)
                continue;
            bool isOuterBorder = false;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;
                    if (i + x < 0 || i + x >= pixels.GetLength(0) || j + y < 0 || j + y >= pixels.GetLength(1)) {
                       // isOuterBorder = true;
                        continue;
                    }
                    if (pixels[i + x, j + y].type == compareType)
                    {
                        isOuterBorder = true;
                    }
                }
            }
            if (isOuterBorder == false)
                pixels[i, j].type = fallbackType;
            else
                outerBorder.Add(pix);
        }
        return outerBorder;
    }

    void ReplacePixelType(Pixel[,] pixels, Pixel from , Pixel to)
    {
        for (int i = 0; i < pixels.GetLength(0); i++)
        {
            for (int j = 0; j < pixels.GetLength(1); j++)
            {
                if (pixels[i, j].type == from.type)
                    pixels[i, j] = to;
            }
        }
    }

    List<Coord> GetPointsNearBorder(Pixel[,] pixels, Coord nearCoord, int pointCount)
    {
        List<Coord> points = new List<Coord>();
        List<int> distances = new List<int>();

        for (int i = 0; i < pixels.GetLength(0); i++)
        {
            for (int j = 0; j < pixels.GetLength(1); j++)
            {
                Coord currentCoord = new Coord(i, j);
                if (pixels[i, j].type == PixelType.Solid && hasNeighbourOfType(pixels, currentCoord, PixelType.Border))
                {
                    int currentDist = nearCoord.Distance(currentCoord);

                    if (points.Count < pointCount)
                    {
                        points.Add(currentCoord);
                        distances.Add(currentDist);
                    }
                    else
                    {
                        for (int k = 0; k < points.Count; k++)
                        {
                            if (currentDist < distances[k])
                            {
                                points[k] = currentCoord;
                                distances[k] = currentDist;
                                break;
                            }
                        }
                    }
                }
            }
        }
        return points;
    }

    void ApplyToTexture(Pixel[,] pixels, Texture2D tex)
    {
        for (int i = 0; i < pixels.GetLength(0); i++)
        {
            for (int j = 0; j < pixels.GetLength(1); j++)
            {
                Color currentCol;
                if (pixels[i, j].type == PixelType.Border || pixels[i, j].type == PixelType.ColorGroup)
                {
                    currentCol = Color.black;
                }
                else
                {
                    currentCol = Color.white;
                }
                tex.SetPixel(i, j, currentCol);
            }
        }
    }


    //21 71 80
    //16 89 71
    //14 96 60
    //10 100 44
    public bool m_usePaletteOverride;
    public List<Color> m_paletteOverride;
    public List<Color> m_paletteOverride2;

    void ApplyToTextureColor(Pixel[,] pixels, Texture2D tex)
    {
        
        List<Color> palette = new List<Color>();
        if (m_usePaletteOverride)
        {
            palette = m_paletteOverride;
        }
        else { 
            float randHue = ((float)m_pseudoRandom.NextDouble() * (m_hueMinMax[1] - m_hueMinMax[0])) + m_hueMinMax[0]; 
            for (int i = 0; i < m_numberOfColors; i++)
            {
                float saturation = (((float)m_pseudoRandom.NextDouble()) * ((m_saturationMinMax[1] - m_saturationMinMax[0]) / m_numberOfColors * i)) + m_saturationMinMax[0];
                float hue = randHue + ((float)m_pseudoRandom.NextDouble()) * (m_hueDifferenceMinMax[1] - m_hueDifferenceMinMax[0]) + m_hueDifferenceMinMax[0];
                float luminance = (((float)m_pseudoRandom.NextDouble()) * ((m_luminanceMinMax[1] - m_luminanceMinMax[0])) / m_numberOfColors * i) + m_luminanceMinMax[0];
             //   print("saturation " + saturation + " hue " + hue + " luminance " + luminance);
                palette.Add(Color.HSVToRGB(hue, saturation, luminance));
            }
        }

        /*List<Color> colorGroups = new List<Color>();
        for(int i = 0; i < m_numberOfAreas; i++)
        {
            float shiftedHue = (hue + (i*(1f/ m_numberOfAreas))) % m_hueMinMax[1];
            colorGroups.Add(Color.HSVToRGB(shiftedHue, saturation, luminance));
        }*/
        for (int i = 0; i < pixels.GetLength(0); i++)
        {
            for (int j = 0; j < pixels.GetLength(1); j++)
            {
                Color color = Color.clear;
                PixelType pixType =  pixels[i, j].type;
                int pixArea = pixels[i, j].area;
                bool b = false;
                if (GetParentAnchor().position.x == i && GetParentAnchor().position.y == j)
                {
                    color = Color.green;
                    b = true;
                }
                else
                {
                    for (int l = 0; l < GetChildAnchors().Count; l++)
                    {
                        if (GetChildAnchors()[l].position.x == i && GetChildAnchors()[l].position.y == j)
                        {
                            color = Color.green;
                            b = true;
                        }
                    }
                }
                
                if(b == false)
                {
                    // float brightness = Mathf.Sin((i / pixels.GetLength(0)) * Mathf.PI) * (1f - m_brightnessNoise) + Random.Range(0f, 3f) * m_brightnessNoise;
                    if (pixType == PixelType.Solid)
                    {
                        color = Color.blue;
                    }
                    else if (pixType == PixelType.Border)
                    {
                        color = Color.black;
                    }
                    else if (pixType == PixelType.Empty)
                    {
                        color = Color.clear;
                    }
                    else if (pixType == PixelType.ColorGroup)
                    {
                        color = Color.yellow;//m_paletteOverride2[pixArea - 1] + (m_paletteOverride2[pixArea - 1]  * Random.Range(-m_colorNoise, m_colorNoise));
                    }
                    else if (pixType == PixelType.Undecided)
                    {
                        color = Color.black;
                    }
                    else
                    {
                        color = palette[(int)pixType - (int)PixelType.SolidColor1] + palette[(int)pixType - (int)PixelType.SolidColor1] * Random.Range(-m_colorNoise, m_colorNoise);// Color.HSVToRGB(hue, saturation, brightness);
                        color.a = 1;
                    }
                }
                tex.SetPixel(i, j, color);
            }
        }
    }

    /*
    public float m_scaler;
    int SampleRandomColor(int x, int y, int maxValue)
    {

        int val = Mathf.RoundToInt(maxValue * Mathf.Pow(Mathf.Abs(Mathf.PerlinNoise((x * m_seed.GetHashCode()) * m_scaler, (y * m_seed.GetHashCode()) * m_scaler)), m_noiseStrenght));
        return val;
    }
    */
    float CalculateShadow(Coord lightPos, Coord pixelPos)
    {
        //
        //(x-a)² + (y-b)² <= r²
        int distX = lightPos.x - pixelPos.x;
        //0,1,2,3,4
        int distY = lightPos.y - pixelPos.y;

        int x = distX * distX;
        int y = distY * distY;
        //0,1,4,9,16
        float lightXSquared = (m_lightRadius.x * m_lightRadius.x);
        float lightYSquared = (m_lightRadius.y * m_lightRadius.y);
        //25 25

        float light = ((x / lightXSquared) + (y / lightYSquared));
        float luminance = 0f;
        if(light < 1)
        {
            float rad = Mathf.Sqrt(x + y);
            luminance = 1f - ((Mathf.Floor(rad / m_shadowSize) * m_shadowSize/ (m_lightRadius.x * m_lightRadius.y)) * m_shadowStep);
        } 
        //print("pos " + pixelPos.x + ":" + pixelPos.y  + "  x " + x + " y " + y + " light " + light + " luminance " + luminance);
     
        return Mathf.Clamp01(luminance);
    }

    void MirrorX(Pixel[,] pixels)
    {

        for (int i = 0; i < pixels.GetLength(0)/2; i++)
        {
            for (int j = 0; j < pixels.GetLength(1); j++)
            {
                pixels[i, j] = pixels[pixels.GetLength(0) - i -1, j];
            }
        }
    }
    public bool hasNeighbourOfType(Pixel[,] pixels, Coord pixelPos, PixelType type)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if ((x != 0 && y != 0) || (x == 0 && y == 0))
                    continue;
                if ((pixelPos.x + x) < 0 || (pixelPos.x + x) >= pixels.GetLength(0) || (pixelPos.y + y) < 0 || (pixelPos.y + y) >= pixels.GetLength(1))
                    continue;
                if (pixels[pixelPos.x + x, pixelPos.y + y].type == type)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public Anchor GetParentAnchor()
    {
        return m_parentAnchor;
    }

    public List<Anchor> GetChildAnchors()
    {
        return m_childAnchors;
    }

    public void CleanUp()
    {
        if (m_currentSprite != null)
            Destroy(m_currentSprite);
        m_parentAnchor = null;
        if(m_childAnchors != null)
            m_childAnchors.Clear();
    }

    public struct Pixel
    {
        public PixelType type;
        public int area;
        public Pixel(PixelType pixelType, int pixelArea)
        {
            type = pixelType;
            area = pixelArea;
        }
    }
   
    public static float pixelSize = 0.01f;
    public class Anchor
    {
        public Coord position;
        public int value;
        public int maxX;
        public int maxY;
        public Anchor(Coord pos, int val, int xMax, int yMax)
        {
            position = pos;
            value = val;
            maxX = xMax;
            maxY = yMax;
        }

        public Vector2 GetAnchorLocalPos(Transform anchoredTo)
        {
            float width = (anchoredTo.localScale.x * (pixelSize * maxX));
            float height = (anchoredTo.localScale.y * (pixelSize * maxY));
            float x = (width * ((position.x * 1f / maxX))) - (width/2);
            float y = (height * ((position.y * 1f / maxY))) - (height/2);
            return new Vector2(x, y);
        }
    }

}
