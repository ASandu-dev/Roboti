using UnityEngine;
using System.Linq;

public class WaypointManager : MonoBehaviour
{
    public bool autoAssignIndices = true;
    public bool autoConnect = true;

    void Awake()
    {
        var waypoints = FindObjectsOfType<Waypoint>().OrderBy(w => w.transform.position.x).ThenBy(w => w.transform.position.z).ToArray();
        
        for (int i = 0; i < waypoints.Length; i++)
        {
            waypoints[i].nodeIndex = i;
        }
        
        if (autoConnect)
        {
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                waypoints[i].ConnectTo(waypoints[i + 1]);
            }
            if (waypoints.Length > 2)
            {
                waypoints[0].ConnectTo(waypoints[2]);
            }
        }
        
        Debug.Log("WaypointManager: " + waypoints.Length + " waypoints configured");
    }
}