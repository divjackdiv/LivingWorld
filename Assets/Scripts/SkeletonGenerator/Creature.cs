using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Creature : MonoBehaviour {
    public Skeleton m_skeleton;
    public Joint m_head;
    public Joint m_baseOfNeck;
    public Joint m_baseOfTail;
    public Joint m_endOfTail;
    public List<Joint> m_feet;
    [Header("Debug")]
    public bool attachToCamera;
    private Transform attachedCamera;
    // Use this for initialization
    void Start () {;        
        m_head = m_skeleton.head.gameObject.GetComponent<Joint>();
        m_baseOfNeck = m_skeleton.baseOfNeck.gameObject.GetComponent<Joint>();
        m_baseOfTail = m_skeleton.baseOfTail.gameObject.GetComponent<Joint>();
        m_endOfTail = m_skeleton.endOfTail.gameObject.GetComponent<Joint>();
        print("spawned with " + m_skeleton.feet.Count + " feet");
        for(int i = 0; i < m_skeleton.feet.Count; i++)
        {
            m_feet.Add(m_skeleton.feet[i].gameObject.GetComponent<Joint>());
        }
        attachedCamera = Camera.main.transform;
        if (attachToCamera)
        {
            Vector3 cameraPos = attachedCamera.transform.position - m_head.transform.position;
            cameraPos.z = transform.position.z;
            transform.position = attachedCamera.transform.position;
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (attachToCamera)
        {
            attachedCamera.position = new Vector3(m_head.transform.position.x, attachedCamera.position.y, attachedCamera.position.z);
        }
    }
}
