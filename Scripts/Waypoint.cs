using UnityEngine;
using System.Collections.Generic;

public class Waypoint : MonoBehaviour
{
    public List<Waypoint> connections = new List<Waypoint>();
    public int nodeIndex;
    
    [HideInInspector]
    public float gCost;
    [HideInInspector]
    public float hCost;
    [HideInInspector]
    public Waypoint parent;
    [HideInInspector]
    public bool visited;

    public float fCost => gCost + hCost;

    void Start()
    {
        UpdateColor();
    }

    void UpdateColor()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            if (nodeIndex == 0)
                renderer.material.color = Color.green;
            else if (nodeIndex == WaypointGraph.Instance.waypoints.Count - 1)
                renderer.material.color = Color.red;
            else
                renderer.material.color = Color.blue;
        }
    }

    public void ResetPathfindingData()
    {
        gCost = 0;
        hCost = 0;
        parent = null;
        visited = false;
    }

    public void ConnectTo(Waypoint other)
    {
        if (!connections.Contains(other))
        {
            connections.Add(other);
        }
        if (!other.connections.Contains(this))
        {
            other.connections.Add(this);
        }
    }

    public void DisconnectFrom(Waypoint other)
    {
        connections.Remove(other);
        other.connections.Remove(this);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = nodeIndex == 0 ? Color.green : Color.blue;
        Gizmos.DrawSphere(transform.position, 0.5f);

        Gizmos.color = Color.yellow;
        foreach (var connection in connections)
        {
            if (connection != null)
            {
                Gizmos.DrawLine(transform.position, connection.transform.position);
            }
        }
    }
}
