using UnityEngine;

namespace TowerDefense.Waypoints
{
    public class Waypoint : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = Color.cyan;
        [SerializeField] private float gizmoRadius = 0.3f;

        public Vector3 Position => transform.position;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        }
    }
}
