using UnityEngine;
using UnityEngine.SceneManagement;
using TowerDefense.Managers;
using TowerDefense.Towers;

namespace TowerDefense.UI
{
    /// <summary>
    /// Giao diện trong trận phong cách Kingdom Rush tự động co giãn theo độ phân giải màn hình
    /// (Hỗ trợ sắc nét từ HD, Full HD 1080p đến QHD 2K 1440p và 4K):
    /// - HUD góc trên bên trái: Tiền Vàng to rõ, Máu căn cứ kèm Thanh Máu tiến trình trực quan, Số tháp, Đợt quái.
    /// - Nút Tạm Dừng góc trên bên phải: To, rõ, nổi bật dễ thao tác.
    /// - Nút Sẵn Sàng / Xuất Quân góc dưới bên phải: To bản, bắt mắt.
    /// - Menu Tạm Dừng, Bảng Chiến Thắng và Bảng Thất Bại tích hợp toàn diện.
    /// - 100% Font chuẩn tiếng Việt (Segoe UI / Arial / Tahoma), không emoji gây lỗi ô vuông.
    /// </summary>
    public class SimpleHUD : MonoBehaviour
    {
        // ── Styles ────────────────────────────────────────────────────────────
        private GUIStyle _hudBoxStyle;
        private GUIStyle _popupBoxStyle;
        private GUIStyle _chipStyle;
        private GUIStyle _ribbonStyle;
        private GUIStyle _ribbonGreenStyle;

        private GUIStyle _chipTitleGoldStyle;
        private GUIStyle _chipValGoldStyle;
        private GUIStyle _chipTitleHpStyle;
        private GUIStyle _chipValHpStyle;
        private GUIStyle _chipTitleTowerStyle;
        private GUIStyle _chipValTowerStyle;

        private GUIStyle _waveTextStyle;
        private GUIStyle _subTextStyle;

        private GUIStyle _btnPrimary;
        private GUIStyle _btnSecondary;
        private GUIStyle _btnPause;
        private GUIStyle _btnDanger;

        private GUIStyle _popupTitleStyle;
        private GUIStyle _defeatTitleStyle;
        private GUIStyle _popupDescStyle;
        private GUIStyle _popupStatsStyle;
        private GUIStyle _promptLabelStyle;

        private Texture2D _overlayTex;
        private Texture2D _whiteTex;
        private bool _stylesInitialized = false;
        private float _lastUiScale = -1f;

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
            // Tỷ lệ co giãn UI dựa trên chiều cao màn hình (Chuẩn 1080p = 1.0, QHD 1440p = 1.33x, 4K = 1.85x)
            float uiScale = Mathf.Clamp(Screen.height / 1080f, 0.85f, 1.85f);

            if (!_stylesInitialized || Mathf.Abs(_lastUiScale - uiScale) > 0.02f || _hudBoxStyle == null || _hudBoxStyle.normal.background == null)
            {
                InitStyles(uiScale);
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
            // 1. Ô HIỂN THỊ TIỀN, MÁU (KÈM THANH MÁU), THÁP & WAVE QUÁI (TO RÕ RÀNG)
            // ════════════════════════════════════════════════════════════════
            DrawResourceAndWaveHUD(gold, hp, builtCount, maxSpots, currentWave, totalWaves, isEndless, aliveCount, isReady, mapLabel, activeScene, uiScale);

            // ════════════════════════════════════════════════════════════════
            // 2. NÚT TẠM DỪNG TO RÕ GÓC TRÊN BÊN PHẢI
            // ════════════════════════════════════════════════════════════════
            if (!hasWon && !isGameOver)
            {
                DrawTopRightControls(isPaused, uiScale);
            }

            // ════════════════════════════════════════════════════════════════
            // 3. Ô BẮT ĐẦU MỖI ROUND / SẴN SÀNG (GÓC DƯỚI BÊN PHẢI)
            // ════════════════════════════════════════════════════════════════
            if (!isReady && !hasWon && !isGameOver && spawner != null)
            {
                DrawRoundStartPanel(currentWave, spawner, uiScale);
            }

            // ════════════════════════════════════════════════════════════════
            // 4. MENU TẠM DỪNG TÍCH HỢP (KHI ĐANG PAUSE)
            // ════════════════════════════════════════════════════════════════
            if (isPaused && !hasWon && !isGameOver)
            {
                DrawIntegratedPauseMenu(gold, hp, currentWave, totalWaves, activeScene, mapLabel, uiScale);
            }
            // ════════════════════════════════════════════════════════════════
            // 5. BẢNG CHIẾN THẮNG
            // ════════════════════════════════════════════════════════════════
            else if (hasWon)
            {
                DrawVictoryPopup(gold, hp, builtCount, maxSpots, totalWaves, activeScene, isLevel2, isLevel3, mapLabel, uiScale);
            }
            // ════════════════════════════════════════════════════════════════
            // 6. BẢNG THẤT BẠI
            // ════════════════════════════════════════════════════════════════
            else if (isGameOver)
            {
                DrawDefeatPopup(gold, currentWave, totalWaves, activeScene, mapLabel, uiScale);
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // 1. HUD GÓC TRÊN TRÁI: TIỀN, THANH MÁU, THÁP, ĐỢT QUÁI (PHÓNG TO RÕ NÉT)
        // ────────────────────────────────────────────────────────────────────
        private void DrawResourceAndWaveHUD(int gold, int hp, int builtCount, int maxSpots,
            int currentWave, int totalWaves, bool isEndless, int aliveCount, bool isReady, string mapLabel, string activeScene, float scale)
        {
            // Chiều rộng bảo đảm không che ô bệ pháo BuildSpot_2 (-3.8, 3.8) ở mọi độ phân giải
            float maxHudW = Mathf.Max(420f, Screen.width * 0.228f);
            float hudW    = Mathf.Min(maxHudW, 460f * scale);
            float hudH    = 192f * scale;
            float marginX = 18f * scale;
            float marginY = 16f * scale;
            Rect hudRect  = new Rect(marginX, marginY, hudW, hudH);

            GUI.Box(hudRect, "", _hudBoxStyle);

            float padX = hudRect.x + 12f * scale;
            float padW = hudRect.width - 24f * scale;
            float curY = hudRect.y + 12f * scale;

            // Hàng 1: 3 Huy hiệu (Chips) thông số mở rộng: Vàng | Thanh Máu | Tháp
            float chipGap = 8f * scale;
            float chipW   = (padW - chipGap * 2f) / 3f;
            float chipH   = 60f * scale;

            // 1. Chip Tiền Vàng (Số to nổi bật)
            Rect chipGoldRect = new Rect(padX, curY, chipW, chipH);
            GUI.Box(chipGoldRect, "", _chipStyle);
            GUI.Label(new Rect(chipGoldRect.x, chipGoldRect.y + 4f * scale, chipGoldRect.width, 18f * scale), "★ TIỀN VÀNG", _chipTitleGoldStyle);
            GUI.Label(new Rect(chipGoldRect.x, chipGoldRect.y + 22f * scale, chipGoldRect.width, 32f * scale), $"<b>{gold}</b>", _chipValGoldStyle);

            // 2. Chip Máu Căn Cứ & Thanh Máu Visual Đồ Họa
            int maxHp = (activeScene.Contains("Level3") || activeScene.Contains("Map3")) ? 30
                : ((activeScene.Contains("Level2") || activeScene.Contains("Map2")) ? 25 : 20);
            float hpPercent = Mathf.Clamp01((float)hp / maxHp);

            Rect chipHpRect = new Rect(padX + chipW + chipGap, curY, chipW, chipH);
            GUI.Box(chipHpRect, "", _chipStyle);
            GUI.Label(new Rect(chipHpRect.x, chipHpRect.y + 3f * scale, chipHpRect.width, 18f * scale), "♥ CĂN CỨ", _chipTitleHpStyle);
            GUI.Label(new Rect(chipHpRect.x, chipHpRect.y + 20f * scale, chipHpRect.width, 24f * scale), $"<b>{hp} / {maxHp}</b>", _chipValHpStyle);

            // Thanh tiến trình Máu Visual dày dặn, đổi màu sinh động
            if (_whiteTex != null)
            {
                float barH = 7f * scale;
                Rect hpBarBg = new Rect(chipHpRect.x + 8f * scale, chipHpRect.y + chipH - barH - 6f * scale, chipHpRect.width - 16f * scale, barH);
                GUI.color = new Color(0.12f, 0.08f, 0.06f, 0.95f);
                GUI.DrawTexture(hpBarBg, _whiteTex);

                Color hpBarColor = hpPercent > 0.5f
                    ? new Color(0.25f, 0.90f, 0.32f, 1f)
                    : (hpPercent > 0.25f ? new Color(1f, 0.84f, 0.18f, 1f) : new Color(0.96f, 0.24f, 0.24f, 1f));
                Rect hpBarFill = new Rect(hpBarBg.x, hpBarBg.y, hpBarBg.width * hpPercent, hpBarBg.height);
                GUI.color = hpBarColor;
                GUI.DrawTexture(hpBarFill, _whiteTex);
                GUI.color = Color.white;
            }

            // 3. Chip Tháp Pháo (Số lượng tháp xây dựng)
            Rect chipTowerRect = new Rect(padX + (chipW + chipGap) * 2f, curY, chipW, chipH);
            GUI.Box(chipTowerRect, "", _chipStyle);
            GUI.Label(new Rect(chipTowerRect.x, chipTowerRect.y + 4f * scale, chipTowerRect.width, 18f * scale), "THÁP PHÁO", _chipTitleTowerStyle);
            GUI.Label(new Rect(chipTowerRect.x, chipTowerRect.y + 22f * scale, chipTowerRect.width, 32f * scale), $"<b>{builtCount}/{maxSpots}</b>", _chipValTowerStyle);

            curY += chipH + 10f * scale;

            // Hàng 2: Dải Ribbon đợt quái to bản
            float ribbonH = 36f * scale;
            Rect ribbonRect = new Rect(padX, curY, padW, ribbonH);
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

            curY += ribbonH + 8f * scale;

            // Hàng 3: Gợi ý thao tác ngắn gọn, sắc nét
            float subLineH = 19f * scale;
            GUI.Label(new Rect(padX + 2f, curY, padW, subLineH),
                "• <b>Bấm ô bệ</b>: Pháo Đơn (70) / Tên Lửa (100) / Laser Tím (150)", _subTextStyle);
            curY += subLineH;
            GUI.Label(new Rect(padX + 2f, curY, padW, subLineH),
                "• <b>Bấm tháp đã xây</b>: Nâng Cấp Lên Cấp 2 & Cấp 3 (Tăng ST)", _subTextStyle);
        }

        // ────────────────────────────────────────────────────────────────────
        // 2. NÚT TẠM DỪNG TO RÕ RÀNG GÓC TRÊN PHẢI
        // ────────────────────────────────────────────────────────────────────
        private void DrawTopRightControls(bool isPaused, float scale)
        {
            float pauseBtnW = 230f * scale;
            float pauseBtnH = 60f * scale;
            float pauseX    = Screen.width - pauseBtnW - 20f * scale;
            float pauseY    = 16f * scale;
            Rect pauseBtnRect = new Rect(pauseX, pauseY, pauseBtnW, pauseBtnH);

            string pauseLabel = isPaused ? "▶   TIẾP TỤC" : "||   TẠM DỪNG";
            GUIStyle pStyle   = isPaused ? _btnPrimary : _btnPause;

            if (GUI.Button(pauseBtnRect, pauseLabel, pStyle))
            {
                GameManager.Instance.TogglePause();
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // 3. Ô BẮT ĐẦU MỖI ROUND / SẴN SÀNG (GÓC DƯỚI PHẢI)
        // ────────────────────────────────────────────────────────────────────
        private void DrawRoundStartPanel(int currentWave, WaveSpawner spawner, float scale)
        {
            float panelW = 380f * scale;
            float panelH = 125f * scale;
            Rect panelRect = new Rect(
                Screen.width - panelW - 20f * scale,
                Screen.height - panelH - 20f * scale,
                panelW,
                panelH
            );

            GUI.Box(panelRect, "", _hudBoxStyle);

            string prompt = currentWave <= 0
                ? "Bố trí pháo phòng thủ rồi bấm xuất quân:"
                : $"Đã xong Đợt {currentWave}! Nâng cấp pháo rồi bấm:";

            GUI.Label(new Rect(panelRect.x + 12f * scale, panelRect.y + 10f * scale, panelW - 24f * scale, 28f * scale), prompt, _promptLabelStyle);

            string btnText = currentWave <= 0
                ? "▶   SẴN SÀNG CHIẾN ĐẤU"
                : $"▶   XUẤT QUÂN ĐỢT {currentWave + 1}";

            Rect btnRect = new Rect(panelRect.x + 14f * scale, panelRect.y + 42f * scale, panelW - 28f * scale, 68f * scale);
            if (GUI.Button(btnRect, btnText, _btnPrimary))
            {
                spawner.StartBattle();
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // 4. MENU TẠM DỪNG TÍCH HỢP (IN-GAME MAIN MENU POPUP)
        // ────────────────────────────────────────────────────────────────────
        private void DrawIntegratedPauseMenu(int gold, int hp, int currentWave, int totalWaves, string activeScene, string mapLabel, float scale)
        {
            if (_overlayTex != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex);

            float w = Mathf.Min(600f * scale, Screen.width - 40f);
            float h = 530f * scale;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            Rect winRect = new Rect(x, y, w, h);
            GUI.Box(winRect, "", _popupBoxStyle);

            float padX = winRect.x + 40f * scale;
            float padW = winRect.width - 80f * scale;
            float curY = winRect.y + 26f * scale;

            // Tiêu đề lớn đổ bóng
            ShadowLabel(new Rect(padX, curY, padW, 50f * scale), "★   TRÒ CHƠI TẠM DỪNG   ★", _popupTitleStyle);
            curY += 56f * scale;

            // Dải ruy băng phụ đề
            Rect ribbon = new Rect(padX, curY, padW, 34f * scale);
            GUI.Box(ribbon, "", _ribbonStyle);
            GUI.Label(ribbon, "Mọi hoạt động và quái vật đang được tạm dừng", _waveTextStyle);
            curY += 46f * scale;

            // Khung tóm tắt thông số trận đấu hiện tại
            Rect statsBox = new Rect(padX, curY, padW, 66f * scale);
            GUI.Box(statsBox, "", _chipStyle);
            GUI.Label(new Rect(statsBox.x, statsBox.y + 8f * scale, statsBox.width, 24f * scale),
                $"Chiến trường: <color=#FFE680><b>{mapLabel}</b></color>", _popupStatsStyle);
            GUI.Label(new Rect(statsBox.x, statsBox.y + 34f * scale, statsBox.width, 24f * scale),
                $"Vàng: <color=#FFDD44><b>{gold}</b></color>   |   Máu căn cứ: <color=#FF6666><b>{hp}</b></color>   |   Đợt quái: <color=#66DDFF><b>{currentWave}/{totalWaves}</b></color>",
                _popupStatsStyle);
            curY += 80f * scale;

            // Nút 1: TIẾP TỤC CHIẾN ĐẤU (Xanh lục to nhất)
            if (GUI.Button(new Rect(padX, curY, padW, 60f * scale), "▶   TIẾP TỤC CHIẾN ĐẤU", _btnPrimary))
            {
                GameManager.Instance.SetPaused(false);
            }
            curY += 70f * scale;

            // Nút 2: CHƠI LẠI MÀN NÀY (Xanh lam)
            if (GUI.Button(new Rect(padX, curY, padW, 52f * scale), "[↺]   CHƠI LẠI MÀN NÀY", _btnSecondary))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(activeScene);
            }
            curY += 60f * scale;

            // Nút 3: CHỌN MÀN CHƠI KHÁC (Gỗ viền vàng kim)
            if (GUI.Button(new Rect(padX, curY, padW, 52f * scale), "CHỌN MÀN CHƠI KHÁC", _btnPause))
            {
                Time.timeScale = 1f;
                MainMenuUI.OpenLevelSelectOnStart = true;
                SceneManager.LoadScene("MainMenu");
            }
            curY += 60f * scale;

            // Nút 4: VỀ TRANG CHỦ MENU (Đỏ gỗ)
            if (GUI.Button(new Rect(padX, curY, padW, 48f * scale), "VỀ TRANG CHỦ MENU", _btnDanger))
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
            string activeScene, bool isLevel2, bool isLevel3, string mapLabel, float scale)
        {
            if (_overlayTex != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex);

            bool hasNextLevel = !isLevel3;
            float w = Mathf.Min(640f * scale, Screen.width - 40f);
            float h = (hasNextLevel ? 510f : 450f) * scale;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            Rect rect = new Rect(x, y, w, h);
            GUI.Box(rect, "", _popupBoxStyle);

            float padX = rect.x + 40f * scale;
            float padW = rect.width - 80f * scale;
            float curY = rect.y + 26f * scale;

            ShadowLabel(new Rect(padX, curY, padW, 52f * scale), "★   CHIẾN THẮNG OANH LIỆT!   ★", _popupTitleStyle);
            curY += 56f * scale;

            string victoryMsg = isLevel3
                ? $"<b>Chúc mừng Đại Chỉ Huy!</b>\nBạn đã chinh phục hoàn toàn 3 chiến trường và bảo vệ thành công <b>{mapLabel}</b> trước {totalWaves} đợt tổng tấn công!"
                : $"<b>Chúc mừng Chỉ Huy!</b>\nBạn đã xuất sắc tiêu diệt toàn bộ {totalWaves} đợt quái vật và bảo vệ <b>{mapLabel}</b> an toàn tuyệt đối!";
            GUI.Label(new Rect(padX, curY, padW, 64f * scale), victoryMsg, _popupDescStyle);
            curY += 68f * scale;

            Rect statsBox = new Rect(padX, curY, padW, 42f * scale);
            GUI.Box(statsBox, "", _chipStyle);
            string statsText = $"Vàng tích lũy: <b>{gold}</b>   |   Máu còn lại: <b>{hp}</b>   |   Số tháp: <b>{builtCount}/{maxSpots}</b>";
            GUI.Label(statsBox, statsText, _popupStatsStyle);
            curY += 52f * scale;

            // Nút chơi màn kế tiếp
            if (!isLevel2 && !isLevel3)
            {
                if (GUI.Button(new Rect(padX, curY, padW, 60f * scale), "CHƠI TIẾP MÀN 2: SA MẠC LỬA  ▶", _btnPrimary))
                {
                    SceneManager.LoadScene("Level2Scene");
                }
                curY += 70f * scale;
            }
            else if (isLevel2)
            {
                if (GUI.Button(new Rect(padX, curY, padW, 60f * scale), "CHƠI TIẾP MÀN 3: PHÁO ĐÀI BĂNG GIÁ  ▶", _btnPrimary))
                {
                    SceneManager.LoadScene("Level3Scene");
                }
                curY += 70f * scale;
            }

            if (GUI.Button(new Rect(padX, curY, padW, 52f * scale), "QUAY VỀ CHỌN MÀN CHƠI", _btnSecondary))
            {
                MainMenuUI.OpenLevelSelectOnStart = true;
                SceneManager.LoadScene("MainMenu");
            }
            curY += 60f * scale;

            if (GUI.Button(new Rect(padX, curY, padW, 48f * scale), $"[↺]   CHƠI LẠI {mapLabel}", _btnPause))
            {
                SceneManager.LoadScene(activeScene);
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // 6. BẢNG THẤT BẠI (DEFEAT POPUP)
        // ────────────────────────────────────────────────────────────────────
        private void DrawDefeatPopup(int gold, int currentWave, int totalWaves, string activeScene, string mapLabel, float scale)
        {
            if (_overlayTex != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex);

            float w = Mathf.Min(600f * scale, Screen.width - 40f);
            float h = 440f * scale;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            Rect rect = new Rect(x, y, w, h);
            GUI.Box(rect, "", _popupBoxStyle);

            float padX = rect.x + 40f * scale;
            float padW = rect.width - 80f * scale;
            float curY = rect.y + 26f * scale;

            ShadowLabel(new Rect(padX, curY, padW, 52f * scale), "[!]   CĂN CỨ ĐÃ BỊ PHÁ HỦY!   [!]", _defeatTitleStyle);
            curY += 56f * scale;

            string defeatMsg = $"Kẻ địch đã xuyên thủng phòng tuyến tại <b>{mapLabel}</b> ở Đợt {currentWave}/{totalWaves}.\nHãy bổ sung thêm tháp pháo và nâng cấp Cấp 2 kịp thời để đẩy lui quái vật!";
            GUI.Label(new Rect(padX, curY, padW, 64f * scale), defeatMsg, _popupDescStyle);
            curY += 72f * scale;

            if (GUI.Button(new Rect(padX, curY, padW, 60f * scale), "[↺]   THỬ LẠI NGAY", _btnPrimary))
            {
                SceneManager.LoadScene(activeScene);
            }
            curY += 70f * scale;

            if (GUI.Button(new Rect(padX, curY, padW, 52f * scale), "QUAY VỀ CHỌN MÀN CHƠI", _btnSecondary))
            {
                MainMenuUI.OpenLevelSelectOnStart = true;
                SceneManager.LoadScene("MainMenu");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // KHỞI TẠO STYLES ĐỒNG BỘ THEO SCALE MÀN HÌNH
        // ════════════════════════════════════════════════════════════════════
        private void InitStyles(float scale)
        {
            _lastUiScale = scale;

            if (_overlayTex == null)
                _overlayTex = MakeTex(2, 2, new Color(0.02f, 0.03f, 0.05f, 0.55f));

            if (_whiteTex == null)
                _whiteTex = MakeTex(1, 1, Color.white);

            int szTitle  = Mathf.RoundToInt(28 * scale);
            int szHeader = Mathf.RoundToInt(22 * scale);
            int szBtn    = Mathf.RoundToInt(18 * scale);
            int szText   = Mathf.RoundToInt(15 * scale);
            int szSmall  = Mathf.RoundToInt(13 * scale);

            _fontTitle  = GetVietnameseFont(szTitle);
            _fontHeader = GetVietnameseFont(szHeader);
            _fontBtn    = GetVietnameseFont(szBtn);
            _fontText   = GetVietnameseFont(szText);
            _fontSmall  = GetVietnameseFont(szSmall);

            Color goldBorder = new Color(0.95f, 0.74f, 0.27f);
            Color goldHi     = new Color(1f, 0.90f, 0.45f);

            // 1. Khung hộp gỗ viền vàng kim
            _hudBoxStyle = BoxStyle(
                new Color(0.24f, 0.15f, 0.09f, 0.96f),
                new Color(0.12f, 0.08f, 0.05f, 0.96f),
                goldBorder, Mathf.Max(2, Mathf.RoundToInt(2.5f * scale))
            );

            _popupBoxStyle = BoxStyle(
                new Color(0.30f, 0.19f, 0.11f, 0.98f),
                new Color(0.14f, 0.09f, 0.05f, 0.98f),
                goldHi, Mathf.Max(3, Mathf.RoundToInt(3.5f * scale))
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

            // 2. Kiểu chữ huy hiệu thông số HUD mở rộng phóng to
            _chipTitleGoldStyle  = LabelStyle(_fontSmall, szSmall, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.95f, 0.78f, 0.30f));
            _chipValGoldStyle    = LabelStyle(_fontHeader, Mathf.RoundToInt(24 * scale), FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.35f));

            _chipTitleHpStyle    = LabelStyle(_fontSmall, szSmall, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.65f, 0.65f));
            _chipValHpStyle      = LabelStyle(_fontText, Mathf.RoundToInt(17 * scale), FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            _chipTitleTowerStyle = LabelStyle(_fontSmall, szSmall, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.55f, 0.85f, 1f));
            _chipValTowerStyle   = LabelStyle(_fontHeader, Mathf.RoundToInt(22 * scale), FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.65f, 0.95f, 1f));

            _waveTextStyle   = LabelStyle(_fontText, Mathf.RoundToInt(15 * scale), FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.82f));
            _subTextStyle    = LabelStyle(_fontSmall, Mathf.RoundToInt(13 * scale), FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.92f, 0.88f, 0.80f));
            _subTextStyle.richText = true;

            _promptLabelStyle= LabelStyle(_fontSmall, Mathf.RoundToInt(14 * scale), FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.70f));
            _promptLabelStyle.richText = true;

            _popupTitleStyle = LabelStyle(_fontTitle, Mathf.RoundToInt(27 * scale), FontStyle.Bold, TextAnchor.MiddleCenter, goldHi);
            _defeatTitleStyle= LabelStyle(_fontTitle, Mathf.RoundToInt(27 * scale), FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.35f));

            _popupDescStyle  = LabelStyle(_fontText, Mathf.RoundToInt(15 * scale), FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.96f, 0.92f, 0.84f));
            _popupDescStyle.wordWrap = true;
            _popupDescStyle.richText = true;

            _popupStatsStyle = LabelStyle(_fontText, Mathf.RoundToInt(14 * scale), FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.92f, 0.90f, 0.85f));
            _popupStatsStyle.richText = true;

            // 3. Nút bấm phong cách Kingdom Rush to bản sắc sảo
            _btnPrimary = ButtonStyle(
                Mathf.RoundToInt(18 * scale),
                new Color(0.38f, 0.82f, 0.26f),
                new Color(0.14f, 0.50f, 0.12f),
                goldBorder,
                Color.white
            );

            _btnSecondary = ButtonStyle(
                Mathf.RoundToInt(16 * scale),
                new Color(0.30f, 0.58f, 0.92f),
                new Color(0.12f, 0.30f, 0.62f),
                goldBorder,
                Color.white
            );

            _btnPause = ButtonStyle(
                Mathf.RoundToInt(18 * scale),
                new Color(0.48f, 0.32f, 0.16f),
                new Color(0.26f, 0.16f, 0.08f),
                goldBorder,
                new Color(1f, 0.92f, 0.70f)
            );

            _btnDanger = ButtonStyle(
                Mathf.RoundToInt(16 * scale),
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
