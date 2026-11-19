using UnityEngine;
using TowerDefense.Managers;

namespace TowerDefense.Combat
{
    public class ExplosionEffect : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float duration = 0.36f;
        [SerializeField] private float startScale = 0.85f;
        [SerializeField] private float endScale = 1.2f;

        private SpriteRenderer spriteRenderer;
        private Sprite[] _frames;
        private float timer = 0f;
        private Color initialColor = Color.white;

        private void Awake()
        {
            duration = 0.36f;
            startScale = 0.85f;
            endScale = 1.2f;

            spriteRenderer = GetComponent<SpriteRenderer>();
            _frames = CyberAssetLoader.GetExplosionFrames();
            if (spriteRenderer != null)
            {
                if (_frames != null && _frames.Length > 0 && _frames[0] != null)
                {
                    spriteRenderer.sprite = _frames[0];
                    spriteRenderer.color = Color.white;
                }
                initialColor = spriteRenderer.color;
            }
            transform.localScale = Vector3.one * startScale;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            // Chuyển 4 frame hoạt ảnh nổ tung Kingdom Rush (Chớp sáng -> Quả cầu lửa -> Vòng khói -> Khói tan)
            if (spriteRenderer != null && _frames != null && _frames.Length > 0)
            {
                int idx = Mathf.Min(_frames.Length - 1, Mathf.FloorToInt(progress * _frames.Length));
                if (_frames[idx] != null && spriteRenderer.sprite != _frames[idx])
                {
                    spriteRenderer.sprite = _frames[idx];
                }
            }

            // Hiệu ứng nổ bung kích thước nhẹ mượt mà
            float currentScale = Mathf.Lerp(startScale, endScale, progress);
            transform.localScale = Vector3.one * currentScale;

            // Giữ sắc nét 3 frame đầu, chỉ mờ dần ở frame khói tàn cuối (> 72%)
            if (spriteRenderer != null)
            {
                Color c = initialColor;
                c.a = (progress > 0.72f) ? Mathf.Lerp(1f, 0f, (progress - 0.72f) / 0.28f) : 1f;
                spriteRenderer.color = c;
            }

            if (timer >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
