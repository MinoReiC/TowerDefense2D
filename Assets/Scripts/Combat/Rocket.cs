using UnityEngine;
using TowerDefense.Enemies;

namespace TowerDefense.Combat
{
    /// <summary>
    /// Tên lửa: chậm hơn đạn thường, gây sát thương diện rộng (AoE) khi trúng mục tiêu.
    /// </summary>
    public class Rocket : MonoBehaviour
    {
        [Header("Rocket Settings")]
        [SerializeField] private float speed = 5.5f;
        [SerializeField] private float damage = 47f;
        [SerializeField] private float splashRadius = 1.6f;
        [SerializeField] private float armorPenetration = 0.55f;
        [SerializeField] private float lifeTime = 6f;

        [Header("Explosion FX")]
        [SerializeField] private GameObject explosionPrefab;

        private Transform target;
        private bool hasExploded = false;

        public void Seek(Transform targetTransform)
        {
            target = targetTransform;
        }

        public void SetDamage(float newDamage, float newSplash = 1.6f, float newArmorPen = 0.55f)
        {
            damage = newDamage;
            splashRadius = newSplash;
            armorPenetration = newArmorPen;
        }

        public void SetProjectileVisual(Sprite projSprite, Color tintColor, float scaleMultiplier = 1f)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (projSprite != null) sr.sprite = projSprite;
                sr.color = tintColor;
            }
            transform.localScale *= scaleMultiplier;
        }

        public void SetExplosionPrefab(GameObject prefab)
        {
            explosionPrefab = prefab;
        }

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            if (target == null)
            {
                // Mục tiêu bị tiêu diệt trước khi tên lửa đến – bay thẳng tiếp
                transform.Translate(Vector3.up * (speed * Time.deltaTime), Space.Self);
                return;
            }

            Vector3 dir = target.position - transform.position;
            dir.z = 0;
            float distanceThisFrame = speed * Time.deltaTime;

            if (dir.magnitude <= distanceThisFrame)
            {
                Explode();
                return;
            }

            // Xoay tên lửa hướng vào mục tiêu
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            transform.position += dir.normalized * distanceThisFrame;
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            // Gây sát thương tất cả quái trong bán kính splash (kèm Xuyên Giáp Hạng Nặng kiểu Pháo Binh Kingdom Rush)
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, splashRadius);
            foreach (Collider2D col in cols)
            {
                if (col.CompareTag("Enemy"))
                {
                    Enemy enemy = col.GetComponent<Enemy>();
                    if (enemy != null && !enemy.IsDead)
                    {
                        // Sát thương giảm dần theo khoảng cách (từ tâm ra ngoài)
                        float dist = Vector2.Distance(transform.position, col.transform.position);
                        float dmgFactor = 1f - Mathf.Clamp01(dist / splashRadius) * 0.35f; // min 65%
                        enemy.TakeDamage(damage * dmgFactor, armorPenetration);
                    }
                }
            }

            // Hiệu ứng nổ lớn hơn đạn thường
            if (explosionPrefab != null)
            {
                GameObject fx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                fx.transform.localScale = Vector3.one * (splashRadius > 1.8f ? 2.2f : 1.6f);
            }

            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Enemy"))
            {
                target = collision.transform;
                Explode();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, splashRadius);
        }
    }
}
