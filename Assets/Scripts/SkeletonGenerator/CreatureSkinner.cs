using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Applies a skin to a skeleton, which controls how it looks.
public class CreatureSkinner : MonoBehaviour {

    public GameObject m_creaturePrefab;
    public GameObject m_jointPrefab;

    public bool m_useSprites;
    public Sprite m_headSprite;
    public Sprite m_bodySprite;
    public Sprite m_legSprite;
    public Sprite m_feetSprite;

    [Header("Lines")]
    public Vector2 m_bodyLineRendererScaleMinMax = new Vector2(0.08f, 0.2f);
    public Vector2 m_legsLineRendererScaleMinMax = new Vector2(0.08f, 0.2f);

    [Header("Palette Colors")]
    public Coord m_nbOfColorsMinMax = new Coord(2, 5);
    public Vector2 m_hueDifferenceMinMax = new Vector2(0.02f, 0.08f);
    public Vector2 m_hueMinMax = new Vector2(0f, 1f);
    public Vector2 m_saturationMinMax = new Vector2(0f, 1f);
    public Vector2 m_luminanceMinMax = new Vector2(0f, 1f);
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}


    public GameObject CreateSkeletonGameObject(Skeleton s, System.Random random)
    {
        GameObject creature = Instantiate(m_creaturePrefab);
        creature.GetComponent<Creature>().m_skeleton = s;
        List<BoneJoint> jointsToVisit = new List<BoneJoint>();
        jointsToVisit.Add(s.head);
        GameObject lastJointGO = creature;
        float bodyLineRendererScale = Mathf.Lerp(m_bodyLineRendererScaleMinMax.x, m_bodyLineRendererScaleMinMax.y, (float)random.NextDouble());
        float legsLineRendererScale = Mathf.Lerp(m_legsLineRendererScaleMinMax.x, m_legsLineRendererScaleMinMax.y, (float)random.NextDouble());
        while (jointsToVisit.Count > 0)
        {
            BoneJoint currentJoint = jointsToVisit[0];
            if (currentJoint.previousJoint != null)
                lastJointGO = currentJoint.previousJoint.gameObject;
            GameObject currentJointGO = Instantiate(m_jointPrefab);
            currentJointGO.transform.parent = creature.transform;// lastJointGO.transform;
            currentJointGO.transform.localScale = currentJoint.scale;
            currentJointGO.transform.position = lastJointGO.transform.position;

            Vector2 pos = Utility.PolarToCartesian(currentJoint.distanceFromLastBone, currentJoint.angleFromLastBone);
            currentJointGO.transform.position += new Vector3(pos.x, pos.y, 0);
            currentJointGO.GetComponent<SpriteRenderer>().material.color = currentJoint.color;
            if (m_useSprites)
            {
                Sprite correctSprite = GetCorrectSprite(currentJoint);
                if(correctSprite != null)
                    currentJointGO.GetComponent<SpriteRenderer>().sprite = correctSprite;
            }
            Joint joint = currentJointGO.AddComponent<Joint>();
            joint.m_boneJoint = currentJoint;

            LineRenderer lr = currentJointGO.GetComponent<LineRenderer>();
            lr.SetPosition(0, currentJointGO.transform.position);
            lr.startColor = currentJoint.color;
            lr.startWidth = currentJoint.scale.x * (currentJoint.isPartOfBody() ? bodyLineRendererScale : legsLineRendererScale);
            lr.SetPosition(1, lastJointGO.transform.position);
            if (currentJoint.previousJoint != null)
            {
                lr.endWidth = currentJoint.previousJoint.scale.x * (currentJoint.previousJoint.isPartOfBody() ? bodyLineRendererScale : legsLineRendererScale); 
                lr.endColor = currentJoint.previousJoint.color;
            }
            currentJoint.gameObject = currentJointGO;
            currentJoint.transform = currentJointGO.transform;
            jointsToVisit.RemoveAt(0);
            if (currentJoint.nextJoints != null)
                jointsToVisit.AddRange(currentJoint.nextJoints);
        }
        return creature;
    }

    public List<Color> GenerateRandomColorPalette(System.Random random)
    {
        return GenerateRandomColorPalette(random, m_nbOfColorsMinMax, m_hueMinMax, m_saturationMinMax, m_luminanceMinMax, m_hueDifferenceMinMax);
    }

    public List<Color> GenerateRandomColorPalette(System.Random random, Coord numberOfColorsMinMax, Vector2 hueMinMax, Vector2 saturationMinMax, Vector2 luminanceMinMax, Vector2 hueDifferenceMinMax)
    {
        List<Color> palette = new List<Color>();
        int numberOfColors = (int)(Mathf.PerlinNoise((float)random.NextDouble() * 10f, 0) * (numberOfColorsMinMax.y - numberOfColorsMinMax.x) + numberOfColorsMinMax.x);
        float randHue = (Mathf.PerlinNoise((float)random.NextDouble() * 10f, 0) * (hueMinMax[1] - hueMinMax[0])) + hueMinMax[0];
        for (int i = 0; i < numberOfColors; i++)
        {
            float saturation = ((Mathf.PerlinNoise((float)random.NextDouble() * 10f, 0)) * ((saturationMinMax[1] - saturationMinMax[0]) / numberOfColors * i)) + saturationMinMax[0];
            float hue = (randHue + (Mathf.PerlinNoise((float)random.NextDouble() * 10f, 0)) * (hueDifferenceMinMax[1] - hueDifferenceMinMax[0]) + hueDifferenceMinMax[0]) % 1f;
            float luminance = ((Mathf.PerlinNoise((float)random.NextDouble() * 10f, 0)) * ((luminanceMinMax[1] - luminanceMinMax[0])) / numberOfColors * i) + luminanceMinMax[0];
            //   print("saturation " + saturation + " hue " + hue + " luminance " + luminance);
            palette.Add(Color.HSVToRGB(hue, saturation, luminance));
            randHue = hue;
        }

        return palette;
    }

    Sprite GetCorrectSprite(BoneJoint joint)
    {
        if (joint.isPartOfBody())
        {
            if(joint.previousJoint == null)
                return m_headSprite;
            else
                return m_bodySprite;
        }
        else
        {
            if (joint.nextJoints == null || joint.nextJoints.Count == 0)
                return m_feetSprite;
            else
                return m_legSprite;
        }
    }
}
