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
        private System.Collections.Generic.List<Vector3> points;   // Đường cong mịn khớp lòng đường trên ảnh nền
        private int currentWaypointIndex = 0;
        private bool hasTarget = false;

        public float MoveSpeed => moveSpeed;
        public float AngleOffset => spriteAngleOffset;

        public void InitPath(WaypointPath waypointPath)
        {
            path = waypointPath;
            currentWaypointIndex = 0;
            hasTarget = false;
            points = path != null ? path.GetSmoothPoints() : null;
            if (points != null && points.Count > 0)
            {
                transform.position = points[0];
                currentWaypointIndex = 1;
                hasTarget = points.Count > 1;
                if (hasTarget)
                {
                    Vector3 d = points[1] - points[0];
                    float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + spriteAngleOffset;
                    transform.rotation = Quaternion.Euler(0, 0, a);
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
            if (!hasTarget || points == null) return;

            // Di chuyển dọc theo đường cong, quãng đường dư được chuyển tiếp sang điểm kế tiếp
            // để tốc độ đều và không bị khựng ở các điểm mẫu.
            float remaining = moveSpeed * Time.deltaTime;
            Vector3 pos = transform.position;
            Vector3 direction = Vector3.zero;

            while (remaining > 0f && currentWaypointIndex < points.Count)
            {
                Vector3 target = points[currentWaypointIndex];
                Vector3 delta = target - pos;
                delta.z = 0f;
                float dist = delta.magnitude;
                if (dist > 0.0001f) direction = delta;

                if (dist <= remaining)
                {
                    pos = target;
                    remaining -= dist;
                    currentWaypointIndex++;
                }
                else
                {
                    pos += delta / dist * remaining;
                    remaining = 0f;
                }
            }
            transform.position = pos;

            // Xoay đầu xe/máy bay mượt theo hướng tiếp tuyến của đường cong
            if (direction.sqrMagnitude > 0.000001f)
            {
                float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
                Quaternion targetRot = Quaternion.Euler(0, 0, targetAngle);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // Đã đi hết đường → vào căn cứ
            if (currentWaypointIndex >= points.Count)
            {
                hasTarget = false;
                OnReachDestination();
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
