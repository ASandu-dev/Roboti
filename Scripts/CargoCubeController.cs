using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CargoCubeController : MonoBehaviour
{
    public Transform[] waypoints; // 0 is pickup, 1-9 are delivery points
    public float speed = 5f;
    public float obstacleCheckDistance = 2f;
    public LayerMask obstacleLayer;

    private int currentWaypointIndex = 0;
    private State currentState = State.MovingToPickup;
    private bool hasCargo = false;
    private List<int> deliveryIndices;

    // Public properties for UI
    public float timer = 0f;
    public float totalDistance = 0f;
    public int tasksCompleted = 0;
    public int cargoCount = 0;
    private Vector3 lastPosition;

    public enum State
    {
        MovingToPickup,
        DeliveringCargo,
        WaitingForObstacle
    }

    void Start()
    {
        if (waypoints.Length != 10)
        {
            Debug.LogError("Please assign exactly 10 waypoints.");
            enabled = false;
            return;
        }
        
        InitializeDeliveryList();
        lastPosition = transform.position;
        StartCoroutine(CubeStateMachine());
    }

    void InitializeDeliveryList()
    {
        deliveryIndices = new List<int>();
        for (int i = 1; i < waypoints.Length; i++)
        {
            deliveryIndices.Add(i);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        totalDistance += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
    }

    IEnumerator CubeStateMachine()
    {
        while (true) // Loop indefinitely
        {
            switch (currentState)
            {
                case State.MovingToPickup:
                    tasksCompleted = 0; // Reset task count for the new run
                    yield return StartCoroutine(MoveTo(waypoints[0].position));
                    hasCargo = true;
                    cargoCount = waypoints.Length - 1; // Stock up cargo
                    Debug.Log("Picked up cargo. Stock: " + cargoCount);
                    currentState = State.DeliveringCargo;
                    currentWaypointIndex = GetNextDeliveryPoint();
                    break;

                case State.DeliveringCargo:
                    yield return StartCoroutine(MoveTo(waypoints[currentWaypointIndex].position));
                    Debug.Log("Delivered cargo to point " + currentWaypointIndex);
                    cargoCount--;
                    tasksCompleted++;
                    deliveryIndices.Remove(currentWaypointIndex);
                    
                    currentWaypointIndex = GetNextDeliveryPoint();
                    if(currentWaypointIndex == -1)
                    {
                        // All deliveries for this run are done, go back to pickup
                        Debug.Log("All deliveries completed for this cycle. Returning to base.");
                        InitializeDeliveryList(); // Reset for next run
                        currentState = State.MovingToPickup;
                    }
                    break;

                case State.WaitingForObstacle:
                    yield return new WaitForSeconds(0.5f); // Wait before re-checking
                    if (!IsObstacleAhead())
                    {
                        currentState = hasCargo ? State.DeliveringCargo : State.MovingToPickup;
                        Debug.Log("Path is clear. Resuming movement.");
                    }
                    break;
            }
            yield return null;
        }
    }

    IEnumerator MoveTo(Vector3 destination)
    {
        while (Vector3.Distance(transform.position, destination) > 0.1f)
        {
            if (IsObstacleAhead())
            {
                currentState = State.WaitingForObstacle;
                Debug.Log("Obstacle detected! Waiting...");
                yield break; 
            }

            Vector3 direction = (destination - transform.position).normalized;
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(direction);
            yield return null;
        }
    }

    bool IsObstacleAhead()
    {
        return Physics.Raycast(transform.position, transform.forward, obstacleCheckDistance, obstacleLayer);
    }

    int GetNextDeliveryPoint()
    {
        if (deliveryIndices.Count > 0)
        {
            return deliveryIndices[0];
        }
        return -1; // No more points for this run
    }

    void OnDrawGizmos()
    {
        // Draw a line to the current target
        if (waypoints.Length > 0)
        {
            Vector3 targetPosition;
            if (currentState == State.MovingToPickup)
            {
                targetPosition = waypoints[0].position;
            }
            else if (currentState == State.DeliveringCargo && currentWaypointIndex != -1)
            {
                targetPosition = waypoints[currentWaypointIndex].position;
            }
            else
            {
                return;
            }
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
        }

        // Draw raycast for obstacle detection
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * obstacleCheckDistance);
    }

    // Public properties for UI
    public string GetCurrentState() => currentState.ToString();
    public int GetTasksCompleted() => tasksCompleted;
}