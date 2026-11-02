using UnityEngine;
using TowerDefense.Enemies;

namespace TowerDefense.UI
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 0.65f, 0);
        [SerializeField] private Vector2 barSize = new Vector2(0.85f, 0.12f);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        [SerializeField] private Color healthColor = new Color(0.2f, 0.9f, 0.2f, 1f);

        private Transform barContainer;
        private Transform fillTransform;
        private SpriteRenderer fillRenderer;
        private Enemy enemy;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            CreateBarVisuals();
        }

        private void OnEnable()
        {
            if (enemy != null)
            {
                enemy.OnHealthChanged += UpdateHealthBar;
            }
        }

        private void OnDisable()
        {
            if (enemy != null)
            {
                enemy.OnHealthChanged -= UpdateHealthBar;
            }
        }

        public void SetBarConfiguration(Vector2 size, Vector3 newOffset, Color? customHealthColor = null)
        {
            barSize = size;
            offset = newOffset;
            if (customHealthColor.HasValue)
            {
                healthColor = customHealthColor.Value;
            }
        }

        private void CreateBarVisuals()
        {
            GameObject container = new GameObject("HealthBar");
            barContainer = container.transform;
            barContainer.SetParent(transform);
            barContainer.localPosition = offset;

            // Tạo Sprite 1x1 trắng động
            Texture2D tex = Texture2D.whiteTexture;
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            // Khung nền đen
            GameObject bgGO = new GameObject("BG");
            bgGO.transform.SetParent(barContainer, false);
            SpriteRenderer bgSr = bgGO.AddComponent<SpriteRenderer>();
            bgSr.sprite = sprite;
            bgSr.color = backgroundColor;
            bgSr.sortingOrder = 15;
            bgGO.transform.localScale = new Vector3(barSize.x + 0.04f, barSize.y + 0.04f, 1f);

            // Thanh máu màu (nằm đè lên nền)
            GameObject fillGO = new GameObject("Fill");
            fillTransform = fillGO.transform;
            fillTransform.SetParent(barContainer, false);
            fillRenderer = fillGO.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = sprite;
            fillRenderer.color = healthColor;
            fillRenderer.sortingOrder = 16;
            fillTransform.localScale = new Vector3(barSize.x, barSize.y, 1f);
        }

        private void LateUpdate()
        {
            // Giữ cho thanh máu luôn nằm ngang độc lập khi xe tăng xoay hướng
            if (barContainer != null)
            {
                barContainer.rotation = Quaternion.identity;
                barContainer.position = transform.position + offset;
            }
        }

        public void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (fillTransform == null || maxHealth <= 0f) return;

            float pct = Mathf.Clamp01(currentHealth / maxHealth);
            fillTransform.localScale = new Vector3(barSize.x * pct, barSize.y, 1f);

            // Canh lề trái thanh máu
            float xOffset = (pct - 1f) * barSize.x * 0.5f;
            fillTransform.localPosition = new Vector3(xOffset, 0, 0);

            // Đổi màu: Xanh lá -> Vàng -> Đỏ khi máu giảm dần
            if (fillRenderer != null)
            {
                if (pct > 0.5f)
                    fillRenderer.color = Color.Lerp(Color.yellow, healthColor, (pct - 0.5f) * 2f);
                else
                    fillRenderer.color = Color.Lerp(Color.red, Color.yellow, pct * 2f);
            }
        }
    }
}
