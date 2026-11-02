using UnityEngine;

namespace TowerDefense.Towers
{
    // Script này đã được thay thế hoàn toàn bằng hệ thống 5 TowerBuildSpot chuyên dụng.
    // Giữ lại class rỗng để tránh lỗi missing script trên các GameObject cũ.
    public class TowerPlacement : MonoBehaviour
    {
        private void Awake()
        {
            // Tự hủy component cũ nếu còn tồn tại trong Scene
            Destroy(this);
        }
    }
}
