using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Waypoints
{
    public class WaypointPath : MonoBehaviour
    {
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField] private Color pathColor = Color.green;

        public int Count => waypoints.Count;

        public Transform GetWaypoint(int index)
        {
            if (index >= 0 && index < waypoints.Count)
            {
                return waypoints[index];
            }
            return null;
        }

        public void FetchChildWaypoints()
        {
            waypoints.Clear();
            foreach (Transform child in transform)
            {
                waypoints.Add(child);
            }
        }

        public void SetWaypoints(List<Transform> newPoints)
        {
            waypoints = newPoints;
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count < 2) return;

            Gizmos.color = pathColor;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                }
            }
        }
    }
}
