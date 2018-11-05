using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureMovement : MonoBehaviour {
    public Transform m_followDebug;
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
    public float m_lerpSpeed = 6f;
    public AnimationCurve m_legHeightCurve;

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
        if(m_followDebug != null && m_debugJoint !=null)
            MoveTo(m_followDebug.position, m_debugJoint);
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
            else if (m_currentMovingFeetCount == 0 || m_currentMovingFeetCount + 1 <= ((m_creatureFeet.Count/100f) * m_percentageOfMovingFeet))
            {
                if (currentFoot.ShouldTakeAStep(finalPosition, m_feetCatchUp))
                {
                    print("taking a new step " + i);
                    m_currentMovingFeetCount++;
                    StepForward(currentFoot, finalPosition);
                }
            }
            else if(m_currentMovingFeetCount > 0 && m_currentMovingFeetCount + 1 >= ((m_creatureFeet.Count / 100f) * 66f))
            {
                print("leg " + i + " cannot move cause of other legs ");
            }
            else
            {
                print("cant take a step " + i);
            }
        }
        
    }

    
    void MoveBodyJointTo(Vector2 finalPosition, Joint leadingJoint)
    {
        //Find the most constraining foot in the body, TODO this will never change so keep it stored and remove the following loop
        Vector3 constrainedPosition = finalPosition;
        for (int i = 0; i < m_creature.m_feet.Count; i++)
        {
            Foot currentFoot = m_creatureFeet[i];
            BoneJoint closestBodyJoint = currentFoot.joint.closestBodyJoint;
            float currentDist = currentFoot.joint.maxDistanceFromBodyJoint + closestBodyJoint.maxDistanceFromHead;
            constrainedPosition = GetPosLocked(m_creature.m_feet[i].transform.position, constrainedPosition, currentDist);
        }

        leadingJoint.transform.position = Vector2.MoveTowards(leadingJoint.transform.position, constrainedPosition, m_movementSpeed * Time.deltaTime);
        UpdatePosToNeighbours(leadingJoint);
    }


    public bool move = false;
    public float minRand = 0.5f;
    public float maxRand = 1f;
    public float completeness;
    Vector2 m_target;
    public void StepForward(Foot foot, Vector2 targetDestination)
    {
        BoneJoint footJoint = foot.joint;
        if (foot.lastPos == foot.currentTarget) // if this is the first frame in this step, figure out where the foot is going
        {
            Vector3 stepTarget = footJoint.closestBodyJoint.transform.position;
            float xDistTargetToFoot = targetDestination.x - footJoint.transform.position.x;
            stepTarget.x += ((xDistTargetToFoot > 0) ? 1 : -1) * (m_footStepWidth * footJoint.maxDistanceFromBodyJoint);
            stepTarget.y = m_floorY; //DEBUG until I implement raycasts
            foot.currentStepHeight = m_footStepHeight * footJoint.maxDistanceFromBodyJoint;// Random.Range(minRand, maxRand);
            foot.currentTarget = GetPosLocked(footJoint.transform.position, stepTarget, footJoint.maxDistanceFromBodyJoint);
            m_target = foot.currentTarget;
            m_vector2Gizmos = foot.currentTarget;
        }
        if (foot.lastPos == foot.currentTarget) // if we are already here
            return;
        /*float stepCompleteness = footJoint.maxDistanceFromBodyJoint - Vector2.Distance(footJoint.transform.position, foot.currentTarget);
        stepCompleteness /= footJoint.maxDistanceFromBodyJoint; //divide by the step distance, change this if the dist varies
        completeness = stepCompleteness;*/
        

        float footXToTargetX = (foot.currentTarget.x - footJoint.transform.position.x);
        float stepCompleteness = footXToTargetX / (foot.currentTarget.x - foot.lastPos.x);
        completeness = stepCompleteness;

        int targetIsToTheRight = footXToTargetX >= 0 ? 1 : -1;
        //float dist = Mathf.Min(Vector2.Distance(footJoint.transform.position, foot.currentTarget), footJoint.maxDistanceFromBodyJoint);
        Vector2 pathToTarget;
        // float interpolatedHeight = Mathf.Lerp(foot.lastPos.y, foot.currentTarget.y, stepCompleteness); 
        float diffFromLastFrame = (m_feetSpeed * Time.deltaTime);
        pathToTarget.y = m_floorY + (m_legHeightCurve.Evaluate(stepCompleteness + diffFromLastFrame) * foot.currentStepHeight);
        pathToTarget.x = footJoint.transform.position.x + (diffFromLastFrame * targetIsToTheRight);
        if (Mathf.Abs(diffFromLastFrame) > Mathf.Abs(footXToTargetX))
        {
            print("eh lfh " + (diffFromLastFrame * targetIsToTheRight) +  " rhs " + footXToTargetX);
            pathToTarget.x = foot.currentTarget.x;
        }
        /*
        Vector2 footToTarget;
        footToTarget.x = foot.currentTarget.x - footJoint.transform.position.x;
        footToTarget.y = m_floorY + (m_legHeightCurve.Evaluate(stepCompleteness) * foot.currentStepHeight);
        Vector2 unitVector = footToTarget.normalized;
        Vector2 target = unitVector * dist;
        target = (Vector2) footJoint.transform.position + target;*/
        // m_vector2Gizmos = target;
        if (move)
        {
            footJoint.transform.position = pathToTarget;// Vector2.MoveTowards(footJoint.transform.position, pathToTarget, m_feetSpeed * Time.deltaTime);
            if (Vector2.Distance(footJoint.transform.position, foot.currentTarget) < 0.01f)
            {
                foot.isGrounded = true;
                foot.lastPos = foot.currentTarget;
            }
            else
                foot.isGrounded = false;
            //UpdatePosToNeighbours(foot);
        }
    }
    private Vector2 m_vector2Gizmos;
    private void OnDrawGizmos()
    {
        if (m_debugJoint != null)
        {
            if (m_target != null)
            {
                Gizmos.DrawLine(m_creature.m_feet[0].transform.position, m_target);
                Gizmos.DrawCube(m_creature.m_feet[0].transform.position, Vector3.one * 0.05f);
                Gizmos.DrawWireSphere(m_target, 0.05f);
            }
            /* for(int i = 0; i < m_creatureFeet.Count; i++)
             {
                 if (m_creatureFeet[i].ShouldTakeAStep())
                 {
                     Gizmos.DrawLine(m_creatureFeet[i].joint.transform.position, m_creatureFeet[i].currentTarget);
                     Gizmos.DrawCube(m_creatureFeet[i].joint.transform.position, Vector3.one * 0.05f);
                 }
             }*/
        }
        
    }

    void UpdatePosToNeighbours(Joint joint, Joint jointToIgnore = null)
    {
        if (joint.m_boneJoint.previousJoint != null) {
            Joint previousJoint = joint.m_boneJoint.previousJoint.gameObject.GetComponent<Joint>();
            if (previousJoint != jointToIgnore)
            {
                if(!joint.m_boneJoint.isLegJoint || previousJoint.m_boneJoint.isLegJoint) //don't update from leg to body. Only body to body, body to leg, or leg to leg
                    FeedBackward(joint, previousJoint);
            }
        }
        for (int i = 0; i < joint.m_boneJoint.nextJoints.Count;i++)
        {
            Joint nextJoint = joint.m_boneJoint.nextJoints[i].gameObject.GetComponent<Joint>();
            if (nextJoint != jointToIgnore)
            {

                if (!joint.m_boneJoint.isLegJoint || nextJoint.m_boneJoint.isLegJoint) //don't update from leg to body. Only body to body, body to leg, or leg to leg
                    FeedForward(joint, nextJoint);
            }
        }
    }

    void FeedBackward(Joint from, Joint previous)
    {
        if (from.m_boneJoint == null || previous.m_boneJoint == null)
            return;
        float currentAngle = Vector2.SignedAngle(Vector3.right, from.transform.position - previous.transform.position);
        currentAngle = Mathf.Lerp(DegToRad(currentAngle), from.m_boneJoint.angleFromLastBone, m_lerpWeight);
        Vector3 vecDifference = PolarToCartesian(from.m_boneJoint.distanceFromLastBone, currentAngle);

        previous.transform.position = from.transform.position - vecDifference;
        UpdatePosToNeighbours(previous, from);
    }
    
    void FeedForward(Joint from, Joint next)
    {
        if (from.m_boneJoint == null || next.m_boneJoint == null)
            return;
        Vector2 fromPos = from.transform.position;
        Vector2 nextPos = next.transform.position;

        Vector2 targetPos = (nextPos - fromPos).normalized;
        targetPos = ((Vector2) fromPos) + (targetPos * next.m_boneJoint.distanceFromLastBone);
        if (next.m_boneJoint.isLegJoint)
        {
            if (next.m_boneJoint.nextJoints != null && next.m_boneJoint.nextJoints.Count == 1)
            {
                BoneJoint debugFoot = next.m_boneJoint.nextJoints[0];
                float distFromFoot = next.m_boneJoint.distanceFromLastBone;
                while (debugFoot.nextJoints != null && debugFoot.nextJoints.Count > 0) //TODO optimise this (ie store it)
                {
                    debugFoot = debugFoot.nextJoints[0];
                    distFromFoot += debugFoot.distanceFromLastBone;                    
                }


                Vector2 lockedTo = debugFoot.transform.position;
                Vector3 lockedPos = GetPosLocked(fromPos, nextPos, next.m_boneJoint.distanceFromLastBone, false);
                targetPos = GetPosLocked(lockedTo, lockedPos, distFromFoot, true);
            }
            else
                targetPos = nextPos;
        }
        next.transform.position = Vector2.MoveTowards(next.transform.position, targetPos, m_lerpSpeed * Time.deltaTime);
        UpdatePosToNeighbours(next, from);
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
    public BoneJoint joint;
    public Vector2 lastPos;
    public Vector2 currentTarget; //Stored here is the current target of this foot, ie where it's gonna step towards.
    public float currentStepHeight;
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