using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public static bool OpenLevelSelectOnStart = false;

        private enum MenuState
        {
            Home,
            LevelSelect,
            HowToPlay
        }

        [Header("Background")]
        [SerializeField] private Texture2D backgroundTexture;

        private MenuState currentState = MenuState.Home;

        // Styles (Non-static để không bị mất màu khi đổi Scene)
        private GUIStyle _panelBoxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subTitleStyle;
        private GUIStyle _btnPrimary;
        private GUIStyle _btnSecondary;
        private GUIStyle _btnLocked;
        private GUIStyle _btnDanger;
        private GUIStyle _cardBoxStyle;
        private GUIStyle _cardTitleStyle;
        private GUIStyle _cardDescStyle;
        private GUIStyle _infoTextStyle;

        private Texture2D _overlayTex;

        // Cache Font để không khởi tạo lại nhiều lần gây tụt FPS
        private Font _fontTitle;
        private Font _fontBtn;
        private Font _fontText;
        private Font _fontSmall;

        private void Awake()
        {
            Time.timeScale = 1f;

            // Nếu vừa hoàn thành trận đấu và chọn "Quay về chọn màn chơi", mở thẳng LevelSelect
            if (OpenLevelSelectOnStart)
            {
                currentState = MenuState.LevelSelect;
                OpenLevelSelectOnStart = false;
            }

            // Tự động load ảnh nền từ Resources nếu chưa được gán
            if (backgroundTexture == null)
            {
                backgroundTexture = Resources.Load<Texture2D>("MainMenu_BG");
            }
        }

        private void OnGUI()
        {
            // Tự động khởi tạo lại styles nếu texture bị giải phóng khi load lại scene
            if (_panelBoxStyle == null || _btnPrimary == null || _btnPrimary.normal.background == null)
            {
                InitStyles();
            }

            // 1. Vẽ hình nền chiến trường toàn màn hình
            if (backgroundTexture != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundTexture, ScaleMode.ScaleAndCrop);
            }

            // 2. Lớp phủ tối 45% mờ
            if (_overlayTex != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex);
            }

            // 3. Vẽ màn hình theo trạng thái
            switch (currentState)
            {
                case MenuState.Home:
                    DrawHomeScreen();
                    break;
                case MenuState.LevelSelect:
                    DrawLevelSelectScreen();
                    break;
                case MenuState.HowToPlay:
                    DrawHowToPlayScreen();
                    break;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 1. MÀN HÌNH TRANG CHỦ
        // ════════════════════════════════════════════════════════════════════
        private void DrawHomeScreen()
        {
            float centerX = Screen.width / 2f;
            float panelW = Mathf.Min(640f, Screen.width - 40f);
            float panelH = 530f;
            float startY = Mathf.Max(25f, (Screen.height - panelH) / 2f);

            Rect panelRect = new Rect(centerX - panelW / 2f, startY, panelW, panelH);
            GUI.Box(panelRect, "", _panelBoxStyle);

            float contentX = panelRect.x + 36f;
            float contentW = panelW - 72f;
            float currentY = panelRect.y + 35f;

            // TIÊU ĐỀ CHÍNH
            GUI.Label(new Rect(contentX, currentY, contentW, 55f), "★  TOWER DEFENSE 2D  ★", _titleStyle);
            currentY += 58f;

            // PHỤ ĐỀ
            GUI.Label(new Rect(contentX, currentY, contentW, 28f), "Chiến Thuật Phòng Thủ Tháp — Bảo Vệ Căn Cứ", _subTitleStyle);
            currentY += 48f;

            // NÚT 1: BẮT ĐẦU CHƠI
            if (GUI.Button(new Rect(contentX, currentY, contentW, 68f), "▶   BẮT ĐẦU CHƠI", _btnPrimary))
            {
                currentState = MenuState.LevelSelect;
            }
            currentY += 86f;

            // NÚT 2: HƯỚNG DẪN CÁCH CHƠI
            if (GUI.Button(new Rect(contentX, currentY, contentW, 60f), "HƯỚNG DẪN CÁCH CHƠI", _btnSecondary))
            {
                currentState = MenuState.HowToPlay;
            }
            currentY += 76f;

            // NÚT 3: THOÁT GAME
            if (GUI.Button(new Rect(contentX, currentY, contentW, 54f), "✕   THOÁT GAME", _btnDanger))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            currentY += 72f;

            // FOOTER
            GUI.Label(new Rect(contentX, currentY, contentW, 24f), "Phiên bản v3.0 • Đã mở khóa trọn bộ 3 Màn Chơi!", _infoTextStyle);
        }

        // ════════════════════════════════════════════════════════════════════
        // 2. MÀN HÌNH CHỌN CẤP ĐỘ (LEVEL SELECTION)
        // ════════════════════════════════════════════════════════════════════
        private void DrawLevelSelectScreen()
        {
            float centerX = Screen.width / 2f;
            float totalW = Mathf.Min(1080f, Screen.width - 40f);
            float totalH = 570f;
            float startY = Mathf.Max(25f, (Screen.height - totalH) / 2f);

            Rect mainRect = new Rect(centerX - totalW / 2f, startY, totalW, totalH);
            GUI.Box(mainRect, "", _panelBoxStyle);

            float currentY = mainRect.y + 28f;

            // Tiêu đề
            GUI.Label(new Rect(mainRect.x, currentY, totalW, 52f), "★   CHỌN CHIẾN TRƯỜNG THỬ THÁCH   ★", _titleStyle);
            currentY += 48f;
            GUI.Label(new Rect(mainRect.x, currentY, totalW, 26f), "Hãy chọn một màn chơi bên dưới để xuất quân bảo vệ căn cứ", _subTitleStyle);
            currentY += 42f;

            // 3 Card màn chơi
            float cardSpacing = 20f;
            float cardW = (totalW - 60f - (cardSpacing * 2f)) / 3f;
            float cardH = 300f;
            float cardStartX = mainRect.x + 30f;

            // ── THẺ MÀN 1: BÌNH NGUYÊN (SẴN SÀNG) ──
            Rect card1 = new Rect(cardStartX, currentY, cardW, cardH);
            DrawLevelCard(
                card1,
                title: "MÀN 1: BÌNH NGUYÊN",
                badge: "● SẴN SÀNG (OPEN)",
                desc: "Thảo nguyên xanh ven sông\n• Bản đồ tiêu chuẩn (5 Ô đặt pháo)\n• 5 Đợt quái dồn dập\n• Xuất hiện Xe Tăng Hạng Nặng\n• Siêu Boss ở Round 5",
                btnText: "▶  VÀO TRẬN (MÀN 1)",
                isUnlocked: true,
                onPlay: () =>
                {
                    Debug.Log("<color=green>[Menu] 🚀 Đang tải Màn 1 (SampleScene)...</color>");
                    SceneManager.LoadScene("SampleScene");
                }
            );

            // ── THẺ MÀN 2: SA MẠC LỬA (SẴN SÀNG) ──
            Rect card2 = new Rect(cardStartX + cardW + cardSpacing, currentY, cardW, cardH);
            DrawLevelCard(
                card2,
                title: "MÀN 2: SA MẠC LỬA",
                badge: "● SẴN SÀNG (OPEN)",
                desc: "Chiến trường sa mạc rộng lớn\n• Bản đồ TO HƠN & đường đi Zíc-Zắc\n• 8 Ô đặt pháo chiến thuật\n• Khởi đầu 160 Vàng & 25 Máu\n• 6 Đợt quái + 2 Siêu Boss!",
                btnText: "▶  VÀO TRẬN (MÀN 2)",
                isUnlocked: true,
                onPlay: () =>
                {
                    Debug.Log("<color=yellow>[Menu] 🏜️ Đang tải Màn 2: Sa Mạc Lửa (Level2Scene)...</color>");
                    SceneManager.LoadScene("Level2Scene");
                }
            );

            // ── THẺ MÀN 3: PHÁO ĐÀI BĂNG GIÁ (ĐÃ MỞ KHÓA!) ──
            Rect card3 = new Rect(cardStartX + (cardW + cardSpacing) * 2f, currentY, cardW, cardH);
            DrawLevelCard(
                card3,
                title: "MÀN 3: PHÁO ĐÀI BĂNG GIÁ",
                badge: "★ MỚI MỞ KHÓA (NEW!)",
                desc: "Đại chiến thành lũy đá băng\n• Bản đồ ĐẠI CHIẾN 35×23 (Xoắn ốc)\n• 10 Ô đặt pháo công sự\n• Khởi đầu 220 Vàng & 30 Máu\n• 7 Đợt tổng lực + 3 Siêu Boss!",
                btnText: "▶  VÀO TRẬN (MÀN 3)",
                isUnlocked: true,
                onPlay: () =>
                {
                    Debug.Log("<color=cyan>[Menu] ❄️ Đang tải Màn 3: Pháo Đài Băng Giá (Level3Scene)...</color>");
                    SceneManager.LoadScene("Level3Scene");
                }
            );

            // NÚT QUAY LẠI TRANG CHỦ
            float btnBackW = 280f;
            float btnBackH = 50f;
            float btnBackY = currentY + cardH + 26f;
            if (GUI.Button(new Rect(centerX - btnBackW / 2f, btnBackY, btnBackW, btnBackH), "↩   QUAY LẠI TRANG CHỦ", _btnSecondary))
            {
                currentState = MenuState.Home;
            }
        }

        private void DrawLevelCard(Rect rect, string title, string badge, string desc, string btnText, bool isUnlocked, System.Action onPlay)
        {
            GUI.Box(rect, "", _cardBoxStyle);

            float padX = rect.x + 18f;
            float padW = rect.width - 36f;
            float y = rect.y + 18f;

            GUI.Label(new Rect(padX, y, padW, 28f), title, _cardTitleStyle);
            y += 32f;

            string badgeColor = isUnlocked ? "#55FF55" : "#FFAA33";
            GUI.Label(new Rect(padX, y, padW, 24f), $"<color={badgeColor}><b>{badge}</b></color>", _infoTextStyle);
            y += 32f;

            GUI.Label(new Rect(padX, y, padW, 125f), desc, _cardDescStyle);

            float btnH = 52f;
            float btnY = rect.y + rect.height - btnH - 18f;

            if (isUnlocked)
            {
                if (GUI.Button(new Rect(padX, btnY, padW, btnH), btnText, _btnPrimary))
                {
                    onPlay?.Invoke();
                }
            }
            else
            {
                GUI.Button(new Rect(padX, btnY, padW, btnH), btnText, _btnLocked);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 3. MÀN HÌNH HƯỚNG DẪN CÁCH CHƠI (ĐÃ SỬA FONT & CHỮ TRÀN HÀNG)
        // ════════════════════════════════════════════════════════════════════
        private void DrawHowToPlayScreen()
        {
            float centerX = Screen.width / 2f;
            float panelW = Mathf.Min(840f, Screen.width - 40f);
            float panelH = 580f;
            float startY = Mathf.Max(20f, (Screen.height - panelH) / 2f);

            Rect panelRect = new Rect(centerX - panelW / 2f, startY, panelW, panelH);
            GUI.Box(panelRect, "", _panelBoxStyle);

            float padX = panelRect.x + 40f;
            float padW = panelW - 80f;
            float y = panelRect.y + 28f;

            // TIÊU ĐỀ GỌN GÀNG, KHÔNG BỊ TRÀN DÒNG HOẶC ĐÈ NỘI DUNG
            GUI.Label(new Rect(padX, y, padW, 52f), "★   HƯỚNG DẪN CÁCH CHƠI   ★", _titleStyle);
            y += 56f;

            string guideContent =
                "<b>1. MUA VÀ XÂY 3 LOẠI PHÁO CYBER (CƠ CHẾ KINGDOM RUSH):</b>\n" +
                "• Click chuột trái lên các ô bệ đá nhấp nháy dọc đường đi để chọn mua 1 trong 3 loại pháo:\n" +
                "  - <b>[Pháo Đơn Cyber] (70 Vàng)</b>: Sát thương 16-24, bắn nhanh, có tỉ lệ Chí mạng (Crit).\n" +
                "  - <b>[Pháo Tên Lửa] (100 Vàng)</b>: Sát thương 38-56 nổ lan (AoE), Xuyên giáp hạng nặng 55%.\n" +
                "  - <b>[Tháp Laser Tím Mới!] (150 Vàng)</b>: Sát thương cực lớn 85-125, Xuyên giáp 70% - vũ khí tối thượng!\n\n" +
                "<b>2. NÂNG CẤP PHÁO 3 CẤP ĐỘ (CẤP 1 ➔ CẤP 2 ➔ CẤP 3 MAX):</b>\n" +
                "• Click trực tiếp vào tháp đã xây để nâng cấp lên <b>Cấp 2</b> và <b>Cấp 3 Tối Thượng</b>:\n" +
                "  - Mỗi cấp nâng cấp sẽ <b>thay đổi ngoại hình ụ súng, đế trụ và đạn bắn ra</b> kèm sát thương đột biến!\n\n" +
                "<b>3. CHIẾN ĐẤU TỪNG ĐỢT & GIÁP QUÁI VẬT:</b>\n" +
                "• Đầu trận bấm <b>[SẴN SÀNG CHIẾN ĐẤU]</b> ở góc dưới bên phải để bắt đầu Đợt 1.\n" +
                "• Sau khi thắng mỗi đợt, nút sẽ đổi thành <b>[TIẾP TỤC ĐÁNH ĐỢT TIẾP THEO]</b> để bạn chủ động nâng cấp tháp!";

            GUI.Label(new Rect(padX, y, padW, 380f), guideContent, _cardDescStyle);

            // Nút quay lại
            float btnW = 280f;
            float btnH = 48f;
            float btnY = panelRect.y + panelH - btnH - 22f;
            if (GUI.Button(new Rect(centerX - btnW / 2f, btnY, btnW, btnH), "↩   QUAY LẠI TRANG CHỦ", _btnSecondary))
            {
                currentState = MenuState.Home;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // KHỞI TẠO STYLES VỚI FONT CHUẨN TIẾNG VIỆT TỪ HỆ THỐNG
        // ════════════════════════════════════════════════════════════════════
        private void InitStyles()
        {
            if (_overlayTex == null)
                _overlayTex = MakeTex(2, 2, new Color(0.02f, 0.04f, 0.07f, 0.30f));

            // Load Font hệ thống hỗ trợ tiếng Việt có dấu chuẩn 100% (Cache để không tạo lại)
            if (_fontTitle == null) _fontTitle = GetVietnameseFont(32);
            if (_fontBtn   == null) _fontBtn   = GetVietnameseFont(18);
            if (_fontText  == null) _fontText  = GetVietnameseFont(15);
            if (_fontSmall == null) _fontSmall = GetVietnameseFont(13);

            Font fontTitle = _fontTitle;
            Font fontBtn   = _fontBtn;
            Font fontText  = _fontText;
            Font fontSmall = _fontSmall;

            _panelBoxStyle = new GUIStyle(GUI.skin.box);
            _panelBoxStyle.normal.background = MakeTex(2, 2, new Color(0.06f, 0.09f, 0.14f, 0.94f));
            _panelBoxStyle.border = new RectOffset(10, 10, 10, 10);

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = fontTitle,
                fontStyle = FontStyle.Bold,
                fontSize = 30,
                alignment = TextAnchor.MiddleCenter
            };
            _titleStyle.normal.textColor = new Color(1f, 0.88f, 0.25f, 1f);

            _subTitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = fontText,
                fontStyle = FontStyle.Normal,
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            _subTitleStyle.normal.textColor = new Color(0.88f, 0.94f, 1f, 0.95f);

            _btnPrimary = new GUIStyle(GUI.skin.button)
            {
                font = fontBtn,
                fontStyle = FontStyle.Bold,
                fontSize = 19,
                alignment = TextAnchor.MiddleCenter
            };
            _btnPrimary.normal.background = MakeTex(2, 2, new Color(0.12f, 0.65f, 0.20f, 0.98f));
            _btnPrimary.hover.background = MakeTex(2, 2, new Color(0.18f, 0.88f, 0.28f, 1f));
            _btnPrimary.active.background = MakeTex(2, 2, new Color(0.06f, 0.42f, 0.12f, 1f));
            _btnPrimary.normal.textColor = Color.white;
            _btnPrimary.hover.textColor = Color.white;

            _btnSecondary = new GUIStyle(GUI.skin.button)
            {
                font = fontBtn,
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            _btnSecondary.normal.background = MakeTex(2, 2, new Color(0.14f, 0.38f, 0.70f, 0.96f));
            _btnSecondary.hover.background = MakeTex(2, 2, new Color(0.22f, 0.54f, 0.92f, 1f));
            _btnSecondary.active.background = MakeTex(2, 2, new Color(0.08f, 0.24f, 0.48f, 1f));
            _btnSecondary.normal.textColor = Color.white;
            _btnSecondary.hover.textColor = Color.white;

            _btnLocked = new GUIStyle(GUI.skin.button)
            {
                font = fontBtn,
                fontStyle = FontStyle.Bold,
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            _btnLocked.normal.background = MakeTex(2, 2, new Color(0.24f, 0.27f, 0.32f, 0.85f));
            _btnLocked.normal.textColor = new Color(0.72f, 0.72f, 0.72f, 0.85f);

            _btnDanger = new GUIStyle(GUI.skin.button)
            {
                font = fontBtn,
                fontStyle = FontStyle.Bold,
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            _btnDanger.normal.background = MakeTex(2, 2, new Color(0.58f, 0.12f, 0.12f, 0.92f));
            _btnDanger.hover.background = MakeTex(2, 2, new Color(0.82f, 0.18f, 0.18f, 1f));
            _btnDanger.normal.textColor = new Color(1f, 0.85f, 0.85f, 1f);
            _btnDanger.hover.textColor = Color.white;

            _cardBoxStyle = new GUIStyle(GUI.skin.box);
            _cardBoxStyle.normal.background = MakeTex(2, 2, new Color(0.09f, 0.13f, 0.19f, 0.96f));
            _cardBoxStyle.border = new RectOffset(8, 8, 8, 8);

            _cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = fontBtn,
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                alignment = TextAnchor.UpperLeft
            };
            _cardTitleStyle.normal.textColor = new Color(1f, 0.92f, 0.45f, 1f);

            _cardDescStyle = new GUIStyle(GUI.skin.label)
            {
                font = fontText,
                fontSize = 14,
                wordWrap = true
            };
            _cardDescStyle.normal.textColor = new Color(0.90f, 0.94f, 1f, 0.96f);

            _infoTextStyle = new GUIStyle(GUI.skin.label)
            {
                font = fontSmall,
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            _infoTextStyle.normal.textColor = new Color(0.65f, 0.75f, 0.85f, 0.9f);
        }

        private static Font GetVietnameseFont(int size)
        {
            Font f = Font.CreateDynamicFontFromOSFont(new string[] { "Segoe UI", "Arial", "Tahoma" }, size);
            return f;
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
