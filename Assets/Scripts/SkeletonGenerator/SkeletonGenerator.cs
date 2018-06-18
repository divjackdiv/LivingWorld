using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneJoint
{
    public Vector2 scale;
    public float distanceFromLastBone;
    public float angleFromLastBone;
    public BoneJoint previousJoint;
    public List<BoneJoint> nextJoints;
    public GameObject gameObject;
    public Color color;
    public BoneJoint(BoneJoint previous, float distLastBone, float angLastBone, float xScale, float yScale, Color col)
    {
        previousJoint = previous;
        scale = new Vector2(xScale, yScale);
        distanceFromLastBone = distLastBone;
        angleFromLastBone = angLastBone;
        nextJoints = new List<BoneJoint>();
        color = col;
    }
}

public class Skeleton
{
    public BoneJoint head;
}

public class SkeletonGenerator : MonoBehaviour {
    [Header("General")]
    public string m_seed;
    public bool m_useRandomSeed;
    public GameObject m_creaturePrefab;


    [Header("Body Joints")]
    public GameObject m_jointPrefab;
    public int m_minJointCount = 10;
    public int m_maxJointCount = 30;
    public float m_minJointScale = 1;
    public float m_maxJointScale = 1;
    public float m_minDist = 0.5f;
    public float m_maxDist = 2;
    public float m_minAngleDiff = -1f;
    public float m_maxAngleDiff = 1;


    [Header("Leg Joints")]
    public int m_minLegCount = 1;
    public int m_maxLegCount = 10;
    public int m_minLegJointCount = 10;
    public int m_maxLegJointCount = 30;
    public float m_minLegJointScale = 1;
    public float m_maxLegJointScale = 1;
    public float m_minLegDist = 0.5f;
    public float m_maxLegDist = 2;
    public float m_minLegAngleDiff = -1;
    public float m_maxLegAngleDiff = 1;
    public float m_minYPos = 0;

    [Header("Lines")]
    public float m_lineRendererScale = 0.1f;

    [Header("Palette Colors")]
    public Coord m_nbOfColorsMinMax = new Coord(2, 5);
    public Vector2 m_hueDifferenceMinMax = new Vector2(0.02f, 0.08f);
    public Vector2 m_hueMinMax = new Vector2(0f, 1f);
    public Vector2 m_saturationMinMax = new Vector2(0f, 1f);
    public Vector2 m_luminanceMinMax = new Vector2(0f, 1f);

    [Header("Misc")]
    public bool m_takeScreenshot;


    private System.Random m_pseudoRandom;
    // Use this for initialization
    void Start ()
    {
       
	}

    GameObject m_currentSkeleton;
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (m_currentSkeleton != null)
                Destroy(m_currentSkeleton);
            UpdateSeed();
            Skeleton skel = GenerateRandomSkeleton();
            m_currentSkeleton = CreateSkeletonGameObject(skel);
            if(m_takeScreenshot)
                ScreenCapture.CaptureScreenshot("CreatureScreenshots/"+m_seed+".png");
        }
	}

    Skeleton GenerateRandomSkeleton()
    {
        int jointCount = m_minJointCount + (int)(RandomFloat() * (m_maxJointCount - m_minJointCount));
        int legCount = m_minLegCount + (int)(RandomFloat() * (m_maxLegCount - m_minLegCount));
        int legJointCount = m_minLegJointCount + (int)(RandomFloat() * (m_maxLegJointCount - m_minLegJointCount));
        List<Color> colorPalette = GenerateRandomColorPalette(m_nbOfColorsMinMax, m_hueMinMax, m_saturationMinMax, m_luminanceMinMax, m_hueDifferenceMinMax);
        Skeleton skel = new Skeleton();
        skel.head = GenerateRandomSkeletonJoints(jointCount, legCount, legJointCount, colorPalette);
        return skel;
    }

    
    public BoneJoint GenerateRandomSkeletonJoints(int jointCount, int legCount, int legJointCount, List<Color> palette)
    {
        float perlinNoiseSeedOffset = RandomFloat(); //without this we would get the same result every time
        float perlinNoiseXOffset = RandomFloat() * 10f; //for nicer results we get a random subsection of the perlin noise
        float perlinNoiseX = 0f;
        BoneJoint firstJoint = GenerateRandomBoneJoint(null, m_minJointScale, m_maxJointScale, 0, 0, 0, 0, perlinNoiseSeedOffset + perlinNoiseX + perlinNoiseXOffset, perlinNoiseSeedOffset + 0f, palette);
        if (jointCount == 1)
        {//if only a head and no other body joints, attach legs to head, otherwise never have head to leg connection
            for (int i = 0; i < legCount; i++)
            {
                GenerateLeg(legJointCount, firstJoint, perlinNoiseSeedOffset + ((i*1f) / legCount), palette);
            }
        }
        BoneJoint previousFrameJoint = firstJoint;
        int leg = 0;
        int[] legIndexes = RandomIntArray(legCount, 1, jointCount-1);
        for (int i = 1; i < jointCount; i++)
        {
            perlinNoiseX = (i*1f + 1f) / jointCount;
            BoneJoint newJoint = GenerateRandomBoneJoint(previousFrameJoint, m_minJointScale, m_maxJointScale, m_minDist, m_maxDist, m_minAngleDiff, m_maxAngleDiff, perlinNoiseSeedOffset + perlinNoiseX, perlinNoiseSeedOffset + 0f, palette);
            previousFrameJoint.nextJoints.Add(newJoint);
            previousFrameJoint = newJoint;
            if (leg < legIndexes.Length)
            {
                for (int j = 0; j < legCount; j++)
                {
                    if (legIndexes[j] == i)
                    {
                        GenerateLeg(legJointCount, newJoint, perlinNoiseSeedOffset + ((leg * 1f) / legCount), palette);
                        leg++;
                    }
                }
            }
        }
        return firstJoint;
    }

    public void GenerateLeg(int jointCount, BoneJoint previousLegJoint, float perlinNoiseSeedOffset, List<Color> palette)
    {
        float legPerlinNoiseX = 0f;
        for (int k = 0; k < jointCount; k++)
        {
            BoneJoint newLegJoint = GenerateRandomBoneJoint(previousLegJoint, m_minLegJointScale, m_maxLegJointScale, m_minLegDist, m_maxLegDist, m_minLegAngleDiff, m_maxLegAngleDiff, perlinNoiseSeedOffset + legPerlinNoiseX, perlinNoiseSeedOffset + 0f, palette);
            previousLegJoint.nextJoints.Add(newLegJoint);
            previousLegJoint = newLegJoint;
            legPerlinNoiseX = (k * 1f) / jointCount;
        }
    }

    public BoneJoint GenerateRandomBoneJoint(BoneJoint previousJoint, float minJointScale, float maxJointScale, float minDist, float maxDist, float minAngleDiff, float maxAngleDiff, float perlinNoiseX, float perlinNoiseY, List<Color> ColorPalette)
    {
        float dist = minDist + (Mathf.PerlinNoise(perlinNoiseX, perlinNoiseY) * (maxDist - minDist));
        float angle = minAngleDiff + (Mathf.PerlinNoise(perlinNoiseX, (perlinNoiseY+0.1f)) * (maxAngleDiff - minAngleDiff)); 
        float xScale = minJointScale + (Mathf.PerlinNoise(perlinNoiseX, perlinNoiseY) * (maxJointScale - minJointScale));
        float yScale = xScale;
        Color color = ColorPalette[(int)(Mathf.PerlinNoise(perlinNoiseX, perlinNoiseY) * ColorPalette.Count)];
        return new BoneJoint(previousJoint, dist, angle, xScale, yScale, color);
    }
    
    public int[] RandomIntArray(int numberOfInts, int minValue, int maxValue)
    {
        int[] randomArr = new int[numberOfInts];
        for(int i = 0; i < numberOfInts; i++)
        {
            int rand = minValue + (int)(RandomFloat() * (maxValue - minValue));
            randomArr[i] = rand;
        }
        return randomArr;
    }

    void UpdateSeed()
    {
        if (m_useRandomSeed)
        {
            m_seed = System.DateTime.Now.Ticks.ToString();
        }
        m_pseudoRandom = new System.Random(m_seed.GetHashCode());
    }

    float RandomFloat()
    {
        return (float) m_pseudoRandom.NextDouble();
    }

    float RandomUnsignedFloat()
    {
        return (2f * (float)m_pseudoRandom.NextDouble()) -1f;
    }

    GameObject CreateSkeletonGameObject(Skeleton s)
    {
        GameObject creature = Instantiate(m_creaturePrefab);
        List<BoneJoint> jointsToVisit = new List<BoneJoint>();
        jointsToVisit.Add(s.head);
        GameObject lastJointGO = creature;
        while (jointsToVisit.Count > 0)
        {
            BoneJoint currentJoint = jointsToVisit[0];
            if(currentJoint.previousJoint != null)
                lastJointGO = currentJoint.previousJoint.gameObject;
            GameObject currentJointGO = Instantiate(m_jointPrefab);
            currentJointGO.transform.parent = creature.transform;
            currentJointGO.transform.localScale = currentJoint.scale;
            currentJointGO.transform.parent = creature.transform;// lastJointGO.transform;
            currentJointGO.transform.position = lastJointGO.transform.position;
            Vector2 pos = PolarToCartesian(currentJoint.distanceFromLastBone, currentJoint.angleFromLastBone);
            currentJointGO.transform.position += new Vector3(pos.x, pos.y,0);
            currentJointGO.GetComponent<Renderer>().material.color = currentJoint.color;
            Joint joint = currentJointGO.AddComponent<Joint>();
            joint.m_boneJoint = currentJoint;

            LineRenderer lr = currentJointGO.GetComponent<LineRenderer>();
            lr.SetPosition(0, currentJointGO.transform.position);
            lr.startColor = currentJoint.color;
            lr.startWidth = currentJoint.scale.x * m_lineRendererScale;
            lr.SetPosition(1, lastJointGO.transform.position);
            if (currentJoint.previousJoint != null)
            {
                lr.endWidth = currentJoint.previousJoint.scale.x * m_lineRendererScale;
                lr.endColor = currentJoint.previousJoint.color;
            }
            currentJoint.gameObject = currentJointGO;
            jointsToVisit.RemoveAt(0);
            if (currentJoint.nextJoints != null)
                jointsToVisit.AddRange(currentJoint.nextJoints);
        }
        return creature;
    }
    
    Vector2 PolarToCartesian(float distance, float angle)
    {
        return new Vector2(distance * Mathf.Cos(angle), distance * Mathf.Sin(angle));
    }

    List<Color> GenerateRandomColorPalette(Coord numberOfColorsMinMax, Vector2 hueMinMax, Vector2 saturationMinMax, Vector2 luminanceMinMax, Vector2 hueDifferenceMinMax)
    {
        List<Color> palette = new List<Color>();
        int numberOfColors =(int) (Mathf.PerlinNoise((float)m_pseudoRandom.NextDouble() * 10f, 0) * (numberOfColorsMinMax.y - numberOfColorsMinMax.x) + numberOfColorsMinMax.x);
        float randHue = (Mathf.PerlinNoise((float)m_pseudoRandom.NextDouble() * 10f,0) * (hueMinMax[1] - hueMinMax[0])) + hueMinMax[0];
        for (int i = 0; i < numberOfColors; i++)
        {
            float saturation = ((Mathf.PerlinNoise((float)m_pseudoRandom.NextDouble() * 10f, 0)) * ((saturationMinMax[1] - saturationMinMax[0]) / numberOfColors * i)) + saturationMinMax[0];
            float hue = (randHue + (Mathf.PerlinNoise((float)m_pseudoRandom.NextDouble() * 10f, 0))  * (hueDifferenceMinMax[1] - hueDifferenceMinMax[0]) + hueDifferenceMinMax[0])%1f;
            float luminance = ((Mathf.PerlinNoise((float)m_pseudoRandom.NextDouble() * 10f, 0)) * ((luminanceMinMax[1] - luminanceMinMax[0])) / numberOfColors * i) + luminanceMinMax[0];
            //   print("saturation " + saturation + " hue " + hue + " luminance " + luminance);
            palette.Add(Color.HSVToRGB(hue, saturation, luminance));
            randHue = hue;
        }
        
        return palette;
    }
}
