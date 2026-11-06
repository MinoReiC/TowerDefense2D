using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Economy & Base (Kingdom Rush Balance)")]
        [SerializeField] private int startingGold = 150;
        [SerializeField] private int baseHealth = 20;

        public int Gold { get; private set; }
        public int BaseHealth { get; private set; }
        public bool IsGameOver => BaseHealth <= 0;
        public bool IsPaused { get; private set; } = false;

        public event Action<int> OnGoldChanged;
        public event Action<int> OnBaseHealthChanged;
        public event Action<bool> OnPauseStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Cân bằng lượng vàng khởi đầu theo từng màn chuẩn Kingdom Rush
            // (Đủ xây 2 Pháo Thường 70x2=140 hoặc 1 Pháo Tên Lửa 100 ở đầu trận)
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "Level3Scene" || sceneName == "Map3Scene")
            {
                startingGold = 250;
                baseHealth   = 30;
            }
            else if (sceneName == "Level2Scene" || sceneName == "Map2Scene")
            {
                startingGold = 200;
                baseHealth   = 25;
            }
            else
            {
                startingGold = 150;
                baseHealth   = 20;
            }

            Gold = startingGold;
            BaseHealth = baseHealth;
            IsPaused = false;
            Time.timeScale = 1f;

            // Đảm bảo các đế trụ và vật trang trí pixel cũ được dọn sạch
            CleanupFixedTurretBaseDecorations();

            // Thiết lập nền đồ họa vẽ tay chuẩn Kingdom Rush cho cả Map 1, Map 2 và Map 3
            SetupMapBackground(sceneName);
        }

        private void SetupMapBackground(string sceneName)
        {
            string bgResourceName = "Map1_Battlefield_BG";
            string bgObjectName   = "Map1_Background";
            float orthoSize       = 5.0f;

            if (sceneName == "Level2Scene" || sceneName == "Map2Scene")
            {
                bgResourceName = "Map2_Battlefield_BG";
                bgObjectName   = "Map2_Background";
                orthoSize      = 6.8f;
            }
            else if (sceneName == "Level3Scene" || sceneName == "Map3Scene")
            {
                bgResourceName = "Map3_Battlefield_BG";
                bgObjectName   = "Map3_Background";
                orthoSize      = 7.2f;
            }
            else if (sceneName != "SampleScene" && sceneName != "Map1Scene" && sceneName != "Level1Scene")
            {
                return;
            }

            // 1. Tắt TilemapRenderer cũ (nền ô vuông pixel) và Decorations cũ để hiển thị bản đồ vẽ tay mịn màng
            GameObject tilemapObj = GameObject.Find("Tilemap");
            if (tilemapObj != null)
            {
                var tmRenderer = tilemapObj.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
                if (tmRenderer != null) tmRenderer.enabled = false;
            }

            GameObject decorObj = GameObject.Find("Decorations");
            if (decorObj != null)
            {
                decorObj.SetActive(false);
            }

            // 2. Khởi tạo / Cập nhật GameObject Background
            GameObject bgObj = GameObject.Find(bgObjectName);
            if (bgObj == null)
            {
                bgObj = new GameObject(bgObjectName);
            }

            bgObj.transform.position = Vector3.zero;
            // Tỷ lệ scale khớp chuẩn 100% với viewport camera orthographic size (Rộng 4*orthoSize x Cao 2*orthoSize world units)
            float scaleRatio = orthoSize / 5.0f;
            bgObj.transform.localScale = new Vector3(1.5625f * scaleRatio, 1.3020833f * scaleRatio, 1f);

            SpriteRenderer sr = bgObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = bgObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -10;

            Texture2D tex = Resources.Load<Texture2D>(bgResourceName);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Bilinear;
                Sprite bgSprite = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                bgSprite.name = bgResourceName;
                sr.sprite = bgSprite;
            }
        }

        private void OnDestroy()
        {
            // Reset timeScale khi chuyển scene
            Time.timeScale = 1f;
        }

        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        public void SetPaused(bool paused)
        {
            if (IsGameOver) return;
            IsPaused = paused;
            Time.timeScale = IsPaused ? 0f : 1f;
            OnPauseStateChanged?.Invoke(IsPaused);
            Debug.Log($"<color=cyan>[GameManager] Trạng thái tạm dừng: {IsPaused} (Time.timeScale = {Time.timeScale})</color>");
        }

        private void CleanupFixedTurretBaseDecorations()
        {
            GameObject decorParent = GameObject.Find("Decorations");
            if (decorParent == null) return;

            SpriteRenderer[] renderers = decorParent.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr == null || sr.sprite == null) continue;
                string sName = sr.sprite.name;
                if (sName.Contains("tile180") || sName.Contains("tile181") || sName.Contains("tile182") || sName.Contains("tile183"))
                {
                    sr.enabled = false;
                }
            }
        }

        public bool CanAfford(int cost)
        {
            return Gold >= cost;
        }

        public bool SpendGold(int cost)
        {
            if (Gold >= cost)
            {
                Gold -= cost;
                OnGoldChanged?.Invoke(Gold);
                return true;
            }
            return false;
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public void TakeBaseDamage(int damage)
        {
            BaseHealth = Mathf.Max(0, BaseHealth - damage);
            OnBaseHealthChanged?.Invoke(BaseHealth);
            if (BaseHealth <= 0)
            {
                Debug.Log("<color=red><b>💀 GAME OVER! Căn cứ đã bị phá hủy!</b></color>");
            }
        }

        public void SetStartingStats(int gold, int hp)
        {
            startingGold = gold;
            baseHealth = hp;
        }
    }
}
