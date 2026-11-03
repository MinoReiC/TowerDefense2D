using UnityEngine;
using UnityEngine.SceneManagement;
using TowerDefense.Managers;
using TowerDefense.Towers;

namespace TowerDefense.UI
{
    public class SimpleHUD : MonoBehaviour
    {
        private GUIStyle headerStyle;
        private GUIStyle waveStyle;
        private GUIStyle subStyle;
        private GUIStyle boxStyle;
        private GUIStyle homeBtnStyle;
        private GUIStyle pauseBtnStyle;

        // Victory & Defeat Popup Styles (separate styles, không mutate chung)
        private GUIStyle victoryBoxStyle;
        private GUIStyle victoryTitleStyle;
        private GUIStyle defeatTitleStyle;      // ← Style riêng cho Defeat, không mutate victoryTitleStyle
        private GUIStyle victoryDescStyle;
        private GUIStyle victoryBtnPrimary;
        private GUIStyle victoryBtnSecondary;
        private GUIStyle pauseOverlayStyle;

        private bool stylesInitialized = false;

        // ── Cache để tránh Find/Create mỗi frame ──────────────────────────────
        private WaveSpawner        _cachedSpawner;
        private TowerBuildSpot[]   _cachedSpots;
        private float              _spotCacheTime = -99f;
        private Font               _fontHeader;
        private Font               _fontWave;
        private Font               _fontSub;
        private Font               _fontTitle;
        // ──────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Cache tham chiếu một lần khi khởi động, không Find mỗi frame
            _cachedSpawner = Object.FindAnyObjectByType<WaveSpawner>();
        }

        private void InitStyles()
        {
            // Chỉ tạo Font nếu chưa có (tránh tạo lại mỗi lần styles reset)
            if (_fontHeader == null) _fontHeader = GetVietnameseFont(21);
            if (_fontWave   == null) _fontWave   = GetVietnameseFont(18);
            if (_fontSub    == null) _fontSub    = GetVietnameseFont(14);
            if (_fontTitle  == null) _fontTitle  = GetVietnameseFont(28);

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                font = _fontHeader,
                fontSize = 21,
                fontStyle = FontStyle.Bold
            };

            waveStyle = new GUIStyle(GUI.skin.label)
            {
                font = _fontWave,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };

            subStyle = new GUIStyle(GUI.skin.label)
            {
                font = _fontSub,
                fontSize = 14,
                fontStyle = FontStyle.Normal
            };

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.08f, 0.10f, 0.16f, 0.94f));

            // Nút Về Menu ở góc trên bên phải
            homeBtnStyle = new GUIStyle(GUI.skin.button)
            {
                font = _fontSub,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            homeBtnStyle.normal.background = MakeTex(2, 2, new Color(0.12f, 0.28f, 0.52f, 0.95f));
            homeBtnStyle.hover.background  = MakeTex(2, 2, new Color(0.18f, 0.42f, 0.75f, 1f));
            homeBtnStyle.normal.textColor  = Color.white;
            homeBtnStyle.hover.textColor   = Color.white;

            // Nút Tạm Dừng / Tiếp Tục
            pauseBtnStyle = new GUIStyle(GUI.skin.button)
            {
                font = _fontSub,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            pauseBtnStyle.normal.background = MakeTex(2, 2, new Color(0.18f, 0.45f, 0.20f, 0.95f));
            pauseBtnStyle.hover.background  = MakeTex(2, 2, new Color(0.25f, 0.65f, 0.30f, 1f));
            pauseBtnStyle.normal.textColor  = Color.white;
            pauseBtnStyle.hover.textColor   = Color.white;

            // Màn phủ đen mờ khi Tạm Dừng (Pause Overlay)
            pauseOverlayStyle = new GUIStyle(GUI.skin.box);
            pauseOverlayStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.65f));

            // Box Popup chung
            victoryBoxStyle = new GUIStyle(GUI.skin.box);
            victoryBoxStyle.normal.background = MakeTex(2, 2, new Color(0.06f, 0.10f, 0.16f, 0.98f));
            victoryBoxStyle.border = new RectOffset(12, 12, 12, 12);

            // Title CHIẾN THẮNG (vàng)
            victoryTitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = _fontTitle,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            victoryTitleStyle.normal.textColor = new Color(1f, 0.88f, 0.25f, 1f);

            // Title THẤT BẠI (đỏ) — style riêng, không bao giờ mutate victoryTitleStyle
            defeatTitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = _fontTitle,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            defeatTitleStyle.normal.textColor = new Color(1f, 0.35f, 0.35f, 1f);

            victoryDescStyle = new GUIStyle(GUI.skin.label)
            {
                font = _fontSub,
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            victoryDescStyle.normal.textColor = new Color(0.9f, 0.95f, 1f, 0.95f);

            victoryBtnPrimary = new GUIStyle(GUI.skin.button)
            {
                font = _fontWave,
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            victoryBtnPrimary.normal.background = MakeTex(2, 2, new Color(0.12f, 0.65f, 0.20f, 0.98f));
            victoryBtnPrimary.hover.background  = MakeTex(2, 2, new Color(0.18f, 0.88f, 0.28f, 1f));
            victoryBtnPrimary.normal.textColor  = Color.white;

            victoryBtnSecondary = new GUIStyle(GUI.skin.button)
            {
                font = _fontWave,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            victoryBtnSecondary.normal.background = MakeTex(2, 2, new Color(0.15f, 0.38f, 0.70f, 0.96f));
            victoryBtnSecondary.hover.background  = MakeTex(2, 2, new Color(0.22f, 0.52f, 0.90f, 1f));
            victoryBtnSecondary.normal.textColor  = Color.white;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!stylesInitialized || boxStyle == null || boxStyle.normal.background == null)
            {
                InitStyles();
            }

            int gold = GameManager.Instance != null ? GameManager.Instance.Gold : 0;
            int hp   = GameManager.Instance != null ? GameManager.Instance.BaseHealth : 20;

            // ── Dùng cache thay vì Find mỗi frame ─────────────────────────────
            if (_cachedSpawner == null)
                _cachedSpawner = Object.FindAnyObjectByType<WaveSpawner>();

            // Làm mới danh sách spots mỗi 1 giây thay vì mỗi frame
            if (Time.time - _spotCacheTime > 1f)
            {
                _cachedSpots   = Object.FindObjectsByType<TowerBuildSpot>(FindObjectsSortMode.None);
                _spotCacheTime = Time.time;
            }
            // ──────────────────────────────────────────────────────────────────

            WaveSpawner spawner  = _cachedSpawner;
            int currentWave = spawner != null ? spawner.CurrentWave  : 1;
            int totalWaves  = spawner != null ? spawner.TotalWaves   : 5;
            bool isEndless  = spawner != null && spawner.IsEndless;
            int aliveCount  = spawner != null ? spawner.AliveEnemies : 0;
            bool hasWon     = spawner != null && spawner.HasWon;
            bool isReady    = spawner == null || spawner.IsPlayerReady;
            bool isGameOver = GameManager.Instance != null && GameManager.Instance.IsGameOver;
            bool isPaused   = GameManager.Instance != null && GameManager.Instance.IsPaused;

            string activeScene = SceneManager.GetActiveScene().name;
            bool isLevel2 = activeScene == "Level2Scene";
            bool isLevel3 = activeScene == "Level3Scene";
            string mapLabel = isLevel3
                ? "MÀN 3: PHÁO ĐÀI BĂNG GIÁ"
                : (isLevel2 ? "MÀN 2: SA MẠC LỬA" : "MÀN 1: BÌNH NGUYÊN");

            TowerBuildSpot[] spots = _cachedSpots ?? new TowerBuildSpot[0];
            int builtCount = 0;
            int maxSpots   = spots.Length > 0 ? spots.Length : 5;
            foreach (var s in spots)
            {
                if (s != null && s.HasTower) builtCount++;
            }

            // 1. BẢNG HUD GÓC TRÊN BÊN TRÁI
            GUILayout.BeginArea(new Rect(18, 18, 565, 168), boxStyle);
            GUILayout.Space(8);

            headerStyle.normal.textColor = new Color(1f, 0.88f, 0.2f);
            GUILayout.Label($"💰 VÀNG: {gold}    ❤️ MÁU: {hp}    🗼 THÁP: {builtCount}/{maxSpots}", headerStyle);

            GUILayout.Space(2);

            if (!isReady)
            {
                waveStyle.normal.textColor = new Color(0.35f, 1f, 0.45f);
                string prepText = currentWave <= 0
                    ? $"[{mapLabel}]  •  CHUẨN BỊ ({totalWaves} Đợt) — Chờ bấm SẴN SÀNG"
                    : $"[{mapLabel}]  •  ĐÃ THẮNG ĐỢT {currentWave}/{totalWaves} — Chờ bấm TIẾP TỤC";
                GUILayout.Label(prepText, waveStyle);
            }
            else
            {
                waveStyle.normal.textColor = new Color(0.2f, 0.95f, 1f);
                string waveText = isEndless
                    ? $"[{mapLabel}]  •  ĐỢT: {currentWave} (Vô tận)"
                    : $"[{mapLabel}]  •  ĐỢT QUÁI: {currentWave}/{totalWaves}";
                if (aliveCount > 0) waveText += $"  (Còn: {aliveCount})";
                GUILayout.Label(waveText, waveStyle);
            }

            GUILayout.Space(4);

            subStyle.normal.textColor = new Color(0.92f, 0.96f, 1f);
            GUILayout.Label("• <b>Click ô bệ trống</b>: Pháo Đơn (70) / Tên Lửa (100) / Tháp Laser Tím (150)", subStyle);
            GUILayout.Label("• <b>Click tháp đã xây</b>: NÂNG CẤP LÊN CẤP 2 & CẤP 3 (Đổi ụ súng & Tăng ST)", subStyle);

            GUILayout.EndArea();

            // 2. NÚT VỀ MENU & NÚT TẠM DỪNG GÓC TRÊN BÊN PHẢI
            if (!hasWon && !isGameOver)
            {
                float btnH = 42f;

                // Nút VỀ MENU
                float homeBtnW = 130f;
                Rect homeBtnRect = new Rect(Screen.width - homeBtnW - 18f, 18f, homeBtnW, btnH);
                if (GUI.Button(homeBtnRect, "VỀ MENU", homeBtnStyle))
                {
                    Time.timeScale = 1f;
                    MainMenuUI.OpenLevelSelectOnStart = true;
                    SceneManager.LoadScene("MainMenu");
                }

                // Nút TẠM DỪNG / TIẾP TỤC
                float pauseBtnW = 145f;
                Rect pauseBtnRect = new Rect(homeBtnRect.x - pauseBtnW - 10f, 18f, pauseBtnW, btnH);
                string pauseLabel = isPaused ? "▶ TIẾP TỤC" : "⏸ TẠM DỪNG";
                if (GUI.Button(pauseBtnRect, pauseLabel, pauseBtnStyle))
                {
                    GameManager.Instance.TogglePause();
                }
            }

            // 2B. NÚT "SẴN SÀNG" / "TIẾP TỤC ĐÁNH ĐỢT TIẾP THEO" Ở GÓC DƯỚI CÙNG BÊN PHẢI
            if (!isReady && !hasWon && !isGameOver && spawner != null)
            {
                float readyPanelW = 315f;
                float readyPanelH = 100f;
                Rect readyPanelRect = new Rect(
                    Screen.width - readyPanelW - 20f,
                    Screen.height - readyPanelH - 20f,
                    readyPanelW,
                    readyPanelH
                );

                GUI.Box(readyPanelRect, "", boxStyle);

                string promptLabel = currentWave <= 0
                    ? "<b>Hãy đặt pháo rồi bấm để xuất quân:</b>"
                    : $"<b>Đã thắng Đợt {currentWave}! Nâng cấp pháo rồi bấm:</b>";

                string btnText = currentWave <= 0
                    ? "▶   SẴN SÀNG CHIẾN ĐẤU"
                    : $"▶   TIẾP TỤC ĐÁNH ĐỢT {currentWave + 1}";

                GUI.Label(
                    new Rect(readyPanelRect.x + 12f, readyPanelRect.y + 8f, readyPanelW - 24f, 24f),
                    promptLabel,
                    victoryDescStyle
                );

                Rect readyBtnRect = new Rect(
                    readyPanelRect.x + 14f,
                    readyPanelRect.y + 36f,
                    readyPanelW - 28f,
                    50f
                );

                if (GUI.Button(readyBtnRect, btnText, victoryBtnPrimary))
                {
                    spawner.StartBattle();
                }
            }

            // 3. POPUP CHIẾN THẮNG
            if (hasWon)
            {
                DrawVictoryPopup(gold, hp, builtCount, maxSpots, totalWaves, activeScene, isLevel2, isLevel3, mapLabel);
            }
            // 4. POPUP THẤT BẠI
            else if (isGameOver)
            {
                DrawDefeatPopup(gold, currentWave, totalWaves, activeScene);
            }
            // 5. BẢNG TẠM DỪNG (KHI ĐANG PAUSE)
            else if (isPaused)
            {
                DrawPauseMenu(activeScene);
            }
        }

        private void DrawPauseMenu(string activeScene)
        {
            // Màn phủ làm mờ toàn màn hình
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", pauseOverlayStyle);

            float w = Mathf.Min(480f, Screen.width - 40f);
            float h = 330f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;

            Rect rect = new Rect(x, y, w, h);
            GUI.Box(rect, "", victoryBoxStyle);

            float padX = rect.x + 30f;
            float padW = rect.width - 60f;
            float curY = rect.y + 25f;

            GUI.Label(new Rect(padX, curY, padW, 44f), "⏸ TRÒ CHƠI ĐANG TẠM DỪNG", victoryTitleStyle);
            curY += 50f;

            GUI.Label(new Rect(padX, curY, padW, 30f), "Mọi hành động và quái vật đang được dừng lại.", victoryDescStyle);
            curY += 45f;

            // Nút Tiếp Tục Chơi
            if (GUI.Button(new Rect(padX, curY, padW, 50f), "▶ TIẾP TỤC CHƠI", victoryBtnPrimary))
            {
                GameManager.Instance.SetPaused(false);
            }
            curY += 60f;

            // Nút Chơi Lại Màn Này
            if (GUI.Button(new Rect(padX, curY, padW, 46f), "🔄 CHƠI LẠI MÀN NÀY", victoryBtnSecondary))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(activeScene);
            }
            curY += 54f;

            // Nút Về Menu Chọn Màn
            if (GUI.Button(new Rect(padX, curY, padW, 42f), "QUAY VỀ CHỌN MÀN CHƠI", homeBtnStyle))
            {
                Time.timeScale = 1f;
                MainMenuUI.OpenLevelSelectOnStart = true;
                SceneManager.LoadScene("MainMenu");
            }
        }

        private void DrawVictoryPopup(int gold, int hp, int builtCount, int maxSpots, int totalWaves, string activeScene, bool isLevel2, bool isLevel3, string mapLabel)
        {
            bool hasNextLevel = !isLevel3;
            float w = Mathf.Min(600f, Screen.width - 40f);
            float h = hasNextLevel ? 445f : 385f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;

            Rect rect = new Rect(x, y, w, h);
            GUI.Box(rect, "", victoryBoxStyle);

            float padX = rect.x + 30f;
            float padW = rect.width - 60f;
            float curY = rect.y + 24f;

            GUI.Label(new Rect(padX, curY, padW, 46f), "CHIẾN THẮNG OANH LIỆT!", victoryTitleStyle);
            curY += 48f;

            string victoryMsg = isLevel3
                ? $"<b>Chúc mừng Đại Chỉ Huy!</b>\nBạn đã chinh phục hoàn toàn cả 3 màn chơi và bảo vệ thành công <b>{mapLabel}</b> trước {totalWaves} đợt tổng tấn công!"
                : $"<b>Chúc mừng Chỉ Huy!</b>\nBạn đã xuất sắc tiêu diệt toàn bộ {totalWaves} đợt quái vật và bảo vệ căn cứ <b>{mapLabel}</b> thành công rực rỡ!";
            GUI.Label(new Rect(padX, curY, padW, 54f), victoryMsg, victoryDescStyle);
            curY += 58f;

            string statsText = $"Vàng tích lũy: <b>{gold}</b>    |    Máu căn cứ còn lại: <b>{hp}</b>    |    Số tháp đã xây: <b>{builtCount}/{maxSpots}</b>";
            GUI.Label(new Rect(padX, curY, padW, 28f), statsText, victoryDescStyle);
            curY += 40f;

            // Nếu vừa thắng Màn 1 -> sang Màn 2; nếu vừa thắng Màn 2 -> sang Màn 3!
            if (!isLevel2 && !isLevel3)
            {
                if (GUI.Button(new Rect(padX, curY, padW, 56f), "CHƠI TIẾP MÀN 2: SA MẠC LỬA  ▶", victoryBtnPrimary))
                {
                    SceneManager.LoadScene("Level2Scene");
                }
                curY += 66f;
            }
            else if (isLevel2)
            {
                if (GUI.Button(new Rect(padX, curY, padW, 56f), "CHƠI TIẾP MÀN 3: PHÁO ĐÀI BĂNG GIÁ  ▶", victoryBtnPrimary))
                {
                    SceneManager.LoadScene("Level3Scene");
                }
                curY += 66f;
            }

            if (GUI.Button(new Rect(padX, curY, padW, 52f), "QUAY VỀ CHỌN MÀN CHƠI", isLevel3 ? victoryBtnPrimary : victoryBtnSecondary))
            {
                MainMenuUI.OpenLevelSelectOnStart = true;
                SceneManager.LoadScene("MainMenu");
            }
            curY += 62f;

            if (GUI.Button(new Rect(padX, curY, padW, 44f), $"CHƠI LẠI {mapLabel}", victoryBtnSecondary))
            {
                SceneManager.LoadScene(activeScene);
            }
        }

        private void DrawDefeatPopup(int gold, int currentWave, int totalWaves, string activeScene)
        {
            float w = Mathf.Min(560f, Screen.width - 40f);
            float h = 350f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;

            Rect rect = new Rect(x, y, w, h);
            GUI.Box(rect, "", victoryBoxStyle);

            float padX = rect.x + 30f;
            float padW = rect.width - 60f;
            float curY = rect.y + 25f;

            // Dùng defeatTitleStyle riêng — KHÔNG mutate victoryTitleStyle
            GUI.Label(new Rect(padX, curY, padW, 46f), "CĂN CỨ ĐÃ BỊ PHÁ HỦY!", defeatTitleStyle);
            curY += 50f;

            string defeatMsg = $"Kẻ địch đã đột phá hàng phòng ngự ở Đợt {currentWave}/{totalWaves}.\nHãy xây thêm pháo và nâng cấp Cấp 2 kịp thời để bảo vệ căn cứ!";
            GUI.Label(new Rect(padX, curY, padW, 55f), defeatMsg, victoryDescStyle);
            curY += 65f;

            if (GUI.Button(new Rect(padX, curY, padW, 56f), "THỬ LẠI NGAY", victoryBtnPrimary))
            {
                SceneManager.LoadScene(activeScene);
            }
            curY += 66f;

            if (GUI.Button(new Rect(padX, curY, padW, 46f), "QUAY VỀ CHỌN MÀN CHƠI", victoryBtnSecondary))
            {
                MainMenuUI.OpenLevelSelectOnStart = true;
                SceneManager.LoadScene("MainMenu");
            }
        }

        private static Font GetVietnameseFont(int size)
        {
            return Font.CreateDynamicFontFromOSFont(new string[] { "Segoe UI", "Arial", "Tahoma" }, size);
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D t = new Texture2D(w, h);
            t.SetPixels(pix);
            t.Apply();
            return t;
        }
    }
}
