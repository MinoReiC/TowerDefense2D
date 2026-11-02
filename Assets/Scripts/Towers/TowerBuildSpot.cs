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
        private bool isBuilt          = false;
        private bool isMenuOpen       = false;
        private bool isUpgradeMenuOpen = false;
        private Tower currentTower    = null;

        private Camera mainCamera;
        private Vector3 initialScale;

        // GUI popup rect
        private Rect popupRect;
        private const float BUILD_POPUP_W = 390f;
        private const float BUILD_POPUP_H = 258f;
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

            // Ô chờ xây tháp: CHƯA hiện đế trụ cố định, chỉ hiện vòng định vị Hologram & dấu (+) nhấp nháy.
            // Sau khi mua trụ pháo xong thì Đế Trụ (tile180..183) mới xuất hiện kèm theo Pháo!
            if (baseRenderer != null)
            {
                Sprite emptyRing = CyberAssetLoader.GetEmptyBuildSpotRingSprite();
                if (emptyRing != null) baseRenderer.sprite = emptyRing;
                baseRenderer.color = normalColor;
                baseRenderer.enabled = !HasTower;
            }
            if (indicatorRenderer != null)
            {
                Sprite emptyPlus = CyberAssetLoader.GetEmptyBuildSpotPlusSprite();
                if (emptyPlus != null) indicatorRenderer.sprite = emptyPlus;
                indicatorRenderer.transform.localScale = Vector3.one * 0.75f;
                indicatorRenderer.enabled = !HasTower;
            }
        }

        private void Update()
        {
            Vector2 mouseWorld = GetMouseWorldPosition();
            float dist = Vector2.Distance(mouseWorld, transform.position);
            bool isHovered = (dist <= clickRadius);

            // Hiệu ứng nhấp nháy & co giãn nhịp nhàng khi chờ xây tháp pháo
            float pulse = 1f + Mathf.Sin(Time.time * 4.5f) * 0.08f;
            float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * 5.0f);

            // 1. Khi đang mở menu (menu Xây hoặc menu Nâng cấp)
            if (isMenuOpen || isUpgradeMenuOpen)
            {
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
                transform.localScale = initialScale;
                if (baseRenderer != null) baseRenderer.enabled = false;
                if (indicatorRenderer != null) indicatorRenderer.enabled = false;
                if (isHovered && WasLeftMouseClicked())
                {
                    OpenUpgradeMenu();
                }
                return;
            }

            // 3. Khi ô CHƯA CÓ THÁP: hiển thị vòng định vị nhấp nháy chờ xây tháp & hover mời bấm MUA THÁP
            if (baseRenderer != null) baseRenderer.enabled = true;
            if (isHovered)
            {
                transform.localScale = initialScale * (pulse * 1.15f);

                bool canAfford = GameManager.Instance != null && GameManager.Instance.CanAfford(cannonCost);
                Color targetColor = canAfford ? hoverAffordableColor : hoverUnaffordableColor;

                if (baseRenderer != null) baseRenderer.color = targetColor;
                if (indicatorRenderer != null)
                {
                    indicatorRenderer.enabled = true;
                    indicatorRenderer.color = targetColor;
                    indicatorRenderer.transform.localScale = Vector3.one * 0.88f;
                }

                if (WasLeftMouseClicked())
                {
                    OpenBuildMenu();
                }
            }
            else
            {
                transform.localScale = initialScale * pulse;
                if (baseRenderer != null)
                {
                    baseRenderer.color = Color.Lerp(normalColor, new Color(0.60f, 1f, 0.75f, 0.95f), blink * 0.45f);
                }
                if (indicatorRenderer != null)
                {
                    indicatorRenderer.enabled = true;
                    indicatorRenderer.color = new Color(
                        Mathf.Lerp(0.55f, 1f, blink),
                        1f,
                        Mathf.Lerp(0.55f, 0.85f, blink),
                        Mathf.Lerp(0.35f, 0.95f, blink)
                    );
                    indicatorRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.65f, 0.82f, blink);
                }
            }
        }

        private void CloseAllMenus()
        {
            isMenuOpen = false;
            isUpgradeMenuOpen = false;
            ResetVisual();
        }

        // ─── Mở popup chọn loại pháo ───────────────────────────────────────
        private void OpenBuildMenu()
        {
            if (HasTower) return;
            isUpgradeMenuOpen = false;

            CalculatePopupRect(BUILD_POPUP_W, BUILD_POPUP_H);
            isMenuOpen = true;
        }

        // ─── Mở popup nâng cấp tháp ─────────────────────────────────────────
        private void OpenUpgradeMenu()
        {
            if (!HasTower || currentTower == null) return;
            isMenuOpen = false;

            CalculatePopupRect(UPGRADE_POPUP_W, UPGRADE_POPUP_H);
            isUpgradeMenuOpen = true;
        }

        private void CalculatePopupRect(float width, float height)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            Vector3 screenPt = mainCamera != null
                ? mainCamera.WorldToScreenPoint(transform.position)
                : new Vector3(Screen.width / 2f, Screen.height / 2f, 0);

            float guiY = Screen.height - screenPt.y - height - 30f;
            float guiX = screenPt.x - width / 2f;
            guiX = Mathf.Clamp(guiX, 15f, Screen.width  - width - 15f);
            guiY = Mathf.Clamp(guiY, 15f, Screen.height - height - 15f);

            popupRect = new Rect(guiX, guiY, width, height);
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

        // Popup Xây Tháp Mới (3 Loại Pháo: Pháo Đơn 70, Tên Lửa 100, Tháp Laser Tím 150)
        private void DrawBuildPopup()
        {
            GUI.Box(popupRect, "", _boxStyle);

            float innerX = popupRect.x + 14f;
            float innerY = popupRect.y + 12f;
            float btnW   = BUILD_POPUP_W - 28f;

            GUI.Label(new Rect(innerX, innerY, btnW, 28f), "🏗  CHỌN LOẠI PHÁO ĐỂ XÂY:", _titleStyle);
            innerY += 32f;

            // 1. Nút Pháo Đơn Cyber (70 Vàng)
            bool canAffordCannon = GameManager.Instance != null && GameManager.Instance.CanAfford(cannonCost);
            string cannonLabel = $"🗼  Pháo Đơn Cyber (ST: 16-24, Crit)  —  {cannonCost} 🪙";
            if (!canAffordCannon) cannonLabel += "  ❌";

            GUI.enabled = cannonTowerPrefab != null && canAffordCannon;
            if (GUI.Button(new Rect(innerX, innerY, btnW, 44f), cannonLabel, _btnGreen))
            {
                Build(cannonTowerPrefab, cannonCost, TowerType.Cannon);
            }
            GUI.enabled = true;
            innerY += 50f;

            // 2. Nút Pháo Tên Lửa (100 Vàng)
            bool canAffordRocket = GameManager.Instance != null && GameManager.Instance.CanAfford(rocketCost);
            string rocketLabel = $"🚀  Pháo Tên Lửa (ST: 38-56, Nổ AoE)  —  {rocketCost} 🪙";
            if (!canAffordRocket) rocketLabel += "  ❌";

            GUI.enabled = rocketTowerPrefab != null && canAffordRocket;
            if (GUI.Button(new Rect(innerX, innerY, btnW, 44f), rocketLabel, _btnBlue))
            {
                Build(rocketTowerPrefab, rocketCost, TowerType.Rocket);
            }
            GUI.enabled = true;
            innerY += 50f;

            // 3. Nút Tháp Laser/Plasma Tím Mới (150 Vàng - Sát thương siêu to & Xuyên giáp cao!)
            bool canAffordLaser = GameManager.Instance != null && GameManager.Instance.CanAfford(laserCost);
            string laserLabel = $"🔮  Tháp Laser Tím (ST: 85-125, Siêu Dame)  —  {laserCost} 🪙";
            if (!canAffordLaser) laserLabel += "  ❌";

            GameObject laserPrefabToUse = laserTowerPrefab != null ? laserTowerPrefab : cannonTowerPrefab;
            GUI.enabled = laserPrefabToUse != null && canAffordLaser;
            if (GUI.Button(new Rect(innerX, innerY, btnW, 44f), laserLabel, _btnPurple))
            {
                Build(laserPrefabToUse, laserCost, TowerType.Laser);
            }
            GUI.enabled = true;
            innerY += 50f;

            // Nút Huỷ
            if (GUI.Button(new Rect(innerX + btnW / 2f - 44f, innerY, 88f, 26f), "✕  Đóng", _btnCancel))
            {
                CloseAllMenus();
            }
        }

        // Popup Nâng Cấp Tháp (Hỗ trợ Cấp 1 -> Cấp 2 -> Cấp 3 MAX)
        private void DrawUpgradePopup()
        {
            GUI.Box(popupRect, "", _boxStyle);

            float innerX = popupRect.x + 16f;
            float innerY = popupRect.y + 14f;
            float btnW   = UPGRADE_POPUP_W - 32f;

            string levelBadge = currentTower.Level >= currentTower.MaxLevel
                ? "⭐⭐⭐ (CẤP 3 MAX)"
                : (currentTower.Level == 2 ? "⭐⭐ (Cấp 2/3)" : "⭐ (Cấp 1/3)");
            GUI.Label(new Rect(innerX, innerY, btnW, 28f), $"{currentTower.TowerTypeName}  {levelBadge}", _titleStyle);
            innerY += 30f;

            // Thông số chi tiết tháp
            int armorPenPct = Mathf.RoundToInt(currentTower.ArmorPenetration * 100f);
            string stats = $"Sát thương: <b>{currentTower.DamageRangeText}</b>  |  Tầm: <b>{currentTower.AttackRange:F1}</b>  |  Phá giáp: <b>{armorPenPct}%</b>";
            GUI.Label(new Rect(innerX, innerY, btnW, 24f), stats, _subTitleStyle);
            innerY += 30f;

            if (currentTower.CanUpgrade)
            {
                int upCost = currentTower.UpgradeCost;
                int nextLv = currentTower.Level + 1;
                bool canAfford = GameManager.Instance != null && GameManager.Instance.CanAfford(upCost);

                string upLabel = $"⭐  NÂNG CẤP LÊN CẤP {nextLv}  —  {upCost} 🪙";
                if (!canAfford) upLabel += "  (Thiếu vàng)";

                GUI.enabled = canAfford;
                if (GUI.Button(new Rect(innerX, innerY, btnW, 46f), upLabel, _btnYellow))
                {
                    PerformUpgrade(upCost);
                }
                GUI.enabled = true;
                innerY += 54f;

                // Chi tiết thông số tăng thêm khi nâng cấp lên Cấp 2 hoặc Cấp 3
                GUI.Label(new Rect(innerX, innerY, btnW, 24f), $"<color=#A0FFA0>⚡ Cấp {nextLv}: {currentTower.NextUpgradeBonusSummary}</color>", _subTitleStyle);
                innerY += 30f;
            }
            else
            {
                GUI.Label(new Rect(innerX, innerY, btnW, 36f), "✨ THÁP ĐÃ ĐẠT CẤP 3 TỐI ĐA (LEVEL 3 MAX)", _titleStyle);
                innerY += 46f;
            }

            // Nút Đóng
            if (GUI.Button(new Rect(innerX + btnW / 2f - 44f, innerY, 88f, 26f), "✕  Đóng", _btnCancel))
            {
                CloseAllMenus();
            }
        }

        // ─── Xử lý Xây & Nâng Cấp ──────────────────────────────────────────
        private void Build(GameObject prefab, int cost, TowerType towerType = TowerType.Cannon)
        {
            if (HasTower || prefab == null) return;
            if (GameManager.Instance == null || !GameManager.Instance.CanAfford(cost))
            {
                Debug.Log($"<color=red>[Xây Tháp] ❌ Không đủ vàng! Cần {cost} vàng.</color>");
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
                currentTower.ConfigureForType(towerType);
            }

            GameObject towersGroup = GameObject.Find("Towers");
            if (towersGroup != null)
                towerGO.transform.SetParent(towersGroup.transform, true);

            if (buildEffectPrefab != null)
                Instantiate(buildEffectPrefab, transform.position, Quaternion.identity);

            // Ẩn toàn bộ hình chờ của ô đất vì Tháp vừa mua đã mang theo Đế Trụ (tile180..183) + Ụ Pháo!
            if (baseRenderer != null) baseRenderer.enabled = false;
            if (indicatorRenderer != null) indicatorRenderer.enabled = false;

            string name = currentTower != null ? currentTower.TowerTypeName : "Tháp";
            Debug.Log($"<color=green>[Xây Tháp] ✅ {name} xây thành công tại {transform.position}! Vàng còn: {GameManager.Instance.Gold}</color>");
        }

        private void PerformUpgrade(int cost)
        {
            if (currentTower == null || !currentTower.CanUpgrade) return;
            if (GameManager.Instance == null || !GameManager.Instance.CanAfford(cost))
            {
                Debug.Log($"<color=red>[Nâng Cấp] ❌ Không đủ vàng! Cần {cost} vàng.</color>");
                return;
            }

            GameManager.Instance.SpendGold(cost);
            currentTower.UpgradeToNextLevel(buildEffectPrefab);
            CloseAllMenus();
        }

        // ─── Helper ────────────────────────────────────────────────────────
        private void ResetVisual()
        {
            transform.localScale = initialScale;
            if (HasTower)
            {
                if (baseRenderer != null) baseRenderer.enabled = false;
                if (indicatorRenderer != null) indicatorRenderer.enabled = false;
            }
            else
            {
                if (baseRenderer != null)
                {
                    baseRenderer.enabled = true;
                    baseRenderer.color = normalColor;
                }
                if (indicatorRenderer != null)
                {
                    indicatorRenderer.enabled = true;
                    indicatorRenderer.color = new Color(1f, 1f, 1f, 0.7f);
                }
            }
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

        private static void InitStyles()
        {
            if (_stylesInit && _boxStyle != null && _boxStyle.normal.background != null && _btnPurple != null) return;
            _stylesInit = true;

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = MakeTex(2, 2, new Color(0.08f, 0.08f, 0.14f, 0.96f));
            _boxStyle.border = new RectOffset(8, 8, 8, 8);

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.fontSize  = 16;
            _titleStyle.normal.textColor = new Color(1f, 0.92f, 0.45f, 1f);

            _subTitleStyle = new GUIStyle(GUI.skin.label);
            _subTitleStyle.fontSize = 12;
            _subTitleStyle.normal.textColor = new Color(0.9f, 0.95f, 1f, 0.95f);

            // Nút Pháo Đơn Cyber (xanh lá)
            _btnGreen = new GUIStyle(GUI.skin.button);
            _btnGreen.fontStyle = FontStyle.Bold;
            _btnGreen.fontSize  = 14;
            _btnGreen.normal.background  = MakeTex(2, 2, new Color(0.10f, 0.55f, 0.10f, 0.96f));
            _btnGreen.hover.background   = MakeTex(2, 2, new Color(0.18f, 0.80f, 0.18f, 1f));
            _btnGreen.active.background  = MakeTex(2, 2, new Color(0.04f, 0.35f, 0.04f, 1f));
            _btnGreen.normal.textColor   = Color.white;
            _btnGreen.hover.textColor    = Color.white;

            // Nút Pháo Tên Lửa (xanh dương)
            _btnBlue = new GUIStyle(GUI.skin.button);
            _btnBlue.fontStyle = FontStyle.Bold;
            _btnBlue.fontSize  = 14;
            _btnBlue.normal.background  = MakeTex(2, 2, new Color(0.08f, 0.30f, 0.72f, 0.96f));
            _btnBlue.hover.background   = MakeTex(2, 2, new Color(0.18f, 0.48f, 1f,    1f));
            _btnBlue.active.background  = MakeTex(2, 2, new Color(0.04f, 0.18f, 0.50f, 1f));
            _btnBlue.normal.textColor   = Color.white;
            _btnBlue.hover.textColor    = Color.white;

            // Nút Tháp Laser/Plasma Tím (Tím Hoàng Gia Neon)
            _btnPurple = new GUIStyle(GUI.skin.button);
            _btnPurple.fontStyle = FontStyle.Bold;
            _btnPurple.fontSize  = 14;
            _btnPurple.normal.background  = MakeTex(2, 2, new Color(0.45f, 0.12f, 0.68f, 0.97f));
            _btnPurple.hover.background   = MakeTex(2, 2, new Color(0.65f, 0.22f, 0.92f, 1f));
            _btnPurple.active.background  = MakeTex(2, 2, new Color(0.32f, 0.06f, 0.50f, 1f));
            _btnPurple.normal.textColor   = new Color(1f, 0.94f, 1f, 1f);
            _btnPurple.hover.textColor    = Color.white;

            // Nút Nâng Cấp Level 2 & 3 (Vàng Cam)
            _btnYellow = new GUIStyle(GUI.skin.button);
            _btnYellow.fontStyle = FontStyle.Bold;
            _btnYellow.fontSize  = 14;
            _btnYellow.normal.background  = MakeTex(2, 2, new Color(0.85f, 0.55f, 0.05f, 0.96f));
            _btnYellow.hover.background   = MakeTex(2, 2, new Color(1f,    0.72f, 0.15f, 1f));
            _btnYellow.active.background  = MakeTex(2, 2, new Color(0.60f, 0.35f, 0.02f, 1f));
            _btnYellow.normal.textColor   = Color.white;
            _btnYellow.hover.textColor    = Color.white;

            // Nút Huỷ (đỏ tối)
            _btnCancel = new GUIStyle(GUI.skin.button);
            _btnCancel.fontStyle = FontStyle.Bold;
            _btnCancel.fontSize = 12;
            _btnCancel.normal.background = MakeTex(2, 2, new Color(0.45f, 0.08f, 0.08f, 0.92f));
            _btnCancel.hover.background  = MakeTex(2, 2, new Color(0.72f, 0.12f, 0.12f, 1f));
            _btnCancel.normal.textColor  = new Color(1f, 0.75f, 0.75f, 1f);
            _btnCancel.hover.textColor   = Color.white;
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
