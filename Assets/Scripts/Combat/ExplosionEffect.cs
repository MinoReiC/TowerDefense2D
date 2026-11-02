using UnityEngine;

namespace TowerDefense.Combat
{
    public class ExplosionEffect : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float duration = 0.35f;
        [SerializeField] private float startScale = 0.4f;
        [SerializeField] private float endScale = 1.3f;

        private SpriteRenderer spriteRenderer;
        private float timer = 0f;
        private Color initialColor;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                initialColor = spriteRenderer.color;
            }
            transform.localScale = Vector3.one * startScale;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            // Hiệu ứng nổ bung to
            float currentScale = Mathf.Lerp(startScale, endScale, progress);
            transform.localScale = Vector3.one * currentScale;

            // Mờ dần theo thời gian
            if (spriteRenderer != null)
            {
                Color c = initialColor;
                c.a = Mathf.Lerp(1f, 0f, progress);
                spriteRenderer.color = c;
            }

            if (timer >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
