using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CargoCubeController : MonoBehaviour
{
    public float speed = 5f;
    public float obstacleCheckDistance = 2f;
    public LayerMask obstacleLayer;

    private Waypoint currentWaypoint;
    private Waypoint targetWaypoint;
    private List<Waypoint> currentPath = new List<Waypoint>();
    private int pathIndex = 0;
    
    private State currentState = State.Idle;
    private bool hasCargo = false;
    private List<Waypoint> deliveryPoints;
    private int currentDeliveryIndex = 0;

    public float timer = 0f;
    public float totalDistance = 0f;
    public int tasksCompleted = 0;
    public int cargoCount = 0;
    private Vector3 lastPosition;

    public enum State
    {
        Idle,
        MovingToPickup,
        DeliveringCargo,
        ReturningToBase,
        WaitingForObstacle
    }

    void Start()
    {
        deliveryPoints = new List<Waypoint>();
        lastPosition = transform.position;
        
        StartCoroutine(CubeStateMachine());
    }

    void Update()
    {
        timer += Time.deltaTime;
        totalDistance += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
    }

    IEnumerator CubeStateMachine()
    {
        yield return new WaitForSeconds(1f);

        Waypoint startWp = WaypointGraph.Instance.GetStartPoint();
        Waypoint pickupWp = WaypointGraph.Instance.waypoints.Count > 0 ? WaypointGraph.Instance.waypoints[0] : null;

        if (startWp == null || pickupWp == null)
        {
            Debug.LogError("No waypoints found!");
            yield break;
        }

        while (true)
        {
            switch (currentState)
            {
                case State.Idle:
                    tasksCompleted = 0;
                    cargoCount = 0;
                    currentDeliveryIndex = 0;
                    
                    SetupDeliveryPoints();
                    currentState = State.MovingToPickup;
                    break;

                case State.MovingToPickup:
                    currentPath = AStarPathfinder.FindPath(currentWaypoint != null ? currentWaypoint : startWp, pickupWp);
                    
                    if (currentPath.Count > 0)
                    {
                        yield return StartCoroutine(FollowPath());
                    }
                    
                    hasCargo = true;
                    cargoCount = deliveryPoints.Count;
                    Debug.Log("Picked up cargo. Stock: " + cargoCount);
                    
                    if (deliveryPoints.Count > 0)
                    {
                        currentDeliveryIndex = 0;
                        currentState = State.DeliveringCargo;
                    }
                    else
                    {
                        currentState = State.ReturningToBase;
                    }
                    break;

                case State.DeliveringCargo:
                    if (currentDeliveryIndex >= deliveryPoints.Count)
                    {
                        currentState = State.ReturningToBase;
                        break;
                    }
                    
                    Waypoint deliveryWp = deliveryPoints[currentDeliveryIndex];
                    currentPath = AStarPathfinder.FindPath(currentWaypoint, deliveryWp);
                    
                    if (currentPath.Count > 0)
                    {
                        yield return StartCoroutine(FollowPath());
                    }
                    
                    Debug.Log("Delivered cargo to point " + deliveryWp.nodeIndex);
                    cargoCount--;
                    tasksCompleted++;
                    currentDeliveryIndex++;
                    
                    if (currentDeliveryIndex >= deliveryPoints.Count)
                    {
                        currentState = State.ReturningToBase;
                    }
                    break;

                case State.ReturningToBase:
                    currentPath = AStarPathfinder.FindPath(currentWaypoint, pickupWp);
                    
                    if (currentPath.Count > 0)
                    {
                        yield return StartCoroutine(FollowPath());
                    }
                    
                    Debug.Log("Returned to base. Cycle complete.");
                    currentState = State.Idle;
                    break;
            }
            yield return null;
        }
    }

    void SetupDeliveryPoints()
    {
        deliveryPoints.Clear();
        var goals = WaypointGraph.Instance.GetGoalPoints();
        
        foreach (var goal in goals)
        {
            if (goal.nodeIndex != 0)
            {
                deliveryPoints.Add(goal);
            }
        }
    }

    IEnumerator FollowPath()
    {
        if (currentPath.Count == 0) yield break;

        pathIndex = 0;
        
        while (pathIndex < currentPath.Count)
        {
            if (IsObstacleAhead())
            {
                currentState = State.WaitingForObstacle;
                Debug.Log("Obstacle detected! Waiting...");
                
                yield return new WaitForSeconds(0.5f);
                
                if (!IsObstacleAhead())
                {
                    currentState = hasCargo ? State.DeliveringCargo : State.MovingToPickup;
                    Debug.Log("Path is clear. Resuming.");
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
                yield return null;
                continue;
            }

            Waypoint targetNode = currentPath[pathIndex];
            Vector3 direction = (targetNode.transform.position - transform.position).normalized;
            
            transform.position = Vector3.MoveTowards(transform.position, targetNode.transform.position, speed * Time.deltaTime);
            
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            if (Vector3.Distance(transform.position, targetNode.transform.position) < 0.2f)
            {
                currentWaypoint = targetNode;
                pathIndex++;
            }

            yield return null;
        }
    }

    bool IsObstacleAhead()
    {
        return Physics.Raycast(transform.position, transform.forward, obstacleCheckDistance, obstacleLayer);
    }

    public string GetCurrentState() => currentState.ToString();
    public int GetTasksCompleted() => tasksCompleted;
}
