using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Managers
{
    /// <summary>
    /// Bộ tải và lưu đệm (Cache) toàn bộ Sprite trong bộ Redesigned_Assets (Tháp Cấp 1-3, Tháp Tím Laser,
    /// Đế trụ Cyber, Đạn, Tên Lửa, Xe Tăng + Nòng Xe Tăng, Máy Bay).
    /// Sử dụng full khung 128x128 với tâm xoay (0.5, 0.5) giúp ụ súng khớp hoàn hảo 100% vào tâm đế trụ!
    /// </summary>
    public static class CyberAssetLoader
    {
        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        public static Sprite GetSprite(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return null;

            // Bỏ đuôi .png nếu có
            if (assetName.EndsWith(".png"))
                assetName = assetName.Substring(0, assetName.Length - 4);

            if (_spriteCache.TryGetValue(assetName, out Sprite cached) && cached != null)
            {
                return cached;
            }

            // 1. Tải Texture2D từ Resources/Redesigned/ để tạo Sprite chuẩn tâm (64, 64) trên khung 128x128
            Texture2D tex = Resources.Load<Texture2D>("Redesigned/" + assetName);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Bilinear;
                Sprite created = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                created.name = assetName;
                _spriteCache[assetName] = created;
                return created;
            }

            // 2. Fallback: Tải Sprite trực tiếp nếu có
            Sprite resSprite = Resources.Load<Sprite>("Redesigned/" + assetName);
            if (resSprite != null)
            {
                _spriteCache[assetName] = resSprite;
                return resSprite;
            }

            return null;
        }

        private static Sprite _emptySpotRingSprite;
        private static Sprite _emptySpotCenterSprite;

        /// <summary>
        /// Tạo Sprite vòng tròn nét đứt phát sáng nhẹ nhàng cho Ô Đặt Pháo khi CHƯA MUA TRỤ
        /// (Để các Base Trụ tile180..183 chỉ xuất hiện SAU KHI người chơi mua pháo!).
        /// </summary>
        public static Sprite GetEmptyBuildSpotRingSprite()
        {
            if (_emptySpotRingSprite != null) return _emptySpotRingSprite;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float dist = p.magnitude;
                    float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                    if (angle < 0f) angle += 360f;

                    Color col = Color.clear;

                    // Nền tròn mờ nhẹ bên trong
                    if (dist < 42f)
                    {
                        col = new Color(0.85f, 1f, 0.95f, 0.16f * (1f - dist / 46f));
                    }

                    // Vòng tròn viền nét đứt 8 đoạn (hologram vị trí đặt pháo)
                    bool inDash = (angle % 45f) > 8f && (angle % 45f) < 37f;
                    if (inDash && dist >= 40f && dist <= 46f)
                    {
                        float edge = 1f - Mathf.Abs(dist - 43f) / 3.5f;
                        col = new Color(0.90f, 1f, 0.95f, Mathf.Clamp01(edge) * 0.88f);
                    }

                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _emptySpotRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _emptySpotRingSprite.name = "EmptyBuildSpotRing";
            return _emptySpotRingSprite;
        }

        /// <summary>
        /// Tạo biểu tượng dấu cộng (+) nhỏ ở tâm ô trống mời người chơi click đặt pháo.
        /// </summary>
        public static Sprite GetEmptyBuildSpotPlusSprite()
        {
            if (_emptySpotCenterSprite != null) return _emptySpotCenterSprite;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - half);
                    float dy = Mathf.Abs(y + 0.5f - half);
                    bool inHoriz = dy <= 3.5f && dx <= 13f;
                    bool inVert  = dx <= 3.5f && dy <= 13f;
                    pixels[y * size + x] = (inHoriz || inVert)
                        ? new Color(0.95f, 1f, 0.95f, 0.92f)
                        : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _emptySpotCenterSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _emptySpotCenterSprite.name = "EmptyBuildSpotPlus";
            return _emptySpotCenterSprite;
        }
    }
}
