using UnityEngine;
using TowerDefense.Managers;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TowerDefense.Towers
{
    /// <summary>
    /// Ô bệ pháo chiến lược:
    /// - Khi chưa xây: Click chuột trái để mở Popup CHỌN MUA (Pháo Thường 50🪙 hoặc Pháo Tên Lửa 80🪙).
    /// - Khi đã xây: Click chuột trái vào tháp để mở Popup NÂNG CẤP (Level 1 -> Level 2).
    /// </summary>
    public class TowerBuildSpot : MonoBehaviour
    {
        [Header("Tower Prefabs")]
        [SerializeField] private GameObject cannonTowerPrefab;    // Pháo đơn Cyber
        [SerializeField] private GameObject rocketTowerPrefab;    // Pháo tên lửa
        [SerializeField] private GameObject laserTowerPrefab;     // Tháp Laser/Plasma Tím (Mới!)

        [Header("Costs (Kingdom Rush Balance)")]
        [SerializeField] private int cannonCost = 70;
        [SerializeField] private int rocketCost = 100;
        [SerializeField] private int laserCost  = 150;

        [Header("Settings")]
        [SerializeField] private float clickRadius = 0.75f;
        [SerializeField] private GameObject buildEffectPrefab;

        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private SpriteRenderer indicatorRenderer;

        [Header("Colors & Animation")]
        [SerializeField] private Color normalColor       = new Color(0.85f, 0.95f, 1f, 0.8f);
        [SerializeField] private Color hoverAffordableColor   = new Color(0.25f, 1f, 0.35f, 1f);
        [SerializeField] private Color hoverUnaffordableColor = new Color(1f, 0.3f, 0.3f, 0.9f);
        [SerializeField] private Color menuOpenColor     = new Color(1f, 0.85f, 0.1f, 1f);

        // State
        private bool isBuilt           = false;
        private bool isMenuOpen        = false;
        private bool isUpgradeMenuOpen = false;
        private Tower currentTower     = null;

        private Camera mainCamera;
        private Vector3 initialScale;

        // ── Tap Squash-and-Stretch & Radial Menu Spring Pop State (Zero-GC)
        private bool  _isTapAnimating    = false;
        private float _tapAnimElapsed    = 0f;
        private const float TAP_DURATION = 0.24f;
        private float _radialOpenElapsed = 0f;
        private const float RADIAL_POP_DURATION = 0.22f;

        // ── Range Preview Ring khi Hover nút trong Vòng Tròn Đặt Trụ
        private SpriteRenderer _previewRangeRenderer;

        // ── Zero-GC Cached Price Strings & Textures cho Radial Menu Kingdom Rush
        private const string STR_PRICE_CANNON = "70";
        private const string STR_PRICE_ROCKET = "100";
        private const string STR_PRICE_LASER  = "150";
        private const string STR_TOOLTIP_CANNON = "PHÁO ĐƠN  •  ST: 16-24 (Bắn nhanh)";
        private const string STR_TOOLTIP_ROCKET = "PHÁO TÊN LỬA  •  ST: 38-56 (Nổ AoE)";
        private const string STR_TOOLTIP_LASER  = "PHÁO LASER  •  ST: 85-125 (Xuyên giáp)";

        private static Texture2D _texRadialRing;
        private static Texture2D _texBtnCannon;
        private static Texture2D _texBtnRocket;
        private static Texture2D _texBtnLaser;
        private static Texture2D _texBtnUpgrade;
        private static Texture2D _texBtnMax;
        private static Texture2D _texBtnSell;
        private static Texture2D _texBtnConfirmSell;
        private static Texture2D _texPriceBadge;
        private static Texture2D _texBtnCancel;
        private static GUIStyle  _priceLabelStyle;
        private static GUIStyle  _priceUnaffordableStyle;
        private static GUIStyle  _radialTooltipStyle;

        // ── Zero-GC String Caches cho Menu Nâng Cấp & Bán Pháo
        private const string STR_MAX_LEVEL = "MAX";
        private static readonly System.Collections.Generic.Dictionary<int, string> _cachedCostStrings   = new System.Collections.Generic.Dictionary<int, string>(16);
        private static readonly System.Collections.Generic.Dictionary<int, string> _cachedRefundStrings = new System.Collections.Generic.Dictionary<int, string>(16);
        private static readonly System.Collections.Generic.Dictionary<int, string> _cachedHeaderStrings = new System.Collections.Generic.Dictionary<int, string>(16);
        private bool _isConfirmingSell = false;

        // GUI popup rect
        private Rect popupRect;
        private const float BUILD_POPUP_W   = 320f;
        private const float BUILD_POPUP_H   = 320f;
        private const float UPGRADE_POPUP_W = 400f;
        private const float UPGRADE_POPUP_H = 240f;

        public bool HasTower => isBuilt && currentTower != null;
        public Tower CurrentTower => currentTower;

        // ── Styles
        private static GUIStyle _boxStyle;
        private static GUIStyle _titleStyle;
        private static GUIStyle _subTitleStyle;
        private static GUIStyle _btnGreen;
        private static GUIStyle _btnBlue;
        private static GUIStyle _btnPurple;
        private static GUIStyle _btnYellow;
        private static GUIStyle _btnCancel;
        private static bool _stylesInit = false;

        private void Awake()
        {
            mainCamera   = Camera.main;
            initialScale = transform.localScale;
            if (initialScale.sqrMagnitude < 0.01f) initialScale = Vector3.one;

            float mapScale = GameManager.GetMapTowerVisualScale();
            initialScale *= mapScale;
            transform.localScale = initialScale;
            clickRadius = Mathf.Max(clickRadius, 0.65f * mapScale);

            // Chuẩn hóa giá xây dựng theo kinh tế Kingdom Rush (Pháo Đơn: 70, Tên Lửa: 100, Tháp Laser Tím: 150)
            cannonCost = 70;
            rocketCost = 100;
            laserCost  = 150;

            if (baseRenderer == null)
                baseRenderer = GetComponent<SpriteRenderer>();

            if (indicatorRenderer == null)
            {
                Transform ind = transform.Find("Indicator");
                if (ind != null) indicatorRenderer = ind.GetComponent<SpriteRenderer>();
            }

            // Ô chờ xây tháp: Sử dụng trực tiếp hình ảnh Ô Đất Cát + Biển Búa Gỗ trên nền Map Kingdom Rush,
            // chỉ bật vòng sáng vàng ấm khi người chơi di chuột (Hover) vào ô đất!
            if (baseRenderer != null)
            {
                Sprite emptyRing = CyberAssetLoader.GetEmptyBuildSpotRingSprite();
                if (emptyRing != null) baseRenderer.sprite = emptyRing;
                baseRenderer.color = new Color(1f, 0.90f, 0.35f, 0.85f);
                baseRenderer.enabled = false;
            }
            if (indicatorRenderer != null)
            {
                indicatorRenderer.enabled = false;
            }

            // Tạo sẵn Range Preview Renderer cho ô đất (Zero-GC trong runtime khi hover nút Radial Menu)
            Transform previewTr = transform.Find("PreviewRange");
            if (previewTr != null)
            {
                _previewRangeRenderer = previewTr.GetComponent<SpriteRenderer>();
            }
            else
            {
                GameObject pGO = new GameObject("PreviewRange");
                pGO.transform.SetParent(transform, false);
                pGO.transform.localPosition = Vector3.zero;
                _previewRangeRenderer = pGO.AddComponent<SpriteRenderer>();
            }
            if (_previewRangeRenderer != null)
            {
                _previewRangeRenderer.sprite = CyberAssetLoader.GetRangeCircleSprite();
                _previewRangeRenderer.sortingOrder = 1;
                _previewRangeRenderer.enabled = false;
            }
        }

        private void Update()
        {
            // Animation nhấp vào ô đất trống (Squash-and-Stretch Zero-GC)
            if (_isTapAnimating)
            {
                _tapAnimElapsed += Time.unscaledDeltaTime;
                if (_tapAnimElapsed >= TAP_DURATION)
                {
                    _isTapAnimating = false;
                    transform.localScale = initialScale;
                }
                else
                {
                    float t = _tapAnimElapsed / TAP_DURATION;
                    float sx, sy;
                    if (t < 0.35f)
                    {
                        float p = t / 0.35f;
                        sx = Mathf.Lerp(1f, 1.25f, p);
                        sy = Mathf.Lerp(1f, 0.76f, p);
                    }
                    else if (t < 0.72f)
                    {
                        float p = (t - 0.35f) / 0.37f;
                        sx = Mathf.Lerp(1.25f, 0.90f, p);
                        sy = Mathf.Lerp(0.76f, 1.16f, p);
                    }
                    else
                    {
                        float p = (t - 0.72f) / 0.28f;
                        sx = Mathf.Lerp(0.90f, 1f, p);
                        sy = Mathf.Lerp(1.16f, 1f, p);
                    }
                    transform.localScale = new Vector3(initialScale.x * sx, initialScale.y * sy, initialScale.z);
                }
            }

            Vector2 mouseWorld = GetMouseWorldPosition();
            float dist = Vector2.Distance(mouseWorld, transform.position);
            bool isHovered = (dist <= clickRadius);

            // Hiệu ứng nhấp nháy & co giãn nhịp nhàng khi chờ xây tháp pháo
            float pulse = 1f + Mathf.Sin(Time.time * 4.5f) * 0.08f;
            float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * 5.0f);

            // 1. Khi đang mở menu (menu Xây vòng tròn Radial hoặc menu Nâng cấp & Bán vòng tròn Radial)
            if (isMenuOpen || isUpgradeMenuOpen)
            {
                if (_radialOpenElapsed < RADIAL_POP_DURATION)
                {
                    _radialOpenElapsed += Time.unscaledDeltaTime;
                }

                if (WasLeftMouseClicked())
                {
                    Vector2 screenPos = GetMouseScreenPosition();
                    Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
                    if (!popupRect.Contains(guiPos))
                    {
                        CloseAllMenus();
                    }
                }
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    CloseAllMenus();
                }
#endif
                return;
            }

            // 2. Khi ô ĐÃ CÓ THÁP: ẩn vòng chờ xây vì bản thân Tháp đã hiển thị Đế Trụ + Ụ Pháo đi kèm!
            if (HasTower)
            {
                if (!_isTapAnimating) transform.localScale = initialScale;
                if (baseRenderer != null) baseRenderer.enabled = false;
                if (indicatorRenderer != null) indicatorRenderer.enabled = false;
                if (isHovered && WasLeftMouseClicked())
                {
                    TriggerTapBounce();
                    OpenUpgradeMenu();
                }
                return;
            }

            // 3. Khi ô CHƯA CÓ THÁP: đồng bộ với nền Map vẽ tay (ô đất + biển búa gỗ trên Map).
            // Khi bình thường ẩn vòng hologram để không đè lên ô đất vẽ tay; khi Hover mới hiện vòng sáng vàng ấm mời bấm xây trụ!
            if (indicatorRenderer != null) indicatorRenderer.enabled = false;
            if (isHovered)
            {
                if (!_isTapAnimating)
                    transform.localScale = initialScale * (pulse * 1.10f);

                bool canAfford = GameManager.Instance != null && GameManager.Instance.CanAfford(cannonCost);
                Color targetColor = canAfford
                    ? new Color(1f, 0.90f, 0.35f, 0.85f)
                    : new Color(1f, 0.35f, 0.30f, 0.85f);

                if (baseRenderer != null)
                {
                    baseRenderer.enabled = true;
                    baseRenderer.color = targetColor;
                }

                if (WasLeftMouseClicked())
                {
                    TriggerTapBounce();
                    OpenBuildMenu();
                }
            }
            else
            {
                if (!_isTapAnimating)
                    transform.localScale = initialScale;
                if (baseRenderer != null)
                {
                    baseRenderer.enabled = false;
                }
            }
        }

        private void TriggerTapBounce()
        {
            _isTapAnimating = true;
            _tapAnimElapsed = 0f;
        }

        private void SetPreviewRange(bool visible, float range = 3.6f)
        {
            if (_previewRangeRenderer == null) return;
            if (_previewRangeRenderer.enabled != visible)
                _previewRangeRenderer.enabled = visible;
            if (visible)
            {
                float parentScale = Mathf.Max(0.01f, transform.localScale.x);
                float diameter = (range * 2f) / parentScale;
                _previewRangeRenderer.transform.localScale = new Vector3(diameter, diameter, 1f);
            }
        }

        private void CloseAllMenus()
        {
            isMenuOpen = false;
            isUpgradeMenuOpen = false;
            _isConfirmingSell = false;
            SetPreviewRange(false);
            if (currentTower != null)
            {
                currentTower.SetRangeVisible(false);
            }
            ResetVisual();
        }

        // ─── Mở Vòng Tròn Chọn Loại Pháo (Kingdom Rush Radial Build Menu) ────
        private void OpenBuildMenu()
        {
            if (HasTower) return;
            isUpgradeMenuOpen = false;
            _radialOpenElapsed = 0f;

            CalculateRadialPopupRect();
            isMenuOpen = true;
        }

        private void CalculateRadialPopupRect()
        {
            float uiScale = Mathf.Clamp(Screen.height / 1080f, 0.85f, 1.85f);
            float scaledW = BUILD_POPUP_W * uiScale;
            float scaledH = BUILD_POPUP_H * uiScale;

            if (mainCamera == null) mainCamera = Camera.main;
            Vector3 screenPt = mainCamera != null
                ? mainCamera.WorldToScreenPoint(transform.position)
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            float centerGuiX = screenPt.x;
            float centerGuiY = Screen.height - screenPt.y;

            float guiX = Mathf.Clamp(centerGuiX - scaledW * 0.5f, 12f, Screen.width  - scaledW - 12f);
            float guiY = Mathf.Clamp(centerGuiY - scaledH * 0.5f, 12f, Screen.height - scaledH - 12f);

            popupRect = new Rect(guiX, guiY, scaledW, scaledH);
        }

        // ─── Mở Vòng Tròn Nâng Cấp & Bán Pháo (Kingdom Rush Radial Action Menu) ──
        private void OpenUpgradeMenu()
        {
            if (!HasTower || currentTower == null) return;
            isMenuOpen = false;
            _isConfirmingSell = false;
            _radialOpenElapsed = 0f;

            // Đóng tầm đánh của các tháp khác (nếu có)
            Tower[] allTowers = Object.FindObjectsByType<Tower>(FindObjectsInactive.Exclude);
            foreach (var t in allTowers)
            {
                if (t != null && t != currentTower) t.SetRangeVisible(false);
            }

            // Hiện vòng tầm đánh hiện tại của pháo này
            currentTower.SetRangeVisible(true);

            CalculateRadialPopupRect();
            isUpgradeMenuOpen = true;
        }

        private void CalculatePopupRect(float width, float height)
        {
            float uiScale = Mathf.Clamp(Screen.height / 1080f, 0.85f, 1.85f);
            float scaledW = width * uiScale;
            float scaledH = height * uiScale;

            if (mainCamera == null) mainCamera = Camera.main;
            Vector3 screenPt = mainCamera != null
                ? mainCamera.WorldToScreenPoint(transform.position)
                : new Vector3(Screen.width / 2f, Screen.height / 2f, 0);

            float guiY = Screen.height - screenPt.y - scaledH - 30f * uiScale;
            float guiX = screenPt.x - scaledW / 2f;
            guiX = Mathf.Clamp(guiX, 15f, Screen.width  - scaledW - 15f);
            guiY = Mathf.Clamp(guiY, 15f, Screen.height - scaledH - 15f);

            popupRect = new Rect(guiX, guiY, scaledW, scaledH);
        }

        // ─── Vẽ GUI Popup ──────────────────────────────────────────────────
        private void OnGUI()
        {
            if (!isMenuOpen && !isUpgradeMenuOpen) return;

            InitStyles();

            if (isMenuOpen && !HasTower)
            {
                DrawBuildPopup();
            }
            else if (isUpgradeMenuOpen && HasTower && currentTower != null)
            {
                DrawUpgradePopup();
            }
        }

        // ─── Vẽ Vòng Tròn Đặt Trụ Kingdom Rush (3 Nút Tròn: Pháo Đơn 70, Tên Lửa 100, Laser 150 + Nút Đóng X)
        private void DrawBuildPopup()
        {
            float uiScale = Mathf.Clamp(Screen.height / 1080f, 0.85f, 1.85f);
            CalculateRadialPopupRect();

            // Spring Pop Scale (EaseOutBack từ 0 -> 1 trong 0.22s, không Dust Puff, không sao xoay)
            float t = Mathf.Clamp01(_radialOpenElapsed / RADIAL_POP_DURATION);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float tm1 = t - 1f;
            float popScale = Mathf.Max(0.05f, 1f + c3 * tm1 * tm1 * tm1 + c1 * tm1 * tm1);

            float cx = popupRect.center.x;
            float cy = popupRect.center.y;

            // 1. Vẽ Khung Vòng Tròn Gỗ Trung Tâm (Ring Frame)
            float ringSize = 212f * uiScale * popScale;
            Rect ringRect  = new Rect(cx - ringSize * 0.5f, cy - ringSize * 0.5f, ringSize, ringSize);
            if (_texRadialRing != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(ringRect, _texRadialRing, ScaleMode.StretchToFill, true);
            }

            Vector2 screenMouse = GetMouseScreenPosition();
            Vector2 guiMouse    = new Vector2(screenMouse.x, Screen.height - screenMouse.y);

            float orbitRadius = 86f * uiScale * popScale;
            float btnSize     = 84f * uiScale * popScale;

            int hoveredOption = -1;

            // Nút 1 (Góc 135° Trái-Trên): Pháo Đơn (70 Vàng, Tầm 3.6)
            float radCannon = 135f * Mathf.Deg2Rad;
            Vector2 posCannon = new Vector2(cx + Mathf.Cos(radCannon) * orbitRadius, cy - Mathf.Sin(radCannon) * orbitRadius);
            bool hoverCannon = DrawRadialTowerNode(posCannon, btnSize, uiScale * popScale, _texBtnCannon, STR_PRICE_CANNON, cannonCost, guiMouse, out bool clickedCannon);
            if (hoverCannon) hoveredOption = 0;
            if (clickedCannon)
            {
                Build(cannonTowerPrefab, cannonCost, TowerType.Cannon);
                return;
            }

            // Nút 2 (Góc 45° Phải-Trên): Pháo Tên Lửa (100 Vàng, Tầm 4.4)
            float radRocket = 45f * Mathf.Deg2Rad;
            Vector2 posRocket = new Vector2(cx + Mathf.Cos(radRocket) * orbitRadius, cy - Mathf.Sin(radRocket) * orbitRadius);
            bool hoverRocket = DrawRadialTowerNode(posRocket, btnSize, uiScale * popScale, _texBtnRocket, STR_PRICE_ROCKET, rocketCost, guiMouse, out bool clickedRocket);
            if (hoverRocket) hoveredOption = 1;
            if (clickedRocket)
            {
                Build(rocketTowerPrefab, rocketCost, TowerType.Rocket);
                return;
            }

            // Nút 3 (Góc 270° Chính-Dưới): Pháo Laser / Điện Từ (150 Vàng, Tầm 4.8)
            float radLaser = 270f * Mathf.Deg2Rad;
            Vector2 posLaser = new Vector2(cx + Mathf.Cos(radLaser) * orbitRadius, cy - Mathf.Sin(radLaser) * orbitRadius);
            GameObject laserPrefabToUse = laserTowerPrefab != null ? laserTowerPrefab : cannonTowerPrefab;
            bool hoverLaser = DrawRadialTowerNode(posLaser, btnSize, uiScale * popScale, _texBtnLaser, STR_PRICE_LASER, laserCost, guiMouse, out bool clickedLaser);
            if (hoverLaser) hoveredOption = 2;
            if (clickedLaser)
            {
                Build(laserPrefabToUse, laserCost, TowerType.Laser);
                return;
            }

            // Nút Đóng [X] Đỏ Tròn (Góc 315° Đông-Nam của Vòng Tròn)
            float radCancel = 315f * Mathf.Deg2Rad;
            float cancelDist = 88f * uiScale * popScale;
            Vector2 posCancel = new Vector2(cx + Mathf.Cos(radCancel) * cancelDist, cy - Mathf.Sin(radCancel) * cancelDist);
            float cancelSize = 42f * uiScale * popScale;
            bool hoverCancel = Vector2.Distance(guiMouse, posCancel) <= (cancelSize * 0.52f);
            float cancelDrawSize = hoverCancel ? cancelSize * 1.12f : cancelSize;
            Rect cancelRect = new Rect(posCancel.x - cancelDrawSize * 0.5f, posCancel.y - cancelDrawSize * 0.5f, cancelDrawSize, cancelDrawSize);

            if (_texBtnCancel != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(cancelRect, _texBtnCancel, ScaleMode.StretchToFill, true);
            }
            if (GUI.Button(cancelRect, GUIContent.none, GUIStyle.none))
            {
                CloseAllMenus();
                return;
            }

            // Hiển thị vòng Tầm Đánh Preview & nhãn thông tin nhanh khi Hover vào 1 trong 3 nút pháo
            if (hoveredOption == 0)
            {
                SetPreviewRange(true, 3.6f);
                DrawRadialTooltip(cx, cy - 134f * uiScale, uiScale, STR_TOOLTIP_CANNON);
            }
            else if (hoveredOption == 1)
            {
                SetPreviewRange(true, 4.4f);
                DrawRadialTooltip(cx, cy - 134f * uiScale, uiScale, STR_TOOLTIP_ROCKET);
            }
            else if (hoveredOption == 2)
            {
                SetPreviewRange(true, 4.8f);
                DrawRadialTooltip(cx, cy - 134f * uiScale, uiScale, STR_TOOLTIP_LASER);
            }
            else
            {
                SetPreviewRange(false);
            }

            GUI.color = Color.white;
        }

        private bool DrawRadialTowerNode(
            Vector2 nodeCenter,
            float baseBtnSize,
            float scale,
            Texture2D iconTex,
            string cachedPriceText,
            int cost,
            Vector2 guiMouse,
            out bool clicked)
        {
            clicked = false;
            bool canAfford = GameManager.Instance != null && GameManager.Instance.CanAfford(cost);
            bool isHovered = Vector2.Distance(guiMouse, nodeCenter) <= (baseBtnSize * 0.54f);

            float drawSize = isHovered ? baseBtnSize * 1.12f : baseBtnSize;
            Rect iconRect  = new Rect(nodeCenter.x - drawSize * 0.5f, nodeCenter.y - drawSize * 0.5f, drawSize, drawSize);

            if (iconTex != null)
            {
                GUI.color = canAfford
                    ? (isHovered ? new Color(1f, 1f, 0.92f, 1f) : Color.white)
                    : new Color(0.55f, 0.52f, 0.50f, 0.92f);
                GUI.DrawTexture(iconRect, iconTex, ScaleMode.StretchToFill, true);
            }

            // Bảng Giá Vàng (Price Badge) gắn ngay dưới đáy nút tròn
            float badgeW = 68f * scale * (isHovered ? 1.08f : 1f);
            float badgeH = 27f * scale * (isHovered ? 1.08f : 1f);
            Rect badgeRect = new Rect(
                nodeCenter.x - badgeW * 0.5f,
                nodeCenter.y + drawSize * 0.34f - badgeH * 0.2f,
                badgeW,
                badgeH
            );

            if (_texPriceBadge != null)
            {
                GUI.color = canAfford ? Color.white : new Color(0.75f, 0.55f, 0.55f, 0.95f);
                GUI.DrawTexture(badgeRect, _texPriceBadge, ScaleMode.StretchToFill, true);
            }

            GUI.color = Color.white;
            // Căn chữ số tiền lệch nhẹ sang phải đồng xu vàng trong Price Badge
            Rect textRect = new Rect(badgeRect.x + badgeW * 0.24f, badgeRect.y - 1f * scale, badgeW * 0.72f, badgeH);
            GUIStyle priceSt = canAfford ? _priceLabelStyle : _priceUnaffordableStyle;
            if (priceSt != null)
            {
                // Viền bóng đổ chữ đậm kiểu Kingdom Rush
                Color origCol = priceSt.normal.textColor;
                priceSt.normal.textColor = new Color(0.16f, 0.08f, 0.02f, 0.95f);
                GUI.Label(new Rect(textRect.x + 1.5f, textRect.y + 1.5f, textRect.width, textRect.height), cachedPriceText, priceSt);
                priceSt.normal.textColor = origCol;
                GUI.Label(textRect, cachedPriceText, priceSt);
            }

            // Vùng bấm phủ trọn cả Icon tròn và Price Badge
            Rect hitRect = new Rect(
                Mathf.Min(iconRect.x, badgeRect.x),
                iconRect.y,
                Mathf.Max(iconRect.width, badgeRect.width),
                (badgeRect.yMax - iconRect.y)
            );

            if (GUI.Button(hitRect, GUIContent.none, GUIStyle.none))
            {
                if (canAfford)
                {
                    clicked = true;
                }
            }

            return isHovered || hitRect.Contains(guiMouse);
        }

        private void DrawRadialTooltip(float cx, float topY, float uiScale, string tooltipText)
        {
            if (_radialTooltipStyle == null) return;
            float w = 268f * uiScale;
            float h = 26f * uiScale;
            Rect r = new Rect(cx - w * 0.5f, Mathf.Max(8f, topY), w, h);
            GUI.Box(r, tooltipText, _radialTooltipStyle);
        }

        private static string GetCachedCostString(int cost)
        {
            if (!_cachedCostStrings.TryGetValue(cost, out string s))
            {
                s = cost.ToString();
                _cachedCostStrings[cost] = s;
            }
            return s;
        }

        private static string GetCachedRefundString(int refund)
        {
            if (!_cachedRefundStrings.TryGetValue(refund, out string s))
            {
                s = "+" + refund.ToString();
                _cachedRefundStrings[refund] = s;
            }
            return s;
        }

        private static string GetCachedTowerHeader(Tower tower)
        {
            int key = ((int)tower.Type * 10) + tower.Level;
            if (!_cachedHeaderStrings.TryGetValue(key, out string s))
            {
                s = $"{tower.TowerTypeName} (Cấp {tower.Level}/{tower.MaxLevel})  •  ST: {tower.DamageRangeText}";
                _cachedHeaderStrings[key] = s;
            }
            return s;
        }

        // ─── Vẽ Vòng Tròn Tương Tác Khi Bấm Vào Tháp Pháo (Nâng Cấp Cấp 1->2->3 & Bán Pháo Hoàn Vàng)
        private void DrawUpgradePopup()
        {
            float uiScale = Mathf.Clamp(Screen.height / 1080f, 0.85f, 1.85f);
            CalculateRadialPopupRect();

            // Spring Pop Scale (EaseOutBack từ 0 -> 1 trong 0.22s)
            float t = Mathf.Clamp01(_radialOpenElapsed / RADIAL_POP_DURATION);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float tm1 = t - 1f;
            float popScale = Mathf.Max(0.05f, 1f + c3 * tm1 * tm1 * tm1 + c1 * tm1 * tm1);

            float cx = popupRect.center.x;
            float cy = popupRect.center.y;

            // 1. Vẽ Khung Vòng Tròn Gỗ Trung Tâm (Ring Frame) bao quanh Tháp Pháo
            float ringSize = 212f * uiScale * popScale;
            Rect ringRect  = new Rect(cx - ringSize * 0.5f, cy - ringSize * 0.5f, ringSize, ringSize);
            if (_texRadialRing != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(ringRect, _texRadialRing, ScaleMode.StretchToFill, true);
            }

            Vector2 screenMouse = GetMouseScreenPosition();
            Vector2 guiMouse    = new Vector2(screenMouse.x, Screen.height - screenMouse.y);

            float orbitRadius = 86f * uiScale * popScale;
            float btnSize     = 84f * uiScale * popScale;

            int hoveredAction = -1; // 0: Upgrade, 1: Sell

            // 2. Nút Trên Cùng (Góc 90° Chính-Bắc / 12h): NÂNG CẤP THÁP (hoặc Huy Hiệu MAX ★★★)
            float radUp = 90f * Mathf.Deg2Rad;
            Vector2 posUpgrade = new Vector2(cx + Mathf.Cos(radUp) * orbitRadius, cy - Mathf.Sin(radUp) * orbitRadius);

            if (currentTower.CanUpgrade)
            {
                int upCost = currentTower.UpgradeCost;
                string upCostStr = GetCachedCostString(upCost);
                Texture2D upIcon = _texBtnUpgrade != null ? _texBtnUpgrade : GetTowerTypeIcon(currentTower.Type);
                bool hoverUp = DrawRadialTowerNode(posUpgrade, btnSize, uiScale * popScale, upIcon, upCostStr, upCost, guiMouse, out bool clickedUp);
                if (hoverUp) hoveredAction = 0;
                if (clickedUp)
                {
                    PerformUpgrade(upCost);
                    return;
                }
            }
            else
            {
                // Đã đạt Cấp 3 Tối Đa (MAX)
                Texture2D maxIcon = _texBtnMax != null ? _texBtnMax : GetTowerTypeIcon(currentTower.Type);
                bool hoverMax = DrawRadialTowerNode(posUpgrade, btnSize, uiScale * popScale, maxIcon, STR_MAX_LEVEL, 0, guiMouse, out _);
                if (hoverMax) hoveredAction = 0;
            }

            // 3. Nút Dưới Cùng (Góc 270° Chính-Nam / 6h): BÁN PHÁO (Hoàn 70% Vàng)
            float radSell = 270f * Mathf.Deg2Rad;
            Vector2 posSell = new Vector2(cx + Mathf.Cos(radSell) * orbitRadius, cy - Mathf.Sin(radSell) * orbitRadius);
            int refundGold = currentTower.SellRefundGold;
            string refundStr = GetCachedRefundString(refundGold);
            Texture2D sellIcon = _isConfirmingSell
                ? (_texBtnConfirmSell != null ? _texBtnConfirmSell : _texBtnSell)
                : _texBtnSell;

            bool hoverSell = DrawRadialSellNode(posSell, btnSize, uiScale * popScale, sellIcon, refundStr, _isConfirmingSell, guiMouse, out bool clickedSell);
            if (hoverSell) hoveredAction = 1;
            if (clickedSell)
            {
                if (!_isConfirmingSell)
                {
                    _isConfirmingSell = true;
                }
                else
                {
                    SellTower();
                    return;
                }
            }

            // 4. Nút Đóng [X] Đỏ Tròn (Góc 315° Đông-Nam của Vòng Tròn)
            float radCancel = 315f * Mathf.Deg2Rad;
            float cancelDist = 88f * uiScale * popScale;
            Vector2 posCancel = new Vector2(cx + Mathf.Cos(radCancel) * cancelDist, cy - Mathf.Sin(radCancel) * cancelDist);
            float cancelSize = 42f * uiScale * popScale;
            bool hoverCancel = Vector2.Distance(guiMouse, posCancel) <= (cancelSize * 0.52f);
            float cancelDrawSize = hoverCancel ? cancelSize * 1.12f : cancelSize;
            Rect cancelRect = new Rect(posCancel.x - cancelDrawSize * 0.5f, posCancel.y - cancelDrawSize * 0.5f, cancelDrawSize, cancelDrawSize);

            if (_texBtnCancel != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(cancelRect, _texBtnCancel, ScaleMode.StretchToFill, true);
            }
            if (GUI.Button(cancelRect, GUIContent.none, GUIStyle.none))
            {
                CloseAllMenus();
                return;
            }

            // 5. Hiển thị thanh thông tin gọn gàng phía trên & Preview tầm bắn cấp tiếp theo khi Hover nút Nâng Cấp
            if (hoveredAction == 0 && currentTower.CanUpgrade)
            {
                SetPreviewRange(true, currentTower.NextLevelAttackRange);
                DrawRadialTooltip(cx, cy - 142f * uiScale, uiScale, currentTower.NextUpgradeBonusSummary);
            }
            else if (hoveredAction == 1)
            {
                SetPreviewRange(false);
                DrawRadialTooltip(
                    cx,
                    cy - 142f * uiScale,
                    uiScale,
                    _isConfirmingSell ? "BẤM LẦN NỮA ĐỂ XÁC NHẬN BÁN PHÁO!" : "BÁN PHÁO  •  Thu hồi 70% Vàng đã đầu tư"
                );
            }
            else
            {
                SetPreviewRange(false);
                DrawRadialTooltip(cx, cy - 142f * uiScale, uiScale, GetCachedTowerHeader(currentTower));
            }

            GUI.color = Color.white;
        }

        private Texture2D GetTowerTypeIcon(TowerType type)
        {
            switch (type)
            {
                case TowerType.Cannon: return _texBtnCannon;
                case TowerType.Rocket: return _texBtnRocket;
                case TowerType.Laser:  return _texBtnLaser;
                default:               return _texBtnCannon;
            }
        }

        private bool DrawRadialSellNode(
            Vector2 nodeCenter,
            float baseBtnSize,
            float scale,
            Texture2D iconTex,
            string cachedRefundText,
            bool isConfirmState,
            Vector2 guiMouse,
            out bool clicked)
        {
            clicked = false;
            bool isHovered = Vector2.Distance(guiMouse, nodeCenter) <= (baseBtnSize * 0.54f);
            float drawSize = isHovered ? baseBtnSize * 1.12f : baseBtnSize;
            Rect iconRect  = new Rect(nodeCenter.x - drawSize * 0.5f, nodeCenter.y - drawSize * 0.5f, drawSize, drawSize);

            if (iconTex != null)
            {
                GUI.color = isHovered ? new Color(1f, 1f, 0.92f, 1f) : Color.white;
                GUI.DrawTexture(iconRect, iconTex, ScaleMode.StretchToFill, true);
            }
            else
            {
                // Fallback nút tròn đỏ/cam vàng khi chưa nạp sprite nút Bán
                GUIStyle fallbackStyle = isConfirmState ? _btnGreen : _btnCancel;
                GUI.Box(iconRect, isConfirmState ? "✔ BÁN" : "💰 BÁN", fallbackStyle);
            }

            // Bảng Giá Vàng Hoàn Lại (+50, +126...) gắn ngay dưới đáy nút Bán
            float badgeW = 72f * scale * (isHovered ? 1.08f : 1f);
            float badgeH = 27f * scale * (isHovered ? 1.08f : 1f);
            Rect badgeRect = new Rect(
                nodeCenter.x - badgeW * 0.5f,
                nodeCenter.y + drawSize * 0.34f - badgeH * 0.2f,
                badgeW,
                badgeH
            );

            if (_texPriceBadge != null)
            {
                GUI.color = isConfirmState ? new Color(0.78f, 1f, 0.78f, 1f) : Color.white;
                GUI.DrawTexture(badgeRect, _texPriceBadge, ScaleMode.StretchToFill, true);
            }

            GUI.color = Color.white;
            Rect textRect = new Rect(badgeRect.x + badgeW * 0.22f, badgeRect.y - 1f * scale, badgeW * 0.75f, badgeH);
            if (_priceLabelStyle != null)
            {
                Color origCol = _priceLabelStyle.normal.textColor;
                _priceLabelStyle.normal.textColor = new Color(0.16f, 0.08f, 0.02f, 0.95f);
                GUI.Label(new Rect(textRect.x + 1.5f, textRect.y + 1.5f, textRect.width, textRect.height), cachedRefundText, _priceLabelStyle);
                _priceLabelStyle.normal.textColor = isConfirmState ? new Color(0.55f, 1f, 0.55f, 1f) : origCol;
                GUI.Label(textRect, cachedRefundText, _priceLabelStyle);
                _priceLabelStyle.normal.textColor = origCol;
            }

            Rect hitRect = new Rect(
                Mathf.Min(iconRect.x, badgeRect.x),
                iconRect.y,
                Mathf.Max(iconRect.width, badgeRect.width),
                (badgeRect.yMax - iconRect.y)
            );

            if (GUI.Button(hitRect, GUIContent.none, GUIStyle.none))
            {
                clicked = true;
            }

            return isHovered || hitRect.Contains(guiMouse);
        }

        // ─── Xử lý Xây & Nâng Cấp ──────────────────────────────────────────
        private void Build(GameObject prefab, int cost, TowerType towerType = TowerType.Cannon)
        {
            if (HasTower || prefab == null) return;
            if (GameManager.Instance == null || !GameManager.Instance.CanAfford(cost))
            {
                Debug.Log($"<color=red>[Xây Tháp] (!) Không đủ vàng! Cần {cost} vàng.</color>");
                CloseAllMenus();
                return;
            }

            isBuilt = true;
            CloseAllMenus();

            GameManager.Instance.SpendGold(cost);

            GameObject towerGO = Instantiate(prefab, transform.position, Quaternion.identity);
            currentTower = towerGO.GetComponent<Tower>();
            if (currentTower != null)
            {
                currentTower.ConfigureForType(towerType, cost);
            }

            GameObject towersGroup = GameObject.Find("Towers");
            if (towersGroup != null)
                towerGO.transform.SetParent(towersGroup.transform, true);

            if (buildEffectPrefab != null)
            {
                GameObject fx = Instantiate(buildEffectPrefab, transform.position, Quaternion.identity);
                fx.transform.localScale *= GameManager.GetMapTowerVisualScale();
            }

            // Ẩn toàn bộ hình chờ của ô đất vì Tháp vừa mua đã mang theo Đế Trụ (tile180..183) + Ụ Pháo!
            if (baseRenderer != null) baseRenderer.enabled = false;
            if (indicatorRenderer != null) indicatorRenderer.enabled = false;

            string name = currentTower != null ? currentTower.TowerTypeName : "Tháp";
            Debug.Log($"<color=green>[Xây Tháp] [OK] {name} xây thành công tại {transform.position}! Vàng còn: {GameManager.Instance.Gold}</color>");
        }

        private void PerformUpgrade(int cost)
        {
            if (currentTower == null || !currentTower.CanUpgrade) return;
            if (GameManager.Instance == null || !GameManager.Instance.CanAfford(cost))
            {
                Debug.Log($"<color=red>[Nâng Cấp] (!) Không đủ vàng! Cần {cost} vàng.</color>");
                return;
            }

            GameManager.Instance.SpendGold(cost);
            currentTower.UpgradeToNextLevel(buildEffectPrefab);
            if (currentTower != null)
            {
                currentTower.UpdateRangeIndicatorScale();
            }
            TriggerTapBounce();
            CloseAllMenus();
        }

        // ─── Bán Pháo & Thu Hồi Vàng (70% Tổng Vàng Đầu Tư) ────────────────
        private void SellTower()
        {
            if (!HasTower || currentTower == null) return;

            int refund = currentTower.SellRefundGold;
            string soldTowerName = currentTower.TowerTypeName;
            int soldLevel = currentTower.Level;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddGold(refund);
            }

            currentTower.OnSold();

            if (buildEffectPrefab != null)
            {
                GameObject fx = Instantiate(buildEffectPrefab, transform.position, Quaternion.identity);
                fx.transform.localScale *= GameManager.GetMapTowerVisualScale();
            }

            Destroy(currentTower.gameObject);
            currentTower = null;
            isBuilt = false;

            CloseAllMenus();
            TriggerTapBounce();

            int currentGold = GameManager.Instance != null ? GameManager.Instance.Gold : 0;
            Debug.Log($"<color=#FFD700><b>💰 [BÁN PHÁO] Đã bán {soldTowerName} (Cấp {soldLevel}) -> Hoàn lại +{refund} Vàng! Tổng vàng hiện tại: {currentGold}</b></color>");
        }

        // ─── Helper ────────────────────────────────────────────────────────
        private void ResetVisual()
        {
            transform.localScale = initialScale;
            if (baseRenderer != null) baseRenderer.enabled = false;
            if (indicatorRenderer != null) indicatorRenderer.enabled = false;
        }

        private Vector2 GetMouseWorldPosition()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return Vector2.zero;
            Vector2 screenPos = GetMouseScreenPosition();
            Vector3 world = mainCamera.ScreenToWorldPoint(screenPos);
            return new Vector2(world.x, world.y);
        }

        private Vector2 GetMouseScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#endif
            return Vector2.zero;
        }

        private bool WasLeftMouseClicked()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#endif
            return false;
        }

        public void SetReferences(
            GameObject cannonPrefab,
            GameObject rocketPrefab,
            GameObject fxPrefab,
            SpriteRenderer bRenderer,
            SpriteRenderer iRenderer,
            GameObject laserPrefab = null)
        {
            cannonTowerPrefab = cannonPrefab;
            rocketTowerPrefab = rocketPrefab;
            if (laserPrefab != null) laserTowerPrefab = laserPrefab;
            buildEffectPrefab = fxPrefab;
            baseRenderer      = bRenderer;
            indicatorRenderer = iRenderer;
        }

        private static Font _fontUi;
        private static Font _fontTitleUi;

        private static float _lastUiScale = -1f;

        private static void InitStyles()
        {
            float uiScale = Mathf.Clamp(Screen.height / 1080f, 0.85f, 1.85f);
            if (_stylesInit && Mathf.Abs(_lastUiScale - uiScale) < 0.02f && _boxStyle != null && _boxStyle.normal.background != null && _btnPurple != null) return;
            _stylesInit = true;
            _lastUiScale = uiScale;

            int szTitle = Mathf.RoundToInt(17 * uiScale);
            int szSub   = Mathf.RoundToInt(13 * uiScale);
            int szBtn   = Mathf.RoundToInt(15 * uiScale);

            _fontUi = Font.CreateDynamicFontFromOSFont(new string[] { "Segoe UI", "Arial", "Tahoma" }, szSub);
            _fontTitleUi = Font.CreateDynamicFontFromOSFont(new string[] { "Segoe UI", "Arial", "Tahoma" }, szTitle);

            Color gold = new Color(0.95f, 0.74f, 0.27f);

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = MakeRounded(48, 14, new Color(0.26f, 0.16f, 0.10f, 0.98f), new Color(0.13f, 0.08f, 0.05f, 0.98f), gold, Mathf.Max(2, Mathf.RoundToInt(2.5f * uiScale)));
            _boxStyle.border = new RectOffset(16, 16, 16, 16);

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.font = _fontTitleUi;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.fontSize  = szTitle;
            _titleStyle.normal.textColor = new Color(1f, 0.90f, 0.40f, 1f);

            _subTitleStyle = new GUIStyle(GUI.skin.label);
            _subTitleStyle.font = _fontUi;
            _subTitleStyle.fontSize = szSub;
            _subTitleStyle.normal.textColor = new Color(0.95f, 0.92f, 0.85f, 0.95f);
            _subTitleStyle.richText = true;

            // Nút Pháo Đơn Cyber (xanh lá bo góc viền vàng)
            _btnGreen = CreateButtonStyle(szBtn, new Color(0.38f, 0.82f, 0.26f), new Color(0.14f, 0.50f, 0.12f), gold, Color.white);

            // Nút Pháo Tên Lửa (xanh dương bo góc viền vàng)
            _btnBlue = CreateButtonStyle(szBtn, new Color(0.30f, 0.58f, 0.92f), new Color(0.12f, 0.30f, 0.62f), gold, Color.white);

            // Nút Tháp Laser/Plasma Tím (Tím Hoàng Gia bo góc viền vàng)
            _btnPurple = CreateButtonStyle(szBtn, new Color(0.65f, 0.22f, 0.92f), new Color(0.35f, 0.08f, 0.52f), gold, new Color(1f, 0.94f, 1f, 1f));

            // Nút Nâng Cấp Level 2 & 3 (Cam Vàng bo góc viền vàng)
            _btnYellow = CreateButtonStyle(szBtn, new Color(1f, 0.68f, 0.15f), new Color(0.70f, 0.35f, 0.04f), gold, Color.white);

            // Nút Huỷ (đỏ gỗ bo góc viền vàng)
            _btnCancel = CreateButtonStyle(Mathf.RoundToInt(13 * uiScale), new Color(0.75f, 0.22f, 0.18f), new Color(0.42f, 0.10f, 0.10f), gold, new Color(1f, 0.90f, 0.90f));

            // ── Tải & Cache bộ Sprite Vòng Tròn Đặt Trụ & Nâng Cấp/Bán Pháo Kingdom Rush (Zero-GC)
            if (_texRadialRing     == null) _texRadialRing     = CyberAssetLoader.GetTexture("ui_radial_ring");
            if (_texBtnCannon      == null) _texBtnCannon      = CyberAssetLoader.GetTexture("ui_radial_btn_cannon");
            if (_texBtnRocket      == null) _texBtnRocket      = CyberAssetLoader.GetTexture("ui_radial_btn_rocket");
            if (_texBtnLaser       == null) _texBtnLaser       = CyberAssetLoader.GetTexture("ui_radial_btn_laser");
            if (_texBtnUpgrade     == null) _texBtnUpgrade     = CyberAssetLoader.GetTexture("ui_radial_btn_upgrade");
            if (_texBtnMax         == null) _texBtnMax         = CyberAssetLoader.GetTexture("ui_radial_btn_max");
            if (_texBtnSell        == null) _texBtnSell        = CyberAssetLoader.GetTexture("ui_radial_btn_sell");
            if (_texBtnConfirmSell == null) _texBtnConfirmSell = CyberAssetLoader.GetTexture("ui_radial_btn_confirm_sell");
            if (_texPriceBadge     == null) _texPriceBadge     = CyberAssetLoader.GetTexture("ui_radial_price_badge");
            if (_texBtnCancel      == null) _texBtnCancel      = CyberAssetLoader.GetTexture("ui_radial_cancel");

            int szPrice = Mathf.RoundToInt(15 * uiScale);
            _priceLabelStyle = new GUIStyle(GUI.skin.label)
            {
                font      = _fontTitleUi,
                fontStyle = FontStyle.Bold,
                fontSize  = szPrice,
                alignment = TextAnchor.MiddleCenter
            };
            _priceLabelStyle.normal.textColor = new Color(1f, 0.96f, 0.78f, 1f);

            _priceUnaffordableStyle = new GUIStyle(_priceLabelStyle);
            _priceUnaffordableStyle.normal.textColor = new Color(1f, 0.38f, 0.38f, 1f);

            _radialTooltipStyle = new GUIStyle(GUI.skin.box)
            {
                font      = _fontUi,
                fontStyle = FontStyle.Bold,
                fontSize  = Mathf.RoundToInt(12 * uiScale),
                alignment = TextAnchor.MiddleCenter
            };
            _radialTooltipStyle.normal.background = MakeRounded(32, 10, new Color(0.22f, 0.14f, 0.08f, 0.95f), new Color(0.12f, 0.07f, 0.04f, 0.95f), gold, 2);
            _radialTooltipStyle.border = new RectOffset(10, 10, 10, 10);
            _radialTooltipStyle.normal.textColor = new Color(1f, 0.94f, 0.72f, 1f);
        }

        private static GUIStyle CreateButtonStyle(int fontSize, Color top, Color bottom, Color border, Color text)
        {
            GUIStyle st = new GUIStyle(GUI.skin.button)
            {
                font = _fontUi,
                fontStyle = FontStyle.Bold,
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter
            };
            st.normal.background = MakeRounded(48, 12, top, bottom, border, 2);
            st.hover.background  = MakeRounded(48, 12, Color.Lerp(top, Color.white, 0.25f), Color.Lerp(bottom, Color.white, 0.18f), new Color(1f, 0.92f, 0.5f), 2);
            st.active.background = MakeRounded(48, 12, Color.Lerp(bottom, Color.black, 0.15f), Color.Lerp(bottom, Color.black, 0.35f), border, 2);
            st.border  = new RectOffset(14, 14, 14, 14);
            st.padding = new RectOffset(4, 4, 2, 2);
            st.margin  = new RectOffset(0, 0, 0, 0);

            st.normal.textColor  = text;
            st.hover.textColor   = Color.white;
            st.active.textColor  = text;
            st.focused.textColor = text;
            return st;
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

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D t = new Texture2D(w, h);
            t.SetPixels(pix);
            t.Apply();
            return t;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, clickRadius);
        }
    }
}
