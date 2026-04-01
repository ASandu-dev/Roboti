using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WaypointGraph : MonoBehaviour
{
    public static WaypointGraph Instance { get; private set; }
    
    public List<Waypoint> waypoints = new List<Waypoint>();
    public Waypoint startPoint;
    public List<Waypoint> goalPoints = new List<Waypoint>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        DiscoverWaypoints();
        AutoConnectWaypoints();
    }

    void DiscoverWaypoints()
    {
        var found = FindObjectsOfType<Waypoint>();
        waypoints = found.OrderBy(w => w.nodeIndex).ToList();
    }

    void AutoConnectWaypoints()
    {
        if (waypoints.Count < 2) return;

        for (int i = 0; i < waypoints.Count; i++)
        {
            var current = waypoints[i];
            
            int nextIndex = i + 1;
            if (nextIndex < waypoints.Count)
            {
                current.ConnectTo(waypoints[nextIndex]);
            }
            
            if (i < waypoints.Count - 1)
            {
                int alternateIndex = i + 2;
                if (alternateIndex < waypoints.Count)
                {
                    current.ConnectTo(waypoints[alternateIndex]);
                }
            }
            
            if (i == 0 && waypoints.Count > 3)
            {
                waypoints[0].ConnectTo(waypoints[3]);
            }
        }

        ConnectDisconnectedWaypoints();
    }

    void ConnectDisconnectedWaypoints()
    {
        foreach (var wp in waypoints)
        {
            if (wp.connections.Count == 0)
            {
                var nearest = waypoints
                    .Where(w => w != wp)
                    .OrderBy(w => Vector3.Distance(wp.transform.position, w.transform.position))
                    .FirstOrDefault();
                
                if (nearest != null)
                {
                    wp.ConnectTo(nearest);
                }
            }
        }
    }

    public Waypoint GetNearestWaypoint(Vector3 position)
    {
        return waypoints
            .OrderBy(w => Vector3.Distance(position, w.transform.position))
            .FirstOrDefault();
    }

    public Waypoint GetStartPoint()
    {
        return startPoint != null ? startPoint : (waypoints.Count > 0 ? waypoints[0] : null);
    }

    public List<Waypoint> GetGoalPoints()
    {
        if (goalPoints.Count > 0)
            return goalPoints.Take(5).ToList();
        
        if (waypoints.Count > 1)
            return waypoints.Skip(1).Take(5).ToList();
        
        return new List<Waypoint>();
    }

    public void ResetAllPathfindingData()
    {
        foreach (var wp in waypoints)
        {
            wp.ResetPathfindingData();
        }
    }
}
