using UnityEngine;
using TowerDefense.Enemies;

namespace TowerDefense.Combat
{
    public class Bullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private float speed = 11f;
        [SerializeField] private float damage = 20f;
        [SerializeField] private float armorPenetration = 0.15f;
        [SerializeField] private float lifeTime = 5f;

        private Transform target;
        private bool hasHit = false;

        public void Seek(Transform targetTransform)
        {
            target = targetTransform;
        }

        public void SetDamage(float newDamage, float newArmorPen = 0.15f, bool isCrit = false)
        {
            damage = newDamage;
            armorPenetration = newArmorPen;
            if (isCrit)
            {
                transform.localScale *= 1.35f;
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(1f, 0.90f, 0.25f, 1f);
            }
        }

        public void SetProjectileVisual(Sprite projSprite, Color tintColor, float scaleMultiplier = 1f, float customSpeed = -1f)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (projSprite != null) sr.sprite = projSprite;
                sr.color = tintColor;
            }
            transform.localScale *= scaleMultiplier;
            if (customSpeed > 0f) speed = customSpeed;
        }

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            if (target == null)
            {
                // Khi mục tiêu bị tiêu diệt trước, đạn bay thẳng tiếp theo hướng hiện tại
                transform.Translate(Vector3.up * (speed * Time.deltaTime), Space.Self);
                return;
            }

            Vector3 dir = target.position - transform.position;
            dir.z = 0;
            float distanceThisFrame = speed * Time.deltaTime;

            if (dir.magnitude <= distanceThisFrame)
            {
                HitTarget();
                return;
            }

            // Xoay viên đạn hướng vào mục tiêu
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            transform.position += dir.normalized * distanceThisFrame;
        }

        private void HitTarget()
        {
            if (hasHit) return;
            hasHit = true;

            if (target != null)
            {
                Enemy enemy = target.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage, armorPenetration);
                }
            }
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (hasHit) return;
            if (collision.CompareTag("Enemy"))
            {
                hasHit = true;
                Enemy enemy = collision.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage, armorPenetration);
                }
                Destroy(gameObject);
            }
        }
    }
}
