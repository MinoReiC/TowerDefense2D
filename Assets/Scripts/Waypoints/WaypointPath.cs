using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.Waypoints
{
    /// <summary>
    /// Đường đi của quái. Ngoài danh sách waypoint gốc (các góc), lớp này còn sinh ra
    /// một đường cong mịn (SmoothPoints) KHỚP CHÍNH XÁC với con đường được vẽ trên ảnh nền:
    /// - Màn 1 (SampleScene): đường Cardinal Spline (GDI+ AddCurve, tension 0.42).
    /// - Màn 2 / Màn 3: đường thẳng bo góc Bezier (BuildRoundedRoadPath, bán kính 78px / 75px).
    /// Các thông số được lấy đúng như script đã render ảnh nền nên quái chạy giữa lòng đường.
    /// </summary>
    public class WaypointPath : MonoBehaviour
    {
        public enum PathShape { Auto, Straight, CardinalSpline, RoundedCorners }

        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField] private Color pathColor = Color.green;

        [Header("Đường cong khớp ảnh nền")]
        [SerializeField] private PathShape shape = PathShape.Auto;
        [SerializeField] private int samplesPerCurve = 20;

        private List<Vector3> _smooth;

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
            _smooth = null;
        }

        public void SetWaypoints(List<Transform> newPoints)
        {
            waypoints = newPoints;
            _smooth = null;
        }

        /// <summary>Danh sách điểm dày đặc dọc theo lòng đường (dùng cho EnemyMovement).</summary>
        public List<Vector3> GetSmoothPoints()
        {
            if (_smooth == null || _smooth.Count == 0) _smooth = BuildSmoothPoints();
            return _smooth;
        }

        // ─── Cấu hình theo từng màn (khớp script render ảnh nền) ───────────────
        private void ResolveSettings(out PathShape s, out float tension, out float radiusPx, out Vector2 ppu)
        {
            string scene = gameObject.scene.IsValid() ? gameObject.scene.name : SceneManager.GetActiveScene().name;
            s = shape;
            tension = 0.42f;
            radiusPx = 78f;
            ppu = new Vector2(64f, 76.8f);

            if (scene.Contains("Level2"))
            {
                if (s == PathShape.Auto) s = PathShape.RoundedCorners;
                radiusPx = 78f;
                ppu = new Vector2(1280f / 27.2f, 768f / 13.6f);
            }
            else if (scene.Contains("Level3"))
            {
                if (s == PathShape.Auto) s = PathShape.RoundedCorners;
                radiusPx = 75f;
                ppu = new Vector2(1280f / 28.8f, 768f / 14.4f);
            }
            else
            {
                if (s == PathShape.Auto) s = PathShape.CardinalSpline;
            }
        }

        private List<Vector3> BuildSmoothPoints()
        {
            var result = new List<Vector3>();
            var pts = new List<Vector3>();
            foreach (var t in waypoints) if (t != null) pts.Add(t.position);
            if (pts.Count < 2) { result.AddRange(pts); return result; }

            ResolveSettings(out PathShape s, out float tension, out float radiusPx, out Vector2 ppu);
            int n = Mathf.Max(4, samplesPerCurve);

            if (s == PathShape.Straight)
            {
                result.AddRange(pts);
            }
            else if (s == PathShape.CardinalSpline)
            {
                // Điểm ảo ở 2 đầu (ảnh nền vẽ đường kéo dài 1.5 đơn vị ra mép bản đồ)
                Vector3 vStart = pts[0] - (pts[1] - pts[0]).normalized * 1.5f;
                Vector3 vEnd   = pts[pts.Count - 1] + (pts[pts.Count - 1] - pts[pts.Count - 2]).normalized * 1.5f;
                var all = new List<Vector3> { vStart };
                all.AddRange(pts);
                all.Add(vEnd);

                float k = tension / 3f;
                result.Add(pts[0]);
                for (int i = 1; i < all.Count - 2; i++)
                {
                    Vector3 p0 = all[i], p1 = all[i + 1];
                    Vector3 c1 = p0 + k * (all[i + 1] - all[i - 1]);
                    Vector3 c2 = p1 - k * (all[i + 2] - all[i]);
                    for (int j = 1; j <= n; j++)
                        result.Add(Bezier(p0, c1, c2, p1, j / (float)n));
                }
            }
            else // RoundedCorners – tính trong hệ pixel như ảnh nền rồi đổi lại toạ độ thế giới
            {
                Vector2 ToPx(Vector3 w) => new Vector2(w.x * ppu.x, w.y * ppu.y);
                Vector3 ToW(Vector2 p) => new Vector3(p.x / ppu.x, p.y / ppu.y, 0f);

                result.Add(pts[0]);
                for (int i = 1; i < pts.Count - 1; i++)
                {
                    Vector2 prev = ToPx(pts[i - 1]), curr = ToPx(pts[i]), next = ToPx(pts[i + 1]);
                    Vector2 d1 = prev - curr, d2 = next - curr;
                    float l1 = d1.magnitude, l2 = d2.magnitude;
                    if (l1 < 0.001f || l2 < 0.001f) continue;
                    float r = Mathf.Min(radiusPx, Mathf.Min(l1 * 0.45f, l2 * 0.45f));
                    Vector2 a = curr + d1 / l1 * r;
                    Vector2 b = curr + d2 / l2 * r;
                    Vector2 c1 = a + (curr - a) * 0.65f;
                    Vector2 c2 = b + (curr - b) * 0.65f;

                    result.Add(ToW(a));
                    for (int j = 1; j <= n; j++)
                        result.Add(ToW(Bezier(a, c1, c2, b, j / (float)n)));
                }
                result.Add(pts[pts.Count - 1]);
            }

            for (int i = 0; i < result.Count; i++) { var p = result[i]; p.z = 0f; result[i] = p; }
            return result;
        }

        private static Vector3 Bezier(Vector3 p0, Vector3 c1, Vector3 c2, Vector3 p1, float t)
        {
            float u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * p1;
        }

        private static Vector2 Bezier(Vector2 p0, Vector2 c1, Vector2 c2, Vector2 p1, float t)
        {
            float u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * p1;
        }

        private void OnValidate() { _smooth = null; }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count < 2) return;

            // Trong Editor luôn tính lại để thấy ngay khi kéo waypoint
            List<Vector3> pts = Application.isPlaying ? GetSmoothPoints() : BuildSmoothPoints();
            Gizmos.color = pathColor;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Gizmos.DrawLine(pts[i], pts[i + 1]);
            }
        }
    }
}
