using UnityEngine;

namespace TowerDefense.Managers
{
    /// <summary>
    /// Quản lý hệ thống hoạt ảnh môi trường 2D độc lập nhiều khung hình (Zero-GC, tương tự hệ thống Pháo & Quái):
    /// - Map 1: Tổ hợp Xe tăng & ụ bao cát (4 frame), Lều chỉ huy + Radar xoay & sóng cyan (4 frame),
    ///          4 Cột cờ cổng căn cứ phất phới so le (4 frame), cùng lá xanh bay nhẹ.
    /// (Không dùng animation cây; toàn bộ cây, rương gỗ, tảng đá trên nền Map 1 giữ nguyên bản.)
    /// </summary>
    public class MapEnvironmentAnimator : MonoBehaviour
    {
        private struct FloatingVfxItem
        {
            public Transform Tr;
            public SpriteRenderer Sr;
            public Vector3 Origin;
            public Vector3 Velocity;
            public float Duration;
            public float Elapsed;
            public float StartScale;
            public float EndScale;
            public float MaxAlpha;
            public float BaseAngle;
            public float SpinSpeed;
            public float FlutterAmp;
            public float FlutterFreq;
        }

        private string _initializedScene;
        private Transform _vfxRoot;

        // Map 1 standalone animated prop loops
        private SpriteRenderer _tankBunkerRenderer;
        private Sprite[] _tankBunkerFrames;

        private SpriteRenderer _commandTentRenderer;
        private Sprite[] _commandTentFrames;

        private SpriteRenderer[] _flagRenderers;
        private Sprite[] _flagFrames;

        private FloatingVfxItem[] _leafPool;

        // Constant Map 1 dimensions (matches GameManager SetupMapBackground)
        private const float M1_HALF_W = 640f;
        private const float M1_HALF_H = 384f;
        private const float M1_BG_PPU = 100f;
        private const float M1_WORLD_PPU_X = 64f;
        private const float M1_WORLD_PPU_Y = 76.8f;

        private static readonly Vector2[] M1_LEAF_ORIGINS = new Vector2[]
        {
            new Vector2(210f, 95f),
            new Vector2(540f, 175f),
            new Vector2(760f, 110f),
            new Vector2(320f, 360f),
            new Vector2(880f, 290f),
            new Vector2(160f, 540f)
        };

        public void Initialize(string sceneName, Transform bgTransform, float orthoSize)
        {
            if (string.IsNullOrEmpty(sceneName) || bgTransform == null) return;
            if (_initializedScene == sceneName) return;
            _initializedScene = sceneName;

            if (sceneName == "SampleScene" || sceneName == "Map1Scene" || sceneName == "Level1Scene")
            {
                SetupMap1(bgTransform);
            }
        }

        private void SetupMap1(Transform bgTransform)
        {
            GameObject vfxRootGO = GameObject.Find("Map1_EnvVFX");
            if (vfxRootGO == null) vfxRootGO = new GameObject("Map1_EnvVFX");
            vfxRootGO.transform.position = Vector3.zero;
            vfxRootGO.transform.localScale = Vector3.one;
            _vfxRoot = vfxRootGO.transform;

            // 1. Standalone Green Tank & Sandbag Bunker (4 frames, 420x361 scaled cleanly to 232px map width)
            Vector2 tankPivot = new Vector2(0.43f, 0.41f);
            float tankTargetW = 232f;
            _tankBunkerFrames = LoadFrameSequence("Env/Map1/m1_tank_bunker_f", 4, tankPivot, tankTargetW);
            _tankBunkerRenderer = CreateStandaloneProp(bgTransform, "M1_TankBunker", _tankBunkerFrames[0],
                1056f, 543f, Vector3.one, -6);

            // 2. Standalone Command Tent + Roof Radar + Sandbags + Barrels (4 frames, 474x404 scaled to 222px map width)
            Vector2 tentPivot = new Vector2(0.50f, 0.41f);
            float tentTargetW = 222f;
            _commandTentFrames = LoadFrameSequence("Env/Map1/m1_command_tent_f", 4, tentPivot, tentTargetW);
            _commandTentRenderer = CreateStandaloneProp(bgTransform, "M1_CommandTent", _commandTentFrames[0],
                287f, 578f, Vector3.one, -6);

            // 3. Four Standalone Base Gate Flags & Silver Flagpoles (4 staggered frames, 289x585 -> 56x74 map px)
            Vector2 flagPivot = new Vector2(0.0727f, 0.0393f);
            float flagBaseW = 48f; // Gives ~97.2px natural height at PPU; scaleY adjusts cleanly to ~74px height
            _flagFrames = LoadFrameSequence("Env/Map1/m1_base_flag_f", 4, flagPivot, flagBaseW);
            float nativeH = (_flagFrames[0] != null)
                ? flagBaseW * (_flagFrames[0].texture.height / (float)_flagFrames[0].texture.width)
                : 97f;
            Vector3 flagScale = new Vector3(56f / flagBaseW, 74f / nativeH, 1f);

            _flagRenderers = new SpriteRenderer[4];
            _flagRenderers[0] = CreateStandaloneProp(bgTransform, "M1_BaseFlag_1", _flagFrames[0], 1200f, 152f, flagScale, -8);
            _flagRenderers[1] = CreateStandaloneProp(bgTransform, "M1_BaseFlag_2", _flagFrames[1], 1204f, 195f, flagScale, -7);
            _flagRenderers[2] = CreateStandaloneProp(bgTransform, "M1_BaseFlag_3", _flagFrames[2], 1202f, 239f, flagScale, -7);
            _flagRenderers[3] = CreateStandaloneProp(bgTransform, "M1_BaseFlag_4", _flagFrames[3], 1205f, 276f, flagScale, -6);

            // 4. Subtle Drifting Meadow Leaves (6 pooled sprites over open meadow)
            Sprite leafSpr = LoadRawEnvSprite("Env/Map1/m1_leaf", new Vector2(0.5f, 0.5f), M1_BG_PPU);
            _leafPool = new FloatingVfxItem[M1_LEAF_ORIGINS.Length];
            for (int i = 0; i < _leafPool.Length; i++)
            {
                Vector2 p = M1_LEAF_ORIGINS[i];
                Vector3 leafOrigin = PixelToWorld(p.x, p.y);
                Transform tr = CreateWorldVfx(leafSpr, "M1_Leaf_" + i, leafOrigin, -5, out SpriteRenderer sr);
                _leafPool[i] = new FloatingVfxItem
                {
                    Tr = tr, Sr = sr, Origin = leafOrigin,
                    Velocity = new Vector3(0.52f + (i % 2) * 0.12f, -0.26f - (i % 3) * 0.05f, 0f),
                    Duration = 4.2f + (i % 3) * 0.7f, Elapsed = i * 0.75f,
                    StartScale = 0.58f, EndScale = 0.66f, MaxAlpha = 0.88f,
                    BaseAngle = -20f + i * 35f, SpinSpeed = (i % 2 == 0 ? 28f : -24f),
                    FlutterAmp = 0.14f, FlutterFreq = 2.6f
                };
            }
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(_initializedScene)) return;

            float t = Time.time;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // 1. Tank Bunker 4-frame idle watch + exhaust smoke cycle (3.2 FPS)
            if (_tankBunkerRenderer != null && _tankBunkerFrames != null)
            {
                int idx = ((int)(t * 3.2f)) & 3;
                Sprite f = _tankBunkerFrames[idx];
                if (f != null && _tankBunkerRenderer.sprite != f) _tankBunkerRenderer.sprite = f;
            }

            // 2. Command Tent + Roof Radar + Cyan Signal Waves 4-frame cycle (3.6 FPS)
            if (_commandTentRenderer != null && _commandTentFrames != null)
            {
                int idx = ((int)(t * 3.6f)) & 3;
                Sprite f = _commandTentFrames[idx];
                if (f != null && _commandTentRenderer.sprite != f) _commandTentRenderer.sprite = f;
            }

            // 3. Base Gate 4 Standalone Flags (staggered 6.5 FPS wind ripple)
            if (_flagRenderers != null && _flagFrames != null && _flagFrames.Length == 4)
            {
                int baseFrame = (int)(t * 6.5f);
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

            // 4. Drifting Meadow Leaves
            UpdateFloatingVfxPool(_leafPool, dt);
        }

        private static void UpdateFloatingVfxPool(FloatingVfxItem[] pool, float dt)
        {
            if (pool == null) return;
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i].Elapsed += dt;
                if (pool[i].Elapsed >= pool[i].Duration)
                    pool[i].Elapsed -= pool[i].Duration;

                float p = Mathf.Clamp01(pool[i].Elapsed / pool[i].Duration);
                Transform tr = pool[i].Tr;
                SpriteRenderer sr = pool[i].Sr;
                if (tr == null || sr == null) continue;

                float flutter = pool[i].FlutterAmp > 0f
                    ? Mathf.Sin(p * Mathf.PI * 2f * pool[i].FlutterFreq) * pool[i].FlutterAmp
                    : 0f;
                tr.position = pool[i].Origin + pool[i].Velocity * (p * pool[i].Duration) + new Vector3(0f, flutter, 0f);

                float sc = Mathf.Lerp(pool[i].StartScale, pool[i].EndScale, p);
                tr.localScale = new Vector3(sc, sc, 1f);
                tr.localRotation = Quaternion.Euler(0f, 0f, pool[i].BaseAngle + p * pool[i].SpinSpeed);

                float alpha = p < 0.20f
                    ? (p / 0.20f)
                    : (1f - (p - 0.20f) / 0.80f);
                sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha) * pool[i].MaxAlpha);
            }
        }

        // ─── Helpers ────────────────────────────────────────────────────────────────

        private static Sprite[] LoadFrameSequence(string prefix, int count, Vector2 pivot, float targetMapPxWidth)
        {
            Sprite[] frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                Texture2D tex = Resources.Load<Texture2D>(prefix + (i + 1));
                if (tex == null) continue;
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                float ppu = tex.width / (targetMapPxWidth / M1_BG_PPU);
                Sprite spr = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), pivot, ppu);
                spr.name = prefix + (i + 1);
                frames[i] = spr;
            }
            return frames;
        }

        private static Sprite LoadRawEnvSprite(string resourcePath, Vector2 pivot, float ppu)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) return null;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Sprite spr = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), pivot, ppu);
            spr.name = resourcePath;
            return spr;
        }

        private static SpriteRenderer CreateStandaloneProp(Transform parentBg, string goName, Sprite initialFrame,
            float pixelX, float pixelY, Vector3 localScale, int sortingOrder)
        {
            if (initialFrame == null) return null;
            GameObject go = new GameObject(goName);
            go.transform.SetParent(parentBg, false);
            go.transform.localPosition = new Vector3((pixelX - M1_HALF_W) / M1_BG_PPU, (M1_HALF_H - pixelY) / M1_BG_PPU, 0f);
            go.transform.localScale = localScale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = initialFrame;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        private Transform CreateWorldVfx(Sprite spr, string goName, Vector3 worldPos, int sortingOrder, out SpriteRenderer sr)
        {
            sr = null;
            if (spr == null || _vfxRoot == null) return null;
            GameObject go = new GameObject(goName);
            go.transform.SetParent(_vfxRoot, false);
            go.transform.position = worldPos;
            sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.sortingOrder = sortingOrder;
            return go.transform;
        }

        private static Vector3 PixelToWorld(float px, float py)
        {
            return new Vector3((px - M1_HALF_W) / M1_WORLD_PPU_X, (M1_HALF_H - py) / M1_WORLD_PPU_Y, 0f);
        }
    }
}
