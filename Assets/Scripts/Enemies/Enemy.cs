using System;
using UnityEngine;
using TowerDefense.Managers;

namespace TowerDefense.Enemies
{
    public class Enemy : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string enemyName = "Xe Tăng Thường";

        [Header("Stats (Kingdom Rush Style)")]
        [SerializeField] private float maxHealth = 105f;
        [Tooltip("Tỉ lệ Giáp vật lý giảm sát thương (0 = 0%, 0.45 = 45% Giáp như Kingdom Rush)")]
        [Range(0f, 0.85f)]
        [SerializeField] private float armor = 0.15f;
        [SerializeField] private int goldReward = 12;
        [Tooltip("Số máu căn cứ bị trừ khi quái lọt vào đích cuối")]
        [SerializeField] private int baseDamage = 1;

        [Header("Effects")]
        [SerializeField] private GameObject explosionPrefab;

        private float currentHealth;
        public bool IsDead { get; private set; }
        public string EnemyName => enemyName;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float Armor => armor;
        public int GoldReward => goldReward;
        public int BaseDamage => baseDamage;

        public event Action<Enemy> OnDeath;
        public event Action<Enemy> OnEscaped;
        public event Action<float, float> OnHealthChanged;

        private void Awake()
        {
            ApplyKingdomRushArchetypeStats();
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Chuẩn hóa chỉ số Máu, Giáp Vật Lý (%) và Tiền Thưởng theo đúng chuẩn cân bằng Kingdom Rush
        /// cho từng chủng loại quái vật ngay khi khởi tạo.
        /// </summary>
        private void ApplyKingdomRushArchetypeStats()
        {
            string id = (gameObject.name + " " + enemyName).ToLowerInvariant();
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            EnemyMovement mov = GetComponent<EnemyMovement>();
            if (mov != null) mov.SetAngleOffset(0f); // Toàn bộ xe tăng & máy bay mới đều hướng mũi sang phải (+X)

            if (id.Contains("fast") || id.Contains("trinh sát") || id.Contains("nhỏ"))
            {
                // Trinh sát tốc độ cao (kiểu Worg/Scout): Máu thấp, Không giáp, ít vàng
                enemyName  = "Máy Bay Trinh Sát Nhỏ";
                maxHealth  = 65f;
                armor      = 0.00f; // 0% Giáp
                goldReward = 8;
                baseDamage = 1;
                if (sr != null)
                {
                    Sprite s = CyberAssetLoader.GetSprite("towerDefense_tile270");
                    if (s != null) sr.sprite = s;
                }
            }
            else if (id.Contains("boss") || id.Contains("siêu"))
            {
                // Siêu Boss Thiết Giáp (kiểu Juggernaut): Máu cực trâu, Giáp 35%, thưởng lớn
                enemyName  = "SIÊU BOSS THIẾT GIÁP";
                maxHealth  = 950f;
                armor      = 0.35f; // 35% Giáp
                goldReward = 75;
                baseDamage = 5;
                if (sr != null)
                {
                    Sprite s = CyberAssetLoader.GetSprite("towerDefense_tile269");
                    if (s != null) sr.sprite = s;
                    AttachTankTurretSprite("towerDefense_tile292", sr.color, sr.sortingOrder + 1);
                }
            }
            else if (id.Contains("heavy") || id.Contains("hạng nặng"))
            {
                // Xe Tăng Hạng Nặng (kiểu Dark Knight): Máu cao, Giáp Bọc Thép 45% (cần Tên Lửa / Laser Tím xuyên giáp)
                enemyName  = "Xe Tăng Hạng Nặng";
                maxHealth  = 320f;
                armor      = 0.45f; // 45% Giáp vật lý
                goldReward = 28;
                baseDamage = 3;
                if (sr != null)
                {
                    Sprite s = CyberAssetLoader.GetSprite("towerDefense_tile269");
                    if (s != null) sr.sprite = s;
                    AttachTankTurretSprite("towerDefense_tile292", Color.white, sr.sortingOrder + 1);
                }
            }
            else if (id.Contains("armored") || id.Contains("bọc thép") || id.Contains("lớn") || id.Contains("stealth"))
            {
                // Chiến Cơ Bọc Thép / Tàng Hình (kiểu Marauder/Gargoyle): Máu khá, Giáp 25%
                enemyName  = "Chiến Cơ Tàng Hình Bọc Thép";
                maxHealth  = 185f;
                armor      = 0.25f; // 25% Giáp
                goldReward = 18;
                baseDamage = 2;
                if (sr != null)
                {
                    // Xen kẽ Chiến Cơ Tàng Hình (plane_stealth_lvl2) và Phản Lực Bọc Thép (tile271)
                    string planeSprite = (UnityEngine.Random.value < 0.5f) ? "plane_stealth_lvl2" : "towerDefense_tile271";
                    Sprite s = CyberAssetLoader.GetSprite(planeSprite);
                    if (s != null) sr.sprite = s;
                }
            }
            else
            {
                // Xe Tăng Thường (kiểu Orc Warrior): Cân bằng, Giáp nhẹ 15%
                enemyName  = "Xe Tăng Thường";
                maxHealth  = 105f;
                armor      = 0.15f; // 15% Giáp
                goldReward = 12;
                baseDamage = 1;
                if (sr != null)
                {
                    Sprite s = CyberAssetLoader.GetSprite("towerDefense_tile268");
                    if (s != null) sr.sprite = s;
                    AttachTankTurretSprite("towerDefense_tile291", Color.white, sr.sortingOrder + 1);
                }
            }
        }

        private void AttachTankTurretSprite(string turretSpriteName, Color tint, int sortingOrder)
        {
            Sprite turretSprite = CyberAssetLoader.GetSprite(turretSpriteName);
            if (turretSprite == null) return;

            Transform existing = transform.Find("TankGun");
            GameObject gunGO = existing != null ? existing.gameObject : new GameObject("TankGun");
            if (existing == null)
            {
                gunGO.transform.SetParent(transform, false);
                gunGO.transform.localPosition = Vector3.zero;
                gunGO.transform.localRotation = Quaternion.identity;
                gunGO.transform.localScale = Vector3.one;
            }
            SpriteRenderer gunSr = gunGO.GetComponent<SpriteRenderer>();
            if (gunSr == null) gunSr = gunGO.AddComponent<SpriteRenderer>();
            gunSr.sprite = turretSprite;
            gunSr.color = tint;
            gunSr.sortingOrder = sortingOrder;
        }

        /// <summary>
        /// Tăng trưởng máu theo từng Đợt (Wave Scaling kiểu Kingdom Rush: +12% HP mỗi đợt sau Wave 1).
        /// </summary>
        public void ApplyWaveScaling(int waveNumber)
        {
            if (waveNumber <= 1) return;
            float multiplier = 1f + (waveNumber - 1) * 0.12f;
            maxHealth = Mathf.Round(maxHealth * multiplier);
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Nhận sát thương có tính Giáp vật lý (Armor) và Xuyên Giáp (Armor Penetration) chuẩn Kingdom Rush.
        /// </summary>
        public void TakeDamage(float damage, float armorPenetration = 0f)
        {
            if (IsDead) return;

            float effectiveArmor = Mathf.Clamp(armor * (1f - Mathf.Clamp01(armorPenetration)), 0f, 0.85f);
            float actualDamage = Mathf.Max(1f, damage * (1f - effectiveArmor));

            currentHealth -= actualDamage;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;

            // Thưởng vàng cho người chơi
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddGold(goldReward);
            }

            // Sinh hiệu ứng nổ tại vị trí quái chết
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }

            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>
        /// Gọi khi quái đi qua vạch waypoint cuối cùng vào đến căn cứ:
        /// Trừ máu căn cứ theo đúng cấp bậc nguy hiểm của quái vật!
        /// </summary>
        public void ReachDestination()
        {
            if (IsDead) return;
            IsDead = true;

            // Trừ máu căn cứ theo cấp độ quái
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TakeBaseDamage(baseDamage);
                Debug.Log($"<color=red><b>⚠️ [CĂN CỨ BỊ ĐỘT PHÁ] {enemyName} đã lọt vào căn cứ! Bị trừ -{baseDamage} Máu! (Máu còn: {GameManager.Instance.BaseHealth})</b></color>");
            }

            OnEscaped?.Invoke(this);
            Destroy(gameObject);
        }

        public void SetStats(string name, float hp, int gold, int damageToBase = 1, float enemyArmor = 0.15f)
        {
            enemyName = name;
            maxHealth = hp;
            currentHealth = hp;
            goldReward = gold;
            baseDamage = damageToBase;
            armor = enemyArmor;
        }

        public void SetExplosionPrefab(GameObject prefab)
        {
            explosionPrefab = prefab;
        }
    }
}
