using UnityEngine;
using TowerDefense.Waypoints;

namespace TowerDefense.Enemies
{
    public class EnemyMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float reachThreshold = 0.1f;

        [Header("Orientation")]
        [Tooltip("0 cho toàn bộ xe tăng & máy bay trong bộ Redesigned_Assets (mũi hướng sang phải +X)")]
        [SerializeField] private float spriteAngleOffset = 0f;

        private WaypointPath path;
        private int currentWaypointIndex = 0;
        private Transform targetWaypoint;

        public float MoveSpeed => moveSpeed;
        public float AngleOffset => spriteAngleOffset;

        public void InitPath(WaypointPath waypointPath)
        {
            path = waypointPath;
            currentWaypointIndex = 0;
            if (path != null && path.Count > 0)
            {
                targetWaypoint = path.GetWaypoint(0);
                transform.position = targetWaypoint.position;
                currentWaypointIndex = 1;
                if (path.Count > 1)
                {
                    targetWaypoint = path.GetWaypoint(1);
                }
            }
        }

        public void SetSpeed(float speed)
        {
            moveSpeed = speed;
        }

        public void SetAngleOffset(float offset)
        {
            spriteAngleOffset = offset;
        }

        private void Update()
        {
            if (targetWaypoint == null) return;

            Vector3 direction = targetWaypoint.position - transform.position;
            direction.z = 0f;

            // Di chuyển thẳng về waypoint mục tiêu
            transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, moveSpeed * Time.deltaTime);

            // Xoay đầu xe/máy bay chuẩn xác theo hướng di chuyển
            if (direction.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
                Quaternion targetRot = Quaternion.Euler(0, 0, targetAngle);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // Kiểm tra đã đến waypoint chưa
            if (Vector3.Distance(transform.position, targetWaypoint.position) <= reachThreshold)
            {
                currentWaypointIndex++;
                if (path != null && currentWaypointIndex < path.Count)
                {
                    targetWaypoint = path.GetWaypoint(currentWaypointIndex);
                }
                else
                {
                    OnReachDestination();
                }
            }
        }

        private void OnReachDestination()
        {
            Enemy enemy = GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.ReachDestination();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
