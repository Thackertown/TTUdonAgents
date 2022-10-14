
using UdonSharp;
using UnityEngine;
using UnityEngine.AI;
using VRC.SDKBase;
using VRC.Udon;

public class TTAIAgent : UdonSharpBehaviour
{

    private NavMeshAgent agent;
    private VRCPlayerApi targetPlayer;
    private Animator targetAnimator; 

    /* AI Behaviour */
    [Header("AI Movement")]
    public float maxWanderDistance = 5f;
    public float wanderIdleTime = 5f;
    
    public GameObject patrolPathRoot;
    public int patrolIndex = 0;

    public bool isMovingToNext;
    public bool hasSetNextPosition;
    private float internalWaitTime;

    /* State Machine */
    [Header("State Machine")]
    public int currentState;
    // 0 - wandering
    // 1 - follow player
    // 2 - queued  
    // 3 - patrolling
    // 4 - interacting
    // 5 - waiting
    // 6 - Idle with Animation

    [Header("Wandering - State 0"), Tooltip("-1 means infinite")]
    public int wanderCycles = -1;

    [Header("Queued - State 2")]
    public GameObject nextPatrolPoint;

    [Header("Waiting - State 5")]
    public int lastState = 0;
    public float waitTime = 3.0f;
    private float waitRemaining = 0.0f;

    [Header("Idle")]
    public string idleTrigger = "";
    
    

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        targetPlayer = Networking.LocalPlayer;

        internalWaitTime = wanderIdleTime;

        isMovingToNext = false;

        /* get animator on child */
        targetAnimator = transform.GetChild(0).GetComponent<Animator>();
    }

    private void Update()
    {

        /* Do a raycast in front of us; if we hit a player, let's stop moving and interact */


        switch (currentState)
        {
            case 0: // WANDERING
                if (!isMovingToNext)
                {
                    hasSetNextPosition = false;

                    internalWaitTime -= Time.deltaTime;
                    if (internalWaitTime < 0f)
                    {
                        isMovingToNext = true;
                        internalWaitTime = wanderIdleTime;
                        StartWandering();
                    }
                }

                if (agent.remainingDistance < 1 && hasSetNextPosition)
                {
                    isMovingToNext = false;

                    internalWaitTime = wanderIdleTime;
                }
                break;
            case 1: // FOLLOW PLAYER
                // TODO: Calculation move out of Update
                agent.SetDestination(targetPlayer.GetPosition());
                break;
                
            case 2: // QUEUED (someone is patrolling to my target patrol point)
                agent.isStopped = true;

                /* check if the patrol point is clear */
                if (nextPatrolPoint.GetComponent<PatrolPoint>().occupant == null
                    || nextPatrolPoint.GetComponent<PatrolPoint>().occupant == gameObject
                    )
                {
                    currentState = 3;
                    nextPatrolPoint.GetComponent<PatrolPoint>().occupant = gameObject;
                    agent.isStopped = false;
                }
                break; 
            case 3: // PATROLLING
                // TODO: Calculation move out of Update
                if (agent.remainingDistance < 1)
                {
                    if(nextPatrolPoint != null)
                        nextPatrolPoint.GetComponent<PatrolPoint>().occupant = null;
                    isMovingToNext = false;
                }

                if (!isMovingToNext)
                {
                    hasSetNextPosition = false;

                    isMovingToNext = true;
                    GetNextPatrolPoint();

                    /* If that patrol point is occupied, we wait until occupied, otherwise: we become the occupant */
                    if (nextPatrolPoint.GetComponent<PatrolPoint>().occupant == null)
                    {
                        nextPatrolPoint.GetComponent<PatrolPoint>().occupant = gameObject;
                    }
                    else currentState = 2;

                }

                break;
            case 6: // IDLE WITH ANIMATION 
                /* start animation first playthrough */
                if(lastState != currentState)
                {
                    lastState = 6;
                    targetAnimator.SetBool("IdleAnimation", true);
                    if (idleTrigger != "")
                        targetAnimator.SetTrigger(idleTrigger);
                }
                break;


        }

        /* Update Animator */
        targetAnimator.SetFloat("Velocity", agent.velocity.sqrMagnitude);



    }

    void FixedUpdate()
    {
        Vector3 fwd = transform.TransformDirection(Vector3.forward);

        //if (Physics.Raycast(transform.position, fwd, 10))
        //    print("There is something in front of the object!");
        int layerMask = 1 << 9;
        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position + Vector3.up, transform.TransformDirection(Vector3.forward), out hit, 1.0f, layerMask))
        {
            Debug.DrawRay(transform.position + Vector3.up, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
            //Debug.Log("Did Hit");
            waitRemaining -= Time.deltaTime;
            if(currentState != 5 && currentState != 2 && waitRemaining < 0)
                StartWaiting();
        }
        else
        {
            Debug.DrawRay(transform.position + Vector3.up, transform.TransformDirection(Vector3.forward), Color.white);
            //Debug.Log("Did not Hit");
            if (currentState == 5)
            {
                waitRemaining -= Time.deltaTime;
                if (waitRemaining < 0.0f)
                    StopWaiting();
            }

        }
    }


    private void StartWaiting()
    {
        lastState = currentState;
        currentState = 5;
        agent.isStopped = true;
        waitRemaining = waitTime;
    }

    private void StopWaiting()
    {
        currentState = lastState;
        agent.isStopped = false;
        waitRemaining = waitTime;
    }

    private void StartWandering()
    {
        SetAITarget(CalculateRandomPosition(maxWanderDistance));
    }

    public void SetAITarget(Vector3 target)
    {
        hasSetNextPosition = true;
        agent.SetDestination(target);
        Debug.Log($"target = {target}");

    }
    public void GetNextPatrolPoint()
    {
        hasSetNextPosition = true;

        /* Pick next target */
        Vector3 target;
        target = patrolPathRoot.transform.GetChild(patrolIndex).position;
        nextPatrolPoint = patrolPathRoot.transform.GetChild(patrolIndex).gameObject;
        patrolIndex++;
        if (patrolIndex == patrolPathRoot.transform.childCount)
        {
            patrolIndex = 0;
        }

        //Debug.Log($"Next Destination Chosen: {patrolPathRoot.transform.GetChild(patrolIndex).gameObject} with index {patrolIndex}" +
        //    $"remaining distance: {agent.remainingDistance}");

        agent.SetDestination(target);
    }

    private Vector3 CalculateRandomPosition(float dist)
    {
        Vector3 randDir = transform.position + Random.insideUnitSphere * dist;

        NavMeshHit hit;

        NavMesh.SamplePosition(randDir, out hit, dist, NavMesh.AllAreas);

        Debug.Log($"hit target = {hit.position}");
        return hit.position;
    }
}
