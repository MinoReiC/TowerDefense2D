using UnityEngine;
using UnityEngine.SceneManagement;
using TowerDefense.Managers;
using TowerDefense.Towers;

namespace TowerDefense.UI
{
    /// <summary>
    /// Giao diện trong trận (HUD, Thanh Tạm Dừng, Nút Sẵn Sàng, Menu Tạm Dừng Tích Hợp,
    /// Bảng Chiến Thắng và Bảng Thất Bại) thiết kế đồng bộ theo phong cách Kingdom Rush
    /// (Khung gỗ tối viền vàng kim bo góc, huy hiệu thông số, nút bóng 3D, kiểu chữ chuẩn tiếng Việt).
    /// </summary>
    public class SimpleHUD : MonoBehaviour
    {
        // ── Styles ────────────────────────────────────────────────────────────
        private GUIStyle _hudBoxStyle;
        private GUIStyle _popupBoxStyle;
        private GUIStyle _chipStyle;
        private GUIStyle _ribbonStyle;
        private GUIStyle _ribbonGreenStyle;

        private GUIStyle _headerGoldStyle;
        private GUIStyle _headerHpStyle;
        private GUIStyle _headerTowerStyle;
        private GUIStyle _waveTextStyle;
        private GUIStyle _subTextStyle;

        private GUIStyle _btnPrimary;
        private GUIStyle _btnSecondary;
        private GUIStyle _btnPause;
        private GUIStyle _btnDanger;
        private GUIStyle _btnHome;

        private GUIStyle _popupTitleStyle;
        private GUIStyle _defeatTitleStyle;
        private GUIStyle _popupDescStyle;
        private GUIStyle _popupStatsStyle;
        private GUIStyle _promptLabelStyle;

        private Texture2D _overlayTex;
        private bool _stylesInitialized = false;

        // ── Cache Fonts ───────────────────────────────────────────────────────
        private Font _fontTitle;
        private Font _fontHeader;
        private Font _fontBtn;
        private Font _fontText;
        private Font _fontSmall;

        // ── Cache Scene Components ────────────────────────────────────────────
        private WaveSpawner      _cachedSpawner;
        private TowerBuildSpot[] _cachedSpots;
        private float            _spotCacheTime = -99f;

        private void Awake()
        {
            _cachedSpawner = Object.FindAnyObjectByType<WaveSpawner>();
        }

        private void OnGUI()
        {
            if (!_stylesInitialized || _hudBoxStyle == null || _hudBoxStyle.normal.background == null)
            {
                InitStyles();
            }

            int gold = GameManager.Instance != null ? GameManager.Instance.Gold : 0;
            int hp   = GameManager.Instance != null ? GameManager.Instance.BaseHealth : 20;

            if (_cachedSpawner == null)
                _cachedSpawner = Object.FindAnyObjectByType<WaveSpawner>();

            if (Time.time - _spotCacheTime > 1f)
            {
                _cachedSpots   = Object.FindObjectsByType<TowerBuildSpot>(FindObjectsSortMode.None);
                _spotCacheTime = Time.time;
            }

            WaveSpawner spawner = _cachedSpawner;
            int currentWave     = spawner != null ? spawner.CurrentWave  : 1;
            int totalWaves      = spawner != null ? spawner.TotalWaves   : 5;
            bool isEndless      = spawner != null && spawner.IsEndless;
            int aliveCount      = spawner != null ? spawner.AliveEnemies : 0;
            bool hasWon         = spawner != null && spawner.HasWon;
            bool isReady        = spawner == null || spawner.IsPlayerReady;
            bool isGameOver     = GameManager.Instance != null && GameManager.Instance.IsGameOver;
            bool isPaused       = GameManager.Instance != null && GameManager.Instance.IsPaused;

            string activeScene  = SceneManager.GetActiveScene().name;
            bool isLevel2       = activeScene == "Level2Scene";
            bool isLevel3       = activeScene == "Level3Scene";
            string mapLabel     = isLevel3
                ? "MÀN 3: PHÁO ĐÀI BĂNG GIÁ"
                : (isLevel2 ? "MÀN 2: SA MẠC LỬA" : "MÀN 1: BÌNH NGUYÊN");

            TowerBuildSpot[] spots = _cachedSpots ?? new TowerBuildSpot[0];
            int builtCount = 0;
            int maxSpots   = spots.Length > 0 ? spots.Length : 5;
            foreach (var s in spots)
            {
                if (s != null && s.HasTower) builtCount++;
            }

            // ════════════════════════════════════════════════════════════════
            // 1. Ô HIỂN THỊ TIỀN, MÁU, WAVE QUÁI (GÓC TRÊN BÊN TRÁI)
            // ════════════════════════════════════════════════════════════════
            DrawResourceAndWaveHUD(gold, hp, builtCount, maxSpots, currentWave, totalWaves, isEndless, aliveCount, isReady, mapLabel);

            // ════════════════════════════════════════════════════════════════
            // 2. Ô CHỌN TẠM DỪNG & VỀ MENU (GÓC TRÊN BÊN PHẢI)
            // ════════════════════════════════════════════════════════════════
            if (!hasWon && !isGameOver)
            {
                DrawTopRightControls(isPaused);
            }

            // ════════════════════════════════════════════════════════════════
            // 3. Ô BẮT ĐẦU MỖI ROUND / SẴN SÀNG (GÓC DƯỚI BÊN PHẢI)
            // ════════════════════════════════════════════════════════════════
            if (!isReady && !hasWon && !isGameOver && spawner != null)
            {
                DrawRoundStartPanel(currentWave, spawner);
            }

            // ════════════════════════════════════════════════════════════════
            // 4. MENU TẠM DỪNG TÍCH HỢP (KHI ĐANG PAUSE)
            // ════════════════════════════════════════════════════════════════
            if (isPaused && !hasWon && !isGameOver)
            {
                DrawIntegratedPauseMenu(gold, hp, currentWave, totalWaves, activeScene, mapLabel);
            }
            // ════════════════════════════════════════════════════════════════
            // 5. BẢNG CHIẾN THẮNG
            // ════════════════════════════════════════════════════════════════
            else if (hasWon)
            {
                DrawVictoryPopup(gold, hp, builtCount, maxSpots, totalWaves, activeScene, isLevel2, isLevel3, mapLabel);
            }
            // ════════════════════════════════════════════════════════════════
            // 6. BẢNG THẤT BẠI
            // ════════════════════════════════════════════════════════════════
            else if (isGameOver)
            {
                DrawDefeatPopup(gold, currentWave, totalWaves, activeScene, mapLabel);
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // 1. HUD GÓC TRÊN TRÁI: TIỀN, MÁU, THÁP, ĐỢT QUÁI
        // ────────────────────────────────────────────────────────────────────
        private void DrawResourceAndWaveHUD(int gold, int hp, int builtCount, int maxSpots,
            int currentWave, int totalWaves, bool isEndless, int aliveCount, bool isReady, string mapLabel)
        {
            float hudW = 440f;
            float hudH = 148f;
            Rect hudRect = new Rect(16f, 16f, hudW, hudH);

            GUI.Box(hudRect, "", _hudBoxStyle);

            float padX = hudRect.x + 12f;
            float padW = hudRect.width - 24f;
            float curY = hudRect.y + 10f;

            // Hàng 1: 3 Huy hiệu (Chips) thông số: Vàng | Máu | Tháp
            float chipGap = 8f;
            float chipW   = (padW - chipGap * 2f) / 3f;
            float chipH   = 32f;

            // Chip Vàng
            Rect chipGoldRect = new Rect(padX, curY, chipW, chipH);
            GUI.Box(chipGoldRect, "", _chipStyle);
            GUI.Label(chipGoldRect, $"🪙 <b>{gold}</b> VÀNG", _headerGoldStyle);

            // Chip Máu
            Rect chipHpRect = new Rect(padX + chipW + chipGap, curY, chipW, chipH);
            GUI.Box(chipHpRect, "", _chipStyle);
            GUI.Label(chipHpRect, $"❤️ <b>{hp}</b> MÁU", _headerHpStyle);

            // Chip Tháp
            Rect chipTowerRect = new Rect(padX + (chipW + chipGap) * 2f, curY, chipW, chipH);
            GUI.Box(chipTowerRect, "", _chipStyle);
            GUI.Label(chipTowerRect, $"🗼 <b>{builtCount}/{maxSpots}</b> THÁP", _headerTowerStyle);

            curY += chipH + 8f;

            // Hàng 2: Dải Ribbon đợt quái
            Rect ribbonRect = new Rect(padX, curY, padW, 30f);
            if (!isReady)
            {
                GUI.Box(ribbonRect, "", _ribbonGreenStyle);
                string prepText = currentWave <= 0
                    ? $"[{mapLabel}]  •  CHUẨN BỊ ({totalWaves} ĐỢT) — CHỜ SẴN SÀNG"
                    : $"[{mapLabel}]  •  THẮNG ĐỢT {currentWave}/{totalWaves} — CHỜ TIẾP TỤC";
                GUI.Label(ribbonRect, prepText, _waveTextStyle);
            }
            else
            {
                GUI.Box(ribbonRect, "", _ribbonStyle);
                string waveText = isEndless
                    ? $"[{mapLabel}]  •  ĐỢT {currentWave} (VÔ TẬN)"
                    : $"[{mapLabel}]  •  ĐỢT QUÁI: {currentWave}/{totalWaves}";
                if (aliveCount > 0) waveText += $"  (CÒN {aliveCount} QUÁI)";
                GUI.Label(ribbonRect, waveText, _waveTextStyle);
            }

            curY += 34f;

            // Hàng 3: Dòng gợi ý click tháp gọn gàng, sắc nét
            GUI.Label(new Rect(padX + 2f, curY, padW, 20f),
                "• <b>Click ô bệ</b>: Pháo Đơn (70) / Tên Lửa (100) / Laser Tím (150)", _subTextStyle);
            curY += 19f;
            GUI.Label(new Rect(padX + 2f, curY, padW, 20f),
                "• <b>Click tháp đã xây</b>: Nâng Cấp Lên Cấp 2 & Cấp 3 (Tăng ST)", _subTextStyle);
        }

        // ────────────────────────────────────────────────────────────────────
        // 2. NÚT TẠM DỪNG & VỀ MENU GÓC TRÊN PHẢI
        // ────────────────────────────────────────────────────────────────────
        private void DrawTopRightControls(bool isPaused)
        {
            float btnH     = 42f;
            float homeBtnW = 120f;
            float homeX    = Screen.width - homeBtnW - 16f;
            Rect homeBtnRect = new Rect(homeX, 16f, homeBtnW, btnH);

            if (GUI.Button(homeBtnRect, "🏠 MENU", _btnHome))
            {
                Time.timeScale = 1f;
                MainMenuUI.OpenLevelSelectOnStart = true;
                SceneManager.LoadScene("MainMenu");
            }

            float pauseBtnW = 135f;
            Rect pauseBtnRect = new Rect(homeX - pauseBtnW - 10f, 16f, pauseBtnW, btnH);
            string pauseLabel = isPaused ? "▶ TIẾP TỤC" : "⏸ TẠM DỪNG";
            GUIStyle pStyle   = isPaused ? _btnPrimary : _btnPause;

            if (GUI.Button(pauseBtnRect, pauseLabel, pStyle))
            {
                GameManager.Instance.TogglePause();
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // 3. Ô BẮT ĐẦU MỖI ROUND / SẴN SÀNG (GÓC DƯỚI PHẢI)
        // ────────────────────────────────────────────────────────────────────
        private void DrawRoundStartPanel(int currentWave, WaveSpawner spawner)
        {
            float panelW = 320f;
            float panelH = 104f;
            Rect panelRect = new Rect(
                Screen.width - panelW - 18f,
                Screen.height - panelH - 18f,
                panelW,
                panelH
            );

            GUI.Box(panelRect, "", _hudBoxStyle);

            string prompt = currentWave <= 0
                ? "Bố trí pháo phòng thủ rồi bấm xuất quân:"
                : $"Đã xong Đợt {currentWave}! Nâng cấp pháo rồi bấm:";

            GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 8f, panelW - 20f, 24f), prompt, _promptLabelStyle);

            string btnText = currentWave <= 0
                ? "▶   SẴN SÀNG CHIẾN ĐẤU"
                : $"▶   XUẤT QUÂN ĐỢT {currentWave + 1}";

            Rect btnRect = new Rect(panelRect.x + 12f, panelRect.y + 36f, panelW - 24f, 54f);
            if (GUI.Button(btnRect, btnText, _btnPrimary))
            {
                spawner.StartBattle();
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // 4. MENU TẠM DỪNG TÍCH HỢP (IN-GAME MAIN MENU POPUP)
        // ────────────────────────────────────────────────────────────────────
        private void DrawIntegratedPauseMenu(int gold, int hp, int currentWave, int totalWaves, string activeScene, string mapLabel)
        {
            if (_overlayTex != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex);

            float w = Mathf.Min(520f, Screen.width - 40f);
            float h = 470f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            Rect winRect = new Rect(x, y, w, h);
            GUI.Box(winRect, "", _popupBoxStyle);

            float padX = winRect.x + 36f;
            float padW = winRect.width - 72f;
            float curY = winRect.y + 24f;

            // Tiêu đề lớn đổ bóng
            ShadowLabel(new Rect(padX, curY, padW, 46f), "★   TRÒ CHƠI TẠM DỪNG   ★", _popupTitleStyle);
            curY += 50f;

            // Dải ruy băng phụ đề
            Rect ribbon = new Rect(padX, curY, padW, 30f);
            GUI.Box(ribbon, "", _ribbonStyle);
            GUI.Label(ribbon, "Mọi hoạt động và quái vật đang được tạm dừng", _waveTextStyle);
            curY += 40f;

            // Khung tóm tắt thông số trận đấu hiện tại
            Rect statsBox = new Rect(padX, curY, padW, 58f);
            GUI.Box(statsBox, "", _chipStyle);
            GUI.Label(new Rect(statsBox.x, statsBox.y + 6f, statsBox.width, 22f),
                $"Chiến trường: <color=#FFE680><b>{mapLabel}</b></color>", _popupStatsStyle);
            GUI.Label(new Rect(statsBox.x, statsBox.y + 30f, statsBox.width, 22f),
                $"Vàng: <color=#FFDD44><b>{gold}</b></color>   |   Máu căn cứ: <color=#FF6666><b>{hp}</b></color>   |   Đợt quái: <color=#66DDFF><b>{currentWave}/{totalWaves}</b></color>",
                _popupStatsStyle);
            curY += 72f;

            // Nút 1: TIẾP TỤC CHIẾN ĐẤU (Xanh lục to nhất)
            if (GUI.Button(new Rect(padX, curY, padW, 54f), "▶   TIẾP TỤC CHIẾN ĐẤU", _btnPrimary))
            {
                GameManager.Instance.SetPaused(false);
            }
            curY += 64f;

            // Nút 2: CHƠI LẠI MÀN NÀY (Xanh lam)
            if (GUI.Button(new Rect(padX, curY, padW, 46f), "🔄   CHƠI LẠI MÀN NÀY", _btnSecondary))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(activeScene);
            }
            curY += 54f;

            // Nút 3: CHỌN MÀN CHƠI KHÁC (Gỗ viền vàng kim)
            if (GUI.Button(new Rect(padX, curY, padW, 46f), "🗺️   CHỌN MÀN CHƠI KHÁC", _btnPause))
            {
                Time.timeScale = 1f;
                MainMenuUI.OpenLevelSelectOnStart = true;
                SceneManager.LoadScene("MainMenu");
            }
            curY += 54f;

            // Nút 4: VỀ TRANG CHỦ MENU (Đỏ gỗ)
            if (GUI.Button(new Rect(padX, curY, padW, 42f), "🏠   VỀ TRANG CHỦ MENU", _btnDanger))
            {
                Time.timeScale = 1f;
                MainMenuUI.OpenLevelSelectOnStart = false;
                SceneManager.LoadScene("MainMenu");
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // 5. BẢNG CHIẾN THẮNG (VICTORY POPUP)
        // ────────────────────────────────────────────────────────────────────
        private void DrawVictoryPopup(int gold, int hp, int builtCount, int maxSpots, int totalWaves,
            string activeScene, bool isLevel2, bool isLevel3, string mapLabel)
        {
            if (_overlayTex != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex);

            bool hasNextLevel = !isLevel3;
            float w = Mathf.Min(580f, Screen.width - 40f);
            float h = hasNextLevel ? 460f : 400f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            Rect rect = new Rect(x, y, w, h);
            GUI.Box(rect, "", _popupBoxStyle);

            float padX = rect.x + 36f;
            float padW = rect.width - 72f;
            float curY = rect.y + 24f;

            ShadowLabel(new Rect(padX, curY, padW, 46f), "★   CHIẾN THẮNG OANH LIỆT!   ★", _popupTitleStyle);
            curY += 50f;

            string victoryMsg = isLevel3
                ? $"<b>Chúc mừng Đại Chỉ Huy!</b>\nBạn đã chinh phục hoàn toàn 3 chiến trường và bảo vệ thành công <b>{mapLabel}</b> trước {totalWaves} đợt tổng tấn công!"
                : $"<b>Chúc mừng Chỉ Huy!</b>\nBạn đã xuất sắc tiêu diệt toàn bộ {totalWaves} đợt quái vật và bảo vệ <b>{mapLabel}</b> an toàn tuyệt đối!";
            GUI.Label(new Rect(padX, curY, padW, 56f), victoryMsg, _popupDescStyle);
            curY += 60f;

            Rect statsBox = new Rect(padX, curY, padW, 36f);
            GUI.Box(statsBox, "", _chipStyle);
            string statsText = $"Vàng tích lũy: <b>{gold}</b>   |   Máu còn lại: <b>{hp}</b>   |   Số tháp: <b>{builtCount}/{maxSpots}</b>";
            GUI.Label(statsBox, statsText, _popupStatsStyle);
            curY += 46f;

            // Nút chơi màn kế tiếp
            if (!isLevel2 && !isLevel3)
            {
                if (GUI.Button(new Rect(padX, curY, padW, 54f), "CHƠI TIẾP MÀN 2: SA MẠC LỬA  ▶", _btnPrimary))
                {
                    SceneManager.LoadScene("Level2Scene");
                }
                curY += 64f;
            }
            else if (isLevel2)
            {
                if (GUI.Button(new Rect(padX, curY, padW, 54f), "CHƠI TIẾP MÀN 3: PHÁO ĐÀI BĂNG GIÁ  ▶", _btnPrimary))
                {
                    SceneManager.LoadScene("Level3Scene");
                }
                curY += 64f;
            }

            if (GUI.Button(new Rect(padX, curY, padW, 46f), "QUAY VỀ CHỌN MÀN CHƠI", _btnSecondary))
            {
                MainMenuUI.OpenLevelSelectOnStart = true;
                SceneManager.LoadScene("MainMenu");
            }
            curY += 54f;

            if (GUI.Button(new Rect(padX, curY, padW, 42f), $"🔄   CHƠI LẠI {mapLabel}", _btnPause))
            {
                SceneManager.LoadScene(activeScene);
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // 6. BẢNG THẤT BẠI (DEFEAT POPUP)
        // ────────────────────────────────────────────────────────────────────
        private void DrawDefeatPopup(int gold, int currentWave, int totalWaves, string activeScene, string mapLabel)
        {
            if (_overlayTex != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex);

            float w = Mathf.Min(540f, Screen.width - 40f);
            float h = 390f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            Rect rect = new Rect(x, y, w, h);
            GUI.Box(rect, "", _popupBoxStyle);

            float padX = rect.x + 36f;
            float padW = rect.width - 72f;
            float curY = rect.y + 24f;

            ShadowLabel(new Rect(padX, curY, padW, 46f), "💀   CĂN CỨ ĐÃ BỊ PHÁ HỦY!   💀", _defeatTitleStyle);
            curY += 50f;

            string defeatMsg = $"Kẻ địch đã xuyên thủng phòng tuyến tại <b>{mapLabel}</b> ở Đợt {currentWave}/{totalWaves}.\nHãy bổ sung thêm tháp pháo và nâng cấp Cấp 2 kịp thời để đẩy lui quái vật!";
            GUI.Label(new Rect(padX, curY, padW, 56f), defeatMsg, _popupDescStyle);
            curY += 64f;

            if (GUI.Button(new Rect(padX, curY, padW, 54f), "🔄   THỬ LẠI NGAY", _btnPrimary))
            {
                SceneManager.LoadScene(activeScene);
            }
            curY += 64f;

            if (GUI.Button(new Rect(padX, curY, padW, 46f), "🗺️   QUAY VỀ CHỌN MÀN CHƠI", _btnSecondary))
            {
                MainMenuUI.OpenLevelSelectOnStart = true;
                SceneManager.LoadScene("MainMenu");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // KHỞI TẠO STYLES ĐỒNG BỘ KINGDOM RUSH
        // ════════════════════════════════════════════════════════════════════
        private void InitStyles()
        {
            if (_overlayTex == null)
                _overlayTex = MakeTex(2, 2, new Color(0.02f, 0.03f, 0.05f, 0.55f));

            if (_fontTitle  == null) _fontTitle  = GetVietnameseFont(28);
            if (_fontHeader == null) _fontHeader = GetVietnameseFont(18);
            if (_fontBtn    == null) _fontBtn    = GetVietnameseFont(16);
            if (_fontText   == null) _fontText   = GetVietnameseFont(14);
            if (_fontSmall  == null) _fontSmall  = GetVietnameseFont(12);

            Color goldBorder = new Color(0.95f, 0.74f, 0.27f);
            Color goldHi     = new Color(1f, 0.90f, 0.45f);

            // 1. Khung hộp gỗ viền vàng kim
            _hudBoxStyle = BoxStyle(
                new Color(0.24f, 0.15f, 0.09f, 0.96f),
                new Color(0.12f, 0.08f, 0.05f, 0.96f),
                goldBorder, 2
            );

            _popupBoxStyle = BoxStyle(
                new Color(0.30f, 0.19f, 0.11f, 0.98f),
                new Color(0.14f, 0.09f, 0.05f, 0.98f),
                goldHi, 3
            );

            _chipStyle = BoxStyle(
                new Color(0.08f, 0.05f, 0.03f, 0.95f),
                new Color(0.12f, 0.08f, 0.05f, 0.95f),
                new Color(0.55f, 0.42f, 0.22f), 1
            );

            _ribbonStyle = BoxStyle(
                new Color(0.55f, 0.13f, 0.12f, 0.98f),
                new Color(0.36f, 0.07f, 0.07f, 0.98f),
                goldBorder, 1
            );

            _ribbonGreenStyle = BoxStyle(
                new Color(0.12f, 0.42f, 0.16f, 0.98f),
                new Color(0.08f, 0.28f, 0.10f, 0.98f),
                goldBorder, 1
            );

            // 2. Kiểu chữ
            _headerGoldStyle = LabelStyle(_fontHeader, 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.90f, 0.32f));
            _headerHpStyle   = LabelStyle(_fontHeader, 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.42f, 0.42f));
            _headerTowerStyle= LabelStyle(_fontHeader, 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.48f, 0.90f, 1f));

            _waveTextStyle   = LabelStyle(_fontText, 13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.82f));
            _subTextStyle    = LabelStyle(_fontSmall, 12, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.92f, 0.88f, 0.80f));
            _subTextStyle.richText = true;

            _promptLabelStyle= LabelStyle(_fontSmall, 13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.70f));
            _promptLabelStyle.richText = true;

            _popupTitleStyle = LabelStyle(_fontTitle, 26, FontStyle.Bold, TextAnchor.MiddleCenter, goldHi);
            _defeatTitleStyle= LabelStyle(_fontTitle, 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.35f));

            _popupDescStyle  = LabelStyle(_fontText, 14, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.96f, 0.92f, 0.84f));
            _popupDescStyle.wordWrap = true;
            _popupDescStyle.richText = true;

            _popupStatsStyle = LabelStyle(_fontText, 13, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.92f, 0.90f, 0.85f));
            _popupStatsStyle.richText = true;

            // 3. Nút bấm phong cách Kingdom Rush (3D gradient + viền vàng)
            _btnPrimary = ButtonStyle(
                17,
                new Color(0.38f, 0.82f, 0.26f),
                new Color(0.14f, 0.50f, 0.12f),
                goldBorder,
                Color.white
            );

            _btnSecondary = ButtonStyle(
                16,
                new Color(0.30f, 0.58f, 0.92f),
                new Color(0.12f, 0.30f, 0.62f),
                goldBorder,
                Color.white
            );

            _btnPause = ButtonStyle(
                15,
                new Color(0.48f, 0.32f, 0.16f),
                new Color(0.26f, 0.16f, 0.08f),
                goldBorder,
                new Color(1f, 0.92f, 0.70f)
            );

            _btnHome = ButtonStyle(
                15,
                new Color(0.24f, 0.40f, 0.68f),
                new Color(0.12f, 0.20f, 0.42f),
                goldBorder,
                Color.white
            );

            _btnDanger = ButtonStyle(
                15,
                new Color(0.82f, 0.28f, 0.22f),
                new Color(0.48f, 0.10f, 0.10f),
                goldBorder,
                new Color(1f, 0.92f, 0.90f)
            );

            _stylesInitialized = true;
        }

        private void ShadowLabel(Rect r, string text, GUIStyle style)
        {
            Color c = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, style);
            style.normal.textColor = c;
            GUI.Label(r, text, style);
        }

        private GUIStyle LabelStyle(Font font, int size, FontStyle fs, TextAnchor anchor, Color color)
        {
            GUIStyle st = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = size,
                fontStyle = fs,
                alignment = anchor,
                richText = true
            };
            st.normal.textColor = color;
            return st;
        }

        private GUIStyle BoxStyle(Color top, Color bottom, Color border, int bw)
        {
            GUIStyle st = new GUIStyle(GUI.skin.box);
            st.normal.background = MakeRounded(48, 14, top, bottom, border, bw);
            st.border = new RectOffset(16, 16, 16, 16);
            st.padding = new RectOffset(0, 0, 0, 0);
            st.margin  = new RectOffset(0, 0, 0, 0);
            return st;
        }

        private GUIStyle ButtonStyle(int fontSize, Color top, Color bottom, Color border, Color text)
        {
            GUIStyle st = new GUIStyle(GUI.skin.button)
            {
                font = _fontBtn,
                fontStyle = FontStyle.Bold,
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter
            };
            Texture2D normal = MakeRounded(48, 14, top, bottom, border, 2);
            Texture2D hover  = MakeRounded(48, 14, Color.Lerp(top, Color.white, 0.25f), Color.Lerp(bottom, Color.white, 0.18f), new Color(1f, 0.92f, 0.5f), 3);
            Texture2D active = MakeRounded(48, 14, Color.Lerp(bottom, Color.black, 0.15f), Color.Lerp(bottom, Color.black, 0.35f), border, 2);

            st.normal.background  = normal;
            st.hover.background   = hover;
            st.active.background  = active;
            st.focused.background = normal;
            st.border  = new RectOffset(16, 16, 16, 16);
            st.padding = new RectOffset(6, 6, 4, 4);
            st.margin  = new RectOffset(0, 0, 0, 0);

            st.normal.textColor  = text;
            st.hover.textColor   = Color.white;
            st.active.textColor  = text;
            st.focused.textColor = text;
            return st;
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

        private static Texture2D MakeRounded(int size, int radius, Color top, Color bottom, Color border, int borderWidth)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] pix = new Color[size * size];
            float half = size * 0.5f;
            float inner = half - radius;

            for (int y = 0; y < size; y++)
            {
                float t = y / (float)(size - 1);
                Color fill = Color.Lerp(bottom, top, t);
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - half) - inner;
                    float py = Mathf.Abs(y + 0.5f - half) - inner;
                    float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
                    float d = outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                    float alpha = Mathf.Clamp01(0.5f - d);

                    Color c = fill;
                    if (d > -borderWidth)
                        c = border;
                    else if (d > -borderWidth - 2f)
                        c = Color.Lerp(fill, Color.white, 0.14f);

                    c.a *= alpha;
                    pix[y * size + x] = c;
                }
            }
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}
