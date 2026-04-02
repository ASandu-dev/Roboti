using UnityEngine;

public class WaypointSetup : MonoBehaviour
{
    void Start()
    {
        var waypoints = FindObjectsOfType<Waypoint>();
        int index = 0;
        foreach (var wp in waypoints)
        {
            wp.nodeIndex = index++;
            Debug.Log("Assigned index " + (index-1) + " to " + wp.name);
        }
        Debug.Log("Total waypoints: " + waypoints.Length);
        Destroy(this);
    }
}