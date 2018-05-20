using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BodyPartType { Head = 0, Trunk = 1, Limb = 2, Accessory }; //Accessories are anything else, like a tail

[Serializable]
public class CharacterTemplate
{
    public SpriteGenerator spriteGenerator;
    public int orderInLayer;
    public BodyPartType type;
}

public class CharacterGenerator : MonoBehaviour {
    
    public List<CharacterTemplate> m_characterParts;
    public GameObject m_characterPrefab;
    public GameObject m_pivotPrefab;
    public float m_scale = 1f;

    private GameObject m_character;
    private Dictionary<int, GameObject> m_parentPivots;
    // Use this for initialization
    void Start () {
        m_parentPivots = new Dictionary<int, GameObject>();
    }

	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.C))
        {
            GenerateCharacter();
        }
    }

    GameObject GenerateCharacter()
    {
        CleanUp();

        m_character = (GameObject)Instantiate(m_characterPrefab);
        List<CharacterTemplate> characterParts = m_characterParts.OrderBy(t => t.spriteGenerator.GetParentAnchor()).ToList();
        for (int i = 0; i < characterParts.Count; i++)
        {
            GameObject generated = characterParts[i].spriteGenerator.GenerateSprite();
            generated.transform.position = new Vector3(0, 0, 0);
            generated.GetComponent<SpriteRenderer>().sortingOrder = characterParts[i].orderInLayer;
            GameObject parentPivot = (GameObject)Instantiate(m_pivotPrefab);
            SpriteGenerator.Anchor parentAnchor = characterParts[i].spriteGenerator.GetParentAnchor();
            if (m_parentPivots.ContainsKey(parentAnchor.value))
            {
                parentPivot.transform.parent = generated.transform;
                parentPivot.transform.localPosition = parentAnchor.GetAnchorLocalPos(generated.transform);
                parentPivot.transform.parent = m_parentPivots[parentAnchor.value].transform;
                generated.transform.parent = parentPivot.transform;
                parentPivot.transform.localPosition = new Vector3(0,0,0);
            }
            else
            {
                parentPivot.transform.parent = generated.transform;
                parentPivot.transform.localPosition = parentAnchor.GetAnchorLocalPos(generated.transform);
                parentPivot.transform.parent = m_character.transform;
                generated.transform.parent = parentPivot.transform;
                parentPivot.transform.localPosition = new Vector3(0, 0, 0);
                m_parentPivots.Add(parentAnchor.value, parentPivot);
            }

            List<SpriteGenerator.Anchor> anchors = characterParts[i].spriteGenerator.GetChildAnchors().OrderBy(t => t.value).ToList();
            for (int a = 0; a < anchors.Count; a++)
            {
                if (m_parentPivots.ContainsKey(anchors[a].value) == false)
                {
                    GameObject secondaryPivot = (GameObject)Instantiate(m_pivotPrefab);
                    secondaryPivot.transform.parent = parentPivot.transform;
                    secondaryPivot.transform.localPosition = new Vector2(parentPivot.transform.GetChild(0).localPosition.x, parentPivot.transform.GetChild(0).localPosition.y) + anchors[a].GetAnchorLocalPos(generated.transform);
                    m_parentPivots.Add(anchors[a].value, secondaryPivot);
                }
            }
        }
        m_character.transform.localScale *= m_scale;
        m_character.transform.position = transform.position;
        return m_character;
    }

    Dictionary<int, GameObject> OrderByKey(Dictionary<int, GameObject> dict)
    {
        Dictionary<int, GameObject> ordered = dict.OrderByDescending(t => t.Key).ToDictionary(mc => mc.Key, t => t.Value);
        return ordered;
    }
    
    void CleanUp()
    {
        if (m_character != null)
            Destroy(m_character);
        m_parentPivots.Clear();
        for (int i = 0; i < m_characterParts.Count; i++)
        {
            m_characterParts[i].spriteGenerator.CleanUp();
        }
    }
}
