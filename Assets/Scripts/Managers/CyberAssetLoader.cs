using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.Managers
{
    /// <summary>
    /// Bộ tải và lưu đệm (Cache) toàn bộ Sprite trong bộ Redesigned_Assets (Tháp Cấp 1-3, Tháp Tím Laser,
    /// Đế trụ Cyber, Đạn, Tên Lửa, Xe Tăng + Nòng Xe Tăng, Máy Bay).
    /// Sử dụng full khung 128x128 với tâm xoay (0.5, 0.5) giúp ụ súng khớp hoàn hảo 100% vào tâm đế trụ!
    /// </summary>
    public static class CyberAssetLoader
    {
        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        public static Sprite GetSprite(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return null;

            // Bỏ đuôi .png nếu có
            if (assetName.EndsWith(".png"))
                assetName = assetName.Substring(0, assetName.Length - 4);

            if (_spriteCache.TryGetValue(assetName, out Sprite cached) && cached != null)
            {
                return cached;
            }

            // 1. Tải Texture2D từ Resources/Redesigned/
            Texture2D tex = Resources.Load<Texture2D>("Redesigned/" + assetName);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Bilinear;

                // Với bộ Pháo Đơn, Pháo Tên Lửa và Tháp Laze Kingdom Rush (2.5D Isometric kèm bệ đá),
                // đặt tâm (Pivot) đúng tâm mặt đế đá/bao cát dưới chân tháp để đứng gọn trong ô đặt súng.
                bool isLaserLvl1 = assetName.StartsWith("kr_laser_lvl1");
                bool isLaserLvl2 = assetName.StartsWith("kr_laser_lvl2");
                bool isLaserLvl3 = assetName.StartsWith("kr_laser_lvl3");
                bool isKingdomRush25DTower = assetName.StartsWith("kr_cannon_lvl")
                                          || assetName.StartsWith("kr_rocket_lvl")
                                          || isLaserLvl1 || isLaserLvl2 || isLaserLvl3
                                          || assetName == "kr_cannon_base";
                Vector2 pivot = isLaserLvl1
                    ? new Vector2(0.5f, 0.25f)
                    : (isLaserLvl2
                        ? new Vector2(0.5f, 0.16f)
                        : (isLaserLvl3
                            ? new Vector2(0.5f, 0.25f)
                            : (isKingdomRush25DTower ? new Vector2(0.5f, 0.40f) : new Vector2(0.5f, 0.5f))));
                float ppu = isKingdomRush25DTower
                    ? 112f
                    : (assetName.StartsWith("kr_enemy_") ? 105f : (assetName.StartsWith("kr_fx_explosion_") ? 110f : 100f));

                Sprite created = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    pivot,
                    ppu
                );
                created.name = assetName;
                _spriteCache[assetName] = created;
                return created;
            }

            // 2. Fallback: Tải Sprite trực tiếp nếu có
            Sprite resSprite = Resources.Load<Sprite>("Redesigned/" + assetName);
            if (resSprite != null)
            {
                _spriteCache[assetName] = resSprite;
                return resSprite;
            }

            return null;
        }

        private static Sprite _emptySpotRingSprite;
        private static Sprite _emptySpotCenterSprite;
        private static Sprite _rangeCircleSprite;
        private static Sprite _laserBeamSprite;
        private static Sprite[] _cannonLvl1DirSprites;
        private static Sprite[] _cannonLvl1FireFrames;
        private static Sprite[] _rocketLvl1DirSprites;
        private static Sprite[] _rocketLvl1FireFrames;
        private static Sprite[] _laserLvl1DirSprites;
        private static Sprite[] _laserLvl1FireFrames;
        private static Sprite[] _laserLvl2FireFrames;
        private static Sprite[] _laserLvl3FireFrames;
        private static Sprite[] _enemyTankLvl1Frames;
        private static Sprite[] _enemyTankLvl2Frames;
        private static Sprite[] _enemyTankLvl3Frames;
        private static Sprite[] _enemyPlaneLvl1Frames;
        private static Sprite[] _enemyPlaneLvl2Frames;
        private static Sprite[] _enemyPlaneLvl3Frames;
        private static Sprite[] _explosionFrames;
        private static readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite[]> _bulletFramesCache = new Dictionary<string, Sprite[]>();

        private static readonly string[] CANNON_LVL1_DIR_NAMES = new string[8]
        {
            "kr_cannon_lvl1_dir0", // 0° Right
            "kr_cannon_lvl1_dir1", // 45° Up-Right
            "kr_cannon_lvl1_dir2", // 90° Up
            "kr_cannon_lvl1_dir3", // 135° Up-Left
            "kr_cannon_lvl1_dir4", // 180° Left
            "kr_cannon_lvl1_dir5", // 225° Down-Left
            "kr_cannon_lvl1_dir6", // 270° Down
            "kr_cannon_lvl1_dir7"  // 315° Down-Right
        };

        private static readonly string[] CANNON_LVL1_FIRE_NAMES = new string[4]
        {
            "kr_cannon_lvl1_fire_f1", // Frame 1: Idle
            "kr_cannon_lvl1_fire_f2", // Frame 2: Anticipation (Fuse spark)
            "kr_cannon_lvl1_fire_f3", // Frame 3: Recoil Kick / Squash!
            "kr_cannon_lvl1_fire_f4"  // Frame 4: Stretch & Settle
        };

        private static readonly string[] ROCKET_LVL1_DIR_NAMES = new string[8]
        {
            "kr_rocket_lvl1_dir0", // 0° Right
            "kr_rocket_lvl1_dir1", // 45° Up-Right
            "kr_rocket_lvl1_dir2", // 90° Up
            "kr_rocket_lvl1_dir3", // 135° Up-Left
            "kr_rocket_lvl1_dir4", // 180° Left
            "kr_rocket_lvl1_dir5", // 225° Down-Left
            "kr_rocket_lvl1_dir6", // 270° Down
            "kr_rocket_lvl1_dir7"  // 315° Down-Right
        };

        private static readonly string[] ROCKET_LVL1_FIRE_NAMES = new string[4]
        {
            "kr_rocket_lvl1_fire_f1", // Frame 1: Idle (4 warheads loaded)
            "kr_rocket_lvl1_fire_f2", // Frame 2: Ignition (Backblast spark)
            "kr_rocket_lvl1_fire_f3", // Frame 3: Launch Kick / Squash!
            "kr_rocket_lvl1_fire_f4"  // Frame 4: Stretch & Settle (Tube smoke puff)
        };

        private static readonly string[] LASER_LVL1_DIR_NAMES = new string[8]
        {
            "kr_laser_lvl1_dir0", // 0° Right
            "kr_laser_lvl1_dir1", // 45° Up-Right
            "kr_laser_lvl1_dir2", // 90° Up
            "kr_laser_lvl1_dir3", // 135° Up-Left
            "kr_laser_lvl1_dir4", // 180° Left
            "kr_laser_lvl1_dir5", // 225° Down-Left
            "kr_laser_lvl1_dir6", // 270° Down
            "kr_laser_lvl1_dir7"  // 315° Down-Right
        };

        private static readonly string[] LASER_LVL1_FIRE_NAMES = new string[4]
        {
            "kr_laser_lvl1_fire_f1", // Frame 1: Idle (cyan orb charged)
            "kr_laser_lvl1_fire_f2", // Frame 2: Flare pulse
            "kr_laser_lvl1_fire_f3", // Frame 3: Zap Blast / Electric arc
            "kr_laser_lvl1_fire_f4"  // Frame 4: Stretch & Cooldown
        };

        private static readonly string[] LASER_LVL2_FIRE_NAMES = new string[4]
        {
            "kr_laser_lvl2_fire_f1", // Frame 1: Charge pulse
            "kr_laser_lvl2_fire_f2", // Frame 2: Rising surge
            "kr_laser_lvl2_fire_f3", // Frame 3: Peak discharge / Zap blast!
            "kr_laser_lvl2_fire_f4"  // Frame 4: Afterglow
        };

        private static readonly string[] LASER_LVL3_FIRE_NAMES = new string[4]
        {
            "kr_laser_lvl3_fire_f1", // Frame 1: Lower ring ignition
            "kr_laser_lvl3_fire_f2", // Frame 2: Ascending ring surge
            "kr_laser_lvl3_fire_f3", // Frame 3: Super Overcharge Blast!
            "kr_laser_lvl3_fire_f4"  // Frame 4: Prismatic afterglow
        };

        private static readonly string[] ENEMY_TANK_LVL1_NAMES = { "kr_enemy_tank_lvl1_f1", "kr_enemy_tank_lvl1_f2", "kr_enemy_tank_lvl1_f3", "kr_enemy_tank_lvl1_f4" };
        private static readonly string[] ENEMY_TANK_LVL2_NAMES = { "kr_enemy_tank_lvl2_f1", "kr_enemy_tank_lvl2_f2", "kr_enemy_tank_lvl2_f3", "kr_enemy_tank_lvl2_f4" };
        private static readonly string[] ENEMY_TANK_LVL3_NAMES = { "kr_enemy_tank_lvl3_f1", "kr_enemy_tank_lvl3_f2", "kr_enemy_tank_lvl3_f3", "kr_enemy_tank_lvl3_f4" };
        private static readonly string[] ENEMY_PLANE_LVL1_NAMES = { "kr_enemy_plane_lvl1_f1", "kr_enemy_plane_lvl1_f2", "kr_enemy_plane_lvl1_f3", "kr_enemy_plane_lvl1_f4" };
        private static readonly string[] ENEMY_PLANE_LVL2_NAMES = { "kr_enemy_plane_lvl2_f1", "kr_enemy_plane_lvl2_f2", "kr_enemy_plane_lvl2_f3", "kr_enemy_plane_lvl2_f4" };
        private static readonly string[] ENEMY_PLANE_LVL3_NAMES = { "kr_enemy_plane_lvl3_f1", "kr_enemy_plane_lvl3_f2", "kr_enemy_plane_lvl3_f3", "kr_enemy_plane_lvl3_f4" };
        private static readonly string[] FX_EXPLOSION_NAMES = { "kr_fx_explosion_f1", "kr_fx_explosion_f2", "kr_fx_explosion_f3", "kr_fx_explosion_f4" };

        private static Sprite[] GetOrLoadFrameSequence(ref Sprite[] cache, string[] assetNames)
        {
            if (cache != null && cache.Length == assetNames.Length && cache[0] != null)
            {
                return cache;
            }
            cache = new Sprite[assetNames.Length];
            for (int i = 0; i < assetNames.Length; i++)
            {
                cache[i] = GetSprite(assetNames[i]);
            }
            return cache;
        }

        public static Sprite[] GetEnemyTankLvl1Frames()  => GetOrLoadFrameSequence(ref _enemyTankLvl1Frames,  ENEMY_TANK_LVL1_NAMES);
        public static Sprite[] GetEnemyTankLvl2Frames()  => GetOrLoadFrameSequence(ref _enemyTankLvl2Frames,  ENEMY_TANK_LVL2_NAMES);
        public static Sprite[] GetEnemyTankLvl3Frames()  => GetOrLoadFrameSequence(ref _enemyTankLvl3Frames,  ENEMY_TANK_LVL3_NAMES);
        public static Sprite[] GetEnemyPlaneLvl1Frames() => GetOrLoadFrameSequence(ref _enemyPlaneLvl1Frames, ENEMY_PLANE_LVL1_NAMES);
        public static Sprite[] GetEnemyPlaneLvl2Frames() => GetOrLoadFrameSequence(ref _enemyPlaneLvl2Frames, ENEMY_PLANE_LVL2_NAMES);
        public static Sprite[] GetEnemyPlaneLvl3Frames() => GetOrLoadFrameSequence(ref _enemyPlaneLvl3Frames, ENEMY_PLANE_LVL3_NAMES);
        public static Sprite[] GetExplosionFrames()      => GetOrLoadFrameSequence(ref _explosionFrames,      FX_EXPLOSION_NAMES);

        /// <summary>
        /// Lấy bộ 8 Sprite góc xoay (0°..315°) của Pháo Đơn Cấp 1 phong cách Kingdom Rush (Zero-GC trong runtime).
        /// </summary>
        public static Sprite[] GetCannonLvl1DirSprites()
        {
            if (_cannonLvl1DirSprites != null && _cannonLvl1DirSprites.Length == 8 && _cannonLvl1DirSprites[0] != null)
            {
                return _cannonLvl1DirSprites;
            }

            _cannonLvl1DirSprites = new Sprite[8];
            for (int i = 0; i < 8; i++)
            {
                _cannonLvl1DirSprites[i] = GetSprite(CANNON_LVL1_DIR_NAMES[i]);
            }
            return _cannonLvl1DirSprites;
        }

        /// <summary>
        /// Lấy bộ 4 Frame Animation khi Pháo Đơn Cấp 1 khai hỏa (Idle -> Anticipation -> Squash -> Stretch & Settle).
        /// </summary>
        public static Sprite[] GetCannonLvl1FireFrames()
        {
            if (_cannonLvl1FireFrames != null && _cannonLvl1FireFrames.Length == 4 && _cannonLvl1FireFrames[0] != null)
            {
                return _cannonLvl1FireFrames;
            }

            _cannonLvl1FireFrames = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                _cannonLvl1FireFrames[i] = GetSprite(CANNON_LVL1_FIRE_NAMES[i]);
            }
            return _cannonLvl1FireFrames;
        }

        /// <summary>
        /// Lấy bộ 8 Sprite góc xoay (0°..315°) của Pháo Tên Lửa Cấp 1 phong cách Kingdom Rush (Zero-GC trong runtime).
        /// </summary>
        public static Sprite[] GetRocketLvl1DirSprites()
        {
            if (_rocketLvl1DirSprites != null && _rocketLvl1DirSprites.Length == 8 && _rocketLvl1DirSprites[0] != null)
            {
                return _rocketLvl1DirSprites;
            }

            _rocketLvl1DirSprites = new Sprite[8];
            for (int i = 0; i < 8; i++)
            {
                _rocketLvl1DirSprites[i] = GetSprite(ROCKET_LVL1_DIR_NAMES[i]);
            }
            return _rocketLvl1DirSprites;
        }

        /// <summary>
        /// Lấy bộ 4 Frame Animation khi Pháo Tên Lửa Cấp 1 khai hỏa (Idle -> Ignition -> Launch Kick/Squash -> Stretch & Settle).
        /// </summary>
        public static Sprite[] GetRocketLvl1FireFrames()
        {
            if (_rocketLvl1FireFrames != null && _rocketLvl1FireFrames.Length == 4 && _rocketLvl1FireFrames[0] != null)
            {
                return _rocketLvl1FireFrames;
            }

            _rocketLvl1FireFrames = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                _rocketLvl1FireFrames[i] = GetSprite(ROCKET_LVL1_FIRE_NAMES[i]);
            }
            return _rocketLvl1FireFrames;
        }

        /// <summary>
        /// Lấy bộ 8 Sprite góc chiếu Laze (0°..315°) của Tháp Laze Cấp 1 phong cách Kingdom Rush (Zero-GC).
        /// </summary>
        public static Sprite[] GetLaserLvl1DirSprites()
        {
            if (_laserLvl1DirSprites != null && _laserLvl1DirSprites.Length == 8 && _laserLvl1DirSprites[0] != null)
            {
                return _laserLvl1DirSprites;
            }

            _laserLvl1DirSprites = new Sprite[8];
            for (int i = 0; i < 8; i++)
            {
                _laserLvl1DirSprites[i] = GetSprite(LASER_LVL1_DIR_NAMES[i]);
            }
            return _laserLvl1DirSprites;
        }

        /// <summary>
        /// Lấy bộ 4 Frame Animation xung năng lượng khi Tháp Laze Cấp 1 bắn tia (Idle -> Flare -> Zap Blast -> Stretch & Cooldown).
        /// </summary>
        public static Sprite[] GetLaserLvl1FireFrames()
        {
            if (_laserLvl1FireFrames != null && _laserLvl1FireFrames.Length == 4 && _laserLvl1FireFrames[0] != null)
            {
                return _laserLvl1FireFrames;
            }

            _laserLvl1FireFrames = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                _laserLvl1FireFrames[i] = GetSprite(LASER_LVL1_FIRE_NAMES[i]);
            }
            return _laserLvl1FireFrames;
        }

        /// <summary>
        /// Lấy bộ 4 Frame Animation xung năng lượng khi Tháp Laze Cấp 2 bắn tia (Zero-GC).
        /// </summary>
        public static Sprite[] GetLaserLvl2FireFrames() => GetOrLoadFrameSequence(ref _laserLvl2FireFrames, LASER_LVL2_FIRE_NAMES);

        /// <summary>
        /// Lấy bộ 4 Frame Animation xung năng lượng khi Tháp Laze Cấp 3 bắn tia (Zero-GC).
        /// </summary>
        public static Sprite[] GetLaserLvl3FireFrames() => GetOrLoadFrameSequence(ref _laserLvl3FireFrames, LASER_LVL3_FIRE_NAMES);

        /// <summary>
        /// Lấy đúng chuỗi 4 Frame Animation khi Tháp Laze bắn tia theo cấp độ (1, 2, 3) (Zero-GC).
        /// </summary>
        public static Sprite[] GetLaserFireFrames(int level)
        {
            if (level >= 3) return GetLaserLvl3FireFrames();
            if (level == 2) return GetLaserLvl2FireFrames();
            return GetLaserLvl1FireFrames();
        }

        /// <summary>
        /// Lấy Sprite Tia Laze năng lượng theo Cấp độ (Level 1: Cyan, Level 2: Dual-core Tím/Lam, Level 3: Prismatic Vàng/Lam).
        /// </summary>
        public static Sprite GetLaserBeamSprite(int level = 1)
        {
            string beamName = (level == 3) ? "kr_laser_beam_lvl3" : ((level == 2) ? "kr_laser_beam_lvl2" : "kr_laser_beam_lvl1");
            return GetSprite(beamName);
        }

        /// <summary>
        /// Lấy Sprite Quả Cầu Năng Lượng Đỉnh Tháp theo Cấp độ (Level 1: Ngọc Lam, Level 2: Sấm Lam, Level 3: Prismatic Hào Quang).
        /// </summary>
        public static Sprite GetLaserOrbSprite(int level = 1)
        {
            string orbName = (level == 3) ? "kr_laser_orb_lvl3" : ((level == 2) ? "kr_laser_orb_lvl2" : "kr_laser_orb_lvl1");
            return GetSprite(orbName);
        }

        /// <summary>
        /// Tải và lưu đệm (Cache) Texture2D từ Resources/Redesigned/ phục vụ Radial Build Menu UI (Zero-GC trong runtime).
        /// </summary>
        public static Texture2D GetTexture(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return null;
            if (assetName.EndsWith(".png"))
                assetName = assetName.Substring(0, assetName.Length - 4);

            if (_textureCache.TryGetValue(assetName, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D tex = Resources.Load<Texture2D>("Redesigned/" + assetName);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Bilinear;
                _textureCache[assetName] = tex;
                return tex;
            }

            Sprite spr = Resources.Load<Sprite>("Redesigned/" + assetName);
            if (spr != null && spr.texture != null)
            {
                _textureCache[assetName] = spr.texture;
                return spr.texture;
            }

            return null;
        }

        /// <summary>
        /// Lấy chuỗi 4 frame cho từng loại đạn theo animation đường đạn (xoay hồ quang / lưỡi từ tính).
        /// </summary>
        public static Sprite[] GetBulletFrames(string prefix, int frameCount = 4)
        {
            if (string.IsNullOrEmpty(prefix)) return null;
            if (_bulletFramesCache.TryGetValue(prefix, out Sprite[] cached) && cached != null && cached.Length == frameCount)
            {
                return cached;
            }

            Sprite[] frames = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                string frameName = $"{prefix}_frame{i + 1}";
                frames[i] = GetSprite(frameName);
            }

            _bulletFramesCache[prefix] = frames;
            return frames;
        }

        /// <summary>
        /// Lấy Sprite tia Laze Tím liền mạch (Beam).
        /// </summary>
        public static Sprite GetLaserBeamSprite()
        {
            if (_laserBeamSprite != null) return _laserBeamSprite;
            _laserBeamSprite = GetSprite("laser_purple_beam");
            return _laserBeamSprite;
        }

        /// <summary>
        /// Tạo Sprite hình tròn bán trong suốt viền neon phát sáng sắc nét để hiển thị tầm đánh (Range Circle) của pháo.
        /// Kích thước texture 256x256, đường kính hình tròn là 200px (100px bán kính ở pixelsPerUnit = 100 => bán kính chuẩn 1 unit trên world space).
        /// Khi gắn vào GameObject, chỉ cần set localScale = Vector3.one * (attackRange * 2f).
        /// </summary>
        public static Sprite GetRangeCircleSprite()
        {
            if (_rangeCircleSprite != null) return _rangeCircleSprite;

            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = 100f; // Bán kính 100 pixels

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float dist = p.magnitude;
                    float diff = dist - radius;

                    Color col = Color.clear;

                    if (dist <= radius)
                    {
                        // Vùng bên trong: gradient mờ nhẹ (vùng phủ tầm đánh)
                        float innerAlpha = Mathf.Lerp(0.04f, 0.16f, dist / radius);
                        col = new Color(0.20f, 0.85f, 1f, innerAlpha);
                    }

                    // Viền phát sáng neon (Outer rim glow)
                    if (Mathf.Abs(diff) <= 3.5f)
                    {
                        float rimAlpha = 1f - (Mathf.Abs(diff) / 3.5f);
                        Color rimCol = new Color(0.40f, 0.95f, 1f, rimAlpha * 0.85f);
                        col = Color.Lerp(col, rimCol, rimAlpha);
                    }

                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            // pixelsPerUnit = 200f (vì đường kính 200px => tương đương đúng 1.0 unit bán kính!)
            _rangeCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 200f);
            _rangeCircleSprite.name = "TowerRangeCircle";
            return _rangeCircleSprite;
        }

        /// <summary>
        /// Tạo Sprite vòng tròn nét đứt phát sáng nhẹ nhàng cho Ô Đặt Pháo khi CHƯA MUA TRỤ
        /// (Để các Base Trụ tile180..183 chỉ xuất hiện SAU KHI người chơi mua pháo!).
        /// </summary>
        public static Sprite GetEmptyBuildSpotRingSprite()
        {
            if (_emptySpotRingSprite != null) return _emptySpotRingSprite;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float dist = p.magnitude;
                    float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                    if (angle < 0f) angle += 360f;

                    Color col = Color.clear;

                    // Nền tròn mờ nhẹ bên trong
                    if (dist < 42f)
                    {
                        col = new Color(0.85f, 1f, 0.95f, 0.16f * (1f - dist / 46f));
                    }

                    // Vòng tròn viền nét đứt 8 đoạn (hologram vị trí đặt pháo)
                    bool inDash = (angle % 45f) > 8f && (angle % 45f) < 37f;
                    if (inDash && dist >= 40f && dist <= 46f)
                    {
                        float edge = 1f - Mathf.Abs(dist - 43f) / 3.5f;
                        col = new Color(0.90f, 1f, 0.95f, Mathf.Clamp01(edge) * 0.88f);
                    }

                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _emptySpotRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _emptySpotRingSprite.name = "EmptyBuildSpotRing";
            return _emptySpotRingSprite;
        }

        /// <summary>
        /// Tạo biểu tượng dấu cộng (+) nhỏ ở tâm ô trống mời người chơi click đặt pháo.
        /// </summary>
        public static Sprite GetEmptyBuildSpotPlusSprite()
        {
            if (_emptySpotCenterSprite != null) return _emptySpotCenterSprite;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - half);
                    float dy = Mathf.Abs(y + 0.5f - half);
                    bool inHoriz = dy <= 3.5f && dx <= 13f;
                    bool inVert  = dx <= 3.5f && dy <= 13f;
                    pixels[y * size + x] = (inHoriz || inVert)
                        ? new Color(0.95f, 1f, 0.95f, 0.92f)
                        : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _emptySpotCenterSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _emptySpotCenterSprite.name = "EmptyBuildSpotPlus";
            return _emptySpotCenterSprite;
        }
    }
}
