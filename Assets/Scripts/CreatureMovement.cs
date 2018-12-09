using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureMovement : MonoBehaviour {
    public Joint m_debugJoint;
    public float m_lerpWeight = 1f;
    public float m_floorY;
    public float m_footStepWidth = 0.3f;
    public float m_footStepHeight = 1f;

    [Tooltip("Speed of the creature as a whole")]
    public float m_movementSpeed = 1f;
    public float m_feetSpeed = 1f;
    [Tooltip("This modifies how far behind a leg has to be before trying to catch up, lower is nearer.")]
    public float m_feetCatchUp = 0.9f;
    [Range(0,100)]
    public float m_percentageOfMovingFeet = 66f;

    [Tooltip("Speed at which bonejoints follow the body")]
    public float m_boneJointsLerpSpeed = 6f;
    public AnimationCurve m_legHeightCurve;

    [Header("Debug")]
    public bool m_followMouse;

    private List<Foot> m_creatureFeet;
    private Creature m_creature;
    private int m_currentMovingFeetCount;

    // Use this for initialization
    void Start () {
        m_creature = GetComponent<Creature>();
        m_creatureFeet = new List<Foot>();
        for (int i=0; i < m_creature.m_feet.Count; i++)
        {
            Foot foot = new Foot(m_creature.m_feet[i].m_boneJoint);
            m_creatureFeet.Add(foot);
        }
        m_debugJoint = m_creature.m_head;
    }

    // Update is called once per frame
    void Update () {
        if(m_followMouse && m_debugJoint != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            MoveTo(mousePos, m_debugJoint);
        }
    }

    void MoveTo(Vector2 finalPosition, Joint leadingJoint)
    {
        //Step 1 - Move the body towards the point
        MoveBodyJointTo(finalPosition, leadingJoint);

        //Step 2 - Make the legs take a step forward if needed    
        m_currentMovingFeetCount = 0;
        for (int i = 0; i < m_creature.m_feet.Count; i++)
        {
            Foot currentFoot = m_creatureFeet[i];
            if (currentFoot.isGrounded == false)
            {
                m_currentMovingFeetCount++;
                StepForward(currentFoot, finalPosition);
            }
            else if (m_currentMovingFeetCount == 0 || ((m_currentMovingFeetCount + 1) <= ((m_creatureFeet.Count/100f) * m_percentageOfMovingFeet)))
            {

                if (currentFoot.ShouldTakeAStep(finalPosition, m_feetCatchUp))
                {
                    m_currentMovingFeetCount++;
                    StepForward(currentFoot, finalPosition);
                }
            }
           /* else if(m_currentMovingFeetCount > 0 && (m_currentMovingFeetCount + 1 >= ((m_creatureFeet.Count / 100f) * 66f)))
            {
                print("leg " + i + " cannot move cause of other legs ");
            }
            else
            {
                print("cant take a step " + i);
            }*/
        }
        
    }

    
    void MoveBodyJointTo(Vector2 finalPosition, Joint leadingJoint)
    {
        Vector2 constrainedPosition = finalPosition;
        for (int i = 0; i < m_creature.m_feet.Count; i++)
        {
            Joint currentFoot = m_creature.m_feet[i];
            Transform currentFootTransform = currentFoot.transform;
            BoneJoint closestBodyJoint = currentFoot.m_boneJoint.closestBodyJoint;

            Vector2 constrainedToBodyJoint = GetPosLocked(currentFootTransform.position, closestBodyJoint.transform.position, currentFoot.m_boneJoint.maxDistanceFromBodyJoint);
            float distance = closestBodyJoint.maxDistanceFromHead;
            distance += currentFoot.m_boneJoint.maxDistanceFromBodyJoint - Vector2.Distance(currentFootTransform.position, closestBodyJoint.transform.position); //add any remaining distance
            constrainedPosition = GetPosLocked(constrainedToBodyJoint, constrainedPosition, distance);       
        }

        leadingJoint.transform.position = Vector2.MoveTowards(leadingJoint.transform.position, constrainedPosition, m_movementSpeed * Time.deltaTime);
        UpdatePosToNeighbours(leadingJoint);
    }


    public bool move = false;
    public float minRand = 0.5f;
    public float maxRand = 1f;
    public float completeness;
    Vector2 m_target;
    Vector2 m_targetVec;
    public float currentDistX;
    public void StepForward(Foot foot, Vector2 targetDestination)
    {
        BoneJoint footJoint = foot.joint;
        if (foot.stepTargetDefined == false) // if this is the first frame in this step, figure out where the foot is going
        {
            bool foundSuitableNextStep = DefineNextStep(foot, targetDestination);
            if (foundSuitableNextStep == false)
                return;
        }
        Vector2 footToTarget = (foot.currentTarget -((Vector2)footJoint.transform.position));
        footToTarget = footToTarget.normalized;

        Vector2 pathToTarget;
        float diffFromLastFrame = (m_feetSpeed * Time.deltaTime);

        pathToTarget.y = footJoint.transform.position.y + (diffFromLastFrame * footToTarget.y);//  targetIsAbove);
        pathToTarget.x = footJoint.transform.position.x + (diffFromLastFrame * footToTarget.x);// targetIsToTheRight);

        // float xToYRatio = 1;
        // if (foot.currentTarget.y - foot.startStepPos.y != 0)
        //     xToYRatio = Mathf.Abs((foot.currentTarget.x - foot.startStepPos.x) / (foot.currentTarget.y - foot.startStepPos.y));

        float xDist  = Mathf.Clamp01(Mathf.Abs((footJoint.transform.position.x - foot.currentTarget.x) /(foot.startStepPos.x - foot.currentTarget.x)));
        foot.stepCompleteness = completeness = 1f - xDist;
        
        Vector2 animationCurve = Vector2.zero;
      //  animationCurve.x = (m_legHeightCurve.Evaluate(foot.stepCompleteness) * foot.currentStepHeight);// * (1 - xToYRatio));
        animationCurve.y = (m_legHeightCurve.Evaluate(foot.stepCompleteness) * foot.currentStepHeight) * (m_feetSpeed * Time.deltaTime);// * xToYRatio);

        pathToTarget += animationCurve;
        m_targetVec = pathToTarget;

        if (move)
        {
            footJoint.transform.position = pathToTarget;// Vector2.MoveTowards(footJoint.transform.position, pathToTarget, m_feetSpeed * Time.deltaTime);
            if (Vector2.Distance(footJoint.transform.position, foot.currentTarget) <= 0.01f)
            {
                foot.isGrounded = true;
                foot.stepTargetDefined = false;
            }
            else
                foot.isGrounded = false;
        }
    }

    private bool DefineNextStep(Foot foot, Vector2 targetDestination)
    {
        BoneJoint footJoint = foot.joint;
        foot.stepTargetDefined = true;
        Vector3 stepTarget = footJoint.closestBodyJoint.transform.position;
        float xDistTargetToFoot = targetDestination.x - footJoint.transform.position.x;
        stepTarget.x += ((xDistTargetToFoot > 0) ? 1 : -1) * (m_footStepWidth * footJoint.maxDistanceFromBodyJoint);
        stepTarget.y = m_floorY; //DEBUG until I implement raycasts
        foot.currentStepHeight = m_footStepHeight * footJoint.maxDistanceFromBodyJoint;// Random.Range(minRand, maxRand);
        foot.currentTarget = GetPosLocked(footJoint.closestBodyJoint.transform.position, stepTarget, footJoint.maxDistanceFromBodyJoint * 0.8f);
        foot.currentTarget.y = m_floorY;
        m_target = foot.currentTarget;

        m_vector2Gizmos = foot.currentTarget;
        foot.stepCompleteness = 0;
        foot.startStepPos = footJoint.transform.position;
        
        if (foot.startStepPos == foot.currentTarget) // if we are already here
        {
            Debug.LogError("oddly we are already here");
            foot.isGrounded = true;
            foot.stepTargetDefined = false;
            return false;
        }
        if (Vector3.Distance(foot.currentTarget, targetDestination) > Vector3.Distance(footJoint.transform.position, targetDestination))
        {
            //Debug.Log("the next step would be worse or equal");
            foot.isGrounded = true;
            foot.stepTargetDefined = false;
            return false;
        }
        return true;
    }
    private Vector2 m_vector2Gizmos;
    private void OnDrawGizmos()
    {
        if (m_debugJoint != null)
        {
            for(int i = 0; i < m_creatureFeet.Count; i++)
            {

               /* Joint currentFoot = m_creature.m_feet[i];
                Transform currentFootTransform = currentFoot.transform;
                BoneJoint closestBodyJoint = currentFoot.m_boneJoint.closestBodyJoint;

                Vector3 constrainedToBodyJoint = GetPosLocked(currentFootTransform.position, closestBodyJoint.transform.position, currentFoot.m_boneJoint.maxDistanceFromBodyJoint);
                Vector3 constrainedPosition = GetPosLocked(constrainedToBodyJoint, m_target, closestBodyJoint.maxDistanceFromHead);
                
                Gizmos.DrawWireSphere(currentFootTransform.position, currentFoot.m_boneJoint.maxDistanceFromBodyJoint);
                Gizmos.DrawLine(currentFootTransform.position, m_vector2Gizmos);

                Gizmos.DrawWireSphere(currentFoot.m_boneJoint.closestBodyJoint.transform.position, currentFoot.m_boneJoint.maxDistanceFromBodyJoint * m_feetCatchUp);
                Gizmos.DrawWireSphere(currentFoot.m_boneJoint.closestBodyJoint.transform.position, currentFoot.m_boneJoint.maxDistanceFromBodyJoint);*/
                //Gizmos.DrawLine(constrainedToBodyJoint, m_target);

            }
        }
        
    }

    void UpdatePosToNeighbours(Joint joint, Joint jointToIgnore = null)
    {
        if (joint.m_boneJoint.previousJoint != null) {
            Joint previousJoint = joint.m_boneJoint.previousJoint.gameObject.GetComponent<Joint>();
            if (previousJoint != jointToIgnore)
            {
                if(!joint.m_boneJoint.isPartOfLeg() || previousJoint.m_boneJoint.isPartOfLeg()) //don't update from leg to body. Only body to body, body to leg, or leg to leg
                    FeedBackward(joint, previousJoint);
            }
        }
        for (int i = 0; i < joint.m_boneJoint.nextJoints.Count;i++)
        {
            Joint nextJoint = joint.m_boneJoint.nextJoints[i].gameObject.GetComponent<Joint>();
            if (nextJoint != jointToIgnore)
            {

                if (!joint.m_boneJoint.isPartOfLeg() || nextJoint.m_boneJoint.isPartOfLeg()) //don't update from leg to body. Only body to body, body to leg, or leg to leg
                    FeedForward(joint, nextJoint);
            }
        }
    }

    void FeedBackward(Joint from, Joint previous)
    {
        print("feed backward ");
        if (from.m_boneJoint == null || previous.m_boneJoint == null)
            return;
        float currentAngle = Vector2.SignedAngle(Vector3.right, from.transform.position - previous.transform.position);
        currentAngle = Mathf.Lerp(DegToRad(currentAngle), from.m_boneJoint.angleFromLastBone, m_lerpWeight);
        Vector3 vecDifference = PolarToCartesian(from.m_boneJoint.distanceFromLastBone, currentAngle);

        previous.transform.position = from.transform.position - vecDifference;
        UpdatePosToNeighbours(previous, from);
    }

    void FeedForward(Joint previous, Joint currentJoint)
    {
        if (previous.m_boneJoint == null || currentJoint.m_boneJoint == null)
            return;
        Vector2 previousJointPos = previous.transform.position;
        Vector2 currentJointPos = currentJoint.transform.position;

        Vector2 targetPos = GetPosLocked(previousJointPos, currentJointPos, currentJoint.m_boneJoint.distanceFromLastBone, false);
        
        if (currentJoint.m_boneJoint.isPartOfLeg()) //is either a legjoint
        {
            if (currentJoint.m_boneJoint.nextJoints != null )
            {
                Vector2 lockedTo = currentJoint.m_boneJoint.attachedFoot.transform.position;
                Vector3 lockedPos = GetPosLocked(previousJointPos, currentJointPos, currentJoint.m_boneJoint.distanceFromLastBone, false);
                targetPos = GetPosLocked(lockedTo, lockedPos, currentJoint.m_boneJoint.distanceFromFoot, true);
            }
            else //if is a foot 
                targetPos = currentJointPos;
        }
        else 
        {
            if (currentJoint.m_boneJoint.type == BoneJoint.BoneJointType.Hip)
            {
                for (int i = 0; i < currentJoint.m_boneJoint.nextJoints.Count; i++)
                {
                    BoneJoint nextJoint = currentJoint.m_boneJoint.nextJoints[i];
                    if (nextJoint.isPartOfLeg())
                    {
                        Vector2 lockedTo = nextJoint.attachedFoot.transform.position;
                        Vector2 jointMaxPos = GetPosLocked(lockedTo, currentJointPos, nextJoint.distanceFromFoot, true); //where the joint should be

                        //now that we have the correct position of the joint, treat it as you would a leg joint
                        targetPos = GetPosLocked(previousJointPos, jointMaxPos, currentJoint.m_boneJoint.distanceFromLastBone, false);
                        targetPos = GetPosLocked(lockedTo, targetPos, nextJoint.distanceFromFoot + currentJoint.m_boneJoint.distanceFromLastBone, true);
                        break;
                    }
                }
            }
            else
            {
                BoneJoint nextHip = currentJoint.m_boneJoint;
                float distFromHip = 0;
                int i = 0;
                while(nextHip.type != BoneJoint.BoneJointType.Hip && nextHip.nextJoints.Count > 0)
                {

                    i++;
                    if (i >= 100) {
                        Debug.LogError("you dumb ass ");
                        break;
                    }
                    nextHip = nextHip.nextJoints[0];
                    distFromHip += nextHip.distanceFromLastBone;
                }
                Vector2 jointMaxPos = GetPosLocked(nextHip.transform.position, currentJointPos, distFromHip, true);
                targetPos = GetPosLocked(previousJointPos, jointMaxPos, currentJoint.m_boneJoint.distanceFromLastBone, false);
            }
        }
        currentJoint.transform.position = targetPos;//Vector2.MoveTowards(currentJoint.transform.position, targetPos, m_boneJointsLerpSpeed * Time.deltaTime);
        UpdatePosToNeighbours(currentJoint, previous);
    }
 
    //Get a position towards a direction while always being locked within X distance of another positon
    Vector2 GetPosLocked(Vector2 lockedTo, Vector2 moveTowards, float maxDistance, bool canBeCloserThanMaxDist = true)
    {
        float posDist = maxDistance;
        float dist = canBeCloserThanMaxDist ? Vector2.Distance(lockedTo, moveTowards) : posDist;

        Vector2 unitVec = (moveTowards - lockedTo).normalized;
        return (lockedTo + (unitVec * Mathf.Min(posDist,dist)));
    }
    

    Vector2 PolarToCartesian(float distance, float angle)
    {
        return new Vector2(distance * Mathf.Cos(angle), distance * Mathf.Sin(angle));
    }

    float DegToRad(float degrees)
    {
        return degrees == 0 ? 0 : degrees/57.2958f;
    }
}

//handy redefinition
public class Foot
{
    public bool isGrounded;
    public bool stepTargetDefined;
    public BoneJoint joint;
    public Vector2 startStepPos;
    public Vector2 currentTarget; //Stored here is the current target of this foot, ie where it's gonna step towards.
    public float currentStepHeight;
    public float stepCompleteness;
    public Foot(BoneJoint footJoint)
    {
        joint = footJoint;
    }

    public bool ShouldTakeAStep(Vector3 target, float distanceMultiplier = 1f)
    {
        float distFromJoint = Vector2.Distance(joint.transform.position, joint.closestBodyJoint.transform.position);
        float distFromTarget = Vector2.Distance(joint.transform.position, target);
        bool isFarAwayEnoughFromBodyJoint = distFromJoint > (joint.maxDistanceFromBodyJoint * distanceMultiplier);
        bool isFutherFromTargetThanBodyJoint = distFromTarget > Vector2.Distance(joint.closestBodyJoint.transform.position, target);

        return (isGrounded == false) || (isFarAwayEnoughFromBodyJoint && isFutherFromTargetThanBodyJoint);
    }
}