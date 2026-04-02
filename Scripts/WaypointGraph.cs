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
        if (waypoints.Count == 0)
        {
            DiscoverWaypoints();
        }
        
        if (startPoint == null && waypoints.Count > 0)
        {
            startPoint = waypoints[0];
        }
        
        if (goalPoints.Count == 0 && waypoints.Count > 1)
        {
            for (int i = 1; i < waypoints.Count; i++)
            {
                if (goalPoints.Count < 5)
                    goalPoints.Add(waypoints[i]);
            }
        }
        
        if (waypoints.Count > 0)
        {
            AutoConnectWaypoints();
            Debug.Log("WaypointGraph initialized with " + waypoints.Count + " waypoints, start: " + (startPoint != null ? startPoint.nodeIndex.ToString() : "null") + ", goals: " + goalPoints.Count);
        }
    }

    void DiscoverWaypoints()
    {
        var found = FindObjectsByType<Waypoint>(FindObjectsSortMode.None);
        
        waypoints = found.OrderBy(w => w.transform.position.x).ThenBy(w => w.transform.position.z).ToList();
        
        for (int i = 0; i < waypoints.Count; i++)
        {
            waypoints[i].nodeIndex = i;
        }
        
        string wpList = "";
        foreach (var wp in waypoints)
        {
            wpList += wp.nodeIndex + "(" + wp.name + ") ";
        }
        Debug.Log("Discovered " + waypoints.Count + " waypoints: " + wpList);
    }

    void AutoConnectWaypoints()
    {
        if (waypoints.Count < 2) return;

        foreach (var wp in waypoints)
        {
            wp.connections.Clear();
        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            var current = waypoints[i];
            
            if (i < waypoints.Count - 1)
            {
                current.ConnectTo(waypoints[i + 1]);
            }
            
            if (i < waypoints.Count - 2)
            {
                current.ConnectTo(waypoints[i + 2]);
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
        if (startPoint != null) return startPoint;
        if (waypoints.Count > 0) return waypoints[0];
        return null;
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

    public int GetWaypointCount()
    {
        return waypoints.Count;
    }
}
