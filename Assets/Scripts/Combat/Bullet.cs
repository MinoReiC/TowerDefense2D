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

        [Header("Animated Frame Settings")]
        [SerializeField] private Sprite[] animationFrames;
        [SerializeField] private float frameRate = 18f; // 18 frames/giây cho chuyển động xoay tia cực kỳ mượt mà
        [SerializeField] private bool rotateContinuously = false;
        [SerializeField] private float spinSpeed = 360f; // Góc xoay gia tốc

        private Transform target;
        private bool hasHit = false;
        private SpriteRenderer spriteRenderer;
        private float frameTimer = 0f;
        private int currentFrameIndex = 0;
        private float _spriteAngleOffset = -90f;
        private Vector3 _lastMoveDir = Vector3.up;

        public void Seek(Transform targetTransform)
        {
            target = targetTransform;
        }

        public void SetDamage(float newDamage, float newArmorPen = 0.15f)
        {
            damage = newDamage;
            armorPenetration = newArmorPen;
        }

        public void SetProjectileVisual(
            Sprite projSprite,
            Color tintColor,
            float scaleMultiplier = 1f,
            float customSpeed = -1f,
            float spriteAngleOffset = -90f,
            bool spinContinuously = false,
            float spinRate = 360f)
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (projSprite != null) spriteRenderer.sprite = projSprite;
                spriteRenderer.color = tintColor;
            }
            transform.localScale *= scaleMultiplier;
            if (customSpeed > 0f) speed = customSpeed;
            _spriteAngleOffset = spriteAngleOffset;
            rotateContinuously = spinContinuously;
            spinSpeed = spinRate;
        }

        /// <summary>
        /// Kích hoạt chuỗi animation 4 frame cho đạn (Ví dụ Đạn Laze Mặt Trời Lượng Tử Cấp 3 xoay lưỡi từ tính & hồ quang).
        /// </summary>
        public void SetAnimationFrames(Sprite[] frames, float fps = 18f, bool spin = false, float spinRate = 360f)
        {
            animationFrames = frames;
            frameRate = fps;
            rotateContinuously = spin;
            spinSpeed = spinRate;
            frameTimer = 0f;
            currentFrameIndex = 0;

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && frames != null && frames.Length > 0 && frames[0] != null)
            {
                spriteRenderer.sprite = frames[0];
            }
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            UpdateAnimation();

            if (target == null)
            {
                // Khi mục tiêu bị tiêu diệt trước, đạn bay thẳng tiếp theo hướng di chuyển cuối cùng (Zero-GC)
                transform.position += _lastMoveDir * (speed * Time.deltaTime);
                return;
            }

            Vector3 dir = target.position - transform.position;
            dir.z = 0f;
            float distanceThisFrame = speed * Time.deltaTime;
            float dist = dir.magnitude;

            if (dist <= distanceThisFrame)
            {
                HitTarget();
                return;
            }

            if (dist > 0.0001f)
            {
                _lastMoveDir = dir / dist;
            }

            // Xoay viên đạn hướng vào mục tiêu (nếu không bật chế độ xoay tròn tự thân như Bom Cấp 3)
            if (!rotateContinuously)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + _spriteAngleOffset;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            transform.position += _lastMoveDir * distanceThisFrame;
        }

        private void UpdateAnimation()
        {
            // 1. Chạy chuỗi frame theo frameRate (Ví dụ 4 frame Laze Lượng Tử Cấp 3: Rời nòng -> Gia tốc -> Cực đại -> Nổ)
            if (animationFrames != null && animationFrames.Length > 0 && frameRate > 0f)
            {
                frameTimer += Time.deltaTime;
                float interval = 1f / frameRate;
                if (frameTimer >= interval)
                {
                    frameTimer -= interval;
                    currentFrameIndex = (currentFrameIndex + 1) % animationFrames.Length;
                    if (spriteRenderer != null && animationFrames[currentFrameIndex] != null)
                    {
                        spriteRenderer.sprite = animationFrames[currentFrameIndex];
                    }
                }
            }

            // 2. Xoay tự thân liên tục nếu bật chế độ xoay lưỡi từ tính
            if (rotateContinuously)
            {
                transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime, Space.Self);
            }
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
