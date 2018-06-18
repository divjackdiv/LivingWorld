using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureMovement : MonoBehaviour {
    public Joint m_debugJoint;
	// Use this for initialization
	void Start () {
		
	}
    public float currentAngleDeb;

    // Update is called once per frame
    void Update () {
        if(m_debugJoint != null)//&& Input.GetKeyDown(KeyCode.Space))
        {
            FeedBackward(m_debugJoint);    
        }
    }

    void MoveTo(Vector3 position) {

    }

    void FeedBackward(Joint joint)
    {
        if (joint.m_boneJoint == null || joint.m_boneJoint.previousJoint == null)
            return;
        Transform previousTransform = joint.m_boneJoint.previousJoint.gameObject.transform;
        float currentAngle = currentAngleDeb = Vector2.SignedAngle(Vector3.right, joint.transform.position - previousTransform.position);
        currentAngle = DegToRad(currentAngle);

        Vector3 vecDifference = PolarToCartesian(joint.m_boneJoint.distanceFromLastBone, currentAngle);

        joint.m_boneJoint.previousJoint.gameObject.transform.position = joint.transform.position - vecDifference;
        if(joint.m_boneJoint.previousJoint != null)
            FeedBackward(joint.m_boneJoint.previousJoint.gameObject.GetComponent<Joint>());
      /*  for (int i = 0; i < joint.m_boneJoint.nextJoints.Count; i++)
        {
            FeedForward(joint.m_boneJoint.nextJoints);
        }*/
    }
    /*
    void FeedForward(Joint joint)
    {
        if (joint.m_boneJoint == null || joint.m_boneJoint.previousJoint == null)
            return;
        Transform previousTransform = joint.m_boneJoint.previousJoint.gameObject.transform;
        float currentAngle = currentAngleDeb = Vector2.SignedAngle(Vector3.right, joint.transform.position - previousTransform.position);
        currentAngle = DegToRad(currentAngle);

        Vector3 vecDifference = PolarToCartesian(joint.m_boneJoint.distanceFromLastBone, currentAngle);

        joint.m_boneJoint.previousJoint.gameObject.transform.position = joint.transform.position - vecDifference;
        if (joint.m_boneJoint.previousJoint != null)
            FeedBackward(joint.m_boneJoint.previousJoint.gameObject.GetComponent<Joint>());
        for (int i = 0; i < joint.m_boneJoint.nextJoints.Count; i++)
        {
            FeedForward(joint.m_boneJoint.nextJoints);
        }
    }*/



    Vector2 PolarToCartesian(float distance, float angle)
    {
        return new Vector2(distance * Mathf.Cos(angle), distance * Mathf.Sin(angle));
    }

    float DegToRad(float degrees)
    {
        return degrees == 0 ? 0 : degrees/57.2958f;
    }
}
