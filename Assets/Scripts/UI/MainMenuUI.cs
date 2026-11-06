using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefense.UI
{
    /// <summary>
    /// Menu chính + màn chọn chiến trường phong cách Kingdom Rush:
    /// khung gỗ viền vàng bo góc (tạo bằng code), thẻ màn chơi có ảnh xem trước bản đồ,
    /// tự co giãn theo mọi độ phân giải (độ phân giải ảo 1280x720).
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public static bool OpenLevelSelectOnStart = false;

        private enum MenuState
        {
            Home,
            LevelSelect,
            HowToPlay
        }

        private const float VW = 1280f;
        private const float VH = 720f;

        [Header("Background")]
        [SerializeField] private Texture2D backgroundTexture;

        private MenuState currentState = MenuState.Home;

        // Styles (Non-static để không bị mất màu khi đổi Scene)
        private GUIStyle _panelStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _cardHoverStyle;
        private GUIStyle _thumbFrameStyle;
        private GUIStyle _chipStyle;
        private GUIStyle _ribbonStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bigTitleStyle;
        private GUIStyle _subTitleStyle;
        private GUIStyle _btnPrimary;
        private GUIStyle _btnSecondary;
        private GUIStyle _btnLocked;
        private GUIStyle _btnDanger;
        private GUIStyle _cardTitleStyle;
        private GUIStyle _cardDescStyle;
        private GUIStyle _chipTextStyle;
        private GUIStyle _starStyle;
        private GUIStyle _infoTextStyle;

        private Texture2D _overlayTex;
        private Texture2D _thumb1, _thumb2, _thumb3;

        private Font _fontTitle, _fontBtn, _fontText, _fontSmall;

        private void Awake()
        {
            Time.timeScale = 1f;

            if (OpenLevelSelectOnStart)
            {
                currentState = MenuState.LevelSelect;
                OpenLevelSelectOnStart = false;
            }

            if (backgroundTexture == null)
                backgroundTexture = Resources.Load<Texture2D>("MainMenu_BG");

            // Ảnh xem trước các bản đồ (dùng chính background của từng màn)
            _thumb1 = Resources.Load<Texture2D>("Map1_Battlefield_BG");
            _thumb2 = Resources.Load<Texture2D>("Map2_Battlefield_BG");
            _thumb3 = Resources.Load<Texture2D>("Map3_Battlefield_BG");
        }

        private void OnGUI()
        {
            if (_panelStyle == null || _btnPrimary == null || _btnPrimary.normal.background == null)
                InitStyles();

            // 1. Nền + lớp phủ tối (theo toàn màn hình thật)
            if (backgroundTexture != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundTexture, ScaleMode.ScaleAndCrop);
            if (_overlayTex != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _overlayTex);

            // 2. Giao diện vẽ trên khung ảo 1280x720, tự co giãn & căn giữa
            float s = Mathf.Min(Screen.width / VW, Screen.height / VH);
            Vector3 offset = new Vector3((Screen.width - VW * s) * 0.5f, (Screen.height - VH * s) * 0.5f, 0f);
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(s, s, 1f));

            switch (currentState)
            {
                case MenuState.Home: DrawHomeScreen(); break;
                case MenuState.LevelSelect: DrawLevelSelectScreen(); break;
                case MenuState.HowToPlay: DrawHowToPlayScreen(); break;
            }

            GUI.matrix = oldMatrix;
        }

        // ════════════════════════════════════════════════════════════════════
        // 1. TRANG CHỦ
        // ════════════════════════════════════════════════════════════════════
        private void DrawHomeScreen()
        {
            Rect panel = new Rect(VW / 2f - 290f, 70f, 580f, 580f);
            GUI.Box(panel, "", _panelStyle);

            float x = panel.x + 50f;
            float w = panel.width - 100f;
            float y = panel.y + 34f;

            ShadowLabel(new Rect(x, y, w, 64f), "TOWER DEFENSE", _bigTitleStyle);
            y += 62f;
            ShadowLabel(new Rect(x, y, w, 44f), "★   2D   ★", _titleStyle);
            y += 52f;

            GUI.Box(new Rect(x + 20f, y, w - 40f, 34f), "", _ribbonStyle);
            GUI.Label(new Rect(x + 20f, y, w - 40f, 34f), "Chiến Thuật Phòng Thủ Tháp — Bảo Vệ Căn Cứ", _subTitleStyle);
            y += 62f;

            if (GUI.Button(new Rect(x, y, w, 72f), "▶    BẮT ĐẦU CHƠI", _btnPrimary))
                currentState = MenuState.LevelSelect;
            y += 90f;

            if (GUI.Button(new Rect(x, y, w, 60f), "HƯỚNG DẪN CÁCH CHƠI", _btnSecondary))
                currentState = MenuState.HowToPlay;
            y += 76f;

            if (GUI.Button(new Rect(x, y, w, 54f), "✕    THOÁT GAME", _btnDanger))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            y += 72f;

            GUI.Label(new Rect(x, y, w, 24f), "Phiên bản v3.0 • Đã mở khóa trọn bộ 3 Màn Chơi", _infoTextStyle);
        }

        // ════════════════════════════════════════════════════════════════════
        // 2. CHỌN MÀN CHƠI
        // ════════════════════════════════════════════════════════════════════
        private void DrawLevelSelectScreen()
        {
            Rect panel = new Rect(60f, 30f, 1160f, 660f);
            GUI.Box(panel, "", _panelStyle);

            ShadowLabel(new Rect(panel.x, panel.y + 18f, panel.width, 54f), "★   CHỌN CHIẾN TRƯỜNG   ★", _titleStyle);
            GUI.Label(new Rect(panel.x, panel.y + 70f, panel.width, 26f), "Chọn một màn chơi để xuất quân bảo vệ căn cứ", _subTitleStyle);

            float cardW = 350f, cardH = 440f, gap = 25f;
            float x0 = panel.x + (panel.width - (cardW * 3f + gap * 2f)) * 0.5f;
            float y0 = panel.y + 112f;

            DrawLevelCard(new Rect(x0, y0, cardW, cardH), _thumb1,
                "MÀN 1: BÌNH NGUYÊN", new Color(0.62f, 0.95f, 0.45f), 1,
                "5 ô pháo", "5 đợt", "150 vàng",
                "Thảo nguyên xanh ven sông. Bản đồ tiêu chuẩn, có Xe Tăng Hạng Nặng và Siêu Boss ở đợt cuối.",
                "▶  VÀO TRẬN", true,
                () => { Debug.Log("[Menu] Đang tải Màn 1 (SampleScene)..."); SceneManager.LoadScene("SampleScene"); });

            DrawLevelCard(new Rect(x0 + cardW + gap, y0, cardW, cardH), _thumb2,
                "MÀN 2: SA MẠC LỬA", new Color(1f, 0.72f, 0.30f), 2,
                "8 ô pháo", "6 đợt", "200 vàng",
                "Sa mạc nung đỏ bên chân núi lửa. Bản đồ rộng, đường Zíc-Zắc, xuất hiện 2 Siêu Boss.",
                "▶  VÀO TRẬN", true,
                () => { Debug.Log("[Menu] Đang tải Màn 2: Sa Mạc Lửa (Level2Scene)..."); SceneManager.LoadScene("Level2Scene"); });

            DrawLevelCard(new Rect(x0 + (cardW + gap) * 2f, y0, cardW, cardH), _thumb3,
                "MÀN 3: PHÁO ĐÀI BĂNG GIÁ", new Color(0.55f, 0.88f, 1f), 3,
                "10 ô pháo", "7 đợt", "250 vàng",
                "Thành lũy băng giá với đường xoắn ốc đại chiến. Tổng lực 7 đợt cùng 3 Siêu Boss.",
                "▶  VÀO TRẬN", true,
                () => { Debug.Log("[Menu] Đang tải Màn 3: Pháo Đài Băng Giá (Level3Scene)..."); SceneManager.LoadScene("Level3Scene"); });

            if (GUI.Button(new Rect(VW / 2f - 150f, panel.y + panel.height - 70f, 300f, 48f), "↩   QUAY LẠI TRANG CHỦ", _btnSecondary))
                currentState = MenuState.Home;
        }

        private void DrawLevelCard(Rect r, Texture2D thumb, string title, Color titleColor, int stars,
                                   string chipA, string chipB, string chipC, string desc,
                                   string btnText, bool unlocked, System.Action onPlay)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            GUI.Box(r, "", hover ? _cardHoverStyle : _cardStyle);

            // Ảnh xem trước bản đồ
            Rect frame = new Rect(r.x + 14f, r.y + 14f, r.width - 28f, 178f);
            GUI.Box(frame, "", _thumbFrameStyle);
            if (thumb != null)
            {
                Rect img = new Rect(frame.x + 4f, frame.y + 4f, frame.width - 8f, frame.height - 8f);
                GUI.DrawTexture(img, thumb, ScaleMode.ScaleAndCrop);
            }

            float x = r.x + 18f;
            float w = r.width - 36f;
            float y = frame.yMax + 12f;

            _cardTitleStyle.normal.textColor = titleColor;
            ShadowLabel(new Rect(x, y, w, 28f), title, _cardTitleStyle);
            y += 30f;

            string starText = "";
            for (int i = 1; i <= 3; i++)
                starText += i <= stars ? "★ " : "☆ ";
            GUI.Label(new Rect(x, y, w, 24f), "Độ khó: " + starText, _starStyle);
            y += 30f;

            // 3 viên thông số
            float chipW = (w - 16f) / 3f;
            DrawChip(new Rect(x, y, chipW, 26f), chipA);
            DrawChip(new Rect(x + chipW + 8f, y, chipW, 26f), chipB);
            DrawChip(new Rect(x + (chipW + 8f) * 2f, y, chipW, 26f), chipC);
            y += 34f;

            GUI.Label(new Rect(x, y, w, 62f), desc, _cardDescStyle);

            Rect btn = new Rect(x, r.yMax - 62f, w, 48f);
            if (unlocked)
            {
                if (GUI.Button(btn, btnText, _btnPrimary)) onPlay?.Invoke();
            }
            else
            {
                GUI.Button(btn, "CHƯA MỞ KHÓA", _btnLocked);
            }
        }

        private void DrawChip(Rect r, string text)
        {
            GUI.Box(r, "", _chipStyle);
            GUI.Label(r, text, _chipTextStyle);
        }

        // ════════════════════════════════════════════════════════════════════
        // 3. HƯỚNG DẪN
        // ════════════════════════════════════════════════════════════════════
        private void DrawHowToPlayScreen()
        {
            Rect panel = new Rect(VW / 2f - 440f, 40f, 880f, 640f);
            GUI.Box(panel, "", _panelStyle);

            float x = panel.x + 44f;
            float w = panel.width - 88f;
            float y = panel.y + 22f;

            ShadowLabel(new Rect(x, y, w, 52f), "★   HƯỚNG DẪN CÁCH CHƠI   ★", _titleStyle);
            y += 62f;

            string guide =
                "<b>1. MUA VÀ XÂY 3 LOẠI PHÁO CYBER:</b>\n" +
                "• Click các ô bệ đá dọc đường đi để chọn mua 1 trong 3 loại pháo:\n" +
                "  - <b>[Pháo Đơn Cyber] (70 Vàng)</b>: Sát thương 16-24, bắn nhanh, có tỉ lệ Chí mạng.\n" +
                "  - <b>[Pháo Tên Lửa] (100 Vàng)</b>: Sát thương 38-56 nổ lan, xuyên giáp 55%.\n" +
                "  - <b>[Tháp Laser Tím] (150 Vàng)</b>: Sát thương 85-125, xuyên giáp 70% - vũ khí tối thượng!\n\n" +
                "<b>2. NÂNG CẤP PHÁO 3 CẤP ĐỘ:</b>\n" +
                "• Click vào tháp đã xây để nâng lên <b>Cấp 2</b> và <b>Cấp 3 Tối Thượng</b>.\n" +
                "  - Mỗi cấp thay đổi ngoại hình ụ súng, đế trụ, đạn bắn và tăng sát thương!\n\n" +
                "<b>3. CHIẾN ĐẤU TỪNG ĐỢT:</b>\n" +
                "• Đầu trận bấm <b>[SẴN SÀNG CHIẾN ĐẤU]</b> ở góc dưới bên phải để bắt đầu Đợt 1.\n" +
                "• Thắng mỗi đợt, nút đổi thành <b>[TIẾP TỤC ĐÁNH ĐỢT TIẾP THEO]</b> để bạn chủ động nâng cấp tháp.\n" +
                "• Bấm nút <b>Tạm dừng</b> góc trên bên phải để nghỉ giữa trận.";

            GUI.Label(new Rect(x, y, w, 430f), guide, _cardDescStyle);

            if (GUI.Button(new Rect(VW / 2f - 150f, panel.yMax - 72f, 300f, 48f), "↩   QUAY LẠI TRANG CHỦ", _btnSecondary))
                currentState = MenuState.Home;
        }

        // ════════════════════════════════════════════════════════════════════
        // STYLES
        // ════════════════════════════════════════════════════════════════════
        private void ShadowLabel(Rect r, string text, GUIStyle style)
        {
            Color c = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(r.x + 2f, r.y + 3f, r.width, r.height), text, style);
            style.normal.textColor = c;
            GUI.Label(r, text, style);
        }

        private void InitStyles()
        {
            if (_overlayTex == null)
                _overlayTex = MakeTex(2, 2, new Color(0.03f, 0.04f, 0.07f, 0.45f));

            if (_fontTitle == null) _fontTitle = GetVietnameseFont(32);
            if (_fontBtn == null) _fontBtn = GetVietnameseFont(18);
            if (_fontText == null) _fontText = GetVietnameseFont(15);
            if (_fontSmall == null) _fontSmall = GetVietnameseFont(13);

            Color gold = new Color(0.95f, 0.74f, 0.27f);
            Color goldHi = new Color(1f, 0.90f, 0.45f);

            // Khung gỗ tối viền vàng
            _panelStyle = BoxStyle(new Color(0.30f, 0.19f, 0.11f, 0.97f), new Color(0.15f, 0.09f, 0.05f, 0.97f), gold, 3);
            _cardStyle = BoxStyle(new Color(0.24f, 0.15f, 0.09f, 0.98f), new Color(0.13f, 0.08f, 0.05f, 0.98f), new Color(0.58f, 0.40f, 0.17f), 3);
            _cardHoverStyle = BoxStyle(new Color(0.30f, 0.20f, 0.11f, 1f), new Color(0.17f, 0.11f, 0.06f, 1f), goldHi, 4);
            _thumbFrameStyle = BoxStyle(new Color(0.08f, 0.05f, 0.03f, 1f), new Color(0.08f, 0.05f, 0.03f, 1f), gold, 3);
            _chipStyle = BoxStyle(new Color(0.09f, 0.06f, 0.04f, 0.95f), new Color(0.12f, 0.08f, 0.05f, 0.95f), new Color(0.45f, 0.32f, 0.15f), 2);
            _ribbonStyle = BoxStyle(new Color(0.55f, 0.13f, 0.12f, 1f), new Color(0.38f, 0.07f, 0.07f, 1f), new Color(0.95f, 0.74f, 0.27f), 2);

            _titleStyle = LabelStyle(_fontTitle, 30, FontStyle.Bold, TextAnchor.MiddleCenter, goldHi);
            _bigTitleStyle = LabelStyle(_fontTitle, 52, FontStyle.Bold, TextAnchor.MiddleCenter, goldHi);
            _subTitleStyle = LabelStyle(_fontText, 16, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.85f));
            _cardTitleStyle = LabelStyle(_fontBtn, 18, FontStyle.Bold, TextAnchor.MiddleCenter, goldHi);
            _starStyle = LabelStyle(_fontText, 15, FontStyle.Bold, TextAnchor.MiddleCenter, gold);
            _chipTextStyle = LabelStyle(_fontSmall, 13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.98f, 0.93f, 0.80f));
            _infoTextStyle = LabelStyle(_fontSmall, 13, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.80f, 0.72f, 0.58f));

            _cardDescStyle = LabelStyle(_fontText, 14, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.84f));
            _cardDescStyle.wordWrap = true;
            _cardDescStyle.richText = true;

            _btnPrimary = ButtonStyle(19, new Color(0.38f, 0.82f, 0.26f), new Color(0.14f, 0.50f, 0.12f), gold, Color.white);
            _btnSecondary = ButtonStyle(16, new Color(0.30f, 0.58f, 0.92f), new Color(0.12f, 0.30f, 0.62f), gold, Color.white);
            _btnDanger = ButtonStyle(15, new Color(0.86f, 0.30f, 0.24f), new Color(0.52f, 0.10f, 0.10f), gold, new Color(1f, 0.92f, 0.90f));
            _btnLocked = ButtonStyle(15, new Color(0.40f, 0.40f, 0.42f), new Color(0.24f, 0.24f, 0.26f), new Color(0.5f, 0.5f, 0.5f), new Color(0.75f, 0.75f, 0.75f));
            _btnLocked.hover.background = _btnLocked.normal.background;
            _btnLocked.active.background = _btnLocked.normal.background;
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
            st.border = new RectOffset(18, 18, 18, 18);
            st.padding = new RectOffset(0, 0, 0, 0);
            st.margin = new RectOffset(0, 0, 0, 0);
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
            Texture2D normal = MakeRounded(48, 14, top, bottom, border, 3);
            Texture2D hover = MakeRounded(48, 14, Color.Lerp(top, Color.white, 0.25f), Color.Lerp(bottom, Color.white, 0.18f), new Color(1f, 0.92f, 0.5f), 3);
            Texture2D active = MakeRounded(48, 14, Color.Lerp(bottom, Color.black, 0.15f), Color.Lerp(bottom, Color.black, 0.35f), border, 3);
            st.normal.background = normal;
            st.hover.background = hover;
            st.active.background = active;
            st.focused.background = normal;
            st.border = new RectOffset(18, 18, 18, 18);
            st.padding = new RectOffset(8, 8, 4, 6);
            st.margin = new RectOffset(0, 0, 0, 0);
            st.normal.textColor = text;
            st.hover.textColor = Color.white;
            st.active.textColor = text;
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

        /// <summary>Tạo texture hình chữ nhật bo góc có viền, tô gradient dọc (dùng cho 9-slice).</summary>
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
                float t = y / (float)(size - 1); // y=0 là đáy
                Color fill = Color.Lerp(bottom, top, t);
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - half) - inner;
                    float py = Mathf.Abs(y + 0.5f - half) - inner;
                    float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
                    float d = outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius; // <0 là bên trong
                    float alpha = Mathf.Clamp01(0.5f - d);

                    Color c = fill;
                    if (d > -borderWidth)
                        c = border;
                    else if (d > -borderWidth - 2f)
                        c = Color.Lerp(fill, Color.white, 0.12f); // viền sáng bên trong tạo chiều sâu

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
