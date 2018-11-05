using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Joint : MonoBehaviour {

    [HideInInspector]
    public BoneJoint m_boneJoint;
    public float m_zPos = -0.001f;

    LineRenderer m_lineRenderer;
    Transform previousJointTransform;
    void Start()
    {
        previousJointTransform = m_boneJoint.previousJoint != null ? m_boneJoint.previousJoint.transform : null;
        m_lineRenderer = GetComponent<LineRenderer>();
        if (previousJointTransform == null)
        {
            GetComponent<LineRenderer>().enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (previousJointTransform != null)
        {
            TrackPrevious();
        }
    }

    void TrackPrevious()
    {
        m_lineRenderer.SetPosition(0, new Vector3(transform.position.x, transform.position.y, transform.position.z + m_zPos));
        m_lineRenderer.SetPosition(1, new Vector3(previousJointTransform.position.x, previousJointTransform.position.y, previousJointTransform.position.z + m_zPos));
    }
}
