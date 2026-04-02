using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CargoCubeController : MonoBehaviour
{
    public float speed = 5f;
    public float obstacleCheckDistance = 2f;
    public LayerMask obstacleLayer;

    private int nextWaypointIndex = 1;
    private State currentState = State.Idle;
    private List<int> visitedWaypointIndices = new List<int>();
    private int reverseIndex = 0;

    public float timer = 0f;
    public float totalDistance = 0f;
    public int tasksCompleted = 0;
    public int cargoCount = 0;
    private Vector3 lastPosition;

    public enum State
    {
        Idle,
        DeliveringCargo,
        ReturningToBase
    }

    void Start()
    {
        lastPosition = transform.position;
        
        if (WaypointGraph.Instance != null && WaypointGraph.Instance.waypoints.Count > 0)
        {
            transform.position = WaypointGraph.Instance.waypoints[0].transform.position;
            StartCoroutine(RunDeliveryCycle());
        }
        else
        {
            Debug.LogError("No waypoints found!");
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        totalDistance += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
    }

    IEnumerator RunDeliveryCycle()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            switch (currentState)
            {
                case State.Idle:
                    tasksCompleted = 0;
                    cargoCount = WaypointGraph.Instance.waypoints.Count - 1;
                    nextWaypointIndex = 1;
                    reverseIndex = 0;
                    visitedWaypointIndices.Clear();
                    
                    transform.position = WaypointGraph.Instance.waypoints[0].transform.position;
                    Debug.Log("=== Cycle start: going to waypoints 1-" + (WaypointGraph.Instance.waypoints.Count - 1));
                    currentState = State.DeliveringCargo;
                    break;

                case State.DeliveringCargo:
                    if (nextWaypointIndex >= WaypointGraph.Instance.waypoints.Count)
                    {
                        Debug.Log("=== All delivered, returning to base via same path");
                        currentState = State.ReturningToBase;
                        break;
                    }

                    Waypoint target = WaypointGraph.Instance.waypoints[nextWaypointIndex];
                    Debug.Log("=== Moving to waypoint " + target.nodeIndex);
                    
                    yield return StartCoroutine(MoveToWaypoint(target));
                    
                    tasksCompleted++;
                    cargoCount--;
                    visitedWaypointIndices.Add(target.nodeIndex);
                    Debug.Log("=== Reached waypoint " + target.nodeIndex);
                    nextWaypointIndex++;
                    
                    if (nextWaypointIndex >= WaypointGraph.Instance.waypoints.Count)
                    {
                        Debug.Log("=== All delivered, returning to base via same path");
                        currentState = State.ReturningToBase;
                    }
                    break;

                case State.ReturningToBase:
                    if (reverseIndex < visitedWaypointIndices.Count)
                    {
                        int waypointToVisit = visitedWaypointIndices[visitedWaypointIndices.Count - 1 - reverseIndex];
                        Waypoint reverseTarget = WaypointGraph.Instance.waypoints[waypointToVisit];
                        Debug.Log("=== Returning: visiting waypoint " + reverseTarget.nodeIndex);
                        
                        yield return StartCoroutine(MoveToWaypoint(reverseTarget));
                        Debug.Log("=== Visited waypoint " + reverseTarget.nodeIndex);
                        reverseIndex++;
                    }
                    else
                    {
                        Debug.Log("=== Returned to start. Cycle complete.");
                        yield return new WaitForSeconds(2f);
                        currentState = State.Idle;
                    }
                    break;
            }
            yield return null;
        }
    }

    IEnumerator MoveToWaypoint(Waypoint target)
    {
        while (target != null && Vector3.Distance(transform.position, target.transform.position) > 0.3f)
        {
            while (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, obstacleCheckDistance, obstacleLayer))
            {
                Debug.Log("Obstacle detected, waiting...");
                yield return new WaitForSeconds(0.5f);
            }

            Vector3 direction = (target.transform.position - transform.position).normalized;
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
            
            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);

            yield return null;
        }
    }

    public string GetCurrentState() => currentState.ToString();
    public int GetTasksCompleted() => tasksCompleted;
}