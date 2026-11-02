using UnityEngine;
using TowerDefense.Enemies;
using TowerDefense.Combat;
using TowerDefense.Managers;

namespace TowerDefense.Towers
{
    public enum TowerType
    {
        Cannon, // 🗼 Pháo Đơn Cyber (Cấp 1 -> 2 -> 3)
        Rocket, // 🚀 Pháo Tên Lửa (Cấp 1 -> 2 -> 3)
        Laser   // 🔮 Tháp Laser/Plasma Tím (Sát thương khủng, xuyên giáp cao - Cấp 1 -> 2 -> 3)
    }

    public class Tower : MonoBehaviour
    {
        [Header("Tower Identity & Level")]
        [SerializeField] private TowerType towerType = TowerType.Cannon;
        [SerializeField] private int level = 1;
        [SerializeField] private int maxLevel = 3;
        [SerializeField] private int upgradeCost = 110;

        [Header("Targeting & Attack (Kingdom Rush Mechanics)")]
        [SerializeField] private float attackRange = 3.6f;
        [SerializeField] private float fireRate = 1.25f;
        [SerializeField] private int minDamage = 16;
        [SerializeField] private int maxDamage = 24;
        [SerializeField] private float damage = 20f;
        [SerializeField] private float splashRadius = 1.6f;
        [SerializeField] private float armorPenetration = 0.15f;
        [SerializeField] private float critChance = 0.15f;
        [SerializeField] private float critMultiplier = 1.5f;

        [Header("References")]
        [SerializeField] private Transform turretGun;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private GameObject upgradeEffectPrefab;

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private SpriteRenderer gunRenderer;

        [Header("Visual Gizmos")]
        [SerializeField] private Color rangeGizmoColor = new Color(1f, 0.2f, 0.2f, 0.35f);

        private Transform currentTarget;
        private float fireCountdown   = 0f;
        private float _targetTimer    = 0f;
        private const float TARGET_INTERVAL = 0.18f;

        private string _currentProjectileSpriteName = "towerDefense_tile273";
        private float _currentProjectileScale = 1f;
        private float _currentProjectileSpeed = 12f;

        public TowerType Type => towerType;
        public int Level => level;
        public int MaxLevel => maxLevel;
        public bool CanUpgrade => level < maxLevel;
        public int UpgradeCost => upgradeCost;
        public float AttackRange => attackRange;
        public float FireRate => fireRate;
        public float Damage => damage;
        public int MinDamage => minDamage;
        public int MaxDamage => maxDamage;
        public float ArmorPenetration => armorPenetration;
        public float CritChance => critChance;
        public string DamageRangeText => $"{minDamage}-{maxDamage}";

        public string TowerTypeName
        {
            get
            {
                switch (towerType)
                {
                    case TowerType.Cannon:
                        if (level == 1) return "Pháo Đơn Tiêu Chuẩn";
                        if (level == 2) return "Pháo Đơn Cường Hóa";
                        return "Đại Pháo Titan Tối Thượng";
                    case TowerType.Rocket:
                        if (level == 1) return "Pháo Tên Lửa Đôi";
                        if (level == 2) return "Bệ 4 Tên Lửa Liên Hoàn";
                        return "Siêu Tên Lửa Đạn Đạo";
                    case TowerType.Laser:
                        if (level == 1) return "Tháp Laser Tím";
                        if (level == 2) return "Tháp Hư Không Tím";
                        return "Siêu Pháo Hủy Diệt Tím";
                    default:
                        return "Tháp Phòng Thủ";
                }
            }
        }

        public string NextUpgradeBonusSummary
        {
            get
            {
                if (!CanUpgrade) return "Đã đạt cấp tối đa";
                int nextLv = level + 1;
                switch (towerType)
                {
                    case TowerType.Cannon:
                        return nextLv == 2
                            ? "ST lên 32-48, Crit 25%, Phá giáp 30%, Ụ súng Cấp 2"
                            : "ST lên 60-88, Crit 35%, Phá giáp 45%, Đại Pháo Titan Cấp 3!";
                    case TowerType.Rocket:
                        return nextLv == 2
                            ? "ST lên 76-110, Phá giáp 70%, Bệ 4 Tên Lửa Cấp 2"
                            : "ST lên 145-210, Phá giáp 85%, Siêu Tên Lửa Đạn Đạo Cấp 3!";
                    case TowerType.Laser:
                        return nextLv == 2
                            ? "ST lên 165-235, Phá giáp 85%, Tầm 5.6 & Hào quang Tím"
                            : "ST lên 300-420, XUYÊN GIÁP 100%, Tầm 6.5 & Chí mạng x2!";
                    default:
                        return "";
                }
            }
        }

        private void Awake()
        {
            maxLevel = 3;
            if (baseRenderer == null) baseRenderer = GetComponent<SpriteRenderer>();
            if (turretGun == null)
            {
                Transform foundGun = transform.Find("TurretGun");
                if (foundGun != null) turretGun = foundGun;
            }
            if (turretGun != null && gunRenderer == null)
                gunRenderer = turretGun.GetComponent<SpriteRenderer>();

            ApplyLevelStatsAndVisuals();
        }

        /// <summary>
        /// Khởi tạo tháp theo đúng loại (Cannon, Rocket, hoặc Laser Tím) ở Cấp 1.
        /// </summary>
        public void ConfigureForType(TowerType newType)
        {
            towerType = newType;
            level = 1;
            maxLevel = 3;
            ApplyLevelStatsAndVisuals();
        }

        /// <summary>
        /// Áp dụng chỉ số chiến đấu Kingdom Rush và cập nhật đúng bộ Sprite (Đế trụ + Ụ súng Cấp 1/2/3 + Đạn)
        /// từ bộ Redesigned_Assets bạn mới gửi.
        /// </summary>
        private void ApplyLevelStatsAndVisuals()
        {
            maxLevel = 3;
            string baseSpriteName = "towerDefense_tile180";
            string gunSpriteName  = "cannon_1barrel_lvl1";
            Color baseColor = Color.white;
            Color gunColor  = Color.white;
            float gunScale  = 1.0f;

            if (towerType == TowerType.Cannon)
            {
                // 🗼 PHÁO ĐƠN CYBER 1 NÒNG (Cấp 1 -> Cấp 2 -> Cấp 3 theo preview_1barrel_upgrades.png)
                if (level == 1)
                {
                    minDamage        = 16;
                    maxDamage        = 24;
                    damage           = 20f;
                    fireRate         = 1.25f;
                    attackRange      = 3.6f;
                    armorPenetration = 0.15f;
                    critChance       = 0.15f;
                    critMultiplier   = 1.5f;
                    upgradeCost      = 110;

                    baseSpriteName = "towerDefense_tile181"; // Đế Lục Giác Cyber Cấp 1
                    gunSpriteName  = "cannon_1barrel_lvl1";
                    _currentProjectileSpriteName = "towerDefense_tile273";
                    _currentProjectileScale = 0.85f;
                    _currentProjectileSpeed = 12f;
                }
                else if (level == 2)
                {
                    minDamage        = 32;
                    maxDamage        = 48;
                    damage           = 40f;
                    fireRate         = 1.60f;
                    attackRange      = 4.4f;
                    armorPenetration = 0.30f;
                    critChance       = 0.25f;
                    critMultiplier   = 1.6f;
                    upgradeCost      = 160;

                    baseSpriteName = "towerDefense_tile180"; // Đế Vòng Tròn Năng Lượng Cấp 2
                    gunSpriteName  = "cannon_1barrel_lvl2";
                    gunScale       = 1.08f;
                    _currentProjectileSpriteName = "towerDefense_tile273";
                    _currentProjectileScale = 1.0f;
                    _currentProjectileSpeed = 13.5f;
                }
                else // Level 3 (MAX)
                {
                    minDamage        = 60;
                    maxDamage        = 88;
                    damage           = 74f;
                    fireRate         = 2.00f;
                    attackRange      = 5.2f;
                    armorPenetration = 0.45f;
                    critChance       = 0.35f;
                    critMultiplier   = 1.75f;
                    upgradeCost      = 0;

                    baseSpriteName = "towerDefense_tile182"; // Đế Bát Giác Hạng Nặng Cấp 3
                    gunSpriteName  = "cannon_1barrel_lvl3";
                    gunScale       = 1.16f;
                    _currentProjectileSpriteName = "towerDefense_tile274"; // Đạn Vàng Kim Titan
                    _currentProjectileScale = 1.12f;
                    _currentProjectileSpeed = 15f;
                }
            }
            else if (towerType == TowerType.Rocket)
            {
                // 🚀 PHÁO TÊN LỬA HẠNG NẶNG (2 Tên Lửa -> 4 Tên Lửa -> Siêu Tên Lửa Đạn Đạo)
                if (level == 1)
                {
                    minDamage        = 38;
                    maxDamage        = 56;
                    damage           = 47f;
                    fireRate         = 0.60f;
                    attackRange      = 4.4f;
                    splashRadius     = 1.6f;
                    armorPenetration = 0.55f;
                    critChance       = 0f;
                    critMultiplier   = 1f;
                    upgradeCost      = 150;

                    baseSpriteName = "towerDefense_tile182"; // Đế Bát Giác Hạng Nặng
                    gunSpriteName  = "towerDefense_tile204"; // Bệ 2 Tên Lửa
                    _currentProjectileSpriteName = "towerDefense_tile251";
                    _currentProjectileScale = 0.90f;
                }
                else if (level == 2)
                {
                    minDamage        = 76;
                    maxDamage        = 110;
                    damage           = 93f;
                    fireRate         = 0.80f;
                    attackRange      = 5.2f;
                    splashRadius     = 2.1f;
                    armorPenetration = 0.70f;
                    critChance       = 0f;
                    critMultiplier   = 1f;
                    upgradeCost      = 220;

                    baseSpriteName = "towerDefense_tile180"; // Đế Vòng Tròn Năng Lượng
                    gunSpriteName  = "turret_missile_lvl2"; // Bệ 4 Tên Lửa Liên Hoàn
                    gunScale       = 1.10f;
                    _currentProjectileSpriteName = "towerDefense_tile251";
                    _currentProjectileScale = 1.05f;
                }
                else // Level 3 (MAX)
                {
                    minDamage        = 145;
                    maxDamage        = 210;
                    damage           = 178f;
                    fireRate         = 1.00f;
                    attackRange      = 6.0f;
                    splashRadius     = 2.7f;
                    armorPenetration = 0.85f;
                    critChance       = 0f;
                    critMultiplier   = 1f;
                    upgradeCost      = 0;

                    baseSpriteName = "towerDefense_tile183"; // Đế Kim Cương Tối Thượng
                    gunSpriteName  = "towerDefense_tile206"; // Siêu Tên Lửa Đạn Đạo Cỡ Đại
                    gunScale       = 1.20f;
                    _currentProjectileSpriteName = "towerDefense_tile252"; // Đầu đạn hạng nặng
                    _currentProjectileScale = 1.20f;
                }
            }
            else // 🔮 TowerType.Laser (THÁP TÍM MỚI — Sát thương siêu to, Xuyên giáp tối thượng, Mắc tiền!)
            {
                if (level == 1)
                {
                    minDamage        = 85;
                    maxDamage        = 125;
                    damage           = 105f;
                    fireRate         = 0.85f;
                    attackRange      = 4.8f;
                    armorPenetration = 0.70f;
                    critChance       = 0.20f;
                    critMultiplier   = 1.6f;
                    upgradeCost      = 200;

                    baseSpriteName = "towerDefense_tile183"; // Đế Kim Cương Cyber
                    gunSpriteName  = "turret_laser_lvl1";    // Ụ Súng Tím Lăng Kính
                    baseColor      = new Color(0.95f, 0.85f, 1f, 1f);
                    _currentProjectileSpriteName = "towerDefense_tile275"; // Quả Cầu Năng Lượng Tím
                    _currentProjectileScale = 1.05f;
                    _currentProjectileSpeed = 14f;
                }
                else if (level == 2)
                {
                    minDamage        = 165;
                    maxDamage        = 235;
                    damage           = 200f;
                    fireRate         = 1.10f;
                    attackRange      = 5.6f;
                    armorPenetration = 0.85f;
                    critChance       = 0.30f;
                    critMultiplier   = 1.75f;
                    upgradeCost      = 280;

                    baseSpriteName = "towerDefense_tile182";
                    gunSpriteName  = "turret_laser_lvl1";
                    baseColor      = new Color(0.90f, 0.72f, 1f, 1f);
                    gunColor       = new Color(1f, 0.88f, 1f, 1f);
                    gunScale       = 1.16f;
                    _currentProjectileSpriteName = "towerDefense_tile275";
                    _currentProjectileScale = 1.28f;
                    _currentProjectileSpeed = 16f;
                }
                else // Level 3 (MAX)
                {
                    minDamage        = 300;
                    maxDamage        = 420;
                    damage           = 360f;
                    fireRate         = 1.35f;
                    attackRange      = 6.5f;
                    armorPenetration = 1.00f; // 100% Xuyên giáp (Sát thương chuẩn!)
                    critChance       = 0.40f;
                    critMultiplier   = 2.0f;
                    upgradeCost      = 0;

                    baseSpriteName = "towerDefense_tile180";
                    gunSpriteName  = "turret_laser_lvl1";
                    baseColor      = new Color(0.85f, 0.60f, 1f, 1f);
                    gunColor       = new Color(1f, 0.82f, 1f, 1f);
                    gunScale       = 1.30f;
                    _currentProjectileSpriteName = "towerDefense_tile275";
                    _currentProjectileScale = 1.50f;
                    _currentProjectileSpeed = 18f;
                }
            }

            // Áp dụng Sprite cho Đế Trụ (Base) — xuất hiện đi kèm ngay dưới Ụ Súng khi mua trụ!
            if (baseRenderer != null)
            {
                Sprite bSpr = CyberAssetLoader.GetSprite(baseSpriteName);
                if (bSpr != null) baseRenderer.sprite = bSpr;
                baseRenderer.color = baseColor;
                baseRenderer.sortingOrder = 2;
                baseRenderer.enabled = true;
            }

            // Áp dụng Sprite cho Ụ Súng (TurretGun)
            if (turretGun != null)
            {
                turretGun.localPosition = Vector3.zero;
                turretGun.localScale = Vector3.one * gunScale;
            }
            if (gunRenderer != null)
            {
                Sprite gSpr = CyberAssetLoader.GetSprite(gunSpriteName);
                if (gSpr != null) gunRenderer.sprite = gSpr;
                gunRenderer.color = gunColor;
                gunRenderer.sortingOrder = 3;
                gunRenderer.enabled = true;
            }
        }

        private void Update()
        {
            _targetTimer += Time.deltaTime;
            if (_targetTimer >= TARGET_INTERVAL)
            {
                _targetTimer = 0f;
                UpdateTarget();
            }

            if (currentTarget != null)
            {
                RotateTurretTowardsTarget();

                if (fireCountdown <= 0f)
                {
                    Shoot();
                    fireCountdown = 1f / fireRate;
                }
            }

            if (fireCountdown > 0f)
                fireCountdown -= Time.deltaTime;
        }

        private void UpdateTarget()
        {
            if (currentTarget != null)
            {
                float dist = Vector2.Distance(transform.position, currentTarget.position);
                if (dist <= attackRange)
                {
                    Enemy enemy = currentTarget.GetComponent<Enemy>();
                    if (enemy != null && !enemy.IsDead) return;
                }
            }

            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, attackRange);
            float shortestDistance = Mathf.Infinity;
            Transform nearestEnemy = null;

            foreach (Collider2D col in colliders)
            {
                if (col.CompareTag("Enemy"))
                {
                    Enemy enemy = col.GetComponent<Enemy>();
                    if (enemy != null && !enemy.IsDead)
                    {
                        float distanceToEnemy = Vector2.Distance(transform.position, col.transform.position);
                        if (distanceToEnemy < shortestDistance)
                        {
                            shortestDistance = distanceToEnemy;
                            nearestEnemy = col.transform;
                        }
                    }
                }
            }

            currentTarget = nearestEnemy;
        }

        private void RotateTurretTowardsTarget()
        {
            if (turretGun == null || currentTarget == null) return;
            Vector3 dir = currentTarget.position - turretGun.position;
            dir.z = 0;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            turretGun.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void Shoot()
        {
            if (bulletPrefab == null) return;
            Vector3 spawnPos = firePoint != null ? firePoint.position : (turretGun != null ? turretGun.position : transform.position);
            Quaternion spawnRot = turretGun != null ? turretGun.rotation : Quaternion.identity;

            // Random sát thương trong khoảng [minDamage, maxDamage] chuẩn Kingdom Rush
            float rolledDamage = Random.Range(minDamage, maxDamage + 1);
            bool isCrit = critChance > 0f && Random.value < critChance;
            if (isCrit)
            {
                rolledDamage = Mathf.Round(rolledDamage * critMultiplier);
            }

            GameObject projGO = Instantiate(bulletPrefab, spawnPos, spawnRot);
            Sprite projSprite = CyberAssetLoader.GetSprite(_currentProjectileSpriteName);

            // Bắn đạn đơn mục tiêu (Pháo Đơn hoặc Tháp Laser Tím)
            Bullet bullet = projGO.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.SetProjectileVisual(projSprite, Color.white, _currentProjectileScale, _currentProjectileSpeed);
                bullet.SetDamage(rolledDamage, armorPenetration, isCrit);
                if (currentTarget != null) bullet.Seek(currentTarget);
                return;
            }

            // Bắn Rocket AoE (Pháo Tên Lửa)
            Rocket rocket = projGO.GetComponent<Rocket>();
            if (rocket != null)
            {
                rocket.SetProjectileVisual(projSprite, Color.white, _currentProjectileScale);
                rocket.SetDamage(rolledDamage, splashRadius, armorPenetration);
                if (currentTarget != null) rocket.Seek(currentTarget);
            }
        }

        /// <summary>
        /// Nâng cấp tháp lên cấp tiếp theo (Cấp 1 -> Cấp 2 -> Cấp 3 MAX),
        /// tự động đổi ụ súng, đế trụ, đạn bắn ra và tăng mạnh sát thương!
        /// </summary>
        public bool UpgradeToNextLevel(GameObject fxPrefab = null)
        {
            if (!CanUpgrade) return false;

            level++;
            ApplyLevelStatsAndVisuals();

            GameObject fxToSpawn = fxPrefab != null ? fxPrefab : upgradeEffectPrefab;
            if (fxToSpawn != null)
            {
                GameObject fx = Instantiate(fxToSpawn, transform.position, Quaternion.identity);
                fx.transform.localScale = Vector3.one * (level >= 3 ? 1.85f : 1.45f);
            }

            Debug.Log($"<color=yellow><b>⭐ [NÂNG CẤP CẤP {level}] {TowerTypeName} (Sát thương: {DamageRangeText}, Tầm bắn: {attackRange}, Tốc độ: {fireRate}/s, Phá giáp: {Mathf.RoundToInt(armorPenetration * 100)}%)</b></color>");
            return true;
        }

        public bool UpgradeToLevel2(GameObject fxPrefab = null)
        {
            return UpgradeToNextLevel(fxPrefab);
        }

        public void SetReferences(Transform gun, Transform fPoint, GameObject bPrefab,
                                  float range = 3.6f, float rate = 1.25f, float dmg = 20f,
                                  TowerType type = TowerType.Cannon, int upCost = 110)
        {
            turretGun    = gun;
            firePoint    = fPoint;
            bulletPrefab = bPrefab;
            attackRange  = range;
            fireRate     = rate;
            damage       = dmg;
            towerType    = type;
            upgradeCost  = upCost;
            maxLevel     = 3;
            if (baseRenderer == null) baseRenderer = GetComponent<SpriteRenderer>();
            if (turretGun != null && gunRenderer == null) gunRenderer = turretGun.GetComponent<SpriteRenderer>();
            ApplyLevelStatsAndVisuals();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = rangeGizmoColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
