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
        [SerializeField] private int totalInvestedGold = 70;
        [SerializeField, Range(0.5f, 0.9f)] private float sellRefundRatio = 0.70f;

        [Header("Targeting & Attack (Kingdom Rush Mechanics)")]
        [SerializeField] private float attackRange = 3.6f;
        [SerializeField] private float fireRate = 1.25f;
        [SerializeField] private int minDamage = 16;
        [SerializeField] private int maxDamage = 24;
        [SerializeField] private float damage = 20f;
        [SerializeField] private float splashRadius = 1.6f;
        [SerializeField] private float armorPenetration = 0.15f;
        [SerializeField] private float critChance = 0f;
        [SerializeField] private float critMultiplier = 1f;

        [Header("References")]
        [SerializeField] private Transform turretGun;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private GameObject upgradeEffectPrefab;

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private SpriteRenderer gunRenderer;

        [Header("Range Visualizer")]
        [SerializeField] private SpriteRenderer rangeRenderer;

        [Header("Laser Beam Visualizer (Continuous Ray)")]
        [SerializeField] private LineRenderer laserLineRenderer;
        [SerializeField] private SpriteRenderer laserMuzzleGlow;
        [SerializeField] private SpriteRenderer laserImpactGlow;

        [Header("Visual Gizmos")]
        [SerializeField] private Color rangeGizmoColor = new Color(1f, 0.2f, 0.2f, 0.35f);

        private Transform currentTarget;
        private float fireCountdown   = 0f;
        private float _targetTimer    = 0f;
        private const float TARGET_INTERVAL = 0.18f;
        private bool _isRangeIndicatorVisible = false;

        private string _currentProjectileSpriteName = "kr_bullet_cannon_lvl1";
        private float _currentProjectileScale = 1f;
        private float _currentProjectileSpeed = 12f;
        private float _currentProjectileAngleOffset = -90f;
        private bool _currentProjectileSpin = false;

        // Bộ đệm Zero-GC cho tìm kiếm mục tiêu & Animation Pháo Đơn / Pháo Tên Lửa Kingdom Rush (2.5D Isometric)
        private static readonly Collider2D[] _overlapBuffer = new Collider2D[64];
        private Sprite[] _cannonDirSprites;
        private Sprite[] _cannonFireFrames;
        private int _currentDirIndex = 0; // 0: Right, 1: Up-Right, 2: Up, 3: Up-Left, 4: Left, 5: Down-Left, 6: Down, 7: Down-Right
        private Vector3 _lastAimDir = Vector3.right;
        private float _baseGunScale = 1.0f;
        private bool _isFiringAnim = false;
        private float _firingAnimTimer = 0f;
        private const float FIRE_ANIM_DURATION = 0.28f;
        private bool IsKingdomRush25DTower => towerType == TowerType.Cannon || towerType == TowerType.Rocket || towerType == TowerType.Laser;

        public TowerType Type => towerType;
        public int Level => level;
        public int MaxLevel => maxLevel;
        public bool CanUpgrade => level < maxLevel;
        public int UpgradeCost => upgradeCost;
        public int TotalInvestedGold => totalInvestedGold;
        public int SellRefundGold
        {
            get
            {
                // Pháo Đơn Cấp 1 (70 vàng) hoàn lại tròn 50 vàng; các cấp khác hoàn 70% tổng vàng đã đầu tư
                if (towerType == TowerType.Cannon && level == 1 && totalInvestedGold == 70)
                    return 50;
                return Mathf.RoundToInt(totalInvestedGold * sellRefundRatio);
            }
        }
        public float NextLevelAttackRange
        {
            get
            {
                if (!CanUpgrade) return attackRange;
                int nextLv = level + 1;
                switch (towerType)
                {
                    case TowerType.Cannon: return nextLv == 2 ? 4.4f : 5.2f;
                    case TowerType.Rocket: return nextLv == 2 ? 5.2f : 6.0f;
                    case TowerType.Laser:  return nextLv == 2 ? 5.6f : 6.5f;
                    default:               return attackRange;
                }
            }
        }
        public float AttackRange => attackRange;
        public float FireRate => fireRate;
        public float Damage => damage;
        public int MinDamage => minDamage;
        public int MaxDamage => maxDamage;
        public float ArmorPenetration => armorPenetration;
        public float CritChance => critChance;
        public float CritMultiplier => critMultiplier;
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
                            ? "ST lên 32-48, Phá giáp 30%, Ụ súng Cấp 2"
                            : "ST lên 60-88, Phá giáp 45%, Đại Pháo Cấp 3!";
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

            SetupRangeIndicator();
            ApplyLevelStatsAndVisuals();
        }

        private void SetupRangeIndicator()
        {
            if (rangeRenderer == null)
            {
                Transform foundRange = transform.Find("RangeIndicator");
                if (foundRange != null)
                {
                    rangeRenderer = foundRange.GetComponent<SpriteRenderer>();
                }
                else
                {
                    GameObject rGO = new GameObject("RangeIndicator");
                    rGO.transform.SetParent(transform, false);
                    rGO.transform.localPosition = Vector3.zero;
                    rangeRenderer = rGO.AddComponent<SpriteRenderer>();
                }
            }

            if (rangeRenderer != null)
            {
                rangeRenderer.sprite = CyberAssetLoader.GetRangeCircleSprite();
                rangeRenderer.sortingOrder = 1; // Nằm trên nền đất, dưới đế pháo (order 2) và nòng súng (order 3)
                rangeRenderer.enabled = false;
                UpdateRangeIndicatorScale();
            }

            SetupLaserBeamComponents();
        }

        private void SetupLaserBeamComponents()
        {
            if (laserLineRenderer == null)
            {
                Transform foundLaser = transform.Find("LaserBeam");
                if (foundLaser != null)
                {
                    laserLineRenderer = foundLaser.GetComponent<LineRenderer>();
                }
                else
                {
                    GameObject laserGO = new GameObject("LaserBeam");
                    laserGO.transform.SetParent(transform, false);
                    laserLineRenderer = laserGO.AddComponent<LineRenderer>();
                }
            }

            if (laserLineRenderer != null)
            {
                laserLineRenderer.useWorldSpace = true;
                laserLineRenderer.positionCount = 2;
                laserLineRenderer.sortingOrder = 6; // Trên bệ và ụ tháp
                laserLineRenderer.textureMode = LineTextureMode.Stretch; // Kéo căng tia laze mượt mà liên tục, không bị ngắt quãng!

                Shader spriteShader = Shader.Find("Sprites/Default");
                if (spriteShader == null) spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                if (spriteShader != null)
                {
                    Material laserMat = new Material(spriteShader);
                    Sprite beamSpr = CyberAssetLoader.GetLaserBeamSprite(level);
                    if (beamSpr != null && beamSpr.texture != null)
                    {
                        laserMat.mainTexture = beamSpr.texture;
                    }
                    laserMat.color = Color.white;
                    laserLineRenderer.material = laserMat;
                }

                if (level == 1)
                {
                    laserLineRenderer.startWidth = 0.24f;
                    laserLineRenderer.endWidth = 0.20f;
                    laserLineRenderer.startColor = new Color(0.35f, 0.95f, 1f, 1f);
                    laserLineRenderer.endColor   = new Color(0.15f, 0.85f, 1f, 0.90f);
                }
                else if (level == 2)
                {
                    laserLineRenderer.startWidth = 0.30f;
                    laserLineRenderer.endWidth = 0.24f;
                    laserLineRenderer.startColor = new Color(0.85f, 0.55f, 1f, 1f);
                    laserLineRenderer.endColor   = new Color(0.35f, 0.90f, 1f, 0.95f);
                }
                else
                {
                    laserLineRenderer.startWidth = 0.38f;
                    laserLineRenderer.endWidth = 0.28f;
                    laserLineRenderer.startColor = new Color(1f, 0.92f, 0.40f, 1f);
                    laserLineRenderer.endColor   = new Color(0.25f, 0.98f, 1f, 1f);
                }
                laserLineRenderer.enabled = false;
            }

            // Điểm sáng đầu nòng (Quả cầu phát sáng đỉnh tháp)
            if (laserMuzzleGlow == null)
            {
                Transform fM = transform.Find("LaserMuzzleGlow");
                if (fM != null) laserMuzzleGlow = fM.GetComponent<SpriteRenderer>();
                else
                {
                    GameObject mGO = new GameObject("LaserMuzzleGlow");
                    mGO.transform.SetParent(transform, false);
                    laserMuzzleGlow = mGO.AddComponent<SpriteRenderer>();
                }
            }
            if (laserMuzzleGlow != null)
            {
                laserMuzzleGlow.sprite = CyberAssetLoader.GetLaserOrbSprite(level);
                laserMuzzleGlow.color = Color.white;
                laserMuzzleGlow.sortingOrder = 7;
                laserMuzzleGlow.transform.localScale = Vector3.one * (level == 3 ? 0.65f : (level == 2 ? 0.55f : 0.45f));
                laserMuzzleGlow.enabled = false;
            }

            // Điểm nổ sáng tại mục tiêu (Impact Glow)
            if (laserImpactGlow == null)
            {
                Transform fI = transform.Find("LaserImpactGlow");
                if (fI != null) laserImpactGlow = fI.GetComponent<SpriteRenderer>();
                else
                {
                    GameObject iGO = new GameObject("LaserImpactGlow");
                    iGO.transform.SetParent(transform, false);
                    laserImpactGlow = iGO.AddComponent<SpriteRenderer>();
                }
            }
            if (laserImpactGlow != null)
            {
                laserImpactGlow.sprite = CyberAssetLoader.GetLaserOrbSprite(level);
                laserImpactGlow.color = (level == 3) ? new Color(1f, 0.92f, 0.45f, 1f) : ((level == 2) ? new Color(0.85f, 0.65f, 1f, 1f) : new Color(0.40f, 0.95f, 1f, 1f));
                laserImpactGlow.sortingOrder = 7;
                laserImpactGlow.transform.localScale = Vector3.one * (level == 3 ? 0.85f : 0.70f);
                laserImpactGlow.enabled = false;
            }
        }

        public void UpdateRangeIndicatorScale()
        {
            if (rangeRenderer != null)
            {
                // RangeCircleSprite có bán kính là 1 unit (đường kính 2 units tương ứng scale 2*attackRange)
                float diameter = attackRange * 2f;
                rangeRenderer.transform.localScale = new Vector3(diameter, diameter, 1f);
            }
        }

        public void SetRangeVisible(bool visible)
        {
            _isRangeIndicatorVisible = visible;
            if (rangeRenderer != null)
            {
                rangeRenderer.enabled = visible;
                if (visible) UpdateRangeIndicatorScale();
            }
        }

        public bool IsRangeVisible => _isRangeIndicatorVisible;

        /// <summary>
        /// Khởi tạo tháp theo đúng loại (Cannon, Rocket, hoặc Laser Tím) ở Cấp 1 kèm chi phí mua ban đầu để tính vàng hoàn lại khi Bán.
        /// </summary>
        public void ConfigureForType(TowerType newType, int initialBuildCost = -1)
        {
            towerType = newType;
            level = 1;
            maxLevel = 3;
            if (initialBuildCost > 0)
            {
                totalInvestedGold = initialBuildCost;
            }
            else
            {
                totalInvestedGold = (newType == TowerType.Cannon) ? 70 : (newType == TowerType.Rocket ? 100 : 150);
            }
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
                // 🗼 PHÁO ĐƠN KINGDOM RUSH (8 Góc Xoay + 4 Frame Bắn Giật Nảy + Đạn Cấp 1 -> 2 -> 3)
                _cannonDirSprites = CyberAssetLoader.GetCannonLvl1DirSprites();
                _cannonFireFrames = CyberAssetLoader.GetCannonLvl1FireFrames();

                if (level == 1)
                {
                    minDamage        = 16;
                    maxDamage        = 24;
                    damage           = 20f;
                    fireRate         = 1.25f;
                    attackRange      = 3.6f;
                    armorPenetration = 0.15f;
                    critChance       = 0f;
                    critMultiplier   = 1f;
                    upgradeCost      = 110;

                    baseSpriteName = "kr_dirt_pad_clean";
                    gunSpriteName  = "kr_cannon_lvl1_dir0";
                    gunScale       = 1.0f;
                    _currentProjectileSpriteName  = "kr_bullet_cannon_lvl1";
                    _currentProjectileScale       = 0.85f;
                    _currentProjectileSpeed       = 13f;
                    _currentProjectileAngleOffset = -22f; // Khớp đuôi gió trắng của bi sắt Cấp 1
                    _currentProjectileSpin        = false;
                }
                else if (level == 2)
                {
                    minDamage        = 32;
                    maxDamage        = 48;
                    damage           = 40f;
                    fireRate         = 1.60f;
                    attackRange      = 4.4f;
                    armorPenetration = 0.30f;
                    critChance       = 0f;
                    critMultiplier   = 1f;
                    upgradeCost      = 160;

                    baseSpriteName = "kr_dirt_pad_clean";
                    gunSpriteName  = "kr_cannon_lvl2_idle";
                    gunScale       = 1.08f;
                    _currentProjectileSpriteName  = "kr_bullet_cannon_lvl2";
                    _currentProjectileScale       = 1.02f;
                    _currentProjectileSpeed       = 14.5f;
                    _currentProjectileAngleOffset = -22f; // Khớp đuôi gió trắng của bi sắt lớn Cấp 2
                    _currentProjectileSpin        = false;
                }
                else // Level 3 (MAX) - ĐẠI PHÁO KINGDOM RUSH + BOM ĐỎ NGÒI NỔ CẤP 3
                {
                    minDamage        = 60;
                    maxDamage        = 88;
                    damage           = 74f;
                    fireRate         = 2.00f;
                    attackRange      = 5.2f;
                    armorPenetration = 0.45f;
                    critChance       = 0f;
                    critMultiplier   = 1f;
                    upgradeCost      = 0;

                    baseSpriteName = "kr_dirt_pad_clean";
                    gunSpriteName  = "kr_cannon_lvl3_idle";
                    gunScale       = 1.16f;
                    _currentProjectileSpriteName  = "kr_bullet_cannon_lvl3";
                    _currentProjectileScale       = 1.12f;
                    _currentProjectileSpeed       = 16f;
                    _currentProjectileAngleOffset = 0f;
                    _currentProjectileSpin        = true; // Bom tròn sọc đỏ xoay tròn trên không
                }
            }
            else if (towerType == TowerType.Rocket)
            {
                // 🚀 PHÁO TÊN LỬA KINGDOM RUSH (8 Góc Xoay + 4 Frame Khai Hỏa Giật Nảy + Tên Lửa Cấp 1 -> 2 -> 3)
                _cannonDirSprites = CyberAssetLoader.GetRocketLvl1DirSprites();
                _cannonFireFrames = CyberAssetLoader.GetRocketLvl1FireFrames();

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

                    baseSpriteName = "kr_dirt_pad_clean";
                    gunSpriteName  = "kr_rocket_lvl1_dir0";
                    gunScale       = 1.0f;
                    _currentProjectileSpriteName  = "kr_missile_lvl1";
                    _currentProjectileScale       = 0.92f;
                    _currentProjectileSpeed       = 8.5f;
                    _currentProjectileAngleOffset = -28f;
                    _currentProjectileSpin        = false;
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

                    baseSpriteName = "kr_dirt_pad_clean";
                    gunSpriteName  = "kr_rocket_lvl2_idle";
                    gunScale       = 1.08f;
                    _currentProjectileSpriteName  = "kr_missile_lvl2";
                    _currentProjectileScale       = 1.04f;
                    _currentProjectileSpeed       = 9.5f;
                    _currentProjectileAngleOffset = -28f;
                    _currentProjectileSpin        = false;
                }
                else // Level 3 (MAX) - SIÊU TÊN LỬA ĐẠN ĐẠO RADAR CẤP 3
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

                    baseSpriteName = "kr_dirt_pad_clean";
                    gunSpriteName  = "kr_rocket_lvl3_idle";
                    gunScale       = 1.16f;
                    _currentProjectileSpriteName  = "kr_missile_lvl3";
                    _currentProjectileScale       = 1.15f;
                    _currentProjectileSpeed       = 10.5f;
                    _currentProjectileAngleOffset = -32f;
                    _currentProjectileSpin        = false;
                }
            }
            else // 🔮 TowerType.Laser (THÁP LAZE ĐỨNG KINGDOM RUSH — Tháp đá thẳng đứng, Quả cầu ngọc lam bắn tia Laze)
            {
                _cannonDirSprites = null; // Tháp Laze đứng thẳng 360°, không dùng sprite hướng xoay có tia vẽ sẵn
                _cannonFireFrames = CyberAssetLoader.GetLaserLvl1FireFrames();
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

                    baseSpriteName = "kr_dirt_pad_clean"; // Che biển búa gỗ trên Map
                    gunSpriteName  = "kr_laser_lvl1_idle"; // Tháp Laze Cấp 1 thẳng đứng
                    baseColor      = Color.white;
                    gunColor       = Color.white;
                    gunScale       = 1.0f;
                    _currentProjectileSpriteName = "kr_laser_orb_lvl1";
                    _currentProjectileScale = 1.0f;
                    _currentProjectileSpeed = 16f;
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

                    baseSpriteName = "kr_dirt_pad_clean";
                    gunSpriteName  = "kr_laser_lvl2_idle"; // Tháp Laze Cấp 2
                    baseColor      = Color.white;
                    gunColor       = Color.white;
                    gunScale       = 1.08f;
                    _currentProjectileSpriteName = "kr_laser_orb_lvl2";
                    _currentProjectileScale = 1.15f;
                    _currentProjectileSpeed = 18f;
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

                    baseSpriteName = "kr_dirt_pad_clean";
                    gunSpriteName  = "kr_laser_lvl3_idle"; // Tháp Laze Cấp 3
                    baseColor      = Color.white;
                    gunColor       = Color.white;
                    gunScale       = 1.15f;
                    _currentProjectileSpriteName = "kr_laser_orb_lvl3";
                    _currentProjectileScale = 1.30f;
                    _currentProjectileSpeed = 20f;
                }
            }

            _baseGunScale = gunScale;
            _isFiringAnim = false;
            _firingAnimTimer = 0f;

            // Áp dụng Sprite cho Đế Trụ (Base)
            // Với Pháo Vương Quốc (Cannon, Rocket, Laser): Tháp đã có sẵn Bệ Đá Tròn 2.5D chuẩn tỉ lệ (86px) ở gunRenderer!
            // Do đó tắt baseRenderer để bệ đá tự nhiên trên mọi bản đồ (Map 1, 2, 3), không bao giờ bị lộ viền xám hay mảng xanh!
            if (baseRenderer != null)
            {
                if (IsKingdomRush25DTower)
                {
                    baseRenderer.enabled = false;
                }
                else
                {
                    Sprite bSpr = CyberAssetLoader.GetSprite(baseSpriteName);
                    if (bSpr != null) baseRenderer.sprite = bSpr;
                    baseRenderer.color = baseColor;
                    baseRenderer.sortingOrder = 2;
                    baseRenderer.enabled = (bSpr != null);
                }
            }

            // Áp dụng Sprite cho Ụ Súng (TurretGun)
            if (turretGun != null)
            {
                turretGun.localPosition = Vector3.zero;
                turretGun.localScale = new Vector3(gunScale, gunScale, 1f);
                if (IsKingdomRush25DTower)
                {
                    turretGun.localRotation = Quaternion.identity;
                }
            }
            if (gunRenderer != null)
            {
                Sprite gSpr = (IsKingdomRush25DTower && towerType != TowerType.Laser && level == 1 && _cannonDirSprites != null && _cannonDirSprites[_currentDirIndex] != null)
                    ? _cannonDirSprites[_currentDirIndex]
                    : CyberAssetLoader.GetSprite(gunSpriteName);
                if (gSpr != null) gunRenderer.sprite = gSpr;
                gunRenderer.color = gunColor;
                gunRenderer.flipX = false;
                gunRenderer.sortingOrder = 3;
                gunRenderer.enabled = true;
            }

            if (_isRangeIndicatorVisible)
            {
                UpdateRangeIndicatorScale();
            }

            if (towerType == TowerType.Laser)
            {
                SetupLaserBeamComponents();
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

                // 🔮 Riêng Tháp Laser Tím: Chiếu Tia Laze liên tục thiêu đốt mục tiêu
                if (towerType == TowerType.Laser)
                {
                    UpdateContinuousLaserBeam();
                }
                else
                {
                    DisableLaserBeam();
                    if (fireCountdown <= 0f)
                    {
                        Shoot();
                        fireCountdown = 1f / fireRate;
                    }
                }
            }
            else
            {
                DisableLaserBeam();
            }

            if (fireCountdown > 0f)
                fireCountdown -= Time.deltaTime;

            if (_isFiringAnim)
            {
                UpdateCannonFiringAnimation();
            }
        }

        private void DisableLaserBeam()
        {
            if (laserLineRenderer != null && laserLineRenderer.enabled) laserLineRenderer.enabled = false;
            if (laserMuzzleGlow != null && laserMuzzleGlow.enabled) laserMuzzleGlow.enabled = false;
            if (laserImpactGlow != null && laserImpactGlow.enabled) laserImpactGlow.enabled = false;

            // Khi ngừng bắn, đưa Tháp Laze Cấp 1 về lại Frame tĩnh (Idle)
            if (towerType == TowerType.Laser && level == 1 && gunRenderer != null)
            {
                Sprite idleSpr = CyberAssetLoader.GetSprite("kr_laser_lvl1_idle");
                if (idleSpr != null) gunRenderer.sprite = idleSpr;
            }
        }

        private void UpdateContinuousLaserBeam()
        {
            if (currentTarget == null)
            {
                DisableLaserBeam();
                return;
            }

            // Điểm xuất phát khớp chuẩn 100% tâm quả cầu phát sáng đỉnh tháp đá đứng (tính chính xác theo pixel + PPU 112 + gunScale)
            float orbOffsetY = (level == 3) ? 0.86f : ((level == 2) ? 0.79f : 0.62f);
            Vector3 startPos = transform.position + new Vector3(0f, orbOffsetY, 0f);
            Vector3 endPos = currentTarget.position;
            startPos.z = 0;
            endPos.z = 0;

            // Kích hoạt và render tia Laze
            if (laserLineRenderer != null)
            {
                laserLineRenderer.enabled = true;
                laserLineRenderer.SetPosition(0, startPos);
                laserLineRenderer.SetPosition(1, endPos);

                // Tia laze dạng chùm năng lượng liên tục (Stretch), sáng mượt không bị đứt đoạn / ngắt quãng
                float width = (level == 3 ? 0.32f : (level == 2 ? 0.26f : 0.20f)) + Mathf.Sin(Time.time * 8f) * 0.012f;
                laserLineRenderer.startWidth = width;
                laserLineRenderer.endWidth = width * 0.85f;
            }

            // Hào quang năng lượng mềm tại đúng tâm quả cầu đỉnh tháp (không chồng đè quả cầu thứ 2)
            if (laserMuzzleGlow != null)
            {
                laserMuzzleGlow.enabled = true;
                laserMuzzleGlow.transform.position = startPos;
                laserMuzzleGlow.transform.localScale = Vector3.one * ((level == 3 ? 0.58f : (level == 2 ? 0.50f : 0.42f)) + Mathf.Sin(Time.time * 14f) * 0.05f);
            }

            // Điểm sáng va chạm tại quái
            if (laserImpactGlow != null)
            {
                laserImpactGlow.enabled = true;
                laserImpactGlow.transform.position = endPos;
                laserImpactGlow.transform.localScale = Vector3.one * ((level == 3 ? 0.85f : 0.70f) + Mathf.Cos(Time.time * 25f) * 0.12f);
                laserImpactGlow.transform.Rotate(0, 0, 180f * Time.deltaTime);
            }

            // Animation xung nhịp năng lượng (4 Frame pulse firing animation cho Cấp 1)
            if (level == 1 && gunRenderer != null)
            {
                Sprite[] fFrames = CyberAssetLoader.GetLaserLvl1FireFrames();
                if (fFrames != null && fFrames.Length >= 4)
                {
                    int animIdx = (int)((Time.time * 8f) % 4);
                    if (fFrames[animIdx] != null) gunRenderer.sprite = fFrames[animIdx];
                }
            }

            // Gây sát thương liên tục theo thời gian thực (DPS)
            float dps = damage; // damage mỗi giây
            float damageThisFrame = dps * Time.deltaTime;
            Enemy enemy = currentTarget.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(damageThisFrame, armorPenetration);
            }
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

            int hitCount = Physics2D.OverlapCircle(transform.position, attackRange, ContactFilter2D.noFilter, _overlapBuffer);
            float shortestDistance = Mathf.Infinity;
            Transform nearestEnemy = null;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D col = _overlapBuffer[i];
                if (col != null && col.CompareTag("Enemy"))
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
            Vector3 dir = currentTarget.position - transform.position;
            dir.z = 0f;

            // 🗼🚀 Pháo Đơn, Pháo Tên Lửa & Tháp Laze Kingdom Rush (2.5D Isometric)
            if (IsKingdomRush25DTower)
            {
                turretGun.localRotation = Quaternion.identity;
                if (towerType == TowerType.Laser)
                {
                    // Tháp Laze đứng: Trụ đá và thân tháp cố định thẳng đứng 100%, tia Laze phát ra 360° từ đỉnh tháp
                    if (gunRenderer != null) gunRenderer.flipX = false;
                    return;
                }

                if (dir.sqrMagnitude > 0.0001f)
                {
                    _lastAimDir = dir.normalized;
                }

                float deg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                if (deg < 0f) deg += 360f;
                int dirIdx = Mathf.RoundToInt(deg / 45f) % 8;
                _currentDirIndex = dirIdx;

                if (gunRenderer != null)
                {
                    if (level == 1)
                    {
                        if (_cannonDirSprites == null)
                        {
                            _cannonDirSprites = (towerType == TowerType.Rocket)
                                ? CyberAssetLoader.GetRocketLvl1DirSprites()
                                : CyberAssetLoader.GetCannonLvl1DirSprites();
                        }
                        // Chỉ đổi sang Sprite góc ngắm khi không bị khóa bởi frame lửa đầu nòng
                        if (!_isFiringAnim && _cannonDirSprites != null && _cannonDirSprites[dirIdx] != null)
                        {
                            gunRenderer.flipX = false;
                            gunRenderer.sprite = _cannonDirSprites[dirIdx];
                        }
                    }
                    else
                    {
                        // Cấp 2 & Cấp 3: Lật hướng Trái/Phải theo mục tiêu
                        if (Mathf.Abs(dir.x) > 0.05f)
                        {
                            gunRenderer.flipX = (dir.x < 0f);
                        }
                    }
                }
                return;
            }

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            turretGun.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// Kích hoạt chuỗi Animation 4 trạng thái khi Pháo Đơn hoặc Pháo Tên Lửa khai hỏa:
        /// Frame 1 (Idle) -> Frame 2 (Anticipation / Ignition) -> Frame 3 (Recoil Kick / Squash!) -> Frame 4 (Stretch & Settle).
        /// Hoàn toàn Zero-GC.
        /// </summary>
        private void TriggerCannonFireAnimation()
        {
            _isFiringAnim = true;
            _firingAnimTimer = 0f;
            if (_cannonFireFrames == null)
            {
                _cannonFireFrames = (towerType == TowerType.Rocket)
                    ? CyberAssetLoader.GetRocketLvl1FireFrames()
                    : CyberAssetLoader.GetCannonLvl1FireFrames();
            }
        }

        private void UpdateCannonFiringAnimation()
        {
            if (turretGun == null)
            {
                _isFiringAnim = false;
                return;
            }

            _firingAnimTimer += Time.deltaTime;
            float t = _firingAnimTimer;

            // Kiểm tra có đang ngắm ngang (Trái/Phải/Chéo) để hiển thị bộ 4 frame lửa mồi & khói đầu nòng Cấp 1 hay không
            bool useLateralFireFrames = (IsKingdomRush25DTower && level == 1 && _cannonFireFrames != null && _currentDirIndex != 2 && _currentDirIndex != 6);
            bool faceLeft = (_lastAimDir.x < -0.05f);

            if (t < 0.05f)
            {
                // Phase 1 (0.00s - 0.05s): Anticipation (Nén hơi & cháy ngòi nổ trước khi bắn)
                float p = t / 0.05f;
                float sx = Mathf.Lerp(1.0f, 0.94f, p) * _baseGunScale;
                float sy = Mathf.Lerp(1.0f, 1.06f, p) * _baseGunScale;
                turretGun.localScale = new Vector3(sx, sy, 1f);
                turretGun.localPosition = -_lastAimDir * (0.03f * p);

                if (useLateralFireFrames && gunRenderer != null && _cannonFireFrames[1] != null)
                {
                    gunRenderer.flipX = faceLeft;
                    gunRenderer.sprite = _cannonFireFrames[1]; // Frame 2: Anticipation
                }
            }
            else if (t < 0.14f)
            {
                // Phase 2 (0.05s - 0.14s): Recoil Kick / Squash! (Giật lùi mạnh & ép dẹp thân pháo)
                float p = (t - 0.05f) / 0.09f;
                float squashT = Mathf.Sin(p * Mathf.PI * 0.5f);
                float sx = Mathf.Lerp(0.94f, 1.22f, squashT) * _baseGunScale;
                float sy = Mathf.Lerp(1.06f, 0.82f, squashT) * _baseGunScale;
                turretGun.localScale = new Vector3(sx, sy, 1f);
                turretGun.localPosition = -_lastAimDir * Mathf.Lerp(0.03f, 0.09f, squashT);

                if (useLateralFireFrames && gunRenderer != null && _cannonFireFrames[2] != null)
                {
                    gunRenderer.flipX = faceLeft;
                    gunRenderer.sprite = _cannonFireFrames[2]; // Frame 3: Recoil Kick (Squash!)
                }
            }
            else if (t < FIRE_ANIM_DURATION)
            {
                // Phase 3 (0.14s - 0.28s): Stretch & Settle (Vươn nòng nhả khói nhẹ và đàn hồi về Idle)
                float p = (t - 0.14f) / (FIRE_ANIM_DURATION - 0.14f);
                float sx = Mathf.Lerp(0.92f, 1.0f, p) * _baseGunScale;
                float sy = Mathf.Lerp(1.10f, 1.0f, p) * _baseGunScale;
                turretGun.localScale = new Vector3(sx, sy, 1f);
                turretGun.localPosition = Vector3.Lerp(_lastAimDir * 0.03f, Vector3.zero, p);

                if (useLateralFireFrames && gunRenderer != null)
                {
                    if (p < 0.65f && _cannonFireFrames[3] != null)
                    {
                        gunRenderer.flipX = faceLeft;
                        gunRenderer.sprite = _cannonFireFrames[3]; // Frame 4: Stretch & Settle
                    }
                    else if (_cannonDirSprites != null && _cannonDirSprites[_currentDirIndex] != null)
                    {
                        gunRenderer.flipX = false;
                        gunRenderer.sprite = _cannonDirSprites[_currentDirIndex];
                    }
                }
            }
            else
            {
                // Kết thúc chu kỳ giật: Khôi phục chuẩn kích thước, vị trí và Sprite góc ngắm hiện tại
                _isFiringAnim = false;
                turretGun.localScale = new Vector3(_baseGunScale, _baseGunScale, 1f);
                turretGun.localPosition = Vector3.zero;

                if (IsKingdomRush25DTower && level == 1 && gunRenderer != null && _cannonDirSprites != null && _cannonDirSprites[_currentDirIndex] != null)
                {
                    gunRenderer.flipX = false;
                    gunRenderer.sprite = _cannonDirSprites[_currentDirIndex];
                }
            }
        }

        private void Shoot()
        {
            if (bulletPrefab == null) return;

            if (IsKingdomRush25DTower)
            {
                TriggerCannonFireAnimation();
            }

            Vector3 spawnPos = firePoint != null ? firePoint.position : (turretGun != null ? turretGun.position : transform.position);
            if (IsKingdomRush25DTower && currentTarget != null)
            {
                // Điểm xuất phát đạn/tên lửa nhô ra ở đầu miệng nòng theo hướng ngắm hiện tại
                spawnPos = transform.position + _lastAimDir * 0.38f;
            }
            Quaternion spawnRot = turretGun != null ? turretGun.rotation : Quaternion.identity;

            // Random sát thương trong khoảng [minDamage, maxDamage] chuẩn Kingdom Rush (đã bỏ chí mạng Pháo Đơn)
            float rolledDamage = Random.Range(minDamage, maxDamage + 1);

            GameObject projGO = Instantiate(bulletPrefab, spawnPos, spawnRot);
            Sprite projSprite = CyberAssetLoader.GetSprite(_currentProjectileSpriteName);

            // Bắn Rocket AoE (Pháo Tên Lửa Kingdom Rush)
            if (towerType == TowerType.Rocket)
            {
                Rocket rocket = projGO.GetComponent<Rocket>();
                if (rocket != null)
                {
                    rocket.SetProjectileVisual(
                        projSprite,
                        Color.white,
                        _currentProjectileScale,
                        _currentProjectileSpeed,
                        _currentProjectileAngleOffset
                    );
                    rocket.SetDamage(rolledDamage, splashRadius, armorPenetration);
                    if (currentTarget != null) rocket.Seek(currentTarget);
                    return;
                }
            }

            // Bắn đạn đơn mục tiêu (Pháo Đơn)
            Bullet bullet = projGO.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.SetProjectileVisual(
                    projSprite,
                    Color.white,
                    _currentProjectileScale,
                    _currentProjectileSpeed,
                    _currentProjectileAngleOffset,
                    _currentProjectileSpin,
                    360f
                );
                bullet.SetDamage(rolledDamage, armorPenetration);

                if (currentTarget != null) bullet.Seek(currentTarget);
                return;
            }

            // Fallback nếu prefab gắn Rocket
            Rocket fallbackRocket = projGO.GetComponent<Rocket>();
            if (fallbackRocket != null)
            {
                fallbackRocket.SetProjectileVisual(
                    projSprite,
                    Color.white,
                    _currentProjectileScale,
                    _currentProjectileSpeed,
                    _currentProjectileAngleOffset
                );
                fallbackRocket.SetDamage(rolledDamage, splashRadius, armorPenetration);
                if (currentTarget != null) fallbackRocket.Seek(currentTarget);
            }
        }

        /// <summary>
        /// Nâng cấp tháp lên cấp tiếp theo (Cấp 1 -> Cấp 2 -> Cấp 3 MAX),
        /// tự động đổi ụ súng, đế trụ, đạn bắn ra, cộng dồn chi phí đầu tư và tăng mạnh sát thương!
        /// </summary>
        public bool UpgradeToNextLevel(GameObject fxPrefab = null)
        {
            if (!CanUpgrade) return false;

            totalInvestedGold += upgradeCost;
            level++;
            ApplyLevelStatsAndVisuals();

            GameObject fxToSpawn = fxPrefab != null ? fxPrefab : upgradeEffectPrefab;
            if (fxToSpawn != null)
            {
                GameObject fx = Instantiate(fxToSpawn, transform.position, Quaternion.identity);
                fx.transform.localScale = Vector3.one * (level >= 3 ? 1.85f : 1.45f);
            }

            Debug.Log($"<color=yellow><b>⭐ [NÂNG CẤP CẤP {level}] {TowerTypeName} (Sát thương: {DamageRangeText}, Tầm bắn: {attackRange}, Hoàn vàng nếu bán: {SellRefundGold})</b></color>");
            return true;
        }

        /// <summary>
        /// Dọn dẹp toàn bộ hiệu ứng tia Laze & vòng tầm đánh trước khi bán/thu hồi tháp.
        /// </summary>
        public void OnSold()
        {
            DisableLaserBeam();
            SetRangeVisible(false);
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
