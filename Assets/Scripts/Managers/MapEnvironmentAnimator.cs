using UnityEngine;

namespace TowerDefense.Managers
{
    /// <summary>
    /// Quản lý hoạt ảnh môi trường 2 lớp & VFX 60 FPS (Zero-GC) cho cả 3 Map:
    /// - Map 1: Xe tăng 2 lớp (M1_TankBase tĩnh + M1_TankTurret xoay 60 FPS + khói động cơ),
    ///          Lều chỉ huy 2 lớp (M1_TentBase tĩnh + M1_TentRadar quét đúng tâm ổ cắm + sóng cyan),
    ///          giữ nguyên 4 lá cờ đỏ tĩnh ở cổng căn cứ.
    /// - Map 2: Xe tăng sa mạc 2 lớp (+ khói động cơ), Radar lều quét trên chân đế (+ sóng cyan),
    ///          2 cột cờ đỏ độc lập 4 frame ở cổng căn cứ, gợn sóng & ánh nắng hồ ốc đảo.
    /// - Map 3: Xe tăng ngụy trang tuyết 2 lớp trong hào (+ khói lạnh động cơ),
    ///          3 cột cờ đỏ độc lập 4 frame ở cổng căn cứ, cùng 42 bông tuyết rơi nhẹ khắp màn hình.
    /// </summary>
    public class MapEnvironmentAnimator : MonoBehaviour
    {
        private struct SmokePuffItem
        {
            public Transform Tr;
            public SpriteRenderer Sr;
            public Vector3 Origin;
            public Vector3 Drift;
            public float Duration;
            public float Elapsed;
            public float StartScale;
            public float EndScale;
            public float MaxAlpha;
            public float WobbleFreq;
        }

        private struct RadarWaveItem
        {
            public Transform Tr;
            public SpriteRenderer Sr;
            public float Duration;
            public float Elapsed;
            public float TravelDist;
            public float StartScale;
            public float EndScale;
        }

        private struct WaterRippleItem
        {
            public Transform Tr;
            public SpriteRenderer Sr;
            public float Duration;
            public float Elapsed;
            public float StartScale;
            public float EndScale;
            public float MaxAlpha;
        }

        private struct WaterGlintItem
        {
            public Transform Tr;
            public SpriteRenderer Sr;
            public Vector3 Origin;
            public float Phase;
            public float Speed;
            public float BaseScaleX;
            public float BaseScaleY;
        }

        private struct SnowFlakeItem
        {
            public Transform Tr;
            public SpriteRenderer Sr;
            public float X;
            public float Y;
            public float FallSpeed;
            public float WindSpeed;
            public float SwayAmp;
            public float SwayFreq;
            public float Phase;
            public float BaseAlpha;
        }

        private string _initializedScene;
        private float _orthoScale = 1f;
        private Transform _propsRoot;

        // Shared 2-layer rotating parts (60 FPS smooth sweep)
        private Transform _tankTurretTr;
        private Transform _radarTr;

        // Zero-GC 60 FPS VFX & sprite-frame pools (populated per active scene)
        private SmokePuffItem[] _smokePool;
        private RadarWaveItem[] _wavePool;
        private Sprite[] _flagFrames;
        private SpriteRenderer[] _flagRenderers;
        private WaterRippleItem[] _waterRipples;
        private WaterGlintItem[] _waterGlints;
        private SnowFlakeItem[] _snowPool;
        private float _snowMinX, _snowMaxX, _snowMinY, _snowMaxY;

        private static readonly Vector2 FLAG_POLE_PIVOT = new Vector2(0.0885f, 0.0402f);

        // Constant 1280x768 -> World camera mapping (matches GameManager SetupMapBackground)
        private const float MAP_HALF_W = 640f;
        private const float MAP_HALF_H = 384f;
        private const float WORLD_PPU_X = 64f;
        private const float WORLD_PPU_Y = 76.8f;

        // ─── Map 1 Tuned Coordinates ────────────────────────────────────────────────
        private const float M1_TANK_PX_X = 1054f;
        private const float M1_TANK_PX_Y = 544f;
        private const float M1_TANK_WORLD_W = 3.25f;
        private const float M1_TURRET_REL_W = 0.595f;
        private static readonly Vector2 M1_TANK_RING_UV = new Vector2(0.622f, 0.718f);
        private static readonly Vector2 M1_TANK_TURRET_PIVOT = new Vector2(0.680f, 0.435f);
        private static readonly Vector2 M1_TANK_EXHAUST_UV = new Vector2(0.81f, 0.75f);

        private const float M1_TENT_PX_X = 294f;
        private const float M1_TENT_PX_Y = 572f;
        private const float M1_TENT_WORLD_W = 3.68f;
        private const float M1_RADAR_REL_W = 0.22f;
        // Centered squarely inside tent grey circular roof collar (fixes right-edge offset)
        private static readonly Vector2 M1_TENT_COLLAR_UV = new Vector2(0.347f, 0.772f);
        private static readonly Vector2 M1_TENT_RADAR_PIVOT = new Vector2(0.246f, 0.052f);
        private static readonly Vector2 M1_TENT_HORN_UV = new Vector2(0.76f, 0.78f);

        // ─── Map 2 Tuned Coordinates ────────────────────────────────────────────────
        private const float M2_TANK_PX_X = 136f;
        private const float M2_TANK_PX_Y = 664f;
        private const float M2_TANK_WORLD_W = 4.18f;
        private const float M2_TURRET_REL_W = 0.520f;
        private static readonly Vector2 M2_TANK_RING_UV = new Vector2(0.392f, 0.684f);
        private static readonly Vector2 M2_TANK_TURRET_PIVOT = new Vector2(0.305f, 0.435f);
        private static readonly Vector2 M2_TANK_EXHAUST_UV = new Vector2(0.14f, 0.72f);

        private const float M2_RADAR_PX_X = 269f;
        private const float M2_RADAR_PX_Y = 83f;
        private const float M2_RADAR_WORLD_W = 0.62f;
        private static readonly Vector2 M2_RADAR_PIVOT = new Vector2(0.252f, 0.045f);
        private static readonly Vector2 M2_RADAR_HORN_UV = new Vector2(0.84f, 0.80f);

        private static readonly Vector2[] M2_WATER_RIPPLE_PTS = new Vector2[]
        {
            new Vector2(890f, 62f),
            new Vector2(938f, 114f),
            new Vector2(1018f, 52f),
            new Vector2(1048f, 86f),
            new Vector2(1084f, 58f),
            new Vector2(1005f, 118f),
            new Vector2(1045f, 58f)
        };

        private static readonly Vector2[] M2_WATER_GLINT_PTS = new Vector2[]
        {
            new Vector2(930f, 42f),
            new Vector2(1015f, 45f),
            new Vector2(1065f, 65f),
            new Vector2(1035f, 95f),
            new Vector2(985f, 118f),
            new Vector2(1078f, 92f),
            new Vector2(1118f, 68f),
            new Vector2(965f, 98f)
        };

        // ─── Map 3 Tuned Coordinates ────────────────────────────────────────────────
        private const float M3_TANK_PX_X = 117f;
        private const float M3_TANK_PX_Y = 656f;
        private const float M3_TANK_WORLD_W = 2.34f;
        private const float M3_TURRET_REL_W = 0.685f;
        private static readonly Vector2 M3_TANK_RING_UV = new Vector2(0.486f, 0.674f);
        private static readonly Vector2 M3_TANK_TURRET_PIVOT = new Vector2(0.305f, 0.365f);
        private static readonly Vector2 M3_TANK_EXHAUST_UV = new Vector2(0.25f, 0.47f);

        public void Initialize(string sceneName, Transform bgTransform, float orthoSize)
        {
            if (string.IsNullOrEmpty(sceneName) || bgTransform == null) return;
            if (_initializedScene == sceneName) return;
            _initializedScene = sceneName;
            _orthoScale = orthoSize > 0.01f ? (orthoSize / 5.0f) : 1f;

            string rootName = sceneName + "_EnvProps";
            GameObject rootGO = GameObject.Find(rootName);
            if (rootGO == null) rootGO = new GameObject(rootName);
            rootGO.transform.position = Vector3.zero;
            rootGO.transform.rotation = Quaternion.identity;
            rootGO.transform.localScale = Vector3.one;
            _propsRoot = rootGO.transform;

            if (sceneName == "SampleScene" || sceneName == "Map1Scene" || sceneName == "Level1Scene")
            {
                SetupMap1();
            }
            else if (sceneName == "Level2Scene" || sceneName == "Map2Scene")
            {
                SetupMap2();
            }
            else if (sceneName == "Level3Scene" || sceneName == "Map3Scene")
            {
                SetupMap3();
            }
        }

        // ─── SCENE SETUPS ───────────────────────────────────────────────────────────

        private void SetupMap1()
        {
            float s = _orthoScale;
            Vector2 centerPivot = new Vector2(0.5f, 0.5f);

            // 1. Tank Bunker 2-layer + Exhaust Smoke
            float tankW = M1_TANK_WORLD_W * s;
            Sprite tankBaseSpr = LoadUniformWorldSprite("Env/Map1/m1_tank_base", centerPivot, tankW);
            Sprite tankTurretSpr = LoadUniformWorldSprite("Env/Map1/m1_tank_turret", M1_TANK_TURRET_PIVOT, tankW * M1_TURRET_REL_W);
            if (tankBaseSpr != null)
            {
                Transform tankBaseTr = CreateWorldProp(_propsRoot, "M1_TankBase", tankBaseSpr,
                    PixelToWorld(M1_TANK_PX_X, M1_TANK_PX_Y, s), -7);
                if (tankTurretSpr != null && tankBaseTr != null)
                {
                    Vector3 ringOff = ComputeChildMountOffset(M1_TANK_RING_UV, centerPivot, tankW, tankBaseSpr);
                    _tankTurretTr = CreateChildProp(tankBaseTr, "M1_TankTurret", tankTurretSpr, ringOff, -6);
                }

                Vector3 exhaustOff = ComputeChildMountOffset(M1_TANK_EXHAUST_UV, centerPivot, tankW, tankBaseSpr);
                SpawnSmokePool(tankBaseTr.position + exhaustOff, new Color(0.76f, 0.78f, 0.72f, 1f),
                    baseScale: 0.24f * s, endScale: 0.56f * s, driftX: 0.24f * s, driftY: 0.42f * s);
            }

            // 2. Command Tent 2-layer + Socket-Centered Radar + Cyan Signal Waves
            float tentW = M1_TENT_WORLD_W * s;
            Sprite tentBaseSpr = LoadUniformWorldSprite("Env/Map1/m1_tent_base", centerPivot, tentW);
            float radarW = tentW * M1_RADAR_REL_W;
            Sprite tentRadarSpr = LoadUniformWorldSprite("Env/Map1/m1_tent_radar", M1_TENT_RADAR_PIVOT, radarW);
            if (tentBaseSpr != null)
            {
                Transform tentBaseTr = CreateWorldProp(_propsRoot, "M1_TentBase", tentBaseSpr,
                    PixelToWorld(M1_TENT_PX_X, M1_TENT_PX_Y, s), -7);
                if (tentRadarSpr != null && tentBaseTr != null)
                {
                    Vector3 collarOff = ComputeChildMountOffset(M1_TENT_COLLAR_UV, centerPivot, tentW, tentBaseSpr);
                    _radarTr = CreateChildProp(tentBaseTr, "M1_TentRadar", tentRadarSpr, collarOff, -6);
                    Vector3 hornOff = ComputeChildMountOffset(M1_TENT_HORN_UV, M1_TENT_RADAR_PIVOT, radarW, tentRadarSpr);
                    SpawnRadarWavePool(_radarTr, hornOff, aimAngleDeg: 38f, waveScale: 0.22f * s, travelDist: 0.78f * s);
                }
            }
        }

        private void SetupMap2()
        {
            float s = _orthoScale;
            Vector2 centerPivot = new Vector2(0.5f, 0.5f);

            // 1. Desert Tank 2-layer + Exhaust Smoke
            float tankW = M2_TANK_WORLD_W * s;
            Sprite tankBaseSpr = LoadUniformWorldSprite("Env/Map2/m2_tank_base", centerPivot, tankW);
            Sprite tankTurretSpr = LoadUniformWorldSprite("Env/Map2/m2_tank_turret", M2_TANK_TURRET_PIVOT, tankW * M2_TURRET_REL_W);
            if (tankBaseSpr != null)
            {
                Transform tankBaseTr = CreateWorldProp(_propsRoot, "M2_TankBase", tankBaseSpr,
                    PixelToWorld(M2_TANK_PX_X, M2_TANK_PX_Y, s), -7);
                if (tankTurretSpr != null && tankBaseTr != null)
                {
                    Vector3 ringOff = ComputeChildMountOffset(M2_TANK_RING_UV, centerPivot, tankW, tankBaseSpr);
                    _tankTurretTr = CreateChildProp(tankBaseTr, "M2_TankTurret", tankTurretSpr, ringOff, -6);
                }

                Vector3 exhaustOff = ComputeChildMountOffset(M2_TANK_EXHAUST_UV, centerPivot, tankW, tankBaseSpr);
                SpawnSmokePool(tankBaseTr.position + exhaustOff, new Color(0.78f, 0.74f, 0.68f, 1f),
                    baseScale: 0.24f * s, endScale: 0.54f * s, driftX: -0.20f * s, driftY: 0.40f * s);
            }

            // 2. Tent Radar Dish mounted on original pedestal + Cyan Signal Waves
            float radarW = M2_RADAR_WORLD_W * s;
            Sprite radarSpr = LoadUniformWorldSprite("Env/Map2/m2_radar_dish", M2_RADAR_PIVOT, radarW);
            if (radarSpr != null)
            {
                _radarTr = CreateWorldProp(_propsRoot, "M2_TentRadar", radarSpr,
                    PixelToWorld(M2_RADAR_PX_X, M2_RADAR_PX_Y, s), -6);
                Vector3 hornOff = ComputeChildMountOffset(M2_RADAR_HORN_UV, M2_RADAR_PIVOT, radarW, radarSpr);
                SpawnRadarWavePool(_radarTr, hornOff, aimAngleDeg: 38f, waveScale: 0.20f * s, travelDist: 0.70f * s);
            }

            // 3. Two Simple Standalone Gate Flags & Poles (48x56 px at ~70.6 px/wu, staggered 4 frames)
            _flagFrames = LoadFlagFrameSequence("Env/Map2/m2_flag_f", 4, FLAG_POLE_PIVOT, 0.68f * s);
            if (_flagFrames != null && _flagFrames[0] != null)
            {
                _flagRenderers = new SpriteRenderer[2];
                _flagRenderers[0] = CreateWorldProp(_propsRoot, "M2_Flag_1", _flagFrames[0],
                    PixelToWorld(953f, 344f, s), -6).GetComponent<SpriteRenderer>();
                _flagRenderers[1] = CreateWorldProp(_propsRoot, "M2_Flag_2", _flagFrames[1],
                    PixelToWorld(1178f, 339f, s), -6).GetComponent<SpriteRenderer>();
            }

            // 4. Oasis Lake Water Ripples & Shimmer Glints
            SpawnOasisWaterPool(s);
        }

        private void SetupMap3()
        {
            float s = _orthoScale;
            Vector2 centerPivot = new Vector2(0.5f, 0.5f);

            // 1. Snow-Camo Tank 2-layer inside trench + Cold Exhaust Smoke
            float tankW = M3_TANK_WORLD_W * s;
            Sprite tankBaseSpr = LoadUniformWorldSprite("Env/Map3/m3_tank_base", centerPivot, tankW);
            Sprite tankTurretSpr = LoadUniformWorldSprite("Env/Map3/m3_tank_turret", M3_TANK_TURRET_PIVOT, tankW * M3_TURRET_REL_W);
            if (tankBaseSpr != null)
            {
                Transform tankBaseTr = CreateWorldProp(_propsRoot, "M3_TankBase", tankBaseSpr,
                    PixelToWorld(M3_TANK_PX_X, M3_TANK_PX_Y, s), -7);
                if (tankTurretSpr != null && tankBaseTr != null)
                {
                    Vector3 ringOff = ComputeChildMountOffset(M3_TANK_RING_UV, centerPivot, tankW, tankBaseSpr);
                    _tankTurretTr = CreateChildProp(tankBaseTr, "M3_TankTurret", tankTurretSpr, ringOff, -6);
                }

                Vector3 exhaustOff = ComputeChildMountOffset(M3_TANK_EXHAUST_UV, centerPivot, tankW, tankBaseSpr);
                SpawnSmokePool(tankBaseTr.position + exhaustOff, new Color(0.84f, 0.89f, 0.94f, 1f),
                    baseScale: 0.19f * s, endScale: 0.46f * s, driftX: -0.22f * s, driftY: 0.38f * s);
            }

            // 2. Three Simple Standalone Red Base Gate Flags & Poles (34x40 px at ~70.8 px/wu, staggered 4 frames)
            _flagFrames = LoadFlagFrameSequence("Env/Map3/m3_flag_f", 4, FLAG_POLE_PIVOT, 0.56f * s);
            if (_flagFrames != null && _flagFrames[0] != null)
            {
                _flagRenderers = new SpriteRenderer[3];

                Transform f1 = CreateWorldProp(_propsRoot, "M3_Flag_1", _flagFrames[0],
                    PixelToWorld(1044f, 534f, s), -6);
                _flagRenderers[0] = f1.GetComponent<SpriteRenderer>();

                Transform f2 = CreateWorldProp(_propsRoot, "M3_Flag_2", _flagFrames[1],
                    PixelToWorld(1110f, 532f, s), -6);
                f2.localScale = new Vector3(0.64f, 0.64f, 1f);
                _flagRenderers[1] = f2.GetComponent<SpriteRenderer>();

                Transform f3 = CreateWorldProp(_propsRoot, "M3_Flag_3", _flagFrames[2],
                    PixelToWorld(1151f, 635f, s), -6);
                f3.localScale = new Vector3(0.74f, 0.74f, 1f);
                _flagRenderers[2] = f3.GetComponent<SpriteRenderer>();
            }

            // 3. Gentle Falling Snow (42 pooled snowflakes over viewport)
            SpawnSnowPool(42, s);
        }

        // ─── ZERO-GC 60 FPS UPDATE ──────────────────────────────────────────────────

        private void Update()
        {
            if (string.IsNullOrEmpty(_initializedScene)) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float t = Time.time;

            // 1. Tank Turret 60 FPS watch sweep (+-8 deg smooth eased dwell)
            if (_tankTurretTr != null)
            {
                float s = Mathf.Sin(t * 0.78f);
                float eased = s * (1.25f - 0.25f * s * s);
                _tankTurretTr.localRotation = Quaternion.Euler(0f, 0f, eased * 8.0f);
            }

            // 2. Radar Dish 60 FPS pan (+-14 deg + 2.5D foreshortening)
            if (_radarTr != null)
            {
                float w = Mathf.Sin(t * 1.15f + 0.7f);
                _radarTr.localRotation = Quaternion.Euler(0f, 0f, w * 14.0f);
                float sx = 1f - 0.08f * Mathf.Abs(w);
                _radarTr.localScale = new Vector3(sx, 1f, 1f);
            }

            // 3. Tank Exhaust Smoke
            if (_smokePool != null)
                UpdateSmokePool(dt);

            // 4. Radar Cyan Signal Waves
            if (_wavePool != null)
                UpdateRadarWavePool(dt);

            // 5. Standalone Gate Flags (relaxed 5.5 FPS staggered wind loop across 4 frames, Zero-GC)
            if (_flagRenderers != null && _flagFrames != null && _flagFrames.Length == 4)
            {
                int baseFrame = (int)(t * 5.5f);
                for (int i = 0; i < _flagRenderers.Length; i++)
                {
                    SpriteRenderer sr = _flagRenderers[i];
                    if (sr != null)
                    {
                        Sprite f = _flagFrames[(baseFrame + i) & 3];
                        if (f != null && sr.sprite != f) sr.sprite = f;
                    }
                }
            }

            // 6. Map 2 Oasis Water Ripples & Shimmer Glints
            if (_waterRipples != null)
                UpdateWaterRipples(dt);
            if (_waterGlints != null)
                UpdateWaterGlints(t);

            // 7. Map 3 Falling Snow
            if (_snowPool != null)
                UpdateSnowPool(t, dt);
        }

        private void UpdateSmokePool(float dt)
        {
            for (int i = 0; i < _smokePool.Length; i++)
            {
                _smokePool[i].Elapsed += dt;
                if (_smokePool[i].Elapsed >= _smokePool[i].Duration)
                    _smokePool[i].Elapsed -= _smokePool[i].Duration;

                float p = Mathf.Clamp01(_smokePool[i].Elapsed / _smokePool[i].Duration);
                Transform tr = _smokePool[i].Tr;
                SpriteRenderer sr = _smokePool[i].Sr;
                if (tr == null || sr == null) continue;

                float curl = Mathf.Sin(p * Mathf.PI * 2f * _smokePool[i].WobbleFreq) * 0.05f * _orthoScale;
                tr.position = _smokePool[i].Origin + _smokePool[i].Drift * (p * _smokePool[i].Duration)
                    + new Vector3(curl, 0f, 0f);
                float sc = Mathf.Lerp(_smokePool[i].StartScale, _smokePool[i].EndScale, p);
                tr.localScale = new Vector3(sc, sc * 0.85f, 1f);

                float alpha = p < 0.22f ? (p / 0.22f) : (1f - (p - 0.22f) / 0.78f);
                Color c = sr.color;
                c.a = Mathf.Clamp01(alpha) * _smokePool[i].MaxAlpha;
                sr.color = c;
            }
        }

        private void UpdateRadarWavePool(float dt)
        {
            for (int i = 0; i < _wavePool.Length; i++)
            {
                _wavePool[i].Elapsed += dt;
                if (_wavePool[i].Elapsed >= _wavePool[i].Duration)
                    _wavePool[i].Elapsed -= _wavePool[i].Duration;

                float p = Mathf.Clamp01(_wavePool[i].Elapsed / _wavePool[i].Duration);
                Transform tr = _wavePool[i].Tr;
                SpriteRenderer sr = _wavePool[i].Sr;
                if (tr == null || sr == null) continue;

                tr.localPosition = new Vector3(_wavePool[i].TravelDist * p, 0f, 0f);
                float sc = Mathf.Lerp(_wavePool[i].StartScale, _wavePool[i].EndScale, p);
                tr.localScale = new Vector3(sc, sc, 1f);

                float alpha = p < 0.16f ? (p / 0.16f) : (1f - (p - 0.16f) / 0.84f);
                sr.color = new Color(0.55f, 0.96f, 1f, Mathf.Clamp01(alpha) * 0.82f);
            }
        }

        private void UpdateWaterRipples(float dt)
        {
            for (int i = 0; i < _waterRipples.Length; i++)
            {
                _waterRipples[i].Elapsed += dt;
                if (_waterRipples[i].Elapsed >= _waterRipples[i].Duration)
                    _waterRipples[i].Elapsed -= _waterRipples[i].Duration;

                float p = Mathf.Clamp01(_waterRipples[i].Elapsed / _waterRipples[i].Duration);
                Transform tr = _waterRipples[i].Tr;
                SpriteRenderer sr = _waterRipples[i].Sr;
                if (tr == null || sr == null) continue;

                float sc = Mathf.Lerp(_waterRipples[i].StartScale, _waterRipples[i].EndScale, p);
                tr.localScale = new Vector3(sc, sc, 1f);

                float alpha = p < 0.25f ? (p / 0.25f) : (1f - (p - 0.25f) / 0.75f);
                sr.color = new Color(0.85f, 0.97f, 1f, Mathf.Clamp01(alpha) * _waterRipples[i].MaxAlpha);
            }
        }

        private void UpdateWaterGlints(float t)
        {
            for (int i = 0; i < _waterGlints.Length; i++)
            {
                Transform tr = _waterGlints[i].Tr;
                SpriteRenderer sr = _waterGlints[i].Sr;
                if (tr == null || sr == null) continue;

                float wave = Mathf.Sin(t * _waterGlints[i].Speed + _waterGlints[i].Phase);
                tr.position = _waterGlints[i].Origin + new Vector3(wave * 0.14f * _orthoScale, 0f, 0f);
                float pulse = 0.75f + 0.25f * Mathf.Sin(t * (_waterGlints[i].Speed * 1.4f) + _waterGlints[i].Phase);
                tr.localScale = new Vector3(_waterGlints[i].BaseScaleX * pulse, _waterGlints[i].BaseScaleY, 1f);
                sr.color = new Color(0.95f, 0.99f, 1f, 0.25f + 0.35f * Mathf.Abs(wave));
            }
        }

        private void UpdateSnowPool(float t, float dt)
        {
            float spanX = _snowMaxX - _snowMinX;
            float spanY = _snowMaxY - _snowMinY;
            for (int i = 0; i < _snowPool.Length; i++)
            {
                float y = _snowPool[i].Y - _snowPool[i].FallSpeed * dt;
                float x = _snowPool[i].X + (_snowPool[i].WindSpeed + Mathf.Sin(t * _snowPool[i].SwayFreq + _snowPool[i].Phase) * _snowPool[i].SwayAmp) * dt;

                if (y < _snowMinY) y += spanY;
                if (x > _snowMaxX) x -= spanX;
                else if (x < _snowMinX) x += spanX;

                _snowPool[i].X = x;
                _snowPool[i].Y = y;

                if (_snowPool[i].Tr != null)
                    _snowPool[i].Tr.position = new Vector3(x, y, 0f);
            }
        }

        // ─── SPAWN POOLS & PROCEDURAL SPRITES ───────────────────────────────────────

        private void SpawnSmokePool(Vector3 worldOrigin, Color tint, float baseScale, float endScale, float driftX, float driftY)
        {
            Sprite smokeSpr = BuildSmokePuffSprite();
            _smokePool = new SmokePuffItem[5];
            for (int i = 0; i < _smokePool.Length; i++)
            {
                Transform tr = CreateWorldProp(_propsRoot, "Env_Smoke_" + i, smokeSpr, worldOrigin, -5);
                SpriteRenderer sr = tr.GetComponent<SpriteRenderer>();
                sr.color = new Color(tint.r, tint.g, tint.b, 0f);
                _smokePool[i] = new SmokePuffItem
                {
                    Tr = tr, Sr = sr,
                    Origin = worldOrigin + new Vector3(((i % 2) - 0.5f) * 0.04f * _orthoScale, 0f, 0f),
                    Drift = new Vector3(driftX + (i % 3 - 1) * 0.05f * _orthoScale, driftY + (i % 2) * 0.06f * _orthoScale, 0f),
                    Duration = 1.6f + (i % 2) * 0.25f,
                    Elapsed = i * 0.34f,
                    StartScale = baseScale,
                    EndScale = endScale,
                    MaxAlpha = 0.45f,
                    WobbleFreq = 1.5f + (i % 2) * 0.4f
                };
            }
        }

        private void SpawnRadarWavePool(Transform radarTr, Vector3 localHornPos, float aimAngleDeg, float waveScale, float travelDist)
        {
            GameObject emitterGO = new GameObject("RadarWaveEmitter");
            emitterGO.transform.SetParent(radarTr, false);
            emitterGO.transform.localPosition = localHornPos;
            emitterGO.transform.localRotation = Quaternion.Euler(0f, 0f, aimAngleDeg);
            emitterGO.transform.localScale = Vector3.one;

            Sprite waveSpr = BuildRadarWaveSprite();
            _wavePool = new RadarWaveItem[3];
            for (int i = 0; i < _wavePool.Length; i++)
            {
                Transform wTr = CreateChildProp(emitterGO.transform, "RadarWave_" + i, waveSpr, Vector3.zero, -5);
                SpriteRenderer sr = wTr.GetComponent<SpriteRenderer>();
                sr.color = new Color(0.55f, 0.96f, 1f, 0f);
                _wavePool[i] = new RadarWaveItem
                {
                    Tr = wTr, Sr = sr,
                    Duration = 1.45f,
                    Elapsed = i * (1.45f / 3f),
                    TravelDist = travelDist,
                    StartScale = waveScale * 0.48f,
                    EndScale = waveScale * 1.45f
                };
            }
        }

        private void SpawnOasisWaterPool(float s)
        {
            Sprite rippleSpr = BuildWaterRippleSprite();
            _waterRipples = new WaterRippleItem[M2_WATER_RIPPLE_PTS.Length];
            for (int i = 0; i < M2_WATER_RIPPLE_PTS.Length; i++)
            {
                Vector2 p = M2_WATER_RIPPLE_PTS[i];
                Transform tr = CreateWorldProp(_propsRoot, "M2_WaterRipple_" + i, rippleSpr, PixelToWorld(p.x, p.y, s), -8);
                SpriteRenderer sr = tr.GetComponent<SpriteRenderer>();
                sr.color = new Color(0.85f, 0.97f, 1f, 0f);
                _waterRipples[i] = new WaterRippleItem
                {
                    Tr = tr, Sr = sr,
                    Duration = 2.0f + (i % 3) * 0.35f,
                    Elapsed = i * 0.38f,
                    StartScale = (0.28f + (i % 2) * 0.06f) * s,
                    EndScale = (0.86f + (i % 2) * 0.14f) * s,
                    MaxAlpha = 0.52f
                };
            }

            Sprite glintSpr = BuildWaterGlintSprite();
            _waterGlints = new WaterGlintItem[M2_WATER_GLINT_PTS.Length];
            for (int i = 0; i < M2_WATER_GLINT_PTS.Length; i++)
            {
                Vector2 p = M2_WATER_GLINT_PTS[i];
                Vector3 wp = PixelToWorld(p.x, p.y, s);
                Transform tr = CreateWorldProp(_propsRoot, "M2_WaterGlint_" + i, glintSpr, wp, -8);
                SpriteRenderer sr = tr.GetComponent<SpriteRenderer>();
                _waterGlints[i] = new WaterGlintItem
                {
                    Tr = tr, Sr = sr, Origin = wp,
                    Phase = i * 0.92f,
                    Speed = 1.6f + (i % 3) * 0.35f,
                    BaseScaleX = (0.48f + (i % 3) * 0.12f) * s,
                    BaseScaleY = 0.34f * s
                };
            }
        }

        private void SpawnSnowPool(int count, float s)
        {
            _snowMinX = -10.5f * s;
            _snowMaxX = 10.5f * s;
            _snowMinY = -5.4f * s;
            _snowMaxY = 5.4f * s;

            Sprite snowSpr = BuildSnowflakeSprite();
            _snowPool = new SnowFlakeItem[count];
            for (int i = 0; i < count; i++)
            {
                // Deterministic low-discrepancy distribution so snow covers Map 3 evenly on frame 1
                float rx = ((i * 37) % 100) / 100f;
                float ry = ((i * 53 + 17) % 100) / 100f;
                float x = Mathf.Lerp(_snowMinX, _snowMaxX, rx);
                float y = Mathf.Lerp(_snowMinY, _snowMaxY, ry);

                Transform tr = CreateWorldProp(_propsRoot, "M3_Snow_" + i, snowSpr, new Vector3(x, y, 0f), 12);
                float sc = (0.28f + (i % 4) * 0.09f) * s;
                tr.localScale = new Vector3(sc, sc, 1f);
                SpriteRenderer sr = tr.GetComponent<SpriteRenderer>();
                float alpha = 0.42f + (i % 4) * 0.09f;
                sr.color = new Color(0.95f, 0.98f, 1f, alpha);

                _snowPool[i] = new SnowFlakeItem
                {
                    Tr = tr, Sr = sr, X = x, Y = y,
                    FallSpeed = (0.72f + (i % 5) * 0.16f) * s,
                    WindSpeed = (0.24f + (i % 3) * 0.09f) * s,
                    SwayAmp = (0.18f + (i % 3) * 0.08f) * s,
                    SwayFreq = 1.2f + (i % 4) * 0.25f,
                    Phase = i * 0.71f,
                    BaseAlpha = alpha
                };
            }
        }

        // ─── PROCEDURAL TEXTURE BUILDERS ────────────────────────────────────────────

        private static Sprite BuildSmokePuffSprite()
        {
            const int S = 24;
            Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float nx = (x + 0.5f) / S * 2f - 1f;
                    float ny = (y + 0.5f) / S * 2f - 1f;
                    float d1 = Mathf.Sqrt(nx * nx + ny * ny);
                    float d2 = Mathf.Sqrt((nx - 0.25f) * (nx - 0.25f) + (ny + 0.15f) * (ny + 0.15f));
                    float a = Mathf.Clamp01(1f - Mathf.Min(d1, d2 * 1.15f));
                    a = a * a * (3f - 2f * a);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 24f);
        }

        private static Sprite BuildRadarWaveSprite()
        {
            const int S = 32;
            Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            // Curved cyan signal arc bowing out to +X centered around left edge
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S;         // 0..1 left -> right
                    float v = (y + 0.5f) / S * 2f - 1f; // -1..+1
                    float targetX = 0.72f - 0.45f * v * v;
                    float dist = Mathf.Abs(u - targetX);
                    float vFade = Mathf.Clamp01(1f - Mathf.Abs(v) * 1.04f);
                    float core = Mathf.Clamp01(1f - dist / 0.10f);
                    float glow = Mathf.Clamp01(1f - dist / 0.24f) * 0.42f;
                    float a = Mathf.Clamp01((core + glow) * vFade * vFade);
                    tex.SetPixel(x, y, new Color(0.55f, 0.96f, 1f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite BuildWaterRippleSprite()
        {
            const int W = 32, H = 16;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float nx = (x + 0.5f) / W * 2f - 1f;
                    float ny = (y + 0.5f) / H * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.74f) / 0.16f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, ring * ring));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite BuildWaterGlintSprite()
        {
            const int W = 24, H = 8;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float nx = (x + 0.5f) / W * 2f - 1f;
                    float ny = (y + 0.5f) / H * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.Clamp01(1f - r);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 24f);
        }

        private static Sprite BuildSnowflakeSprite()
        {
            const int S = 12;
            Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float nx = (x + 0.5f) / S * 2f - 1f;
                    float ny = (y + 0.5f) / S * 2f - 1f;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 24f);
        }

        // ─── TRANSFORM & SPRITE HELPERS ─────────────────────────────────────────────

        private static Sprite[] LoadFlagFrameSequence(string prefix, int count, Vector2 pivotUV, float targetWorldWidth)
        {
            Sprite[] frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = LoadUniformWorldSprite(prefix + (i + 1), pivotUV, targetWorldWidth);
            }
            return frames;
        }

        private static Sprite LoadUniformWorldSprite(string resourcePath, Vector2 pivotUV, float targetWorldWidth)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) return null;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float ppu = tex.width / Mathf.Max(0.01f, targetWorldWidth);
            Sprite spr = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), pivotUV, ppu);
            spr.name = resourcePath;
            return spr;
        }

        private static Transform CreateWorldProp(Transform parent, string goName, Sprite sprite, Vector3 worldPos, int sortingOrder)
        {
            GameObject go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            return go.transform;
        }

        private static Transform CreateChildProp(Transform parent, string goName, Sprite sprite, Vector3 localOffset, int sortingOrder)
        {
            GameObject go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localOffset;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            return go.transform;
        }

        private static Vector3 ComputeChildMountOffset(Vector2 mountUV, Vector2 basePivotUV, float baseWorldW, Sprite baseSprite)
        {
            float baseWorldH = baseWorldW * (baseSprite.texture.height / (float)baseSprite.texture.width);
            return new Vector3(
                (mountUV.x - basePivotUV.x) * baseWorldW,
                (mountUV.y - basePivotUV.y) * baseWorldH,
                0f
            );
        }

        private static Vector3 PixelToWorld(float px, float py, float orthoScale)
        {
            return new Vector3(
                ((px - MAP_HALF_W) / WORLD_PPU_X) * orthoScale,
                ((MAP_HALF_H - py) / WORLD_PPU_Y) * orthoScale,
                0f
            );
        }
    }
}
