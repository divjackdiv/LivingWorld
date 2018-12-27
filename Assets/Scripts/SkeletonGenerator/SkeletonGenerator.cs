using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneJoint
{
    public enum BoneJointType { Body = 0, Hip, Leg, Foot} //TODO consider seperating these into different child classes
    public BoneJointType type;
    public Vector2 scale;
    public float distanceFromLastBone;
    public float angleFromLastBone;
    public BoneJoint previousJoint;
    public List<BoneJoint> nextJoints;
    public GameObject gameObject;
    public Transform transform;
    public Color color;
    //public bool isLegJoint;
    public BoneJoint attachedFoot;
    public float distanceFromFoot;
    public float maxDistanceFromHead; //Only populated and used for feet
    public BoneJoint closestBodyJoint; //Only populated and used for feet
    public float maxDistanceFromBodyJoint; //Only populated and used for feet
    public BoneJoint(BoneJoint previous, float distLastBone, float angLastBone, float xScale, float yScale, Color col, bool isPartOfLeg)
    {
        previousJoint = previous;
        scale = new Vector2(xScale, yScale);
        distanceFromLastBone = distLastBone;
        angleFromLastBone = angLastBone;
        nextJoints = new List<BoneJoint>();
        color = col;
        if (isPartOfLeg)
            type = BoneJointType.Leg;
        else
            type = BoneJointType.Body;
        //  isLegJoint = isPartOfLeg;
        closestBodyJoint = null;
        attachedFoot = null;
        maxDistanceFromBodyJoint = 0;
        maxDistanceFromHead = 0;
        distanceFromFoot = 0;
    }

    public bool isPartOfLeg()
    {
        return (type == BoneJoint.BoneJointType.Leg || type == BoneJoint.BoneJointType.Foot);
    }
    public bool isPartOfBody()
    {
        return (type == BoneJoint.BoneJointType.Body || type == BoneJoint.BoneJointType.Hip);
    }
}

public class Skeleton
{
    public BoneJoint head;
    public BoneJoint baseOfNeck;
    public List<BoneJoint> feet;
    public BoneJoint baseOfTail;
    public BoneJoint endOfTail;
    public int boneCount;

    public void SetupFromHead()
    {
        feet = new List<BoneJoint>();
        List<BoneJoint> bodyJointsToVisit = new List<BoneJoint>();
        bodyJointsToVisit.Add(head);
        boneCount = 0;
        float maxDistFromBodyJoint = 0f;
        float maxDistFromHead = 0f;
        BoneJoint latestBodyJoint = null;
        while (bodyJointsToVisit.Count > 0)
        {
            BoneJoint currentJoint = bodyJointsToVisit[0];
            boneCount++;
            maxDistFromHead += currentJoint.distanceFromLastBone;
            currentJoint.maxDistanceFromHead = maxDistFromHead;
            latestBodyJoint = currentJoint;
            maxDistFromBodyJoint = 0f;
            if (baseOfNeck == null)
            {
                if (currentJoint.nextJoints != null && currentJoint.nextJoints.Count >= 2)
                    baseOfNeck = currentJoint;
            }
            else if (currentJoint.nextJoints != null)
            {
                baseOfTail = currentJoint;
            }
            if(baseOfTail == null)
            {
                baseOfTail = currentJoint;
            }

            endOfTail = currentJoint;
            for (int i = 0; i < currentJoint.nextJoints.Count; i++)
            {
                BoneJoint nextJoint = currentJoint.nextJoints[i];
                if (nextJoint.isPartOfLeg())
                {
                    while (nextJoint != null)
                    {
                        boneCount++;
                        maxDistFromBodyJoint += nextJoint.distanceFromLastBone;
                        nextJoint.maxDistanceFromHead = maxDistFromHead + maxDistFromBodyJoint;
                        nextJoint.maxDistanceFromBodyJoint = maxDistFromBodyJoint;
                        nextJoint.closestBodyJoint = latestBodyJoint;
                        if (nextJoint.nextJoints == null || nextJoint.nextJoints.Count == 0)
                        {
                            nextJoint.type = BoneJoint.BoneJointType.Foot;
                            feet.Add(nextJoint);
                            maxDistFromBodyJoint = 0f;
                            nextJoint = null;
                        }
                        else
                        {
                            nextJoint = nextJoint.nextJoints[0];// No need to worry about other next joints as only a body joint can have multiple next joints       
                        }
                    }
                }
                else
                {
                    bodyJointsToVisit.Add(nextJoint);
                }
            }
            bodyJointsToVisit.RemoveAt(0);
        }
        if(baseOfNeck == null)
        {
            baseOfNeck = head;
        }

        for(int i = 0; i < feet.Count; i++) {
            BoneJoint currentBonejoint = feet[i];
            float dist = 0;
            while (currentBonejoint.previousJoint != null && currentBonejoint.isPartOfLeg())
            {
                currentBonejoint.attachedFoot = feet[i];
                currentBonejoint.distanceFromFoot = dist;
                currentBonejoint = currentBonejoint.previousJoint;
                dist += currentBonejoint.distanceFromLastBone;
            }
            if(currentBonejoint != null && currentBonejoint.isPartOfBody())
            {
                currentBonejoint.type = BoneJoint.BoneJointType.Hip;
            }
        }
    }
}

public class SkeletonGenerator : MonoBehaviour {
    [Header("General")]
    public string m_seed;
    public bool m_useRandomSeed;
    public CreatureSkinner m_skinner;


    [Header("Body Joints")]
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


    [Header("Misc")]
    public bool m_takeScreenshot;


    private System.Random m_pseudoRandom;
    private GameObject m_currentSkeleton;
    // Use this for initialization
    void Start ()
    {
       
	}

	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (m_currentSkeleton != null)
                Destroy(m_currentSkeleton);
            UpdateSeed();
            Skeleton skel = GenerateRandomSkeleton();
            m_currentSkeleton = m_skinner.CreateSkeletonGameObject(skel, m_pseudoRandom);
            if(m_takeScreenshot)
                ScreenCapture.CaptureScreenshot("CreatureScreenshots/"+m_seed+".png");
        }
	}

    Skeleton GenerateRandomSkeleton()
    {
        int jointCount = m_minJointCount + (int)(RandomFloat() * (m_maxJointCount - m_minJointCount));
        int legCount = m_minLegCount + (int)(RandomFloat() * (m_maxLegCount - m_minLegCount));
        int legJointCount = m_minLegJointCount + (int)(RandomFloat() * (m_maxLegJointCount - m_minLegJointCount));
        List<Color> colorPalette = m_skinner.GenerateRandomColorPalette(m_pseudoRandom);
        Skeleton skel = new Skeleton();
        skel.head = GenerateRandomSkeletonJoints(jointCount, legCount, legJointCount, colorPalette);
        skel.SetupFromHead();
        return skel;
    }

    
    public BoneJoint GenerateRandomSkeletonJoints(int jointCount, int legCount, int legJointCount, List<Color> palette)
    {
        float perlinNoiseSeedOffset = RandomFloat(); //without this we would get the same result every time
        float perlinNoiseXOffset = RandomFloat() * 10f; //for nicer results we get a random subsection of the perlin noise
        float perlinNoiseX = 0f;
        BoneJoint firstJoint = GenerateRandomBoneJoint(null, m_minJointScale, m_maxJointScale, 0, 0, 0, 0, perlinNoiseSeedOffset + perlinNoiseX + perlinNoiseXOffset, perlinNoiseSeedOffset + 0f, palette, false);
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
            BoneJoint newJoint = GenerateRandomBoneJoint(previousFrameJoint, m_minJointScale, m_maxJointScale, m_minDist, m_maxDist, m_minAngleDiff, m_maxAngleDiff, perlinNoiseSeedOffset + perlinNoiseX, perlinNoiseSeedOffset + 0f, palette, false);
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
            BoneJoint newLegJoint = GenerateRandomBoneJoint(previousLegJoint, m_minLegJointScale, m_maxLegJointScale, m_minLegDist, m_maxLegDist, m_minLegAngleDiff, m_maxLegAngleDiff, perlinNoiseSeedOffset + legPerlinNoiseX, perlinNoiseSeedOffset + 0f, palette, true);
            previousLegJoint.nextJoints.Add(newLegJoint);
            previousLegJoint = newLegJoint;
            legPerlinNoiseX = (k * 1f) / jointCount;
        }
    }

    public BoneJoint GenerateRandomBoneJoint(BoneJoint previousJoint, float minJointScale, float maxJointScale, float minDist, float maxDist, float minAngleDiff, float maxAngleDiff, float perlinNoiseX, float perlinNoiseY, List<Color> ColorPalette, bool isLegJoint)
    {
        float dist = minDist + (Mathf.PerlinNoise(perlinNoiseX, perlinNoiseY) * (maxDist - minDist));
        float angle = minAngleDiff + (Mathf.PerlinNoise(perlinNoiseX, (perlinNoiseY+0.1f)) * (maxAngleDiff - minAngleDiff)); 
        float xScale = minJointScale + (Mathf.PerlinNoise(perlinNoiseX, perlinNoiseY) * (maxJointScale - minJointScale));
        float yScale = xScale;
        Color color = ColorPalette[(int)(Mathf.PerlinNoise(perlinNoiseX, perlinNoiseY) * ColorPalette.Count)];
        return new BoneJoint(previousJoint, dist, angle, xScale, yScale, color, isLegJoint);
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

}
