using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using TowerDefense.Waypoints;
using TowerDefense.Enemies;
using TowerDefense.Towers;
using TowerDefense.Combat;
using TowerDefense.Managers;
using TowerDefense.UI;

namespace TowerDefense.EditorTools
{
    public static class TowerDefenseAutoSetup
    {
        private const string RetinasPath = "Assets/Sprites/Retina/";
        private const string PrefabsPath = "Assets/Prefabs/";
        private const string Level2ScenePath = "Assets/Scenes/Level2Scene.unity";
        private const string Level3ScenePath = "Assets/Scenes/Level3Scene.unity";

        // Tự động khởi tạo ngầm Level2Scene.unity, Level3Scene.unity và đồng bộ Map 1 ngay khi Unity biên dịch
        [InitializeOnLoadMethod]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!System.IO.File.Exists(Level2ScenePath))
                {
                    Debug.Log("<color=yellow>[AutoSetup] 🏜️ Phát hiện chưa có Level2Scene.unity, đang tự động khởi tạo Map 2: Sa Mạc Lửa...</color>");
                    CreateLevel2SceneInternal(openInEditor: false);
                }
                if (!System.IO.File.Exists(Level3ScenePath))
                {
                    Debug.Log("<color=cyan>[AutoSetup] ❄️ Phát hiện chưa có Level3Scene.unity, đang tự động khởi tạo Map 3: Pháo Đài Băng Giá...</color>");
                    CreateLevel3SceneInternal(openInEditor: false);
                }
                if (!SessionState.GetBool("TD_Map1IndicatorSynced", false))
                {
                    SessionState.SetBool("TD_Map1IndicatorSynced", true);
                    SyncMap1BuildSpotsInternal();
                }
                if (!SessionState.GetBool("TD_CyberAssetsV2Synced", false))
                {
                    SessionState.SetBool("TD_CyberAssetsV2Synced", true);
                    SyncCyberPrefabsInternal();
                }
                EnsureBuildSettingsScenes();
            };
        }

        private static void SyncCyberPrefabsInternal()
        {
            Sprite explosionSprite = LoadSprite(RetinasPath + "towerDefense_tile295.png");
            if (explosionSprite == null) explosionSprite = LoadSprite(RetinasPath + "towerDefense_tile296.png");
            GameObject explosionPrefab = CreateExplosionPrefab(explosionSprite);

            CreateAllEnemyPrefabs(explosionPrefab);

            Sprite baseSprite         = LoadSprite(RetinasPath + "towerDefense_tile180.png");
            Sprite gunSprite          = LoadSprite(RetinasPath + "cannon_1barrel_lvl1.png");
            if (gunSprite == null) gunSprite = LoadSprite(RetinasPath + "towerDefense_tile249.png");
            Sprite bulletSprite       = LoadSprite(RetinasPath + "towerDefense_tile273.png");
            if (bulletSprite == null) bulletSprite = LoadSprite(RetinasPath + "towerDefense_tile297.png");

            Sprite rocketGunSprite    = LoadSprite(RetinasPath + "towerDefense_tile204.png");
            if (rocketGunSprite == null) rocketGunSprite = LoadSprite(RetinasPath + "towerDefense_tile250.png");
            Sprite rocketBulletSprite = LoadSprite(RetinasPath + "towerDefense_tile251.png");
            if (rocketBulletSprite == null) rocketBulletSprite = LoadSprite(RetinasPath + "towerDefense_tile298.png");

            GameObject bulletPrefab = CreateBulletPrefab(bulletSprite);
            GameObject rocketPrefab = CreateRocketPrefab(rocketBulletSprite, explosionPrefab);
            CreateTowerPrefab(baseSprite, gunSprite, bulletPrefab, isRocket: false);
            CreateTowerPrefab(baseSprite, rocketGunSprite, rocketPrefab, isRocket: true);
            AssetDatabase.SaveAssets();
        }

        private static void SyncMap1BuildSpotsInternal()
        {
            string map1Path = "Assets/Scenes/SampleScene.unity";
            if (!System.IO.File.Exists(map1Path)) return;

            bool alreadyOpen = false;
            UnityEngine.SceneManagement.Scene active = EditorSceneManager.GetActiveScene();
            UnityEngine.SceneManagement.Scene map1Scene = default;

            if (active.path == map1Path)
            {
                alreadyOpen = true;
                map1Scene = active;
            }
            else
            {
                map1Scene = EditorSceneManager.OpenScene(map1Path, OpenSceneMode.Additive);
            }

            if (!map1Scene.IsValid()) return;

            Sprite pedestalSprite  = LoadSprite(RetinasPath + "towerDefense_tile181.png");
            Sprite indicatorSprite = LoadSprite(RetinasPath + "towerDefense_tile180.png");
            GameObject towerPrefab       = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + "Tower.prefab");
            GameObject rocketTowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + "TowerRocket.prefab");
            GameObject explosionPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + "Explosion.prefab");

            GameObject spotsParent = FindInScene(map1Scene, "BuildSpots");
            if (spotsParent != null)
            {
                foreach (Transform spot in spotsParent.transform)
                {
                    SpriteRenderer bSr = spot.GetComponent<SpriteRenderer>();
                    if (bSr == null) bSr = spot.gameObject.AddComponent<SpriteRenderer>();
                    if (bSr.sprite == null && pedestalSprite != null) bSr.sprite = pedestalSprite;
                    bSr.sortingOrder = 1;

                    Transform ind = spot.Find("Indicator");
                    if (ind == null)
                    {
                        GameObject indGO = new GameObject("Indicator");
                        indGO.transform.SetParent(spot, false);
                        ind = indGO.transform;
                    }
                    ind.gameObject.SetActive(true);
                    ind.localPosition = Vector3.zero;
                    ind.localScale = new Vector3(0.42f, 0.42f, 1f);

                    SpriteRenderer indSr = ind.GetComponent<SpriteRenderer>();
                    if (indSr == null) indSr = ind.gameObject.AddComponent<SpriteRenderer>();
                    if (indicatorSprite != null) indSr.sprite = indicatorSprite;
                    indSr.sortingOrder = 2;
                    indSr.enabled = true;

                    TowerBuildSpot buildSpot = spot.GetComponent<TowerBuildSpot>();
                    if (buildSpot == null) buildSpot = spot.gameObject.AddComponent<TowerBuildSpot>();
                    if (towerPrefab != null && rocketTowerPrefab != null)
                    {
                        buildSpot.SetReferences(towerPrefab, rocketTowerPrefab, explosionPrefab, bSr, indSr);
                    }
                    EditorUtility.SetDirty(spot.gameObject);
                }
            }

            EditorSceneManager.MarkSceneDirty(map1Scene);
            EditorSceneManager.SaveScene(map1Scene, map1Path);

            if (!alreadyOpen)
            {
                EditorSceneManager.CloseScene(map1Scene, true);
            }
        }

        private static void EnsureBuildSettingsScenes()
        {
            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>();
            string[] paths = new string[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/SampleScene.unity",
                Level2ScenePath,
                Level3ScenePath
            };

            bool changed = false;
            var current = EditorBuildSettings.scenes;

            foreach (string p in paths)
            {
                if (System.IO.File.Exists(p))
                {
                    list.Add(new EditorBuildSettingsScene(p, true));
                }
            }

            if (current.Length != list.Count)
            {
                changed = true;
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (current[i].path != list[i].path || !current[i].enabled)
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                EditorBuildSettings.scenes = list.ToArray();
                AssetDatabase.SaveAssets();
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // 0. MENU TẠO SCENE TRANG CHỦ & CHỌN CẤP ĐỘ (MAIN MENU)
        // ════════════════════════════════════════════════════════════════════════
        [MenuItem("Tower Defense/🏠 Tạo Scene Trang Chủ & Chọn Cấp Độ (MainMenu)", false, 0)]
        public static void CreateMainMenuScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Dừng Play Mode", "Vui lòng bấm nút Play (▶) để DỪNG game trước khi tạo!", "OK");
                return;
            }

            // 1. Lưu scene hiện tại
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            string scenePath = "Assets/Scenes/MainMenu.unity";

            // 2. Tạo Scene mới
            UnityEngine.SceneManagement.Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3. Tạo Camera
            GameObject camGO = new GameObject("Main Camera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.09f, 1f); // Nền tối quân sự
            camGO.transform.position = new Vector3(0, 0, -10f);
            camGO.tag = "MainCamera";
            camGO.AddComponent<AudioListener>();

            // 4. Tạo MainMenuController với component MainMenuUI
            GameObject menuGO = new GameObject("MainMenuController");
            menuGO.AddComponent<MainMenuUI>();

            // 5. Lưu Scene
            EditorSceneManager.SaveScene(newScene, scenePath);

            // 6. Cập nhật EditorBuildSettings (MainMenu, SampleScene, Level2Scene)
            EnsureBuildSettingsScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Tạo Trang Chủ Thành Công! 🎉",
                "Đã tạo hoàn tất Scene Trang Chủ: 'Assets/Scenes/MainMenu.unity'!\n\n" +
                "✔ GIAO DIỆN TRANG CHỦ:\n" +
                "• Tiêu đề 'TOWER DEFENSE 2D' phong cách quân sự sang trọng\n" +
                "• Nút 'Bắt đầu chơi', 'Hướng dẫn cách chơi' và 'Thoát game'\n\n" +
                "✔ MÀN HÌNH CHỌN CẤP ĐỘ MÀN CHƠI:\n" +
                "• MÀN 1 (Bình Nguyên): Mở sẵn ➔ Bấm 'Vào trận' là vào chơi ngay!\n" +
                "• MÀN 2 (Sa Mạc) & MÀN 3 (Băng Giá): Đang khóa (theo đúng ý bạn chưa cần làm màn 2)\n\n" +
                "✔ TRONG TRẬN ĐẤU:\n" +
                "• Có nút '🏠 Về Menu' ở góc trên bên phải để bạn quay lại Trang chủ bất kỳ lúc nào!\n\n" +
                "👉 Bấm nút Play (▶) ngay để trải nghiệm giao diện Trang Chủ!",
                "Tuyệt vời!"
            );
        }

        // ════════════════════════════════════════════════════════════════════════
        // 1. MENU CẬP NHẬT WAVE & QUÁI (AN TOÀN TUYỆT ĐỐI - KHÔNG ĐỤNG ĐẾN MAP)
        // ════════════════════════════════════════════════════════════════════════
        [MenuItem("Tower Defense/⚔ Cập nhật Wave & Các Loại Quái Mới (Bảo Lưu 100% Map & Cây Cỏ)", false, 1)]
        public static void UpdateWavesAndEnemiesOnly()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Dừng Play Mode", "Vui lòng bấm nút Play (▶) để DỪNG game trước khi cập nhật!", "OK");
                return;
            }

            StageUtility.GoToMainStage();
            EnsureTagExists("Enemy");

            // Tạo các prefabs quái vật đa dạng
            Sprite explosionSprite = LoadSprite(RetinasPath + "towerDefense_tile295.png");
            if (explosionSprite == null) explosionSprite = LoadSprite(RetinasPath + "towerDefense_tile296.png");
            GameObject explosionPrefab = CreateExplosionPrefab(explosionSprite);

            var enemyPrefabs = CreateAllEnemyPrefabs(explosionPrefab);

            // Cập nhật Prefabs Tháp & Đạn hỗ trợ Nâng Cấp Level 2
            Sprite baseSprite      = LoadSprite(RetinasPath + "towerDefense_tile180.png");
            Sprite gunSprite       = LoadSprite(RetinasPath + "towerDefense_tile249.png");
            Sprite bulletSprite    = LoadSprite(RetinasPath + "towerDefense_tile297.png");
            Sprite rocketGunSprite = LoadSprite(RetinasPath + "towerDefense_tile250.png");
            if (rocketGunSprite == null) rocketGunSprite = LoadSprite(RetinasPath + "towerDefense_tile246.png");
            if (rocketGunSprite == null) rocketGunSprite = gunSprite;

            Sprite rocketBulletSprite = LoadSprite(RetinasPath + "towerDefense_tile298.png");
            if (rocketBulletSprite == null) rocketBulletSprite = LoadSprite(RetinasPath + "towerDefense_tile296.png");
            if (rocketBulletSprite == null) rocketBulletSprite = bulletSprite;

            GameObject bulletPrefab      = CreateBulletPrefab(bulletSprite);
            GameObject rocketPrefab      = CreateRocketPrefab(rocketBulletSprite, explosionPrefab);
            GameObject towerPrefab       = CreateTowerPrefab(baseSprite, gunSprite, bulletPrefab, false);
            GameObject rocketTowerPrefab = CreateTowerPrefab(baseSprite, rocketGunSprite, rocketPrefab, true);

            // Cập nhật các bệ pháo trong Scene (BẢO LƯU NGUYÊN VẸN VỊ TRÍ)
            GameObject spotsParent = GameObject.Find("BuildSpots");
            if (spotsParent != null)
            {
                foreach (Transform spot in spotsParent.transform)
                {
                    TowerBuildSpot buildSpot = spot.GetComponent<TowerBuildSpot>();
                    if (buildSpot == null) buildSpot = spot.gameObject.AddComponent<TowerBuildSpot>();

                    SpriteRenderer bSr = spot.GetComponent<SpriteRenderer>();
                    if (bSr == null) bSr = spot.gameObject.AddComponent<SpriteRenderer>();
                    if (bSr.sprite == null) bSr.sprite = LoadSprite(RetinasPath + "towerDefense_tile181.png");
                    bSr.sortingOrder = 1;

                    Transform ind = spot.Find("Indicator");
                    if (ind == null)
                    {
                        GameObject indGO = new GameObject("Indicator");
                        indGO.transform.SetParent(spot, false);
                        ind = indGO.transform;
                    }
                    ind.gameObject.SetActive(true);
                    ind.localPosition = Vector3.zero;
                    ind.localScale = new Vector3(0.42f, 0.42f, 1f);

                    SpriteRenderer indSr = ind.GetComponent<SpriteRenderer>();
                    if (indSr == null) indSr = ind.gameObject.AddComponent<SpriteRenderer>();
                    indSr.sprite = LoadSprite(RetinasPath + "towerDefense_tile180.png");
                    indSr.sortingOrder = 2;
                    indSr.enabled = true;

                    buildSpot.SetReferences(towerPrefab, rocketTowerPrefab, explosionPrefab, bSr, indSr);
                    EditorUtility.SetDirty(spot.gameObject);
                }
            }

            // Tìm WaveSpawner trong Scene và gán references
            GameObject spawnerGO = GameObject.Find("WaveSpawner");
            if (spawnerGO == null) spawnerGO = new GameObject("WaveSpawner");

            WaveSpawner spawner = spawnerGO.GetComponent<WaveSpawner>();
            if (spawner == null) spawner = spawnerGO.AddComponent<WaveSpawner>();

            GameObject wpGO = GameObject.Find("Waypoints");
            WaypointPath pathComp = wpGO != null ? wpGO.GetComponent<WaypointPath>() : null;

            spawner.SetReferences(
                enemyPrefabs.standard,
                enemyPrefabs.fast,
                enemyPrefabs.heavy,
                enemyPrefabs.armored,
                enemyPrefabs.boss,
                pathComp
            );
            EditorUtility.SetDirty(spawnerGO);

            // Cập nhật HUD nếu có
            GameObject gmGO = GameObject.Find("GameManager");
            if (gmGO != null)
            {
                SimpleHUD hud = gmGO.GetComponent<SimpleHUD>();
                if (hud == null) gmGO.AddComponent<SimpleHUD>();
                EditorUtility.SetDirty(gmGO);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "Cập nhật thành công! 🎉",
                "Đã bảo lưu NGUYÊN VẸN 100% bản đồ, cây cỏ và bệ pháo của bạn!\n\n" +
                "Đã hoàn thành toàn bộ yêu cầu mới:\n" +
                "✔ HƯỚNG MÁY BAY: Máy bay trinh sát nhỏ & máy bay lớn đã được chỉnh đầu bay về phía trước chuẩn xác!\n" +
                "✔ NÂNG CẤP PHÁO (LEVEL 2): Click vào tháp đã xây để nâng cấp lên Cấp 2 (Pháo Thường 70🪙, Pháo Tên Lửa 110🪙) - Tăng Sát thương, Tầm bắn & Tốc độ!\n" +
                "✔ PHÓNG TO KHUNG HUD: Bảng Vàng 💰, Máu ❤️, Tháp 🗼 và Đợt quái 🌊 đã được mở rộng to rõ, sắc nét, dễ nhìn!\n" +
                "✔ TRỪ MÁU CĂN CỨ THEO CẤP QUÁI: Xe thường (-1), Máy bay (-1 đến -2), Xe hạng nặng (-3), Siêu Boss (-5)!\n\n" +
                "👉 Bạn có thể bấm Play (▶) để trải nghiệm ngay!",
                "Tuyệt vời!"
            );
        }

        // ════════════════════════════════════════════════════════════════════════
        // 3. MENU XUẤT BẢN GAME (.EXE) 1-CLICK ĐỂ GỬI BẠN BÈ CHƠI THỬ
        // ════════════════════════════════════════════════════════════════════════
        [MenuItem("Tower Defense/📦 Xuất bản Game (.exe) 1-Click để gửi bạn bè", false, 20)]
        public static void BuildStandaloneWindowsGame()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Dừng Play Mode", "Vui lòng bấm nút Play (▶) để DỪNG game trước khi xuất bản!", "OK");
                return;
            }

            string projectPath = System.IO.Directory.GetCurrentDirectory();
            string buildFolder = System.IO.Path.Combine(projectPath, "Builds", "TowerDefense_Windows");
            string exePath = System.IO.Path.Combine(buildFolder, "TowerDefense2D.exe");

            if (!System.IO.Directory.Exists(buildFolder))
            {
                System.IO.Directory.CreateDirectory(buildFolder);
            }

            if (!System.IO.File.Exists(Level2ScenePath))
            {
                CreateLevel2SceneInternal(openInEditor: false);
            }
            if (!System.IO.File.Exists(Level3ScenePath))
            {
                CreateLevel3SceneInternal(openInEditor: false);
            }
            EnsureBuildSettingsScenes();

            List<string> sceneList = new List<string>();
            if (System.IO.File.Exists("Assets/Scenes/MainMenu.unity")) sceneList.Add("Assets/Scenes/MainMenu.unity");
            if (System.IO.File.Exists("Assets/Scenes/SampleScene.unity")) sceneList.Add("Assets/Scenes/SampleScene.unity");
            if (System.IO.File.Exists(Level2ScenePath)) sceneList.Add(Level2ScenePath);
            if (System.IO.File.Exists(Level3ScenePath)) sceneList.Add(Level3ScenePath);
            string[] scenes = sceneList.ToArray();

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"<color=cyan><b>🚀 ĐANG TIẾN HÀNH XUẤT BẢN GAME RA: {exePath}...</b></color>");
            EditorUtility.DisplayProgressBar("Xuất bản Game", "Đang đóng gói file game Tower Defense (.exe)... Vui lòng chờ vài giây!", 0.5f);

            try
            {
                var report = BuildPipeline.BuildPlayer(options);
                EditorUtility.ClearProgressBar();

                if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                {
                    Debug.Log($"<color=green><b>✅ XUẤT BẢN GAME THÀNH CÔNG! Dung lượng: {report.summary.totalSize / (1024 * 1024)} MB</b></color>");

                    // Tự động mở thư mục chứa file game trong Windows Explorer
                    EditorUtility.RevealInFinder(exePath);

                    EditorUtility.DisplayDialog(
                        "Xuất bản thành công! 🎉",
                        $"Game đã được đóng gói hoàn tất tại thư mục:\n{buildFolder}\n\n" +
                        "👉 CÁCH GỬI CHO BẠN BÈ CHƠI THỬ:\n" +
                        "1. Chuột phải vào thư mục 'TowerDefense_Windows' ➔ Chọn 'Compress to ZIP file'.\n" +
                        "2. Gửi file .zip đó qua Google Drive, Zalo, Discord hoặc Telegram.\n" +
                        "3. Bạn bè chỉ cần giải nén và nhấp đúp 'TowerDefense2D.exe' là chơi được ngay mà không cần cài Unity!",
                        "Đã hiểu!"
                    );
                }
                else
                {
                    EditorUtility.DisplayDialog("Lỗi xuất bản", "Có lỗi xảy ra trong quá trình đóng gói. Vui lòng kiểm tra Console log!", "OK");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Lỗi", "Lỗi: " + ex.Message, "OK");
            }
        }
        [MenuItem("Tower Defense/⚡ Tự động ghép Map & thiết lập Game (Auto Setup)", false, 2)]
        public static void SetupGame()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Dừng Play Mode", "Vui lòng bấm nút Play (▶) ở giữa màn hình để DỪNG game trước khi bấm Auto Setup!", "OK");
                return;
            }

            StageUtility.GoToMainStage();
            EnsureTagExists("Enemy");

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            // Sprites
            Sprite baseSprite      = LoadSprite(RetinasPath + "towerDefense_tile180.png");
            Sprite gunSprite       = LoadSprite(RetinasPath + "towerDefense_tile249.png");
            Sprite bulletSprite    = LoadSprite(RetinasPath + "towerDefense_tile297.png");
            Sprite explosionSprite = LoadSprite(RetinasPath + "towerDefense_tile295.png");
            if (explosionSprite == null) explosionSprite = LoadSprite(RetinasPath + "towerDefense_tile296.png");

            Sprite rocketGunSprite = LoadSprite(RetinasPath + "towerDefense_tile250.png");
            if (rocketGunSprite == null) rocketGunSprite = LoadSprite(RetinasPath + "towerDefense_tile246.png");
            if (rocketGunSprite == null) rocketGunSprite = gunSprite;

            Sprite rocketBulletSprite = LoadSprite(RetinasPath + "towerDefense_tile298.png");
            if (rocketBulletSprite == null) rocketBulletSprite = LoadSprite(RetinasPath + "towerDefense_tile296.png");
            if (rocketBulletSprite == null) rocketBulletSprite = bulletSprite;

            GameObject explosionPrefab = CreateExplosionPrefab(explosionSprite);
            GameObject bulletPrefab    = CreateBulletPrefab(bulletSprite);
            GameObject rocketPrefab    = CreateRocketPrefab(rocketBulletSprite, explosionPrefab);

            var enemyPrefabs = CreateAllEnemyPrefabs(explosionPrefab);

            GameObject towerPrefab       = CreateTowerPrefab(baseSprite, gunSprite, bulletPrefab, false);
            GameObject rocketTowerPrefab = CreateTowerPrefab(baseSprite, rocketGunSprite, rocketPrefab, true);

            // Thiết lập Scene với cơ chế bảo vệ giao diện đã chỉnh
            SetupSceneElementsPreservingUserCustomization(
                enemyPrefabs,
                towerPrefab,
                rocketTowerPrefab,
                explosionPrefab
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Thiết lập hoàn tất! 🎉",
                "Hệ thống đã cập nhật toàn bộ tính năng:\n" +
                "✔ Bảo lưu nguyên vẹn cây cỏ và vị trí bệ bạn đã căn chỉnh\n" +
                "✔ Đợt quái tăng dần theo wave (Wave 1: 5 xe, Wave 5: 18 xe + Boss)\n" +
                "✔ Round 3 trở đi xuất hiện quái mới: Xe Hạng Nặng & Xe Bọc Thép\n" +
                "✔ Round 5 xuất hiện SIÊU BOSS THIẾT GIÁP (750 HP)\n" +
                "✔ Popup chọn Pháo Thường (50🪙) hoặc Pháo Tên Lửa AoE (80🪙)\n\n" +
                "👉 Bấm Play (▶) để bắt đầu!",
                "Tuyệt vời!"
            );
        }

        // ─── Tạo bộ 5 Prefab Quái Vật theo độ khó ─────────────────────────
        private struct EnemyPrefabGroup
        {
            public GameObject standard;
            public GameObject fast;
            public GameObject heavy;
            public GameObject armored;
            public GameObject boss;
        }

        private static EnemyPrefabGroup CreateAllEnemyPrefabs(GameObject explosionPrefab)
        {
            // 1. Xe Tăng Thường (tile268 + tile291) - 105 HP, 2.0 speed, 12 gold, 15% Giáp, trừ 1 máu nhà (mũi hướng phải -> offset 0)
            Sprite sStd = LoadSprite(RetinasPath + "towerDefense_tile268.png");
            GameObject pStd = CreateCustomEnemyPrefab(
                "Enemy_Standard",
                sStd,
                explosionPrefab,
                name: "Xe Tăng Thường",
                hp: 105f,
                speed: 2.0f,
                gold: 12,
                baseDamage: 1,
                scale: 1f,
                angleOffset: 0f,
                tintColor: Color.white,
                barSize: new Vector2(0.85f, 0.12f),
                barOffset: new Vector3(0, 0.65f, 0),
                armor: 0.15f
            );

            // 2. Máy Bay Trinh Sát Nhỏ (tile270) - 65 HP, 3.2 speed, 8 gold, 0% Giáp, trừ 1 máu
            Sprite sFast = LoadSprite(RetinasPath + "towerDefense_tile270.png");
            if (sFast == null) sFast = sStd;
            GameObject pFast = CreateCustomEnemyPrefab(
                "Enemy_Fast",
                sFast,
                explosionPrefab,
                name: "Máy Bay Trinh Sát Nhỏ",
                hp: 65f,
                speed: 3.2f,
                gold: 8,
                baseDamage: 1,
                scale: 0.9f,
                angleOffset: 0f, // Mũi máy bay bay trước chuẩn xác 100%!
                tintColor: Color.white,
                barSize: new Vector2(0.75f, 0.11f),
                barOffset: new Vector3(0, 0.55f, 0),
                armor: 0.00f
            );

            // 3. Xe Tăng Hạng Nặng (tile269 + tile292) - 320 HP, 1.4 speed, 28 gold, 45% Giáp hạng nặng, trừ 3 máu nhà
            Sprite sHeavy = LoadSprite(RetinasPath + "towerDefense_tile269.png");
            if (sHeavy == null) sHeavy = sStd;
            GameObject pHeavy = CreateCustomEnemyPrefab(
                "Enemy_Heavy",
                sHeavy,
                explosionPrefab,
                name: "Xe Tăng Hạng Nặng",
                hp: 320f,
                speed: 1.4f,
                gold: 28,
                baseDamage: 3, // Xe tăng hạng nặng lọt vào trừ 3 máu
                scale: 1.18f,
                angleOffset: 0f,
                tintColor: Color.white,
                barSize: new Vector2(1.05f, 0.15f),
                barOffset: new Vector3(0, 0.75f, 0),
                armor: 0.45f
            );

            // 4. Máy Bay Lớn / Bọc Thép (tile271 / plane_stealth_lvl2) - 185 HP, 2.2 speed, 18 gold, 25% Giáp, trừ 2 máu nhà
            Sprite sArmored = LoadSprite(RetinasPath + "towerDefense_tile271.png");
            if (sArmored == null) sArmored = sStd;
            GameObject pArmored = CreateCustomEnemyPrefab(
                "Enemy_Armored",
                sArmored,
                explosionPrefab,
                name: "Chiến Cơ Tàng Hình Bọc Thép",
                hp: 185f,
                speed: 2.2f,
                gold: 18,
                baseDamage: 2, // Máy bay lớn trừ 2 máu
                scale: 1.05f,
                angleOffset: 0f, // Mũi máy bay lớn bay trước chuẩn xác 100%!
                tintColor: Color.white,
                barSize: new Vector2(0.95f, 0.13f),
                barOffset: new Vector3(0, 0.68f, 0),
                armor: 0.25f
            );

            // 5. SIÊU BOSS THIẾT GIÁP (tile269 + tile292 tint đỏ cam) - 950 HP, 1.15 speed, 75 gold, 35% Giáp, trừ 5 máu nhà
            Sprite sBoss = LoadSprite(RetinasPath + "towerDefense_tile269.png");
            if (sBoss == null) sBoss = sStd;
            GameObject pBoss = CreateCustomEnemyPrefab(
                "Enemy_Boss",
                sBoss,
                explosionPrefab,
                name: "SIÊU BOSS THIẾT GIÁP",
                hp: 950f,
                speed: 1.15f,
                gold: 75,
                baseDamage: 5, // Boss lọt vào trừ 5 máu căn cứ!
                scale: 1.45f,
                angleOffset: 0f,
                tintColor: new Color(1f, 0.35f, 0.35f, 1f), // Đỏ rực nguy hiểm
                barSize: new Vector2(1.35f, 0.20f),
                barOffset: new Vector3(0, 0.95f, 0),
                healthColor: new Color(1f, 0.2f, 0.1f, 1f),
                armor: 0.35f
            );

            // Cập nhật lại Enemy.prefab cũ trỏ tới standard để không gãy liên kết cũ nếu có
            CreateCustomEnemyPrefab("Enemy", sStd, explosionPrefab, "Xe Tăng Thường", 105f, 2.0f, 12, 1, 1f, 0f, Color.white, new Vector2(0.85f, 0.12f), new Vector3(0, 0.65f, 0), null, 0.15f);

            return new EnemyPrefabGroup
            {
                standard = pStd,
                fast = pFast,
                heavy = pHeavy,
                armored = pArmored,
                boss = pBoss
            };
        }

        private static GameObject CreateCustomEnemyPrefab(
            string prefabName,
            Sprite sprite,
            GameObject explosionPrefab,
            string name,
            float hp,
            float speed,
            int gold,
            int baseDamage,
            float scale,
            float angleOffset,
            Color tintColor,
            Vector2 barSize,
            Vector3 barOffset,
            Color? healthColor = null,
            float armor = 0.15f)
        {
            string path = PrefabsPath + prefabName + ".prefab";
            GameObject go = new GameObject(prefabName);
            go.tag = "Enemy";
            go.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tintColor;
            sr.sortingOrder = 5;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            Enemy enemy = go.AddComponent<Enemy>();
            enemy.SetStats(name, hp, gold, baseDamage, armor);
            enemy.SetExplosionPrefab(explosionPrefab);

            EnemyMovement movement = go.AddComponent<EnemyMovement>();
            movement.SetSpeed(speed);
            movement.SetAngleOffset(angleOffset);

            EnemyHealthBar healthBar = go.AddComponent<EnemyHealthBar>();
            healthBar.SetBarConfiguration(barSize, barOffset, healthColor);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ─── Tạo Explosion prefab ───────────────────────────────────────────
        private static GameObject CreateExplosionPrefab(Sprite sprite)
        {
            string path = PrefabsPath + "Explosion.prefab";
            GameObject go = new GameObject("Explosion");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 20;
            go.AddComponent<ExplosionEffect>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ─── Tạo Bullet prefab ──────────────────────────────────────────────
        private static GameObject CreateBulletPrefab(Sprite sprite)
        {
            string path = PrefabsPath + "Bullet.prefab";
            GameObject go = new GameObject("Bullet");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            go.AddComponent<Bullet>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ─── Tạo Rocket prefab ──────────────────────────────────────────────
        private static GameObject CreateRocketPrefab(Sprite sprite, GameObject explosionPrefab)
        {
            string path = PrefabsPath + "Rocket.prefab";
            GameObject go = new GameObject("Rocket");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;
            sr.color = new Color(1f, 0.5f, 0f, 1f);

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            Rocket rocket = go.AddComponent<Rocket>();
            rocket.SetExplosionPrefab(explosionPrefab);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ─── Tạo Tower prefab ───────────────────────────────────────────────
        private static GameObject CreateTowerPrefab(Sprite baseSprite, Sprite gunSprite, GameObject projPrefab, bool isRocket)
        {
            string name = isRocket ? "TowerRocket" : "Tower";
            string path = PrefabsPath + name + ".prefab";

            GameObject go = new GameObject(name);
            SpriteRenderer baseSr = go.AddComponent<SpriteRenderer>();
            baseSr.sprite = baseSprite;
            baseSr.sortingOrder = 2;
            if (isRocket) baseSr.color = new Color(1f, 0.85f, 0.6f, 1f);

            GameObject gunGo = new GameObject("TurretGun");
            gunGo.transform.SetParent(go.transform, false);
            SpriteRenderer gunSr = gunGo.AddComponent<SpriteRenderer>();
            gunSr.sprite = gunSprite;
            gunSr.sortingOrder = 3;

            GameObject firePointGo = new GameObject("FirePoint");
            firePointGo.transform.SetParent(gunGo.transform, false);
            firePointGo.transform.localPosition = new Vector3(0, 0.55f, 0);

            Tower tower = go.AddComponent<Tower>();
            if (isRocket)
            {
                tower.SetReferences(
                    gunGo.transform,
                    firePointGo.transform,
                    projPrefab,
                    range: 4.4f,
                    rate: 0.60f,
                    dmg: 47f,
                    type: TowerType.Rocket,
                    upCost: 150 // Giá nâng cấp Level 2 chuẩn Kingdom Rush: 150 vàng
                );
            }
            else
            {
                tower.SetReferences(
                    gunGo.transform,
                    firePointGo.transform,
                    projPrefab,
                    range: 3.6f,
                    rate: 1.25f,
                    dmg: 20f,
                    type: TowerType.Cannon,
                    upCost: 110 // Giá nâng cấp Level 2 chuẩn Kingdom Rush: 110 vàng
                );
            }
            EditorUtility.SetDirty(go);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ─── Setup Scene thông minh: KHÔNG BAO GIỜ XÓA MAP HAY CÂY CỎ ĐÃ SỬA ───
        private static void SetupSceneElementsPreservingUserCustomization(
            EnemyPrefabGroup enemies,
            GameObject towerPrefab,
            GameObject rocketTowerPrefab,
            GameObject explosionPrefab)
        {
            // 1. Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0, 0, -10f);
                mainCam.orthographic = true;
                mainCam.orthographicSize = 5f;
                EditorUtility.SetDirty(mainCam.gameObject);
            }

            // 2. Tilemap: Chỉ tạo nếu chưa có, nếu ĐÃ CÓ thì GIỮ NGUYÊN HOÀN TOÀN
            GameObject gridGO = GameObject.Find("Grid");
            if (gridGO == null) gridGO = new GameObject("Grid");
            Grid grid = gridGO.GetComponent<Grid>();
            if (grid == null) grid = gridGO.AddComponent<Grid>();

            Transform tmTrans = gridGO.transform.Find("Tilemap");
            GameObject tmGO = tmTrans != null ? tmTrans.gameObject : null;
            if (tmGO == null)
            {
                tmGO = new GameObject("Tilemap");
                tmGO.transform.SetParent(gridGO.transform, false);
            }
            Tilemap tilemap = tmGO.GetComponent<Tilemap>();
            if (tilemap == null) tilemap = tmGO.AddComponent<Tilemap>();
            TilemapRenderer tr = tmGO.GetComponent<TilemapRenderer>();
            if (tr == null) tr = tmGO.AddComponent<TilemapRenderer>();
            tr.sortingOrder = -5;

            // NẾU TILEMAP ĐÃ CÓ GẠCH (User đã ghép) -> KHÔNG XÓA!
            if (tilemap.GetUsedTilesCount() == 0)
            {
                Tile grassTile = GetOrCreateTile("Tile_Grass.asset", "towerDefense_tile024.png");
                Tile roadTile  = GetOrCreateTile("Tile_Road.asset",  "towerDefense_tile093.png");
                if (grassTile != null)
                {
                    for (int x = -12; x <= 12; x++)
                        for (int y = -7; y <= 7; y++)
                            tilemap.SetTile(new Vector3Int(x, y, 0), grassTile);
                }
                if (roadTile != null)
                {
                    for (int x = -9; x <= -3; x++) tilemap.SetTile(new Vector3Int(x, 2, 0), roadTile);
                    for (int y = -2; y <= 2; y++)  tilemap.SetTile(new Vector3Int(-3, y, 0), roadTile);
                    for (int x = -3; x <= 2; x++)  tilemap.SetTile(new Vector3Int(x, -2, 0), roadTile);
                    for (int y = -2; y <= 2; y++)  tilemap.SetTile(new Vector3Int(2, y, 0), roadTile);
                    for (int x = 2; x <= 9; x++)   tilemap.SetTile(new Vector3Int(x, 2, 0), roadTile);
                }
                tilemap.RefreshAllTiles();
            }

            // 3. Waypoints: Nếu chưa có thì mới tạo, có rồi giữ nguyên
            GameObject waypointsGO = GameObject.Find("Waypoints");
            WaypointPath pathComp = null;
            if (waypointsGO == null || waypointsGO.transform.childCount == 0)
            {
                if (waypointsGO == null) waypointsGO = new GameObject("Waypoints");
                pathComp = waypointsGO.GetComponent<WaypointPath>();
                if (pathComp == null) pathComp = waypointsGO.AddComponent<WaypointPath>();

                Vector3[] points = new Vector3[]
                {
                    new Vector3(-8.5f,  2.5f, 0f),
                    new Vector3(-2.5f,  2.5f, 0f),
                    new Vector3(-2.5f, -1.5f, 0f),
                    new Vector3( 2.5f, -1.5f, 0f),
                    new Vector3( 2.5f,  2.5f, 0f),
                    new Vector3( 8.5f,  2.5f, 0f),
                };
                List<Transform> wpList = new List<Transform>();
                for (int i = 0; i < points.Length; i++)
                {
                    GameObject wp = new GameObject($"Waypoint_{i}");
                    wp.transform.SetParent(waypointsGO.transform, false);
                    wp.transform.position = points[i];
                    wp.AddComponent<Waypoint>();
                    wpList.Add(wp.transform);
                }
                pathComp.SetWaypoints(wpList);
            }
            else
            {
                pathComp = waypointsGO.GetComponent<WaypointPath>();
            }

            // 4. Ẩn MapMau
            GameObject mapMau = GameObject.Find("MapMau");
            if (mapMau != null) { mapMau.SetActive(false); EditorUtility.SetDirty(mapMau); }

            // 5. BuildSpots: GIỮ NGUYÊN VỊ TRÍ NGƯỜI DÙNG ĐÃ ĐẶT!
            GameObject spotsParent = GameObject.Find("BuildSpots");
            if (spotsParent == null || spotsParent.transform.childCount == 0)
            {
                // Chỉ tạo mới nếu hoàn toàn chưa có trong scene
                if (spotsParent != null) Object.DestroyImmediate(spotsParent);
                spotsParent = new GameObject("BuildSpots");

                // Dùng đúng các vị trí người dùng đã tinh chỉnh
                Vector3[] buildSpotPositions = new Vector3[]
                {
                    new Vector3(-5.5f,  0.5f, 0f),
                    new Vector3(-5.08f, 3.8f, 0f), // Vị trí người dùng đã chỉnh!
                    new Vector3(-0.5f,  0.0f, 0f),
                    new Vector3(-0.5f, -3.5f, 0f),
                    new Vector3( 5.5f,  0.5f, 0f),
                };

                Sprite pedestalSprite = LoadSprite(RetinasPath + "towerDefense_tile181.png");
                if (pedestalSprite == null) pedestalSprite = LoadSprite(RetinasPath + "towerDefense_tile180.png");

                for (int i = 0; i < buildSpotPositions.Length; i++)
                {
                    GameObject spotGO = new GameObject($"BuildSpot_{i + 1}");
                    spotGO.transform.SetParent(spotsParent.transform, false);
                    spotGO.transform.position = buildSpotPositions[i];

                    SpriteRenderer bSr = spotGO.AddComponent<SpriteRenderer>();
                    bSr.sprite = pedestalSprite;
                    bSr.sortingOrder = 1;

                    GameObject indGO = new GameObject("Indicator");
                    indGO.transform.SetParent(spotGO.transform, false);
                    SpriteRenderer indSr = indGO.AddComponent<SpriteRenderer>();
                    indSr.sprite = LoadSprite(RetinasPath + "towerDefense_tile180.png");
                    indSr.sortingOrder = 2;
                    indGO.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

                    TowerBuildSpot buildSpot = spotGO.AddComponent<TowerBuildSpot>();
                    buildSpot.SetReferences(towerPrefab, rocketTowerPrefab, explosionPrefab, bSr, indSr);
                }
            }
            else
            {
                // Nếu đã có sẵn trong scene: BẢO TOÀN VỊ TRÍ, chỉ cập nhật reference nếu cần
                foreach (Transform spot in spotsParent.transform)
                {
                    TowerBuildSpot buildSpot = spot.GetComponent<TowerBuildSpot>();
                    if (buildSpot == null) buildSpot = spot.gameObject.AddComponent<TowerBuildSpot>();

                    SpriteRenderer bSr = spot.GetComponent<SpriteRenderer>();
                    if (bSr == null) bSr = spot.gameObject.AddComponent<SpriteRenderer>();
                    if (bSr.sprite == null) bSr.sprite = LoadSprite(RetinasPath + "towerDefense_tile181.png");
                    bSr.sortingOrder = 1;

                    Transform ind = spot.Find("Indicator");
                    if (ind == null)
                    {
                        GameObject indGO = new GameObject("Indicator");
                        indGO.transform.SetParent(spot, false);
                        ind = indGO.transform;
                    }
                    ind.gameObject.SetActive(true);
                    ind.localPosition = Vector3.zero;
                    ind.localScale = new Vector3(0.42f, 0.42f, 1f);

                    SpriteRenderer indSr = ind.GetComponent<SpriteRenderer>();
                    if (indSr == null) indSr = ind.gameObject.AddComponent<SpriteRenderer>();
                    indSr.sprite = LoadSprite(RetinasPath + "towerDefense_tile180.png");
                    indSr.sortingOrder = 2;
                    indSr.enabled = true;

                    buildSpot.SetReferences(towerPrefab, rocketTowerPrefab, explosionPrefab, bSr, indSr);
                    EditorUtility.SetDirty(spot.gameObject);
                }
            }

            // 6. Nhóm Towers
            GameObject towersParent = GameObject.Find("Towers");
            if (towersParent == null) towersParent = new GameObject("Towers");

            // 7. WaveSpawner: Gán đủ cả 5 loại quái
            GameObject spawnerGO = GameObject.Find("WaveSpawner");
            if (spawnerGO == null) spawnerGO = new GameObject("WaveSpawner");
            WaveSpawner spawner = spawnerGO.GetComponent<WaveSpawner>();
            if (spawner == null) spawner = spawnerGO.AddComponent<WaveSpawner>();
            spawner.SetReferences(
                enemies.standard,
                enemies.fast,
                enemies.heavy,
                enemies.armored,
                enemies.boss,
                pathComp
            );
            EditorUtility.SetDirty(spawnerGO);

            // 8. GameManager + SimpleHUD
            GameObject gmGO = GameObject.Find("GameManager");
            if (gmGO == null) gmGO = new GameObject("GameManager");
            TowerPlacement oldPlacement = gmGO.GetComponent<TowerPlacement>();
            if (oldPlacement != null) Object.DestroyImmediate(oldPlacement);
            GameManager gm = gmGO.GetComponent<GameManager>();
            if (gm == null) gm = gmGO.AddComponent<GameManager>();
            SimpleHUD hud = gmGO.GetComponent<SimpleHUD>();
            if (hud == null) hud = gmGO.AddComponent<SimpleHUD>();
            EditorUtility.SetDirty(gmGO);

            // 9. CÂY CỎ / DECORATIONS:
            // NẾU TRONG SCENE ĐÃ CÓ NHÓM "Decorations" -> BẢO TỒN NGUYÊN VẸN 100%, KHÔNG XÓA!
            GameObject decorParent = GameObject.Find("Decorations");
            if (decorParent == null || decorParent.transform.childCount == 0)
            {
                Debug.Log("[AutoSetup] Chưa có cây cỏ trang trí, tiến hành tạo mới.");
                // Chỉ tạo nếu scene chưa hề có Decorations
                AddInitialDecorations();
            }
            else
            {
                Debug.Log($"<color=green>[AutoSetup] ✅ BẢO VỆ NGUYÊN VẸN {decorParent.transform.childCount} CÂY CỎ TRANG TRÍ MÀ NGƯỜI DÙNG ĐÃ TỰ TAY SẮP XẾP!</color>");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void AddInitialDecorations()
        {
            // Các toạ độ đẹp mặc định nếu lần đầu tiên khởi tạo
            GameObject decorParent = new GameObject("Decorations");
            string[] decorTiles = new string[]
            {
                "towerDefense_tile136.png",
                "towerDefense_tile131.png",
                "towerDefense_tile157.png",
                "towerDefense_tile132.png",
                "towerDefense_tile134.png",
                "towerDefense_tile137.png",
                "towerDefense_tile135.png",
                "towerDefense_tile133.png"
            };

            List<Sprite> sprites = new List<Sprite>();
            foreach (var t in decorTiles)
            {
                Sprite s = LoadSprite(RetinasPath + t);
                if (s != null) sprites.Add(s);
            }

            if (sprites.Count == 0) return;

            Vector3[] initialPositions = new Vector3[]
            {
                new Vector3(0.63f, 2.02f, 0),
                new Vector3(-6.5f, -4.5f, 0),
                new Vector3(-9.5f, 4.5f, 0),
                new Vector3(-5.5f, 5.5f, 0),
                new Vector3(-1.25f, 1.33f, 0),
                new Vector3(-7.5f, -0.5f, 0),
                new Vector3(-3.5f, 4.5f, 0),
                new Vector3(-7.5f, 4.5f, 0),
                new Vector3(-3.36f, -0.06f, 0),
                new Vector3(3.95f, 0.5f, 0),
                new Vector3(5.5f, -2.5f, 0),
                new Vector3(-9.5f, -3.5f, 0),
                new Vector3(7.28f, 3.83f, 0),
                new Vector3(3.71f, 3.83f, 0),
                new Vector3(-0.5f, 3.5f, 0),
                new Vector3(8.5f, -0.5f, 0)
            };

            for (int i = 0; i < initialPositions.Length; i++)
            {
                GameObject d = new GameObject($"Deco_{i + 1}");
                d.transform.SetParent(decorParent.transform, false);
                d.transform.position = initialPositions[i];
                d.transform.localScale = Vector3.one * 0.75f;
                SpriteRenderer sr = d.AddComponent<SpriteRenderer>();
                sr.sprite = sprites[i % sprites.Count];
                sr.sortingOrder = 1;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // 4. THIẾT KẾ & KHỞI TẠO MAP 2: SA MẠC LỬA (TO HƠN, ĐƯỜNG ZÍC-ZẮC, 8 Ô ĐẶT PHÁO)
        // ════════════════════════════════════════════════════════════════════════
        [MenuItem("Tower Defense/🏜️ Tạo & Mở Map 2: Sa Mạc Lửa (Level2Scene)", false, 3)]
        public static void CreateLevel2SceneMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Dừng Play Mode", "Vui lòng bấm nút Play (▶) để DỪNG game trước khi mở Map 2!", "OK");
                return;
            }

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            CreateLevel2SceneInternal(openInEditor: true);

            EditorUtility.DisplayDialog(
                "Thiết Kế Map 2: Sa Mạc Lửa Hoàn Tất! 🏜️",
                "Đã tạo và mở Scene Map 2 tại: 'Assets/Scenes/Level2Scene.unity'!\n\n" +
                "✔ BẢN ĐỒ RỘNG LỚN HƠN MAP 1:\n" +
                "• Kích thước 33×21 ô gạch (693 ô) & Camera tầm nhìn lớn (OrthoSize = 6.8)\n" +
                "• Chủ đề Sa Mạc Cát Vàng kết hợp Đường Đá Chiến Trường & Ốc Đảo\n\n" +
                "✔ ĐƯỜNG ĐI ZÍC-ZẮC 9 ĐOẠN (10 WAYPOINTS):\n" +
                "• Quái xuất phát từ Trái-Dưới ➔ Lên Cao ➔ Lao Xuống Đáy Hẻm Núi ➔ Vọt Lên Đỉnh Phải ➔ Căn Cứ\n\n" +
                "✔ 8 Ô ĐẶT PHÁO CHIẾN THUẬT (BuildSpot_1 -> BuildSpot_8):\n" +
                "• Đặt tại các khúc cua chữ U và giao lộ trọng yếu giúp pháo bao quát 2-3 nhánh đường!\n\n" +
                "👉 Bạn có thể chỉnh sửa thêm trên Scene hoặc bấm Play (▶) để chiến ngay!",
                "Tuyệt vời!"
            );
        }

        public static void CreateLevel2SceneInternal(bool openInEditor)
        {
            StageUtility.GoToMainStage();
            EnsureTagExists("Enemy");

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            // Chuẩn bị toàn bộ Prefabs
            Sprite baseSprite      = LoadSprite(RetinasPath + "towerDefense_tile180.png");
            Sprite gunSprite       = LoadSprite(RetinasPath + "towerDefense_tile249.png");
            Sprite bulletSprite    = LoadSprite(RetinasPath + "towerDefense_tile297.png");
            Sprite explosionSprite = LoadSprite(RetinasPath + "towerDefense_tile295.png");
            if (explosionSprite == null) explosionSprite = LoadSprite(RetinasPath + "towerDefense_tile296.png");

            Sprite rocketGunSprite = LoadSprite(RetinasPath + "towerDefense_tile250.png");
            if (rocketGunSprite == null) rocketGunSprite = LoadSprite(RetinasPath + "towerDefense_tile246.png");
            if (rocketGunSprite == null) rocketGunSprite = gunSprite;

            Sprite rocketBulletSprite = LoadSprite(RetinasPath + "towerDefense_tile298.png");
            if (rocketBulletSprite == null) rocketBulletSprite = LoadSprite(RetinasPath + "towerDefense_tile296.png");
            if (rocketBulletSprite == null) rocketBulletSprite = bulletSprite;

            GameObject explosionPrefab   = CreateExplosionPrefab(explosionSprite);
            GameObject bulletPrefab      = CreateBulletPrefab(bulletSprite);
            GameObject rocketPrefab      = CreateRocketPrefab(rocketBulletSprite, explosionPrefab);
            var enemyPrefabs             = CreateAllEnemyPrefabs(explosionPrefab);
            GameObject towerPrefab       = CreateTowerPrefab(baseSprite, gunSprite, bulletPrefab, false);
            GameObject rocketTowerPrefab = CreateTowerPrefab(baseSprite, rocketGunSprite, rocketPrefab, true);

            // Tạo file Level2Scene.unity từ URP2DSceneTemplate để có sẵn Global Light 2D & Camera chuẩn URP
            if (!System.IO.File.Exists(Level2ScenePath))
            {
                if (System.IO.File.Exists("Assets/Settings/Scenes/URP2DSceneTemplate.unity"))
                {
                    AssetDatabase.CopyAsset("Assets/Settings/Scenes/URP2DSceneTemplate.unity", Level2ScenePath);
                }
                else
                {
                    var tmpScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
                    EditorSceneManager.SaveScene(tmpScene, Level2ScenePath);
                    EditorSceneManager.CloseScene(tmpScene, true);
                }
                AssetDatabase.Refresh();
            }

            OpenSceneMode mode = openInEditor ? OpenSceneMode.Single : OpenSceneMode.Additive;
            UnityEngine.SceneManagement.Scene map2Scene = EditorSceneManager.OpenScene(Level2ScenePath, mode);

            SetupMap2SceneElements(
                map2Scene,
                enemyPrefabs,
                towerPrefab,
                rocketTowerPrefab,
                explosionPrefab
            );

            EditorSceneManager.MarkSceneDirty(map2Scene);
            EditorSceneManager.SaveScene(map2Scene, Level2ScenePath);

            if (!openInEditor)
            {
                EditorSceneManager.CloseScene(map2Scene, true);
            }

            EnsureBuildSettingsScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject FindInScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            if (!scene.IsValid()) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }
            return null;
        }

        private static void SetupMap2SceneElements(
            UnityEngine.SceneManagement.Scene scene,
            EnemyPrefabGroup enemies,
            GameObject towerPrefab,
            GameObject rocketTowerPrefab,
            GameObject explosionPrefab)
        {
            // 1. Camera: Mở rộng tầm nhìn TO HƠN Map 1 (OrthoSize = 6.8f so với 5.0f của Map 1)
            GameObject camGO = FindInScene(scene, "Main Camera");
            if (camGO == null)
            {
                camGO = new GameObject("Main Camera");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGO, scene);
                camGO.tag = "MainCamera";
                camGO.AddComponent<AudioListener>();
            }
            Camera mainCam = camGO.GetComponent<Camera>();
            if (mainCam == null) mainCam = camGO.AddComponent<Camera>();
            mainCam.transform.position = new Vector3(0f, 0f, -10f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6.8f; // Map 2 rộng lớn hơn hẳn Map 1!
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.84f, 0.77f, 0.56f, 1f); // Nền cát sa mạc
            EditorUtility.SetDirty(camGO);

            // 2. Grid & Tilemap (33 x 21 ô gạch - Rộng gấp ~2 lần Map 1)
            GameObject gridGO = FindInScene(scene, "Grid");
            if (gridGO == null)
            {
                gridGO = new GameObject("Grid");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gridGO, scene);
            }
            Grid grid = gridGO.GetComponent<Grid>();
            if (grid == null) grid = gridGO.AddComponent<Grid>();

            Transform tmTrans = gridGO.transform.Find("Tilemap");
            GameObject tmGO = tmTrans != null ? tmTrans.gameObject : null;
            if (tmGO == null)
            {
                tmGO = new GameObject("Tilemap");
                tmGO.transform.SetParent(gridGO.transform, false);
            }
            Tilemap tilemap = tmGO.GetComponent<Tilemap>();
            if (tilemap == null) tilemap = tmGO.AddComponent<Tilemap>();
            TilemapRenderer tr = tmGO.GetComponent<TilemapRenderer>();
            if (tr == null) tr = tmGO.AddComponent<TilemapRenderer>();
            tr.sortingOrder = -5;

            // Bảo lưu nếu người dùng đã tự sửa Tilemap trên Map 2
            if (tilemap.GetUsedTilesCount() == 0)
            {
                Tile sandTile      = GetOrCreateTile("Tile_Sand.asset",      "towerDefense_tile029.png");
                Tile stoneRoadTile = GetOrCreateTile("Tile_StoneRoad.asset", "towerDefense_tile034.png");
                Tile grassTile     = GetOrCreateTile("Tile_Grass.asset",     "towerDefense_tile024.png");

                if (sandTile == null) sandTile = grassTile;
                if (stoneRoadTile == null) stoneRoadTile = GetOrCreateTile("Tile_Road.asset", "towerDefense_tile093.png");

                // Trải nền cát sa mạc 33x21 (-16..16, -10..10) + ốc đảo cỏ xanh ở viền ngoài
                for (int x = -16; x <= 16; x++)
                {
                    for (int y = -10; y <= 10; y++)
                    {
                        bool isOuterOasis = (x <= -14 || x >= 14 || y <= -8 || y >= 7);
                        Tile bgTile = (isOuterOasis && grassTile != null) ? grassTile : sandTile;
                        if (bgTile != null)
                            tilemap.SetTile(new Vector3Int(x, y, 0), bgTile);
                    }
                }

                // Vẽ đường đi Zíc-Zắc 9 đoạn xuyên qua hẻm núi sa mạc
                if (stoneRoadTile != null)
                {
                    // Đoạn 1: Cổng xuất quân Trái-Dưới (-12, -3) -> (-8, -3)
                    for (int x = -12; x <= -8; x++) tilemap.SetTile(new Vector3Int(x, -3, 0), stoneRoadTile);
                    // Đoạn 2: Đi lên (-8, -3) -> (-8, 1)
                    for (int y = -3; y <= 1; y++)   tilemap.SetTile(new Vector3Int(-8, y, 0), stoneRoadTile);
                    // Đoạn 3: Sang phải (-8, 1) -> (-3, 1)
                    for (int x = -8; x <= -3; x++)  tilemap.SetTile(new Vector3Int(x, 1, 0), stoneRoadTile);
                    // Đoạn 4: Lao xuống đáy hẻm núi (-3, -5) -> (-3, 1)
                    for (int y = -5; y <= 1; y++)   tilemap.SetTile(new Vector3Int(-3, y, 0), stoneRoadTile);
                    // Đoạn 5: Vượt đáy hẻm núi (-3, -5) -> (3, -5)
                    for (int x = -3; x <= 3; x++)   tilemap.SetTile(new Vector3Int(x, -5, 0), stoneRoadTile);
                    // Đoạn 6: Leo lên đỉnh cao nguyên phải (3, -5) -> (3, 3)
                    for (int y = -5; y <= 3; y++)   tilemap.SetTile(new Vector3Int(3, y, 0), stoneRoadTile);
                    // Đoạn 7: Sang phải trên đỉnh (3, 3) -> (8, 3)
                    for (int x = 3; x <= 8; x++)    tilemap.SetTile(new Vector3Int(x, 3, 0), stoneRoadTile);
                    // Đoạn 8: Đổ dốc xuống cổng căn cứ (8, -2) -> (8, 3)
                    for (int y = -2; y <= 3; y++)   tilemap.SetTile(new Vector3Int(8, y, 0), stoneRoadTile);
                    // Đoạn 9: Tiến thẳng vào Căn Cứ Phải (8, -2) -> (12, -2)
                    for (int x = 8; x <= 12; x++)   tilemap.SetTile(new Vector3Int(x, -2, 0), stoneRoadTile);
                }

                tilemap.RefreshAllTiles();
            }

            // 3. Waypoints (10 điểm mốc khớp chính xác tâm đường đá Zíc-Zắc)
            GameObject waypointsGO = FindInScene(scene, "Waypoints");
            WaypointPath pathComp = null;
            if (waypointsGO == null || waypointsGO.transform.childCount == 0)
            {
                if (waypointsGO == null)
                {
                    waypointsGO = new GameObject("Waypoints");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(waypointsGO, scene);
                }
                pathComp = waypointsGO.GetComponent<WaypointPath>();
                if (pathComp == null) pathComp = waypointsGO.AddComponent<WaypointPath>();

                Vector3[] points = new Vector3[]
                {
                    new Vector3(-11.5f, -2.5f, 0f), // WP 0: Xuất quân Trái-Dưới
                    new Vector3( -7.5f, -2.5f, 0f), // WP 1: Cua lên
                    new Vector3( -7.5f,  1.5f, 0f), // WP 2: Cua phải
                    new Vector3( -2.5f,  1.5f, 0f), // WP 3: Cua xuống đáy
                    new Vector3( -2.5f, -4.5f, 0f), // WP 4: Đáy hẻm núi trái
                    new Vector3(  3.5f, -4.5f, 0f), // WP 5: Đáy hẻm núi phải - cua lên
                    new Vector3(  3.5f,  3.5f, 0f), // WP 6: Đỉnh cao nguyên - cua phải
                    new Vector3(  8.5f,  3.5f, 0f), // WP 7: Cua xuống
                    new Vector3(  8.5f, -1.5f, 0f), // WP 8: Cua vào căn cứ
                    new Vector3( 12.5f, -1.5f, 0f), // WP 9: Căn cứ chỉ huy
                };

                List<Transform> wpList = new List<Transform>();
                for (int i = 0; i < points.Length; i++)
                {
                    GameObject wp = new GameObject($"Waypoint_{i}");
                    wp.transform.SetParent(waypointsGO.transform, false);
                    wp.transform.position = points[i];
                    wp.AddComponent<Waypoint>();
                    wpList.Add(wp.transform);
                }
                pathComp.SetWaypoints(wpList);
            }
            else
            {
                pathComp = waypointsGO.GetComponent<WaypointPath>();
            }

            // 4. BuildSpots: 8 Ô Đặt Pháo Chiến Thuật nằm ở các khúc cua chữ U & giao lộ
            GameObject spotsParent = FindInScene(scene, "BuildSpots");
            if (spotsParent == null || spotsParent.transform.childCount == 0)
            {
                if (spotsParent == null)
                {
                    spotsParent = new GameObject("BuildSpots");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(spotsParent, scene);
                }

                Vector3[] map2BuildSpotPositions = new Vector3[]
                {
                    new Vector3( -9.5f, -0.5f, 0f), // BuildSpot_1: Khúc cua đầu tiên bên trái
                    new Vector3( -5.0f, -1.0f, 0f), // BuildSpot_2: Lõi chữ U thứ nhất (bắn 3 nhánh đường)
                    new Vector3( -5.5f, -4.5f, 0f), // BuildSpot_3: Chốt chặn dưới đường vào & nhánh lao xuống
                    new Vector3(  0.5f,  2.0f, 0f), // BuildSpot_4: Cao nguyên giữa bản đồ (bắn 2 nhánh dọc)
                    new Vector3(  0.5f, -2.5f, 0f), // BuildSpot_5: Lõi hẻm núi đáy chữ U giữa map (bắn 3 nhánh)
                    new Vector3(  6.0f,  1.0f, 0f), // BuildSpot_6: Lõi chữ U cao nguyên bên phải (bắn 3 nhánh)
                    new Vector3(  6.0f, -3.5f, 0f), // BuildSpot_7: Chốt chặn góc dưới bên phải
                    new Vector3( 10.5f,  0.5f, 0f), // BuildSpot_8: Pháo đài cuối bảo vệ cổng căn cứ
                };

                Sprite pedestalSprite = LoadSprite(RetinasPath + "towerDefense_tile181.png");
                if (pedestalSprite == null) pedestalSprite = LoadSprite(RetinasPath + "towerDefense_tile180.png");

                for (int i = 0; i < map2BuildSpotPositions.Length; i++)
                {
                    GameObject spotGO = new GameObject($"BuildSpot_{i + 1}");
                    spotGO.transform.SetParent(spotsParent.transform, false);
                    spotGO.transform.position = map2BuildSpotPositions[i];

                    SpriteRenderer bSr = spotGO.AddComponent<SpriteRenderer>();
                    bSr.sprite = pedestalSprite;
                    bSr.sortingOrder = 1;

                    GameObject indGO = new GameObject("Indicator");
                    indGO.transform.SetParent(spotGO.transform, false);
                    SpriteRenderer indSr = indGO.AddComponent<SpriteRenderer>();
                    indSr.sprite = LoadSprite(RetinasPath + "towerDefense_tile180.png");
                    indSr.sortingOrder = 2;
                    indGO.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

                    TowerBuildSpot buildSpot = spotGO.AddComponent<TowerBuildSpot>();
                    buildSpot.SetReferences(towerPrefab, rocketTowerPrefab, explosionPrefab, bSr, indSr);
                }
            }
            else
            {
                foreach (Transform spot in spotsParent.transform)
                {
                    TowerBuildSpot buildSpot = spot.GetComponent<TowerBuildSpot>();
                    if (buildSpot == null) buildSpot = spot.gameObject.AddComponent<TowerBuildSpot>();

                    SpriteRenderer bSr = spot.GetComponent<SpriteRenderer>();
                    if (bSr == null) bSr = spot.gameObject.AddComponent<SpriteRenderer>();
                    if (bSr.sprite == null) bSr.sprite = LoadSprite(RetinasPath + "towerDefense_tile181.png");
                    bSr.sortingOrder = 1;

                    Transform ind = spot.Find("Indicator");
                    if (ind == null)
                    {
                        GameObject indGO = new GameObject("Indicator");
                        indGO.transform.SetParent(spot, false);
                        ind = indGO.transform;
                    }
                    ind.gameObject.SetActive(true);
                    ind.localPosition = Vector3.zero;
                    ind.localScale = new Vector3(0.42f, 0.42f, 1f);

                    SpriteRenderer indSr = ind.GetComponent<SpriteRenderer>();
                    if (indSr == null) indSr = ind.gameObject.AddComponent<SpriteRenderer>();
                    indSr.sprite = LoadSprite(RetinasPath + "towerDefense_tile180.png");
                    indSr.sortingOrder = 2;
                    indSr.enabled = true;

                    buildSpot.SetReferences(towerPrefab, rocketTowerPrefab, explosionPrefab, bSr, indSr);
                    EditorUtility.SetDirty(spot.gameObject);
                }
            }

            // 5. Nhóm Towers
            GameObject towersParent = FindInScene(scene, "Towers");
            if (towersParent == null)
            {
                towersParent = new GameObject("Towers");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(towersParent, scene);
            }

            // 6. WaveSpawner: 6 Đợt quái thử thách trên Map 2
            GameObject spawnerGO = FindInScene(scene, "WaveSpawner");
            if (spawnerGO == null)
            {
                spawnerGO = new GameObject("WaveSpawner");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(spawnerGO, scene);
            }
            WaveSpawner spawner = spawnerGO.GetComponent<WaveSpawner>();
            if (spawner == null) spawner = spawnerGO.AddComponent<WaveSpawner>();
            spawner.SetReferences(
                enemies.standard,
                enemies.fast,
                enemies.heavy,
                enemies.armored,
                enemies.boss,
                pathComp
            );
            spawner.SetTotalWaves(6); // Map 2 có 6 đợt tấn công!
            EditorUtility.SetDirty(spawnerGO);

            // 7. GameManager + SimpleHUD (Khởi đầu 160 Vàng & 25 Máu cho Map 2 rộng lớn)
            GameObject gmGO = FindInScene(scene, "GameManager");
            if (gmGO == null)
            {
                gmGO = new GameObject("GameManager");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gmGO, scene);
            }
            GameManager gm = gmGO.GetComponent<GameManager>();
            if (gm == null) gm = gmGO.AddComponent<GameManager>();
            gm.SetStartingStats(160, 25);
            SimpleHUD hud = gmGO.GetComponent<SimpleHUD>();
            if (hud == null) hud = gmGO.AddComponent<SimpleHUD>();
            EditorUtility.SetDirty(gmGO);

            // 8. Trang trí Sa Mạc (Đá tảng, xương rồng/bụi cây sa mạc, hố bom, Căn cứ)
            GameObject decorParent = FindInScene(scene, "Decorations");
            if (decorParent == null || decorParent.transform.childCount == 0)
            {
                if (decorParent == null)
                {
                    decorParent = new GameObject("Decorations");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(decorParent, scene);
                }
                AddMap2Decorations(decorParent);
            }
        }

        private static void AddMap2Decorations(GameObject decorParent)
        {
            string[] desertProps = new string[]
            {
                "towerDefense_tile135.png", // Đá tảng 1
                "towerDefense_tile136.png", // Đá tảng lớn 2
                "towerDefense_tile137.png", // Đá ngũ giác 3
                "towerDefense_tile134.png", // Cây xương rồng sao
                "towerDefense_tile132.png", // Bụi lá 3 cánh
                "towerDefense_tile131.png", // Bụi cây nhỏ
            };

            List<Sprite> sprites = new List<Sprite>();
            foreach (var t in desertProps)
            {
                Sprite s = LoadSprite(RetinasPath + t);
                if (s != null) sprites.Add(s);
            }
            if (sprites.Count == 0) return;

            Vector3[] positions = new Vector3[]
            {
                new Vector3(-11.5f, -5.0f, 0f),
                new Vector3(-10.5f,  1.5f, 0f),
                new Vector3( -9.2f, -5.2f, 0f),
                new Vector3( -6.0f,  3.5f, 0f),
                new Vector3( -3.5f,  3.8f, 0f),
                new Vector3( -0.8f,  4.5f, 0f),
                new Vector3(  1.5f,  4.8f, 0f),
                new Vector3(  5.5f,  5.2f, 0f),
                new Vector3(  9.5f,  5.0f, 0f),
                new Vector3( 11.5f,  3.5f, 0f),
                new Vector3( 11.5f,  2.0f, 0f),
                new Vector3( 11.2f, -4.2f, 0f),
                new Vector3(  8.5f, -4.8f, 0f),
                new Vector3(  4.0f, -6.2f, 0f),
                new Vector3(  0.0f, -6.2f, 0f),
                new Vector3( -4.0f, -6.2f, 0f),
                new Vector3( -8.5f, -5.8f, 0f),
                new Vector3(-11.8f,  0.5f, 0f),
                new Vector3(  0.5f, -0.2f, 0f),
                new Vector3(  6.0f, -1.2f, 0f),
                new Vector3( 10.8f, -3.8f, 0f),
                new Vector3( 12.2f,  0.8f, 0f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject d = new GameObject($"DesertDeco_{i + 1}");
                d.transform.SetParent(decorParent.transform, false);
                d.transform.position = positions[i];
                float scale = (i % 3 == 0) ? 0.90f : 0.78f;
                d.transform.localScale = Vector3.one * scale;
                SpriteRenderer sr = d.AddComponent<SpriteRenderer>();
                sr.sprite = sprites[i % sprites.Count];
                sr.sortingOrder = 1;
            }

            // Biểu tượng Căn Cứ Phòng Thủ ở điểm cuối đường đi (12.5, -1.5)
            Sprite baseFortSprite = LoadSprite(RetinasPath + "towerDefense_tile182.png");
            if (baseFortSprite != null)
            {
                GameObject fortGO = new GameObject("BaseFortress_End");
                fortGO.transform.SetParent(decorParent.transform, false);
                fortGO.transform.position = new Vector3(12.5f, -1.5f, 0f);
                fortGO.transform.localScale = Vector3.one * 1.15f;
                SpriteRenderer fortSr = fortGO.AddComponent<SpriteRenderer>();
                fortSr.sprite = baseFortSprite;
                fortSr.color = new Color(0.35f, 0.85f, 1f, 0.95f);
                fortSr.sortingOrder = 1;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // 5. THIẾT KẾ & KHỞI TẠO MAP 3: PHÁO ĐÀI BĂNG GIÁ (35×23, XOẮN ỐC 11 ĐOẠN, 10 Ô ĐẶT PHÁO)
        // ════════════════════════════════════════════════════════════════════════
        [MenuItem("Tower Defense/❄️ Tạo & Mở Map 3: Pháo Đài Băng Giá (Level3Scene)", false, 4)]
        public static void CreateLevel3SceneMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Dừng Play Mode", "Vui lòng bấm nút Play (▶) để DỪNG game trước khi mở Map 3!", "OK");
                return;
            }

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            CreateLevel3SceneInternal(openInEditor: true);

            EditorUtility.DisplayDialog(
                "Thiết Kế Map 3: Pháo Đài Băng Giá Hoàn Tất! ❄️",
                "Đã tạo và mở Scene Map 3 tại: 'Assets/Scenes/Level3Scene.unity'!\n\n" +
                "✔ BẢN ĐỒ ĐẠI CHIẾN LỚN NHẤT GAME (35×23 Ô GẠCH):\n" +
                "• Kích thước 35×23 (805 ô) & Camera tầm nhìn bao quát (OrthoSize = 7.2)\n" +
                "• Chủ đề Thành Lũy Đá Băng Xám Xanh kết hợp Sân Trong Cát Vàng & Rừng Thông\n\n" +
                "✔ ĐƯỜNG ĐI XOẮN ỐC THÀNH LŨY 11 ĐOẠN (12 WAYPOINTS):\n" +
                "• Quái xuất phát từ Tây ➔ Vòng Hào Nam ➔ Xuyên Cổng Giữa ➔ Vượt Đỉnh Đông Bắc ➔ Xoắn Vào Nội Thành!\n\n" +
                "✔ 10 Ô ĐẶT PHÁO CÔNG SỰ (BuildSpot_1 -> BuildSpot_10):\n" +
                "• Bố trí tại các nút thắt giao lộ và lõi vòng xoắn ốc nội thành!\n\n" +
                "👉 Bạn có thể chỉnh sửa thêm trên Scene hoặc bấm Play (▶) để chiến ngay!",
                "Tuyệt vời!"
            );
        }

        public static void CreateLevel3SceneInternal(bool openInEditor)
        {
            StageUtility.GoToMainStage();
            EnsureTagExists("Enemy");

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            // Chuẩn bị toàn bộ Prefabs
            Sprite baseSprite      = LoadSprite(RetinasPath + "towerDefense_tile180.png");
            Sprite gunSprite       = LoadSprite(RetinasPath + "towerDefense_tile249.png");
            Sprite bulletSprite    = LoadSprite(RetinasPath + "towerDefense_tile297.png");
            Sprite explosionSprite = LoadSprite(RetinasPath + "towerDefense_tile295.png");
            if (explosionSprite == null) explosionSprite = LoadSprite(RetinasPath + "towerDefense_tile296.png");

            Sprite rocketGunSprite = LoadSprite(RetinasPath + "towerDefense_tile250.png");
            if (rocketGunSprite == null) rocketGunSprite = LoadSprite(RetinasPath + "towerDefense_tile246.png");
            if (rocketGunSprite == null) rocketGunSprite = gunSprite;

            Sprite rocketBulletSprite = LoadSprite(RetinasPath + "towerDefense_tile298.png");
            if (rocketBulletSprite == null) rocketBulletSprite = LoadSprite(RetinasPath + "towerDefense_tile296.png");
            if (rocketBulletSprite == null) rocketBulletSprite = bulletSprite;

            GameObject explosionPrefab   = CreateExplosionPrefab(explosionSprite);
            GameObject bulletPrefab      = CreateBulletPrefab(bulletSprite);
            GameObject rocketPrefab      = CreateRocketPrefab(rocketBulletSprite, explosionPrefab);
            var enemyPrefabs             = CreateAllEnemyPrefabs(explosionPrefab);
            GameObject towerPrefab       = CreateTowerPrefab(baseSprite, gunSprite, bulletPrefab, false);
            GameObject rocketTowerPrefab = CreateTowerPrefab(baseSprite, rocketGunSprite, rocketPrefab, true);

            // Tạo file Level3Scene.unity từ URP2DSceneTemplate để có sẵn Global Light 2D & Camera chuẩn URP
            if (!System.IO.File.Exists(Level3ScenePath))
            {
                if (System.IO.File.Exists("Assets/Settings/Scenes/URP2DSceneTemplate.unity"))
                {
                    AssetDatabase.CopyAsset("Assets/Settings/Scenes/URP2DSceneTemplate.unity", Level3ScenePath);
                }
                else
                {
                    var tmpScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
                    EditorSceneManager.SaveScene(tmpScene, Level3ScenePath);
                    EditorSceneManager.CloseScene(tmpScene, true);
                }
                AssetDatabase.Refresh();
            }

            OpenSceneMode mode = openInEditor ? OpenSceneMode.Single : OpenSceneMode.Additive;
            UnityEngine.SceneManagement.Scene map3Scene = EditorSceneManager.OpenScene(Level3ScenePath, mode);

            SetupMap3SceneElements(
                map3Scene,
                enemyPrefabs,
                towerPrefab,
                rocketTowerPrefab,
                explosionPrefab
            );

            EditorSceneManager.MarkSceneDirty(map3Scene);
            EditorSceneManager.SaveScene(map3Scene, Level3ScenePath);

            if (!openInEditor)
            {
                EditorSceneManager.CloseScene(map3Scene, true);
            }

            EnsureBuildSettingsScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SetupMap3SceneElements(
            UnityEngine.SceneManagement.Scene scene,
            EnemyPrefabGroup enemies,
            GameObject towerPrefab,
            GameObject rocketTowerPrefab,
            GameObject explosionPrefab)
        {
            // 1. Camera: Bản đồ đại chiến lớn nhất (OrthoSize = 7.2f)
            GameObject camGO = FindInScene(scene, "Main Camera");
            if (camGO == null)
            {
                camGO = new GameObject("Main Camera");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGO, scene);
                camGO.tag = "MainCamera";
                camGO.AddComponent<AudioListener>();
            }
            Camera mainCam = camGO.GetComponent<Camera>();
            if (mainCam == null) mainCam = camGO.AddComponent<Camera>();
            mainCam.transform.position = new Vector3(0f, 0f, -10f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 7.2f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.55f, 0.68f, 0.74f, 1f); // Nền đá băng xám xanh
            EditorUtility.SetDirty(camGO);

            // 2. Grid & Tilemap (35 x 23 ô gạch = 805 ô)
            GameObject gridGO = FindInScene(scene, "Grid");
            if (gridGO == null)
            {
                gridGO = new GameObject("Grid");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gridGO, scene);
            }
            Grid grid = gridGO.GetComponent<Grid>();
            if (grid == null) grid = gridGO.AddComponent<Grid>();

            Transform tmTrans = gridGO.transform.Find("Tilemap");
            GameObject tmGO = tmTrans != null ? tmTrans.gameObject : null;
            if (tmGO == null)
            {
                tmGO = new GameObject("Tilemap");
                tmGO.transform.SetParent(gridGO.transform, false);
            }
            Tilemap tilemap = tmGO.GetComponent<Tilemap>();
            if (tilemap == null) tilemap = tmGO.AddComponent<Tilemap>();
            TilemapRenderer tr = tmGO.GetComponent<TilemapRenderer>();
            if (tr == null) tr = tmGO.AddComponent<TilemapRenderer>();
            tr.sortingOrder = -5;

            if (tilemap.GetUsedTilesCount() == 0)
            {
                Tile stoneFloorTile = GetOrCreateTile("Tile_StoneRoad.asset", "towerDefense_tile034.png");
                Tile trenchRoadTile = GetOrCreateTile("Tile_Road.asset",      "towerDefense_tile093.png");
                Tile sandCourtTile  = GetOrCreateTile("Tile_Sand.asset",      "towerDefense_tile029.png");
                Tile grassBorder    = GetOrCreateTile("Tile_Grass.asset",     "towerDefense_tile024.png");

                if (stoneFloorTile == null) stoneFloorTile = grassBorder;

                // Trải nền Pháo Đài Đá Băng (x: -17..17, y: -11..11) + sân trong + rừng thông ngoại vi
                for (int x = -17; x <= 17; x++)
                {
                    for (int y = -11; y <= 11; y++)
                    {
                        bool isOuterPine = (x <= -15 || x >= 15 || y <= -9 || y >= 8);
                        bool isInnerCitadelCourt = (x >= 1 && x <= 9 && y >= -5 && y <= 0);
                        Tile bgTile = stoneFloorTile;
                        if (isOuterPine && grassBorder != null) bgTile = grassBorder;
                        else if (isInnerCitadelCourt && sandCourtTile != null) bgTile = sandCourtTile;

                        if (bgTile != null)
                            tilemap.SetTile(new Vector3Int(x, y, 0), bgTile);
                    }
                }

                // Vẽ đường đi Xoắn Ốc Thành Lũy 11 đoạn (khác biệt hoàn toàn với Map 1 & Map 2)
                if (trenchRoadTile != null)
                {
                    // Đoạn 1: Xuất quân từ Cổng Tây (-13, 0) -> (-9, 0)
                    for (int x = -13; x <= -9; x++) tilemap.SetTile(new Vector3Int(x, 0, 0), trenchRoadTile);
                    // Đoạn 2: Đổ xuống Hào Nam (-9, -6) -> (-9, 0)
                    for (int y = -6; y <= 0; y++)   tilemap.SetTile(new Vector3Int(-9, y, 0), trenchRoadTile);
                    // Đoạn 3: Tiến dọc Tường Nam (-9, -6) -> (-3, -6)
                    for (int x = -9; x <= -3; x++)  tilemap.SetTile(new Vector3Int(x, -6, 0), trenchRoadTile);
                    // Đoạn 4: Xuyên Cổng Trung Tâm lên Bắc (-3, -6) -> (-3, 2)
                    for (int y = -6; y <= 2; y++)   tilemap.SetTile(new Vector3Int(-3, y, 0), trenchRoadTile);
                    // Đoạn 5: Ngang qua Sân Thượng (-3, 2) -> (4, 2)
                    for (int x = -3; x <= 4; x++)   tilemap.SetTile(new Vector3Int(x, 2, 0), trenchRoadTile);
                    // Đoạn 6: Leo lên Đỉnh Pháo Đài Đông Bắc (4, 2) -> (4, 5)
                    for (int y = 2; y <= 5; y++)    tilemap.SetTile(new Vector3Int(4, y, 0), trenchRoadTile);
                    // Đoạn 7: Vòng qua Sườn Đỉnh Bắc (4, 5) -> (11, 5)
                    for (int x = 4; x <= 11; x++)   tilemap.SetTile(new Vector3Int(x, 5, 0), trenchRoadTile);
                    // Đoạn 8: Lao xuống Tường Đông (11, -1) -> (11, 5)
                    for (int y = -1; y <= 5; y++)   tilemap.SetTile(new Vector3Int(11, y, 0), trenchRoadTile);
                    // Đoạn 9: Đột nhập ngược vào Nội Thành (2, -1) -> (11, -1)
                    for (int x = 2; x <= 11; x++)   tilemap.SetTile(new Vector3Int(x, -1, 0), trenchRoadTile);
                    // Đoạn 10: Xoắn xuống Hào Nội Thành (2, -4) -> (2, -1)
                    for (int y = -4; y <= -1; y++)  tilemap.SetTile(new Vector3Int(2, y, 0), trenchRoadTile);
                    // Đoạn 11: Tổng công kích thẳng vào Sở Chỉ Huy Tối Cao (2, -4) -> (8, -4)
                    for (int x = 2; x <= 8; x++)    tilemap.SetTile(new Vector3Int(x, -4, 0), trenchRoadTile);
                }

                tilemap.RefreshAllTiles();
            }

            // 3. Waypoints (12 điểm mốc khớp chính xác tâm 11 đoạn đường xoắn ốc)
            GameObject waypointsGO = FindInScene(scene, "Waypoints");
            WaypointPath pathComp = null;
            if (waypointsGO == null || waypointsGO.transform.childCount == 0)
            {
                if (waypointsGO == null)
                {
                    waypointsGO = new GameObject("Waypoints");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(waypointsGO, scene);
                }
                pathComp = waypointsGO.GetComponent<WaypointPath>();
                if (pathComp == null) pathComp = waypointsGO.AddComponent<WaypointPath>();

                Vector3[] points = new Vector3[]
                {
                    new Vector3(-12.5f,  0.5f, 0f), // WP 0: Cổng Tây xuất quân
                    new Vector3( -8.5f,  0.5f, 0f), // WP 1: Rẽ xuống Hào Nam
                    new Vector3( -8.5f, -5.5f, 0f), // WP 2: Đáy Hào Tây Nam
                    new Vector3( -2.5f, -5.5f, 0f), // WP 3: Cua lên Cổng Trung Tâm
                    new Vector3( -2.5f,  2.5f, 0f), // WP 4: Cua sang Sân Thượng
                    new Vector3(  4.5f,  2.5f, 0f), // WP 5: Cua lên Đỉnh Đông Bắc
                    new Vector3(  4.5f,  5.5f, 0f), // WP 6: Đỉnh Đông Bắc
                    new Vector3( 11.5f,  5.5f, 0f), // WP 7: Góc Đông Bắc - lao xuống
                    new Vector3( 11.5f, -0.5f, 0f), // WP 8: Cổng Đông - rẽ ngược vào Nội Thành
                    new Vector3(  2.5f, -0.5f, 0f), // WP 9: Tâm Nội Thành - xoắn xuống
                    new Vector3(  2.5f, -3.5f, 0f), // WP 10: Hào Nội Thành - tiến vào căn cứ
                    new Vector3(  8.5f, -3.5f, 0f), // WP 11: Sở Chỉ Huy Tối Cao (Căn Cứ Cuối)
                };

                List<Transform> wpList = new List<Transform>();
                for (int i = 0; i < points.Length; i++)
                {
                    GameObject wp = new GameObject($"Waypoint_{i}");
                    wp.transform.SetParent(waypointsGO.transform, false);
                    wp.transform.position = points[i];
                    wp.AddComponent<Waypoint>();
                    wpList.Add(wp.transform);
                }
                pathComp.SetWaypoints(wpList);
            }
            else
            {
                pathComp = waypointsGO.GetComponent<WaypointPath>();
            }

            // 4. BuildSpots: 10 Ô Đặt Pháo Công Sự bao vây cả vòng ngoài lẫn vòng xoắn nội thành
            GameObject spotsParent = FindInScene(scene, "BuildSpots");
            if (spotsParent == null || spotsParent.transform.childCount == 0)
            {
                if (spotsParent == null)
                {
                    spotsParent = new GameObject("BuildSpots");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(spotsParent, scene);
                }

                Vector3[] map3BuildSpotPositions = new Vector3[]
                {
                    new Vector3(-10.5f, -2.0f, 0f), // BuildSpot_1: Khống chế Cổng Tây & nhánh đổ xuống
                    new Vector3( -6.0f, -3.0f, 0f), // BuildSpot_2: Lõi chữ U Tây Nam (bắn 3 nhánh đường)
                    new Vector3( -5.5f,  1.5f, 0f), // BuildSpot_3: Chốt chặn Cổng Trung Tâm phía Tây
                    new Vector3(  0.0f,  0.8f, 0f), // BuildSpot_4: Giao lộ Trung Tâm (bắn cả Sân Thượng & Nội Thành)
                    new Vector3(  0.0f, -3.0f, 0f), // BuildSpot_5: Giao lộ Nam Trung Tâm (bắn 3 nhánh đường)
                    new Vector3(  1.5f,  4.2f, 0f), // BuildSpot_6: Chốt Bắc Sân Thượng
                    new Vector3(  7.5f,  3.2f, 0f), // BuildSpot_7: Lõi Pháo Đài Đông Bắc (bắn 3 nhánh đỉnh đồi)
                    new Vector3(  6.5f,  1.0f, 0f), // BuildSpot_8: Chốt kẹp giữa Sân Thượng & Đường Nội Thành
                    new Vector3(  5.5f, -2.0f, 0f), // BuildSpot_9: LÕI VÒNG XOẮN NỘI THÀNH (Vị trí vàng cho Pháo Tên Lửa!)
                    new Vector3( 10.5f, -2.5f, 0f), // BuildSpot_10: Công sự cận vệ bảo vệ Sở Chỉ Huy Tối Cao
                };

                Sprite pedestalSprite = LoadSprite(RetinasPath + "towerDefense_tile181.png");
                if (pedestalSprite == null) pedestalSprite = LoadSprite(RetinasPath + "towerDefense_tile180.png");

                for (int i = 0; i < map3BuildSpotPositions.Length; i++)
                {
                    GameObject spotGO = new GameObject($"BuildSpot_{i + 1}");
                    spotGO.transform.SetParent(spotsParent.transform, false);
                    spotGO.transform.position = map3BuildSpotPositions[i];

                    SpriteRenderer bSr = spotGO.AddComponent<SpriteRenderer>();
                    bSr.sprite = pedestalSprite;
                    bSr.sortingOrder = 1;

                    GameObject indGO = new GameObject("Indicator");
                    indGO.transform.SetParent(spotGO.transform, false);
                    SpriteRenderer indSr = indGO.AddComponent<SpriteRenderer>();
                    indSr.sprite = LoadSprite(RetinasPath + "towerDefense_tile180.png");
                    indSr.sortingOrder = 2;
                    indGO.transform.localScale = new Vector3(0.42f, 0.42f, 1f);

                    TowerBuildSpot buildSpot = spotGO.AddComponent<TowerBuildSpot>();
                    buildSpot.SetReferences(towerPrefab, rocketTowerPrefab, explosionPrefab, bSr, indSr);
                }
            }
            else
            {
                foreach (Transform spot in spotsParent.transform)
                {
                    TowerBuildSpot buildSpot = spot.GetComponent<TowerBuildSpot>();
                    if (buildSpot == null) buildSpot = spot.gameObject.AddComponent<TowerBuildSpot>();

                    SpriteRenderer bSr = spot.GetComponent<SpriteRenderer>();
                    if (bSr == null) bSr = spot.gameObject.AddComponent<SpriteRenderer>();
                    if (bSr.sprite == null) bSr.sprite = LoadSprite(RetinasPath + "towerDefense_tile181.png");
                    bSr.sortingOrder = 1;

                    Transform ind = spot.Find("Indicator");
                    if (ind == null)
                    {
                        GameObject indGO = new GameObject("Indicator");
                        indGO.transform.SetParent(spot, false);
                        ind = indGO.transform;
                    }
                    ind.gameObject.SetActive(true);
                    ind.localPosition = Vector3.zero;
                    ind.localScale = new Vector3(0.42f, 0.42f, 1f);

                    SpriteRenderer indSr = ind.GetComponent<SpriteRenderer>();
                    if (indSr == null) indSr = ind.gameObject.AddComponent<SpriteRenderer>();
                    indSr.sprite = LoadSprite(RetinasPath + "towerDefense_tile180.png");
                    indSr.sortingOrder = 2;
                    indSr.enabled = true;

                    buildSpot.SetReferences(towerPrefab, rocketTowerPrefab, explosionPrefab, bSr, indSr);
                    EditorUtility.SetDirty(spot.gameObject);
                }
            }

            // 5. Nhóm Towers
            GameObject towersParent = FindInScene(scene, "Towers");
            if (towersParent == null)
            {
                towersParent = new GameObject("Towers");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(towersParent, scene);
            }

            // 6. WaveSpawner: 7 Đợt tổng tấn công + 3 Siêu Boss trên Map 3!
            GameObject spawnerGO = FindInScene(scene, "WaveSpawner");
            if (spawnerGO == null)
            {
                spawnerGO = new GameObject("WaveSpawner");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(spawnerGO, scene);
            }
            WaveSpawner spawner = spawnerGO.GetComponent<WaveSpawner>();
            if (spawner == null) spawner = spawnerGO.AddComponent<WaveSpawner>();
            spawner.SetReferences(
                enemies.standard,
                enemies.fast,
                enemies.heavy,
                enemies.armored,
                enemies.boss,
                pathComp
            );
            spawner.SetTotalWaves(7); // Map 3 có 7 đợt đại chiến!
            EditorUtility.SetDirty(spawnerGO);

            // 7. GameManager + SimpleHUD (Khởi đầu 220 Vàng & 30 Máu cho Map 3)
            GameObject gmGO = FindInScene(scene, "GameManager");
            if (gmGO == null)
            {
                gmGO = new GameObject("GameManager");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gmGO, scene);
            }
            GameManager gm = gmGO.GetComponent<GameManager>();
            if (gm == null) gm = gmGO.AddComponent<GameManager>();
            gm.SetStartingStats(220, 30);
            SimpleHUD hud = gmGO.GetComponent<SimpleHUD>();
            if (hud == null) hud = gmGO.AddComponent<SimpleHUD>();
            EditorUtility.SetDirty(gmGO);

            // 8. Trang trí Pháo Đài Băng Giá (Tường thành đá vát cạnh, đá tảng, rừng tùng, Sở Chỉ Huy)
            GameObject decorParent = FindInScene(scene, "Decorations");
            if (decorParent == null || decorParent.transform.childCount == 0)
            {
                if (decorParent == null)
                {
                    decorParent = new GameObject("Decorations");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(decorParent, scene);
                }
                AddMap3Decorations(decorParent);
            }
        }

        private static void AddMap3Decorations(GameObject decorParent)
        {
            string[] fortressProps = new string[]
            {
                "towerDefense_tile135.png", // Đá tảng băng xám 1
                "towerDefense_tile136.png", // Đá tảng lớn 2
                "towerDefense_tile137.png", // Đá ngũ giác 3
                "towerDefense_tile130.png", // Tán tùng lớn
                "towerDefense_tile131.png", // Bụi tùng nhỏ
            };

            List<Sprite> sprites = new List<Sprite>();
            foreach (var t in fortressProps)
            {
                Sprite s = LoadSprite(RetinasPath + t);
                if (s != null) sprites.Add(s);
            }
            if (sprites.Count == 0) return;

            Vector3[] positions = new Vector3[]
            {
                new Vector3(-13.0f, -2.5f, 0f),
                new Vector3(-12.5f,  2.5f, 0f),
                new Vector3(-11.0f, -5.5f, 0f),
                new Vector3( -6.0f,  4.0f, 0f),
                new Vector3( -3.5f,  4.8f, 0f),
                new Vector3(  1.5f,  6.2f, 0f),
                new Vector3(  7.5f,  6.8f, 0f),
                new Vector3( 12.8f,  6.2f, 0f),
                new Vector3( 13.5f,  3.5f, 0f),
                new Vector3( 13.5f,  0.5f, 0f),
                new Vector3( 13.2f, -3.5f, 0f),
                new Vector3( 11.5f, -5.5f, 0f),
                new Vector3(  6.5f, -6.2f, 0f),
                new Vector3(  2.5f, -6.2f, 0f),
                new Vector3( -6.0f, -7.2f, 0f),
                new Vector3(-10.5f, -7.2f, 0f),
                new Vector3(-13.5f, -5.0f, 0f),
                new Vector3( -6.0f, -0.5f, 0f),
                new Vector3(  9.2f,  1.5f, 0f),
                new Vector3(  9.2f,  3.2f, 0f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject d = new GameObject($"FortressDeco_{i + 1}");
                d.transform.SetParent(decorParent.transform, false);
                d.transform.position = positions[i];
                float scale = (i % 3 == 0) ? 0.88f : 0.76f;
                d.transform.localScale = Vector3.one * scale;
                SpriteRenderer sr = d.AddComponent<SpriteRenderer>();
                sr.sprite = sprites[i % sprites.Count];
                sr.sortingOrder = 1;
            }

            // Sở Chỉ Huy Tối Cao ở điểm cuối đường đi (8.5, -3.5)
            Sprite baseFortSprite = LoadSprite(RetinasPath + "towerDefense_tile182.png");
            if (baseFortSprite != null)
            {
                GameObject fortGO = new GameObject("CommandCitadel_End");
                fortGO.transform.SetParent(decorParent.transform, false);
                fortGO.transform.position = new Vector3(8.5f, -3.5f, 0f);
                fortGO.transform.localScale = Vector3.one * 1.25f;
                SpriteRenderer fortSr = fortGO.AddComponent<SpriteRenderer>();
                fortSr.sprite = baseFortSprite;
                fortSr.color = new Color(1f, 0.85f, 0.30f, 1f); // Ánh vàng chỉ huy tối cao
                fortSr.sortingOrder = 1;
            }
        }

        private static Tile GetOrCreateTile(string assetName, string spriteName)
        {
            string path = PrefabsPath + assetName;
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                Sprite sprite = LoadSprite(RetinasPath + spriteName);
                if (sprite != null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprite;
                    AssetDatabase.CreateAsset(tile, path);
                }
            }
            return tile;
        }

        private static Sprite LoadSprite(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
                if (a is Sprite s) return s;
            return null;
        }

        private static void EnsureTagExists(string tag)
        {
            Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (asset != null && asset.Length > 0)
            {
                SerializedObject tagManager = new SerializedObject(asset[0]);
                SerializedProperty tagsProp = tagManager.FindProperty("tags");
                bool found = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals(tag))
                    { found = true; break; }
                }
                if (!found)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                    tagManager.ApplyModifiedProperties();
                }
            }
        }
    }
}
