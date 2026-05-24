#if UNITY_EDITOR
using GemsSort.Game;
using GemsSort.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace GemsSort.EditorTools
{
    /// <summary>
    /// Editor utility that wipes the active scene's "Gems Sort UI" canvas children
    /// and rebuilds the HUD, hint bar, coin counter, purchase dialog, and win-flow
    /// next-level button cleanly. Every serialized field on GemsSortHud is wired
    /// automatically. Designed to be safe to run repeatedly.
    /// </summary>
    public static class GemsSortUiBuilder
    {
        private const string GemSpritePath = "Assets/GemsSort/Sprites/gemsort_diamond.png";
        private const string CheckSpritePath = "Assets/GemsSort/Sprites/gemsort_check.png";
        private const string ShineSpritePath = "Assets/GemsSort/Sprites/gemsort_shine.png";
        private const string TileSpritePath = "Assets/GemsSort/Sprites/gemsort_tile.png";
        private const string ConfettiPrefabPath = "Assets/Lana Studio/Hyper Casual FX/Prefabs/Confetti";

        public static void SetupLevelCardPrefabOnly()
        {
            var hud = Object.FindObjectOfType<GemsSortHud>();
            if (hud == null)
            {
                EditorUtility.DisplayDialog("Gems Sort", "Could not find a GemsSortHud in the active scene.", "OK");
                return;
            }

            var levelCardPrefab = EnsureLevelCardPrefab();
            
            var so = new SerializedObject(hud);
            SetRef(so, "levelCardPrefab", levelCardPrefab);
            so.ApplyModifiedPropertiesWithoutUndo();
            
            EditorUtility.SetDirty(hud);
            
            if (hud.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            }
            
            EditorUtility.DisplayDialog("Gems Sort", "Level Card Prefab generated and assigned to HUD successfully without touching other UI elements!", "OK");
        }

        public static void RebuildHud()
        {
            if (!Application.isPlaying)
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            }

            var hud = Object.FindObjectOfType<GemsSortHud>();
            if (hud == null)
            {
                EditorUtility.DisplayDialog("Gems Sort", "Could not find a GemsSortHud in the active scene.", "OK");
                return;
            }

            var canvas = hud.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = hud.GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Gems Sort", "GemsSortHud must live on a Canvas. Add or move it.", "OK");
                return;
            }

            EnsureEventSystem();

            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Rebuild Gems Sort HUD");
            ClearCanvasChildren(canvas.transform);

            var sprites = LoadSprites();

            var topBar = BuildTopBar(canvas.transform, out var levelText, out var settingsButton, out var coinIcon, out var coinsText);
            var bottomBar = BuildBottomBar(canvas.transform,
                out var areaButton, out var areaCount,
                out var wandButton, out var wandCount,
                out var magnetButton, out var magnetCount,
                out var shelfButton, out var shelfCount,
                sprites);
            var winNextLevelButton = BuildWinNextLevelButton(canvas.transform, out var winNextLevelButtonText);
            var transitionPanel = BuildTransitionPanel(canvas.transform, out var transitionText, out var transitionCanvasGroup, out var transitionLogo, sprites.coin);
            var purchaseDialogObj = BuildPurchaseDialog(canvas.transform, sprites);
            var settingsPanel = BuildSettingsPanel(canvas.transform, out var soundToggleButton, out var musicToggleButton, out var panelRestartButton, out var closeSettingsButton, out var openGalleryButton);
            var galleryPanel = BuildGalleryPanel(canvas.transform, out var galleryBackButton, out var galleryContentGrid);
            var lockSprite = LoadSpriteFromSheet("Assets/Violet Theme Ui/Colored Icons/Gold Lock.png", "Gold Lock_0");

            // Wire HUD references via SerializedObject so Unity persists everything.
            var so = new SerializedObject(hud);
            SetRef(so, "levelText", levelText);
            SetRef(so, "settingsButton", settingsButton);
            
            SetRef(so, "settingsPanel", settingsPanel);
            SetRef(so, "soundToggleButton", soundToggleButton);
            SetRef(so, "musicToggleButton", musicToggleButton);
            SetRef(so, "panelRestartButton", panelRestartButton);
            SetRef(so, "closeSettingsButton", closeSettingsButton);
            SetRef(so, "openGalleryButton", openGalleryButton);

            SetRef(so, "galleryPanel", galleryPanel);
            SetRef(so, "galleryBackButton", galleryBackButton);
            SetRef(so, "galleryContentGrid", galleryContentGrid);
            SetSprite(so, "lockSprite", lockSprite);

            var levelCardPrefab = EnsureLevelCardPrefab();
            SetRef(so, "levelCardPrefab", levelCardPrefab);

            SetRef(so, "winNextLevelButton", winNextLevelButton);
            SetRef(so, "winNextLevelButtonText", winNextLevelButtonText);
            SetRef(so, "transitionPanel", transitionPanel);
            SetRef(so, "transitionText", transitionText);
            SetRef(so, "transitionCanvasGroup", transitionCanvasGroup);
            SetRef(so, "transitionLogo", transitionLogo);

            SetRef(so, "areaHintButton", areaButton);
            SetRef(so, "wandHintButton", wandButton);
            SetRef(so, "magnetHintButton", magnetButton);
            SetRef(so, "shelfHintButton", shelfButton);
            SetRef(so, "areaHintCountText", areaCount);
            SetRef(so, "wandHintCountText", wandCount);
            SetRef(so, "magnetHintCountText", magnetCount);
            SetRef(so, "shelfHintCountText", shelfCount);

            SetRef(so, "coinsText", coinsText);
            SetRef(so, "coinIcon", coinIcon);
            SetSprite(so, "coinSprite", sprites.coin);
            SetRef(so, "purchaseDialog", purchaseDialogObj.GetComponent<PurchaseDialog>());

            SetRef(so, "topBarRoot", topBar);
            SetRef(so, "bottomBarRoot", bottomBar);

            // Wire audio service for coin sounds.
            var audioSvc = Object.FindObjectOfType<GemsSortAudio>();
            if (audioSvc != null)
            {
                SetRef(so, "audioService", audioSvc);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // Wire the purchase dialog visuals.
            var dialog = purchaseDialogObj.GetComponent<PurchaseDialog>();
            var dialogSO = new SerializedObject(dialog);
            SetSprite(dialogSO, "hintIconSprite", sprites.hint);
            SetSprite(dialogSO, "coinIconSprite", sprites.coin);
            SetSprite(dialogSO, "iconBackdropSprite", sprites.shine);
            dialogSO.ApplyModifiedPropertiesWithoutUndo();

            // Try to wire a confetti prefab on the effects component if one exists.
            TryAssignConfetti();

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(dialog);

            Debug.Log("Gems Sort HUD rebuilt and rewired.", canvas);
        }

        public static void ResetInventory()
        {
            GemsSortInventory.ResetForTesting();
            Debug.Log("Gems Sort inventory reset. Coins and hint counts restored to defaults.");
        }

        public static void GrantCoins()
        {
            GemsSortInventory.AddCoins(500);
            Debug.Log("Granted 500 coins. Total: " + GemsSortInventory.Coins);
        }

        public static void EmptyAllHints()
        {
            while (GemsSortInventory.GetHintCount(GemsSortInventory.HintType.Area) > 0)
            {
                GemsSortInventory.TrySpendHint(GemsSortInventory.HintType.Area);
            }
            while (GemsSortInventory.GetHintCount(GemsSortInventory.HintType.Wand) > 0)
            {
                GemsSortInventory.TrySpendHint(GemsSortInventory.HintType.Wand);
            }
            while (GemsSortInventory.GetHintCount(GemsSortInventory.HintType.Magnet) > 0)
            {
                GemsSortInventory.TrySpendHint(GemsSortInventory.HintType.Magnet);
            }
            while (GemsSortInventory.GetHintCount(GemsSortInventory.HintType.Shelf) > 0)
            {
                GemsSortInventory.TrySpendHint(GemsSortInventory.HintType.Shelf);
            }
            Debug.Log("All hint counts cleared. Click any hint button to test the purchase dialog.");
        }

        // ---- Builder helpers ------------------------------------------------

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
            }
        }

        private static void ClearCanvasChildren(Transform canvasRoot)
        {
            for (int i = canvasRoot.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(canvasRoot.GetChild(i).gameObject);
            }
        }

        private static GameObject BuildTopBar(Transform canvas,
            out TextMeshProUGUI levelText, out Button settingsButton,
            out RectTransform coinIcon, out TextMeshProUGUI coinsText)
        {
            var bar = NewUiObject("Top Bar", canvas);
            var rect = (RectTransform)bar.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 140f);
            rect.anchoredPosition = new Vector2(0f, 0f);
            // Transparent background.

            // Level text (centered)
            levelText = NewText(bar.transform, "Level Label", "Level 1", 44, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            var levelRect = levelText.rectTransform;
            levelRect.anchorMin = new Vector2(0f, 0f);
            levelRect.anchorMax = new Vector2(1f, 1f);
            levelRect.pivot = new Vector2(0.5f, 0.5f);
            levelRect.offsetMin = new Vector2(280f, 0f);
            levelRect.offsetMax = new Vector2(-180f, 0f);

            // Settings button (right)
            settingsButton = NewButton(bar.transform, "Settings Button", "Settings", new Color(0.55f, 0.42f, 0.85f, 1f));
            var settingsRect = (RectTransform)settingsButton.transform;
            settingsRect.anchorMin = new Vector2(1f, 0.5f);
            settingsRect.anchorMax = new Vector2(1f, 0.5f);
            settingsRect.pivot = new Vector2(1f, 0.5f);
            settingsRect.sizeDelta = new Vector2(180f, 80f);
            settingsRect.anchoredPosition = new Vector2(-40f, 0f);

            // Coin counter lives outside the top bar so it stays visible during
            // the win flow while the rest of the gameplay HUD is hidden.
            BuildCoinCounter(canvas, out coinIcon, out coinsText);

            return bar;
        }

        private static GameObject BuildSettingsPanel(Transform canvas,
            out Button soundToggle, out Button musicToggle, out Button restartBtn, out Button closeBtn, out Button openGalleryBtn)
        {
            // Panel overlay (full-screen dim)
            var panel = NewUiImage(canvas, "Settings Panel", new Color(0f, 0f, 0f, 0.75f));
            panel.SetActive(false); // Hidden by default

            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Card background in center
            var card = NewUiImage(panel.transform, "Card", new Color(0.18f, 0.22f, 0.35f, 1f));
            var cardRect = (RectTransform)card.transform;
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(580f, 720f);
            cardRect.anchoredPosition = Vector2.zero;

            // Title "SETTINGS"
            var title = NewText(card.transform, "Title", "SETTINGS", 54, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-40f, 90f);
            titleRect.anchoredPosition = new Vector2(0f, -40f);

            // Controls container
            float startY = -160f;
            float spacingY = -110f;

            // Sound Toggle button
            soundToggle = NewButton(card.transform, "Sound Toggle Button", "SOUND: ON", new Color(0.27f, 0.78f, 0.46f, 1f));
            var soundRect = (RectTransform)soundToggle.transform;
            soundRect.anchorMin = new Vector2(0.5f, 1f);
            soundRect.anchorMax = new Vector2(0.5f, 1f);
            soundRect.pivot = new Vector2(0.5f, 0.5f);
            soundRect.sizeDelta = new Vector2(440f, 85f);
            soundRect.anchoredPosition = new Vector2(0f, startY);

            // Music Toggle button
            musicToggle = NewButton(card.transform, "Music Toggle Button", "MUSIC: ON", new Color(0.27f, 0.78f, 0.46f, 1f));
            var musicRect = (RectTransform)musicToggle.transform;
            musicRect.anchorMin = new Vector2(0.5f, 1f);
            musicRect.anchorMax = new Vector2(0.5f, 1f);
            musicRect.pivot = new Vector2(0.5f, 0.5f);
            musicRect.sizeDelta = new Vector2(440f, 85f);
            musicRect.anchoredPosition = new Vector2(0f, startY + spacingY);

            // Restart Button inside settings
            restartBtn = NewButton(card.transform, "Restart Button", "RESTART LEVEL", new Color(0.95f, 0.48f, 0.08f, 1f));
            var restartRect = (RectTransform)restartBtn.transform;
            restartRect.anchorMin = new Vector2(0.5f, 1f);
            restartRect.anchorMax = new Vector2(0.5f, 1f);
            restartRect.pivot = new Vector2(0.5f, 0.5f);
            restartRect.sizeDelta = new Vector2(440f, 85f);
            restartRect.anchoredPosition = new Vector2(0f, startY + spacingY * 2f);

            // Open Gallery Button
            openGalleryBtn = NewButton(card.transform, "Open Gallery Button", "LEVEL GALLERY", new Color(0.55f, 0.42f, 0.85f, 1f));
            var galleryBtnRect = (RectTransform)openGalleryBtn.transform;
            galleryBtnRect.anchorMin = new Vector2(0.5f, 1f);
            galleryBtnRect.anchorMax = new Vector2(0.5f, 1f);
            galleryBtnRect.pivot = new Vector2(0.5f, 0.5f);
            galleryBtnRect.sizeDelta = new Vector2(440f, 85f);
            galleryBtnRect.anchoredPosition = new Vector2(0f, startY + spacingY * 3f);

            // Close button at bottom
            closeBtn = NewButton(card.transform, "Close Button", "CLOSE", new Color(0.95f, 0.32f, 0.28f, 1f));
            var closeRect = (RectTransform)closeBtn.transform;
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(440f, 85f);
            closeRect.anchoredPosition = new Vector2(0f, 40f);

            return panel;
        }

        private static GameObject BuildGalleryPanel(Transform canvas,
            out Button backBtn, out Transform galleryGrid)
        {
            // Panel overlay (full-screen dim)
            var panel = NewUiImage(canvas, "Gallery Panel", new Color(0f, 0f, 0f, 0.88f));
            panel.SetActive(false); // Hidden by default

            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Title "LEVEL GALLERY"
            var title = NewText(panel.transform, "Title", "LEVEL GALLERY", 54, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(680f, 100f);
            titleRect.anchoredPosition = new Vector2(0f, -40f);

            // Scroll View for Gallery Grid (takes most of full-screen height)
            var scrollView = new GameObject("Gallery Scroll View", typeof(RectTransform), typeof(ScrollRect));
            Undo.RegisterCreatedObjectUndo(scrollView, "Create Gallery Scroll View");
            scrollView.transform.SetParent(panel.transform, false);

            var scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var scrollRectTrans = (RectTransform)scrollView.transform;
            scrollRectTrans.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTrans.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTrans.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTrans.sizeDelta = new Vector2(680f, 1020f);
            scrollRectTrans.anchoredPosition = new Vector2(0f, -20f);

            // Viewport inside Scroll View
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            Undo.RegisterCreatedObjectUndo(viewport, "Create Scroll View Viewport");
            viewport.transform.SetParent(scrollView.transform, false);

            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            scrollRect.viewport = viewportRect;

            // Content grid inside Viewport
            var content = new GameObject("Content", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(content, "Create Scroll View Content");
            content.transform.SetParent(viewport.transform, false);

            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(0f, -1020f);
            contentRect.offsetMax = new Vector2(0f, 0f);

            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(280f, 360f); // Larger cards
            grid.spacing = new Vector2(40f, 40f);
            grid.padding = new RectOffset(30, 30, 30, 30);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2; // 2 columns

            scrollRect.content = contentRect;
            galleryGrid = content.transform;

            // Back button at bottom
            backBtn = NewButton(panel.transform, "Back Button", "BACK", new Color(0.95f, 0.32f, 0.28f, 1f));
            var backRect = (RectTransform)backBtn.transform;
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.sizeDelta = new Vector2(440f, 80f);
            backRect.anchoredPosition = new Vector2(0f, 30f);

            // Shift scroll view up slightly so it doesn't overlap the back button
            scrollRectTrans.anchoredPosition = new Vector2(0f, 40f);
            scrollRectTrans.sizeDelta = new Vector2(680f, 960f);

            return panel;
        }

        private static void BuildCoinCounter(Transform canvas, out RectTransform coinIcon, out TextMeshProUGUI coinsText)
        {
            var coinHud = NewUiImage(canvas, "Coin Counter", new Color(0.18f, 0.24f, 0.42f, 0.85f));
            var coinRect = (RectTransform)coinHud.transform;
            coinRect.anchorMin = new Vector2(0f, 1f);
            coinRect.anchorMax = new Vector2(0f, 1f);
            coinRect.pivot = new Vector2(0f, 1f);
            coinRect.sizeDelta = new Vector2(240f, 88f);
            coinRect.anchoredPosition = new Vector2(40f, -30f);

            var coinIconObj = NewUiImage(coinHud.transform, "Coin Icon", new Color(0.27f, 0.78f, 0.46f, 1f));
            coinIcon = (RectTransform)coinIconObj.transform;
            coinIcon.anchorMin = new Vector2(0f, 0.5f);
            coinIcon.anchorMax = new Vector2(0f, 0.5f);
            coinIcon.pivot = new Vector2(0f, 0.5f);
            coinIcon.sizeDelta = new Vector2(64f, 64f);
            coinIcon.anchoredPosition = new Vector2(12f, 0f);
            var coinIconImage = coinIconObj.GetComponent<Image>();
            coinIconImage.preserveAspect = true;

            coinsText = NewText(coinHud.transform, "Coins", "0", 42, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            var coinsTextRect = coinsText.rectTransform;
            coinsTextRect.anchorMin = new Vector2(0f, 0f);
            coinsTextRect.anchorMax = new Vector2(1f, 1f);
            coinsTextRect.offsetMin = new Vector2(86f, 4f);
            coinsTextRect.offsetMax = new Vector2(-12f, -4f);
        }

        private static GameObject BuildBottomBar(Transform canvas,
            out Button areaButton, out TextMeshProUGUI areaCount,
            out Button wandButton, out TextMeshProUGUI wandCount,
            out Button magnetButton, out TextMeshProUGUI magnetCount,
            out Button shelfButton, out TextMeshProUGUI shelfCount,
            HudSprites sprites)
        {
            var bar = NewUiObject("Bottom Bar", canvas);
            var rect = (RectTransform)bar.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, 200f);
            rect.anchoredPosition = new Vector2(0f, 0f);

            const float spacing = 190f;
            float startX = -spacing * 1.5f;
            areaButton = BuildHintButton(bar.transform, "Area", "AREA", new Color(0.85f, 0.34f, 0.34f, 1f), startX, sprites, out areaCount);
            wandButton = BuildHintButton(bar.transform, "Wand", "WAND", new Color(0.36f, 0.62f, 0.95f, 1f), startX + spacing, sprites, out wandCount);
            magnetButton = BuildHintButton(bar.transform, "Mag", "MAGNET", new Color(0.58f, 0.42f, 0.85f, 1f), startX + spacing * 2f, sprites, out magnetCount);
            shelfButton = BuildHintButton(bar.transform, "Shelf", "SHELF", new Color(0.28f, 0.72f, 0.45f, 1f), startX + spacing * 3f, sprites, out shelfCount);

            return bar;
        }

        private static Button BuildHintButton(Transform parent, string name, string label, Color color, float xOffset, HudSprites sprites, out TextMeshProUGUI countText)
        {
            var button = NewButton(parent, name, label, color);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(160f, 120f);
            rect.anchoredPosition = new Vector2(xOffset, 0f);

            // Move existing label text down to leave room for an icon on top.
            var label0 = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label0 != null)
            {
                var labelRect = label0.rectTransform;
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(1f, 0f);
                labelRect.pivot = new Vector2(0.5f, 0f);
                labelRect.sizeDelta = new Vector2(0f, 36f);
                labelRect.anchoredPosition = new Vector2(0f, 6f);
                label0.fontSize = 24;
            }

            // Powerup icon
            if (sprites.hint != null)
            {
                var iconObj = NewUiImage(button.transform, "Icon", Color.white);
                var iconRect = (RectTransform)iconObj.transform;
                iconRect.anchorMin = new Vector2(0.5f, 1f);
                iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.pivot = new Vector2(0.5f, 1f);
                iconRect.sizeDelta = new Vector2(76f, 76f);
                iconRect.anchoredPosition = new Vector2(0f, -10f);
                var iconImage = iconObj.GetComponent<Image>();
                iconImage.sprite = sprites.hint;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            // Hint count badge (top-right corner of the button).
            var badge = NewUiImage(button.transform, "HintCount", new Color(0.95f, 0.32f, 0.28f, 1f));
            var badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.sizeDelta = new Vector2(48f, 48f);
            badgeRect.anchoredPosition = new Vector2(8f, 12f);

            countText = NewText(badge.transform, "Count", GemsSortInventory.GetHintCount(NameToHintType(name)).ToString(), 28, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            var countRect = countText.rectTransform;
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;

            return button;
        }

        private static GemsSortInventory.HintType NameToHintType(string name)
        {
            switch (name)
            {
                case "Wand": return GemsSortInventory.HintType.Wand;
                case "Mag": return GemsSortInventory.HintType.Magnet;
                case "Shelf": return GemsSortInventory.HintType.Shelf;
                default: return GemsSortInventory.HintType.Area;
            }
        }

        private static GameObject BuildWinNextLevelButton(Transform canvas, out TextMeshProUGUI winNextLevelButtonText)
        {
            var go = NewUiObject("WinNextLevelButton", canvas);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(420f, 130f);
            rect.anchoredPosition = new Vector2(0f, 220f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.45f, 0.76f, 0.09f, 1f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            winNextLevelButtonText = NewText(go.transform, "Label", "Next Level", 48, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            var textRect = winNextLevelButtonText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            go.SetActive(false); // starts disabled, enabled when level is won
            return go;
        }

        private static GameObject BuildTransitionPanel(Transform canvas, out TextMeshProUGUI transitionText, out CanvasGroup transitionCanvasGroup, out RectTransform transitionLogo, Sprite coinSprite)
        {
            var panel = NewUiObject("TransitionPanel", canvas);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bgImage = panel.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.1f, 0.18f, 1f); // Deep indigo background
            bgImage.raycastTarget = true; // block input during transitions

            transitionCanvasGroup = panel.AddComponent<CanvasGroup>();
            transitionCanvasGroup.alpha = 0f; // start hidden
            panel.SetActive(false);

            // Container for logo and text
            var containerGo = NewUiObject("Container", panel.transform);
            var containerRect = (RectTransform)containerGo.transform;
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(600f, 600f);
            containerRect.anchoredPosition = Vector2.zero;

            // Diamond Logo image
            var logoGo = NewUiObject("Logo", containerGo.transform);
            transitionLogo = (RectTransform)logoGo.transform;
            transitionLogo.anchorMin = new Vector2(0.5f, 0.6f);
            transitionLogo.anchorMax = new Vector2(0.5f, 0.6f);
            transitionLogo.pivot = new Vector2(0.5f, 0.5f);
            transitionLogo.sizeDelta = new Vector2(250f, 250f);
            transitionLogo.anchoredPosition = Vector2.zero;

            var logoImage = logoGo.AddComponent<Image>();
            logoImage.sprite = coinSprite; // The Diamond sprite!
            logoImage.preserveAspect = true;
            logoImage.color = Color.white;

            // Loading Text
            transitionText = NewText(containerGo.transform, "LoadingText", "LOADING...", 44, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            var textRect = transitionText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0.35f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return panel;
        }

        private static GameObject BuildPurchaseDialog(Transform canvas, HudSprites sprites)
        {
            var hostGo = new GameObject("Purchase Dialog", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(hostGo, "Build Purchase Dialog Host");
            hostGo.transform.SetParent(canvas, false);
            var hostRect = (RectTransform)hostGo.transform;
            hostRect.anchorMin = Vector2.zero;
            hostRect.anchorMax = Vector2.one;
            hostRect.offsetMin = Vector2.zero;
            hostRect.offsetMax = Vector2.zero;

            var dialog = hostGo.AddComponent<PurchaseDialog>();
            dialog.Build();

            return hostGo;
        }

        private static void TryAssignConfetti()
        {
            var effects = Object.FindObjectOfType<GemsSortEffects>();
            if (effects == null)
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { ConfettiPrefabPath });
            if (guids == null || guids.Length == 0)
            {
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (prefab == null)
            {
                return;
            }

            var so = new SerializedObject(effects);
            var prop = so.FindProperty("winCelebrationPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(effects);
            }
        }

        // ---- Primitive UI factories ----------------------------------------

        private static GameObject NewUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject NewUiImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return go;
        }

        private static Button NewButton(Transform parent, string name, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = color;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var text = NewText(go.transform, "Label", label, 30, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private static TextMeshProUGUI NewText(Transform parent, string name, string content, int size, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRef(SerializedObject so, string property, Object value)
        {
            var prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void SetSprite(SerializedObject so, string property, Sprite sprite)
        {
            var prop = so.FindProperty(property);
            if (prop != null && sprite != null)
            {
                prop.objectReferenceValue = sprite;
            }
        }

        private static Sprite LoadSpriteFromSheet(string path, string spriteName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }
            return null;
        }

        private struct HudSprites
        {
            public Sprite coin;
            public Sprite hint;
            public Sprite shine;
            public Sprite tile;
        }

        private static HudSprites LoadSprites()
        {
            return new HudSprites
            {
                coin = AssetDatabase.LoadAssetAtPath<Sprite>(GemSpritePath),
                hint = AssetDatabase.LoadAssetAtPath<Sprite>(CheckSpritePath),
                shine = AssetDatabase.LoadAssetAtPath<Sprite>(ShineSpritePath),
                tile = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath)
            };
        }

        private static GameObject EnsureLevelCardPrefab()
        {
            string prefabPath = "Assets/GemsSort/Prefabs/GemsSortLevelCard.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            System.IO.Directory.CreateDirectory("Assets/GemsSort/Prefabs");

            // Build temporary hierarchy
            var cardGo = new GameObject("GemsSortLevelCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(GemsSortLevelCard));
            var cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(280f, 360f);

            var cardImage = cardGo.GetComponent<Image>();
            cardImage.color = new Color(0.2f, 0.24f, 0.38f, 1f);

            var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(cardGo.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-20f, 50f);
            titleRect.anchoredPosition = new Vector2(0f, -10f);

            var titleText = titleGo.GetComponent<TextMeshProUGUI>();
            titleText.font = TMP_Settings.defaultFontAsset;
            titleText.text = "LEVEL 1";
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.raycastTarget = false;

            var previewGo = new GameObject("PreviewImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            previewGo.transform.SetParent(cardGo.transform, false);
            var previewRect = previewGo.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.5f, 0.5f);
            previewRect.anchorMax = new Vector2(0.5f, 0.5f);
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.sizeDelta = new Vector2(200f, 200f);
            previewRect.anchoredPosition = new Vector2(0f, 10f);

            var previewImage = previewGo.GetComponent<Image>();
            previewImage.preserveAspect = true;

            var btnGo = new GameObject("ActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(cardGo.transform, false);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0f);
            btnRect.anchorMax = new Vector2(0.5f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.sizeDelta = new Vector2(240f, 60f);
            btnRect.anchoredPosition = new Vector2(0f, 15f);

            var btnImage = btnGo.GetComponent<Image>();
            var btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = btnImage;

            var btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;

            var btnText = btnTextGo.GetComponent<TextMeshProUGUI>();
            btnText.font = TMP_Settings.defaultFontAsset;
            btnText.fontSize = 24;
            btnText.fontStyle = FontStyles.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.raycastTarget = false;

            var cardComp = cardGo.GetComponent<GemsSortLevelCard>();
            cardComp.TitleText = titleText;
            cardComp.PreviewImage = previewImage;
            cardComp.ActionButton = btn;
            cardComp.ActionButtonText = btnText;

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(cardGo, prefabPath);
            Object.DestroyImmediate(cardGo);

            return prefabAsset;
        }
    }
}
#endif
