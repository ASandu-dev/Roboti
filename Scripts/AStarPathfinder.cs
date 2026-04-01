using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AStarPathfinder
{
    public static List<Waypoint> FindPath(Waypoint start, Waypoint goal)
    {
        WaypointGraph.Instance.ResetAllPathfindingData();
        
        List<Waypoint> openSet = new List<Waypoint>();
        HashSet<Waypoint> closedSet = new HashSet<Waypoint>();

        openSet.Add(start);
        start.visited = true;

        while (openSet.Count > 0)
        {
            Waypoint current = openSet.OrderBy(w => w.fCost).ThenBy(w => w.hCost).First();

            if (current == goal)
            {
                return ReconstructPath(current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Waypoint neighbor in current.connections)
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float newGCost = current.gCost + Vector3.Distance(current.transform.position, neighbor.transform.position);

                if (newGCost < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newGCost;
                    neighbor.hCost = Vector3.Distance(neighbor.transform.position, goal.transform.position);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                        neighbor.visited = true;
                    }
                }
            }
        }

        Debug.LogWarning("No path found from " + start.nodeIndex + " to " + goal.nodeIndex);
        return new List<Waypoint>();
    }

    static List<Waypoint> ReconstructPath(Waypoint current)
    {
        List<Waypoint> path = new List<Waypoint>();
        while (current != null)
        {
            path.Add(current);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    public static float GetPathDistance(List<Waypoint> path)
    {
        if (path.Count < 2) return 0f;
        
        float distance = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            distance += Vector3.Distance(path[i].transform.position, path[i + 1].transform.position);
        }
        return distance;
    }
}
