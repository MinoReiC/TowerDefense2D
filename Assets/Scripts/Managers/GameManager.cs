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

            // Đảm bảo các đế trụ (tile180..183) không bao giờ hiện cố định làm vật trang trí trên bản đồ
            CleanupFixedTurretBaseDecorations();
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
