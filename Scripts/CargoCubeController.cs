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
    private int nextDeliveryIndex = 1;
    private int currentDeliveryIndex = 0;
    private bool returningToStart = false;

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
        lastPosition = transform.position;
        
        Waypoint startWp = WaypointGraph.Instance.GetStartPoint();
        if (startWp != null)
        {
            currentWaypoint = startWp;
            transform.position = startWp.transform.position;
        }
        
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
        yield return new WaitForSeconds(0.5f);

        if (WaypointGraph.Instance.waypoints.Count < 2)
        {
            Debug.LogError("Need at least 2 waypoints!");
            yield break;
        }

        while (true)
        {
            switch (currentState)
            {
                case State.Idle:
                    tasksCompleted = 0;
                    cargoCount = WaypointGraph.Instance.waypoints.Count - 1;
                    nextDeliveryIndex = 1;
                    returningToStart = false;
                    
                    Waypoint startWp = WaypointGraph.Instance.waypoints[0];
                    currentWaypoint = startWp;
                    transform.position = startWp.transform.position;
                    
                    Debug.Log("Starting from waypoint 0. Cargo loaded: " + cargoCount);
                    currentState = State.DeliveringCargo;
                    break;

                case State.DeliveringCargo:
                    if (nextDeliveryIndex >= WaypointGraph.Instance.waypoints.Count)
                    {
                        currentState = State.ReturningToBase;
                        break;
                    }
                    
                    Waypoint deliveryWp = WaypointGraph.Instance.waypoints[nextDeliveryIndex];
                    Debug.Log("Moving to waypoint " + deliveryWp.nodeIndex);
                    
                    currentPath = AStarPathfinder.FindPath(currentWaypoint, deliveryWp);
                    
                    if (currentPath.Count > 0)
                    {
                        yield return StartCoroutine(FollowPath());
                    }
                    
                    Debug.Log("Reached waypoint " + deliveryWp.nodeIndex);
                    tasksCompleted++;
                    currentDeliveryIndex++;
                    cargoCount--;
                    nextDeliveryIndex++;
                    
                    if (nextDeliveryIndex >= WaypointGraph.Instance.waypoints.Count)
                    {
                        currentState = State.ReturningToBase;
                    }
                    break;

                case State.ReturningToBase:
                    if (!returningToStart)
                    {
                        returningToStart = true;
                        Waypoint pickupWp = WaypointGraph.Instance.waypoints[0];
                        Debug.Log("Returning to start point 0");
                        
                        currentPath = AStarPathfinder.FindPath(currentWaypoint, pickupWp);
                        
                        if (currentPath.Count > 0)
                        {
                            yield return StartCoroutine(FollowPath());
                        }
                        
                        Debug.Log("Returned to start. Cycle complete.");
                    }
                    
                    yield return new WaitForSeconds(2f);
                    currentState = State.Idle;
                    break;
            }
            yield return null;
        }
    }

    IEnumerator FollowPath()
    {
        if (currentPath.Count == 0) yield break;

        pathIndex = 0;
        
        while (pathIndex < currentPath.Count)
        {
            while (IsObstacleAhead())
            {
                currentState = State.WaitingForObstacle;
                Debug.Log("Obstacle detected! Waiting...");
                yield return new WaitForSeconds(0.5f);
                
                if (!IsObstacleAhead())
                {
                    Debug.Log("Path is clear. Resuming.");
                    break;
                }
            }

            if (pathIndex >= currentPath.Count) break;

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
