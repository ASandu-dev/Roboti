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
                    pathIndex = 0;
                    currentPath.Clear();
                    
                    Waypoint startWp = WaypointGraph.Instance.waypoints[0];
                    currentWaypoint = startWp;
                    transform.position = startWp.transform.position;
                    
                    Debug.Log("=== IDLE: Starting new cycle. Total waypoints: " + WaypointGraph.Instance.waypoints.Count);
                    currentState = State.DeliveringCargo;
                    break;

                case State.DeliveringCargo:
                    if (nextDeliveryIndex >= WaypointGraph.Instance.waypoints.Count)
                    {
                        currentState = State.ReturningToBase;
                        break;
                    }
                    
                    Waypoint deliveryWp = WaypointGraph.Instance.waypoints[nextDeliveryIndex];
                    targetWaypoint = deliveryWp;
                    Debug.Log("=== Moving to waypoint " + deliveryWp.nodeIndex + " from " + (currentWaypoint != null ? currentWaypoint.nodeIndex.ToString() : "null"));
                    
                    if (currentWaypoint == null)
                    {
                        currentWaypoint = WaypointGraph.Instance.waypoints[0];
                    }
                    
                    currentPath = AStarPathfinder.FindPath(currentWaypoint, deliveryWp);
                    
                    Debug.Log("Path found: " + currentPath.Count + " nodes");
                    
                    if (currentPath.Count == 0)
                    {
                        Debug.LogWarning("No path found - going directly");
                        currentPath = new List<Waypoint> { deliveryWp };
                    }
                    
                    yield return StartCoroutine(FollowPath());
                    
                    Debug.Log("=== Reached waypoint " + deliveryWp.nodeIndex + ", currentWaypoint is now: " + (currentWaypoint != null ? currentWaypoint.nodeIndex.ToString() : "null"));
                    tasksCompleted++;
                    currentDeliveryIndex++;
                    cargoCount--;
                    nextDeliveryIndex++;
                    
                    Debug.Log("=== nextDeliveryIndex now: " + nextDeliveryIndex + ", waypoints.Count: " + WaypointGraph.Instance.waypoints.Count);
                    
                    if (nextDeliveryIndex >= WaypointGraph.Instance.waypoints.Count)
                    {
                        currentState = State.ReturningToBase;
                        Debug.Log("=== All deliveries done, returning to base");
                    }
                    break;

                case State.ReturningToBase:
                    if (!returningToStart)
                    {
                        returningToStart = true;
                        Waypoint pickupWp = WaypointGraph.Instance.waypoints[0];
                        targetWaypoint = pickupWp;
                        Debug.Log("Returning to start point 0");
                        
                        currentPath = AStarPathfinder.FindPath(currentWaypoint, pickupWp);
                        
                        if (currentPath.Count == 0)
                        {
                            currentPath = new List<Waypoint> { pickupWp };
                        }
                        
                        yield return StartCoroutine(FollowPath());
                        
                        Debug.Log("Returned to start. Cycle complete.");
                    }
                    
                    yield return new WaitForSeconds(2f);
                    currentState = State.Idle;
                    break;
            }
            Debug.Log("=== End of switch, currentState: " + currentState);
            yield return null;
        }
    }

    IEnumerator FollowPath()
    {
        if (currentPath.Count == 0)
        {
            Debug.LogWarning("FollowPath: empty path!");
            yield break;
        }

        pathIndex = 0;
        float waitTime = 0f;
        float maxWaitTime = 5f;
        
        Debug.Log("FollowPath started with " + currentPath.Count + " nodes");
        
        while (pathIndex < currentPath.Count)
        {
            if (pathIndex >= currentPath.Count) break;
            
            if (IsObstacleAhead())
            {
                currentState = State.WaitingForObstacle;
                Debug.Log("Obstacle detected! Waiting...");
                waitTime = 0f;
                
                while (pathIndex < currentPath.Count && IsObstacleAhead() && waitTime < maxWaitTime)
                {
                    yield return new WaitForSeconds(0.5f);
                    waitTime += 0.5f;
                }
                
                if (waitTime >= maxWaitTime)
                {
                    Debug.Log("Obstacle blocking too long. Finding new path...");
                    yield return StartCoroutine(RecalculatePath());
                    if (currentPath.Count == 0)
                    {
                        Debug.Log("No path found!");
                        yield break;
                    }
                    pathIndex = 0;
                    continue;
                }
                else
                {
                    Debug.Log("Path is clear. Resuming.");
                }
            }

            if (pathIndex >= currentPath.Count) break;

            Waypoint targetNode = currentPath[pathIndex];
            float dist = Vector3.Distance(transform.position, targetNode.transform.position);
            
            Debug.Log("Moving to node " + pathIndex + " (distance: " + dist + ")");
            
            Vector3 direction = (targetNode.transform.position - transform.position).normalized;
            
            transform.position = Vector3.MoveTowards(transform.position, targetNode.transform.position, speed * Time.deltaTime);
            
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            if (dist < 0.2f)
            {
                Debug.Log("Reached node " + pathIndex);
                currentWaypoint = targetNode;
                pathIndex++;
            }

            yield return null;
        }
        
        Debug.Log("FollowPath completed");
    }

    IEnumerator RecalculatePath()
    {
        if (targetWaypoint != null)
        {
            currentPath = AStarPathfinder.FindPath(currentWaypoint, targetWaypoint);
            Debug.Log("Path recalculated. New path has " + currentPath.Count + " nodes.");
        }
        yield return null;
    }

    bool IsObstacleAhead()
    {
        if (currentPath.Count == 0 || pathIndex >= currentPath.Count)
            return false;
            
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Vector3 direction;
        
        if (currentPath[pathIndex] != null)
        {
            direction = (currentPath[pathIndex].transform.position - transform.position).normalized;
        }
        else
        {
            direction = transform.forward;
        }
        
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, direction, out hit, obstacleCheckDistance, obstacleLayer))
        {
            if (hit.collider.gameObject != gameObject)
            {
                Debug.Log("Obstacle detected: " + hit.collider.name);
                return true;
            }
        }
        return false;
    }

    public string GetCurrentState() => currentState.ToString();
    public int GetTasksCompleted() => tasksCompleted;
}
