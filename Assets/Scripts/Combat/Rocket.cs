using UnityEngine;
using TowerDefense.Enemies;

namespace TowerDefense.Combat
{
    /// <summary>
    /// Tên lửa Kingdom Rush (Cấp 1 -> Cấp 2 -> Cấp 3):
    /// Bay theo mục tiêu với đầu đạn hướng chuẩn theo quỹ đạo bay và gây sát thương nổ diện rộng (AoE) kèm Xuyên Giáp.
    /// Tuân thủ 100% Zero-GC trong runtime (dùng OverlapCircleNonAlloc & Cache SpriteRenderer).
    /// </summary>
    public class Rocket : MonoBehaviour
    {
        [Header("Rocket Settings")]
        [SerializeField] private float speed = 8.5f;
        [SerializeField] private float damage = 47f;
        [SerializeField] private float splashRadius = 1.6f;
        [SerializeField] private float armorPenetration = 0.55f;
        [SerializeField] private float lifeTime = 6f;

        [Header("Explosion FX")]
        [SerializeField] private GameObject explosionPrefab;

        private Transform target;
        private bool hasExploded = false;
        private SpriteRenderer _sr;
        private float _spriteAngleOffset = -28f;
        private Vector3 _lastMoveDir = Vector3.right;

        // Bộ đệm tĩnh Zero-GC phục vụ tính sát thương nổ lan (Splash AoE)
        private static readonly Collider2D[] _explosionBuffer = new Collider2D[48];

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void Seek(Transform targetTransform)
        {
            target = targetTransform;
            if (target != null)
            {
                Vector3 dir = target.position - transform.position;
                dir.z = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    _lastMoveDir = dir.normalized;
                    float angle = Mathf.Atan2(_lastMoveDir.y, _lastMoveDir.x) * Mathf.Rad2Deg + _spriteAngleOffset;
                    transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
            }
        }

        public void SetDamage(float newDamage, float newSplash = 1.6f, float newArmorPen = 0.55f)
        {
            damage = newDamage;
            splashRadius = newSplash;
            armorPenetration = newArmorPen;
        }

        public void SetProjectileVisual(
            Sprite projSprite,
            Color tintColor,
            float scaleMultiplier = 1f,
            float customSpeed = -1f,
            float spriteAngleOffset = -28f)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)
            {
                if (projSprite != null) _sr.sprite = projSprite;
                _sr.color = tintColor;
            }
            transform.localScale = Vector3.one * scaleMultiplier;
            if (customSpeed > 0f) speed = customSpeed;
            _spriteAngleOffset = spriteAngleOffset;
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
                // Mục tiêu bị tiêu diệt trước khi tên lửa đến – bay thẳng tiếp theo hướng cuối cùng
                transform.position += _lastMoveDir * (speed * Time.deltaTime);
                return;
            }

            Vector3 dir = target.position - transform.position;
            dir.z = 0f;
            float distanceThisFrame = speed * Time.deltaTime;

            if (dir.magnitude <= distanceThisFrame)
            {
                Explode();
                return;
            }

            _lastMoveDir = dir.normalized;

            // Xoay đầu tên lửa hướng chuẩn vào mục tiêu (bù góc vẽ chéo 28°-32° của Sprite tên lửa Kingdom Rush)
            float angle = Mathf.Atan2(_lastMoveDir.y, _lastMoveDir.x) * Mathf.Rad2Deg + _spriteAngleOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            transform.position += _lastMoveDir * distanceThisFrame;
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            // Gây sát thương tất cả quái trong bán kính splash bằng OverlapCircle (Zero-GC chuẩn Unity 6)
            int hitCount = Physics2D.OverlapCircle(transform.position, splashRadius, ContactFilter2D.noFilter, _explosionBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D col = _explosionBuffer[i];
                if (col != null && col.CompareTag("Enemy"))
                {
                    Enemy enemy = col.GetComponent<Enemy>();
                    if (enemy != null && !enemy.IsDead)
                    {
                        // Sát thương giảm dần theo khoảng cách (từ tâm ra ngoài, tối thiểu 65%)
                        float dist = Vector2.Distance(transform.position, col.transform.position);
                        float dmgFactor = 1f - Mathf.Clamp01(dist / splashRadius) * 0.35f;
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
