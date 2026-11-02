using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Waypoints;
using TowerDefense.Enemies;

namespace TowerDefense.Managers
{
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Mode & Waves")]
        [Tooltip("Nếu tích chọn, quái sẽ ra vô tận để test")]
        [SerializeField] private bool endlessMode = false;
        [SerializeField] private int totalWaves = 5;

        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject standardEnemyPrefab; // Xe tăng thường (Round 1+)
        [SerializeField] private GameObject fastEnemyPrefab;     // Xe nhanh (Round 2+)
        [SerializeField] private GameObject heavyEnemyPrefab;    // Xe hạng nặng (Round 3+)
        [SerializeField] private GameObject armoredEnemyPrefab;  // Xe bọc thép (Round 4+)
        [SerializeField] private GameObject bossEnemyPrefab;     // Siêu xe tăng Boss (Round 5+)

        [Header("Path Reference")]
        [SerializeField] private WaypointPath waypointPath;

        [Header("Timing")]
        [SerializeField] private float timeBetweenWaves = 4f;
        [SerializeField] private float defaultSpawnInterval = 1.1f;

        private int waveNumber = 0;
        private float countdown = 0.5f;
        private int aliveEnemiesCount = 0;
        private bool isSpawning = false;
        private bool hasWon = false;
        private bool isWaitingForWaveCommand = true;

        public int CurrentWave => waveNumber;
        public int TotalWaves => totalWaves;
        public bool IsEndless => endlessMode;
        public int AliveEnemies => aliveEnemiesCount;
        public bool HasWon => hasWon;
        public bool IsPlayerReady => !isWaitingForWaveCommand;
        public bool IsWaitingForWaveCommand => isWaitingForWaveCommand;

        private void Start()
        {
            countdown = 0.4f;
            isWaitingForWaveCommand = true;
        }

        /// <summary>
        /// Được gọi khi người chơi bấm nút "SẴN SÀNG" (trước Đợt 1) hoặc "TIẾP TỤC" (sau khi thắng mỗi đợt)
        /// ở góc dưới cùng bên phải màn hình.
        /// </summary>
        public void StartBattle()
        {
            if (!isWaitingForWaveCommand || isSpawning || hasWon) return;

            isWaitingForWaveCommand = false;
            countdown = 0.15f;
            int nextWave = waveNumber + 1;
            Debug.Log($"<color=lime><b>⚔ CHỈ HUY PHÁT LỆNH XUẤT QUÂN ĐỢT {nextWave}!</b></color>");
        }

        private void Update()
        {
            if (hasWon) return;
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            // Tạm dừng giữa các đợt để chờ người chơi bấm "SẴN SÀNG" hoặc "TIẾP TỤC ĐÁNH ĐỢT TIẾP THEO"
            if (isWaitingForWaveCommand) return;

            // Nếu đang sinh quái trong đợt hiện tại thì chờ sinh xong
            if (isSpawning) return;

            // Khi chưa bắt đầu sinh đợt mới (vừa bấm SẴN SÀNG / TIẾP TỤC)
            if (countdown > 0f)
            {
                countdown -= Time.deltaTime;
                if (countdown <= 0f)
                {
                    StartCoroutine(SpawnWaveRoutine());
                }
                return;
            }

            // Khi đợt quái hiện tại đã sinh xong và toàn bộ quái của đợt đã bị tiêu diệt
            if (aliveEnemiesCount <= 0)
            {
                if (!endlessMode && waveNumber >= totalWaves)
                {
                    hasWon = true;
                    Debug.Log("<color=green><b>🏆 CHIẾN THẮNG! BẠN ĐÃ TIÊU DIỆT TOÀN BỘ CÁC ĐỢT TẤN CÔNG CỦA KẺ ĐỊCH!</b></color>");
                    return;
                }

                // Đã đánh thắng đợt hiện tại -> Thưởng vàng hoàn thành đợt & Hiện nút "TIẾP TỤC" cho đợt tiếp theo!
                int waveClearBonus = 15 + waveNumber * 5;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddGold(waveClearBonus);
                }

                isWaitingForWaveCommand = true;
                Debug.Log($"<color=yellow><b>✔ ĐÃ CHIẾN THẮNG ĐỢT {waveNumber}! Thưởng +{waveClearBonus} Vàng tiếp viện. Hãy nâng cấp pháo rồi bấm 'TIẾP TỤC' để đánh Đợt {waveNumber + 1}!</b></color>");
            }
        }

        private IEnumerator SpawnWaveRoutine()
        {
            isSpawning = true;
            waveNumber++;

            List<GameObject> enemiesToSpawn = BuildWaveComposition(waveNumber);

            Debug.Log($"<color=cyan><b>⚔ ĐỢT QUÁI {(endlessMode ? $"{waveNumber} (Vô tận)" : $"{waveNumber}/{totalWaves}")}: Xuất trận {enemiesToSpawn.Count} kẻ địch!</b></color>");

            float interval = Mathf.Max(0.6f, defaultSpawnInterval - (waveNumber * 0.08f));

            for (int i = 0; i < enemiesToSpawn.Count; i++)
            {
                GameObject prefab = enemiesToSpawn[i];
                if (prefab != null)
                {
                    SpawnEnemy(prefab);
                }
                yield return new WaitForSeconds(interval);
            }

            isSpawning = false;
            countdown = 0f;
        }

        /// <summary>
        /// Tạo danh sách các loại quái xuất hiện theo từng round:
        /// - Round 1: 5 xe tăng thường
        /// - Round 2: 8 xe (thường + xe trinh sát nhanh)
        /// - Round 3: 12 xe (xuất hiện xe tăng hạng nặng máu trâu + xe nhanh + xe thường)
        /// - Round 4: 15 xe (xe tăng hạng nặng + xe bọc thép + xe nhanh)
        /// - Round 5: 18 xe + SIÊU BOSS XE TĂNG ở cuối đợt
        /// - Round 6+ (Endless): Tăng dần số lượng và máu
        /// </summary>
        private List<GameObject> BuildWaveComposition(int wave)
        {
            List<GameObject> list = new List<GameObject>();

            GameObject std = standardEnemyPrefab;
            GameObject fast = fastEnemyPrefab != null ? fastEnemyPrefab : std;
            GameObject heavy = heavyEnemyPrefab != null ? heavyEnemyPrefab : std;
            GameObject arm = armoredEnemyPrefab != null ? armoredEnemyPrefab : heavy;
            GameObject boss = bossEnemyPrefab != null ? bossEnemyPrefab : heavy;

            switch (wave)
            {
                case 1:
                    // 5 xe thường
                    for (int i = 0; i < 5; i++) list.Add(std);
                    break;

                case 2:
                    // 8 xe: thường + 3 xe nhanh
                    list.Add(std);
                    list.Add(fast);
                    list.Add(std);
                    list.Add(fast);
                    list.Add(std);
                    list.Add(std);
                    list.Add(fast);
                    list.Add(std);
                    break;

                case 3:
                    // 12 xe: BẮT ĐẦU CÓ QUÁI MỚI MẠNH HƠN (Xe Tăng Hạng Nặng)
                    list.Add(std);
                    list.Add(fast);
                    list.Add(heavy); // Quái mới xuất hiện!
                    list.Add(std);
                    list.Add(fast);
                    list.Add(std);
                    list.Add(heavy); // Xe hạng nặng thứ 2
                    list.Add(fast);
                    list.Add(std);
                    list.Add(heavy); // Xe hạng nặng thứ 3
                    list.Add(fast);
                    list.Add(std);
                    break;

                case 4:
                    // 15 xe: Xuất hiện thêm Xe Bọc Thép (Armored APC) + Heavy Tank
                    list.Add(std);
                    list.Add(arm);   // Xe bọc thép mới
                    list.Add(fast);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(fast);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(fast);
                    list.Add(std);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(fast);
                    list.Add(heavy);
                    list.Add(heavy);
                    break;

                case 5:
                    // 18 xe: Đội hình hỗn hợp + SIÊU BOSS cuối wave
                    list.Add(fast);
                    list.Add(fast);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(fast);
                    list.Add(fast);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(fast);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(fast);
                    // SIÊU BOSS XUẤT HIỆN CUỐI CÙNG
                    list.Add(boss);
                    break;

                case 6:
                    // 22 xe (Map 2 - Sa Mạc Lửa): Đợt tổng tấn công + 2 SIÊU BOSS
                    list.Add(fast);
                    list.Add(fast);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(fast);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(boss); // Boss 1 tiên phong giữa đợt
                    list.Add(fast);
                    list.Add(fast);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(fast);
                    list.Add(heavy);
                    list.Add(heavy);
                    list.Add(boss); // Boss 2 khóa đuôi
                    break;

                case 7:
                    // 28 xe (Map 3 - Pháo Đài Băng Giá): Đợt đại chiến cuối cùng + 3 SIÊU BOSS!
                    list.Add(fast);
                    list.Add(fast);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(heavy);
                    list.Add(boss); // Boss 1 mở đường!
                    list.Add(fast);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(fast);
                    list.Add(fast);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(boss); // Boss 2 tấn công giữa trận!
                    list.Add(fast);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(fast);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(heavy);
                    list.Add(heavy);
                    list.Add(arm);
                    list.Add(boss); // Boss 3 trùm cuối!
                    break;

                default:
                    // Endless Mode (Wave 6 trở lên): Tăng dần số lượng
                    int count = 18 + (wave - 5) * 3;
                    for (int i = 0; i < count; i++)
                    {
                        int r = Random.Range(0, 100);
                        if (r < 25) list.Add(fast);
                        else if (r < 55) list.Add(heavy);
                        else if (r < 80) list.Add(arm);
                        else if (r < 95) list.Add(std);
                        else list.Add(boss);
                    }
                    break;
            }

            return list;
        }

        private void SpawnEnemy(GameObject prefab)
        {
            if (prefab == null || waypointPath == null || waypointPath.Count == 0) return;

            Transform startWaypoint = waypointPath.GetWaypoint(0);
            Vector3 spawnPos = startWaypoint != null ? startWaypoint.position : Vector3.zero;

            GameObject enemyGO = Instantiate(prefab, spawnPos, Quaternion.identity);
            aliveEnemiesCount++;

            Enemy enemy = enemyGO.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.ApplyWaveScaling(waveNumber);
                enemy.OnDeath += OnEnemyDefeated;
                enemy.OnEscaped += OnEnemyEscaped;
            }

            EnemyMovement movement = enemyGO.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.InitPath(waypointPath);
            }
        }

        private void OnEnemyDefeated(Enemy enemy)
        {
            aliveEnemiesCount = Mathf.Max(0, aliveEnemiesCount - 1);
        }

        private void OnEnemyEscaped(Enemy enemy)
        {
            aliveEnemiesCount = Mathf.Max(0, aliveEnemiesCount - 1);
        }

        public void SetReferences(
            GameObject stdPrefab,
            GameObject fastPrefab,
            GameObject heavyPrefab,
            GameObject armoredPrefab,
            GameObject bossPrefab,
            WaypointPath wPath)
        {
            standardEnemyPrefab = stdPrefab;
            fastEnemyPrefab = fastPrefab;
            heavyEnemyPrefab = heavyPrefab;
            armoredEnemyPrefab = armoredPrefab;
            bossEnemyPrefab = bossPrefab;
            waypointPath = wPath;
        }

        /// <summary>
        /// Tương thích ngược với code cũ nếu chỉ truyền 1 prefab.
        /// </summary>
        public void SetReferences(GameObject ePrefab, WaypointPath wPath)
        {
            standardEnemyPrefab = ePrefab;
            waypointPath = wPath;
        }

        public void SetTotalWaves(int waves)
        {
            totalWaves = waves;
        }
    }
}
