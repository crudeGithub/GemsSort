using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GemsSort.Core;
using GemsSort.Services;
using TMPro;

namespace GemsSort.Game
{
    public sealed class GemsSortHud : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Button settingsButton;

        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button soundToggleButton;
        [SerializeField] private Button musicToggleButton;
        [SerializeField] private Button panelRestartButton;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button openGalleryButton;

        [Header("Gallery Panel")]
        [SerializeField] private GameObject galleryPanel;
        [SerializeField] private Button galleryBackButton;
        [SerializeField] private Transform galleryContentGrid;
        [SerializeField] private Sprite lockSprite;
        [SerializeField] private GameObject levelCardPrefab;



        [Header("Hints")]
        [SerializeField] private Button areaHintButton;
        [SerializeField] private Button wandHintButton;
        [SerializeField] private Button magnetHintButton;
        [SerializeField] private Button shelfHintButton;
        [Tooltip("Optional hint count badges. If empty the HUD auto-builds them at runtime.")]
        [SerializeField] private TextMeshProUGUI areaHintCountText;
        [SerializeField] private TextMeshProUGUI wandHintCountText;
        [SerializeField] private TextMeshProUGUI magnetHintCountText;
        [SerializeField] private TextMeshProUGUI shelfHintCountText;

        [Header("Coins / Purchase")]
        [Tooltip("Optional pre-built coin counter text. If empty the HUD auto-builds it.")]
        [SerializeField] private TextMeshProUGUI coinsText;
        [Tooltip("Optional pre-built coin icon used as the fly target. Auto-built if empty.")]
        [SerializeField] private RectTransform coinIcon;
        [Tooltip("Sprite for the coin icon when auto-building.")]
        [SerializeField] private Sprite coinSprite;
        [Tooltip("Universal purchase dialog. Auto-built if not assigned.")]
        [SerializeField] private PurchaseDialog purchaseDialog;

        [Header("Win Flow Roots")]
        [Tooltip("Optional root that holds gameplay HUD elements - hidden during the win sequence.")]
        [SerializeField] private GameObject topBarRoot;
        [SerializeField] private GameObject bottomBarRoot;

        [Header("Win Reward Animation")]
        [SerializeField] private int rewardCoinSpawnCount = 12;
        [SerializeField] private float rewardCoinSpawnSpread = 120f;
        [SerializeField] private float rewardCoinFlightDuration = 0.7f;
        [SerializeField] private float rewardCoinStagger = 0.04f;
        [SerializeField] private float rewardCoinSize = 70f;

        [Header("Audio")]
        [Tooltip("Audio service for coin collect sounds. Auto-found if empty.")]
        [SerializeField] private GemsSort.Services.GemsSortAudio audioService;

        [Header("Transition Screen")]
        [SerializeField] private GameObject transitionPanel;
        [SerializeField] private TextMeshProUGUI transitionText;
        [SerializeField] private CanvasGroup transitionCanvasGroup;
        [SerializeField] private RectTransform transitionLogo;

        [Header("Win Next Level")]
        [SerializeField] private GameObject winNextLevelButton;
        [SerializeField] private TextMeshProUGUI winNextLevelButtonText;

        private GemsSortGameController controller;
        private Canvas cachedCanvas;
        private RectTransform cachedCanvasRect;
        private GameObject coinHudRoot;
        private GameObject purchaseDialogRoot;
        private Coroutine coinBumpCoroutine;
        private Vector3 coinIconBaseScale = Vector3.one;
        private bool coinIconBaseScaleSaved;
        private readonly List<Texture2D> generatedTextures = new List<Texture2D>();

        public bool IsPanelActive => 
            (settingsPanel != null && settingsPanel.activeInHierarchy) ||
            (galleryPanel != null && galleryPanel.activeInHierarchy) ||
            (purchaseDialog != null && purchaseDialog.IsOpen);

        private void PlayClickSound()
        {
            if (audioService != null)
            {
                audioService.ButtonClick();
            }
            else if (GemsSortAudio.Instance != null)
            {
                GemsSortAudio.Instance.ButtonClick();
            }
        }

        private void OnSettingsClicked() { PlayClickSound(); OpenSettingsPanel(); }
        private void OnCloseSettingsClicked() { PlayClickSound(); CloseSettingsPanel(); }
        private void OnRestartClicked() { PlayClickSound(); RestartLevelFromPanel(); }
        private void OnToggleSoundClicked() { PlayClickSound(); ToggleSound(); }
        private void OnToggleMusicClicked() { PlayClickSound(); ToggleMusic(); }
        private void OnOpenGalleryClicked() { PlayClickSound(); OpenGalleryPanel(); }
        private void OnCloseGalleryClicked() { PlayClickSound(); CloseGalleryPanel(); }
        private void OnWinNextLevelClicked() { PlayClickSound(); GoToNextLevel(); }
        private void OnAreaHintButtonClicked() { PlayClickSound(); OnAreaHintClicked(); }
        private void OnWandHintButtonClicked() { PlayClickSound(); OnWandHintClicked(); }
        private void OnMagnetHintButtonClicked() { PlayClickSound(); OnMagnetHintClicked(); }
        private void OnShelfHintButtonClicked() { PlayClickSound(); OnShelfHintClicked(); }

        private void OnEnable()
        {
            GemsSortInventory.Changed += RefreshInventoryUi;
        }

        private void OnDisable()
        {
            GemsSortInventory.Changed -= RefreshInventoryUi;
        }

        public void Bind(GemsSortGameController gameController)
        {
            controller = gameController;
            EnsureRuntimeUi();

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (closeSettingsButton != null)
            {
                closeSettingsButton.onClick.RemoveListener(OnCloseSettingsClicked);
                closeSettingsButton.onClick.AddListener(OnCloseSettingsClicked);
            }

            if (panelRestartButton != null)
            {
                panelRestartButton.onClick.RemoveListener(OnRestartClicked);
                panelRestartButton.onClick.AddListener(OnRestartClicked);
            }

            if (soundToggleButton != null)
            {
                soundToggleButton.onClick.RemoveListener(OnToggleSoundClicked);
                soundToggleButton.onClick.AddListener(OnToggleSoundClicked);
            }

            if (musicToggleButton != null)
            {
                musicToggleButton.onClick.RemoveListener(OnToggleMusicClicked);
                musicToggleButton.onClick.AddListener(OnToggleMusicClicked);
            }

            if (openGalleryButton != null)
            {
                openGalleryButton.onClick.RemoveListener(OnOpenGalleryClicked);
                openGalleryButton.onClick.AddListener(OnOpenGalleryClicked);
            }

            if (galleryBackButton != null)
            {
                galleryBackButton.onClick.RemoveListener(OnCloseGalleryClicked);
                galleryBackButton.onClick.AddListener(OnCloseGalleryClicked);
            }

            if (winNextLevelButton != null)
            {
                var winBtnComp = winNextLevelButton.GetComponent<Button>();
                if (winBtnComp != null)
                {
                    winBtnComp.onClick.RemoveListener(OnWinNextLevelClicked);
                    winBtnComp.onClick.AddListener(OnWinNextLevelClicked);
                }
            }

            if (areaHintButton != null)
            {
                areaHintButton.onClick.RemoveListener(OnAreaHintButtonClicked);
                areaHintButton.onClick.AddListener(OnAreaHintButtonClicked);
            }

            if (wandHintButton != null)
            {
                wandHintButton.onClick.RemoveListener(OnWandHintButtonClicked);
                wandHintButton.onClick.AddListener(OnWandHintButtonClicked);
            }

            if (magnetHintButton != null)
            {
                magnetHintButton.onClick.RemoveListener(OnMagnetHintButtonClicked);
                magnetHintButton.onClick.AddListener(OnMagnetHintButtonClicked);
            }

            if (shelfHintButton != null)
            {
                shelfHintButton.onClick.RemoveListener(OnShelfHintButtonClicked);
                shelfHintButton.onClick.AddListener(OnShelfHintButtonClicked);
            }

            if (audioService == null)
            {
                audioService = Object.FindObjectOfType<GemsSort.Services.GemsSortAudio>();
            }

            RefreshInventoryUi();
        }

        private void Start()
        {
            if (transitionLogo != null)
            {
                StartCoroutine(PulseLogoRoutine(transitionLogo));
            }
        }

        public void SetLevel(int levelNumber, int levelCount)
        {
            if (levelText != null)
            {
                levelText.text = "Level " + levelNumber;
            }

            ShowGameplayUi(true);
            HideWinNextButton();
            RefreshInventoryUi();
        }

        public void HideGameplayUiForWin()
        {
            ShowGameplayUi(false);
        }

        public void ShowWinNextButton(bool lastLevel)
        {
            if (winNextLevelButton != null)
            {
                winNextLevelButton.SetActive(true);
                winNextLevelButton.transform.SetAsLastSibling();
            }

            if (winNextLevelButtonText != null)
            {
                winNextLevelButtonText.text = lastLevel ? "Level 1" : "Next Level";
            }
        }

        public void HideWinNextButton()
        {
            if (winNextLevelButton != null)
            {
                winNextLevelButton.SetActive(false);
            }
        }

        public IEnumerator PlayCoinRewardAnimation(int reward)
        {
            EnsureRuntimeUi();

            if (coinIcon == null || cachedCanvasRect == null)
            {
                GemsSortInventory.AddCoins(reward);
                yield break;
            }

            // Wait a frame so layout is current.
            yield return null;

            // Screen center in screen pixels.
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            
            // Coin icon center in screen pixels.
            Vector3[] corners = new Vector3[4];
            coinIcon.GetWorldCorners(corners);
            Vector3 iconCenter = (corners[0] + corners[2]) * 0.5f;

            // Convert target and spawn locations to canvas local space.
            Vector3 localTarget = cachedCanvasRect.InverseTransformPoint(iconCenter);
            
            Vector2 localSpawnCenter;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                cachedCanvasRect, 
                screenCenter, 
                cachedCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cachedCanvas.worldCamera, 
                out localSpawnCenter);

            int count = Mathf.Max(1, rewardCoinSpawnCount);
            int coinsPerStep = Mathf.Max(1, Mathf.RoundToInt(reward / (float)count));
            int distributed = 0;

            // Spread is now directly in Canvas coordinates (no scale factor division needed).
            float spread = rewardCoinSpawnSpread;

            var flyingCoins = new List<CoinFlyData>();

            for (int i = 0; i < count; i++)
            {
                int award = (i == count - 1) ? reward - distributed : coinsPerStep;
                distributed += award;

                // Random direction for the burst.
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                // Random distance from 0.4 to 1.1 of the spread.
                float burstDist = Random.Range(spread * 0.4f, spread * 1.1f);
                Vector3 burstPos = (Vector3)localSpawnCenter + new Vector3(randomDirection.x * burstDist, randomDirection.y * burstDist, 0f);

                // Create coin at local spawn center.
                var coinRect = CreateCoinImage((Vector3)localSpawnCenter);
                coinRect.gameObject.SetActive(false); // Hide until its spawn delay

                // Direction outward from the center.
                Vector3 outwardDir = new Vector3(randomDirection.x, randomDirection.y, 0f);

                flyingCoins.Add(new CoinFlyData
                {
                    rect = coinRect,
                    startPos = (Vector3)localSpawnCenter,
                    burstPos = burstPos,
                    endPos = localTarget,
                    outwardDir = outwardDir,
                    reward = award,
                    spawnDelay = i * 0.015f, // Fast stagger for spawning/bursting
                    burstDuration = 0.35f,
                    holdDuration = 0.05f + i * rewardCoinStagger, // Staggered hold for a nice stream
                    flightDuration = rewardCoinFlightDuration + Random.Range(-0.05f, 0.05f),
                    curveAmount = Random.Range(spread * 0.6f, spread * 1.3f),
                    spinSpeed = Random.Range(-360f, 360f),
                    initialRotation = Random.Range(0f, 360f)
                });
            }

            // Animate all coins.
            float totalTime = 0f;
            foreach (var data in flyingCoins)
            {
                float coinTotalTime = data.spawnDelay + data.burstDuration + data.holdDuration + data.flightDuration;
                if (coinTotalTime > totalTime)
                {
                    totalTime = coinTotalTime;
                }
            }
            totalTime += 0.1f; // Small safety padding
            float elapsed = 0f;

            while (elapsed < totalTime)
            {
                elapsed += Time.deltaTime;
                bool allDone = true;

                for (int i = 0; i < flyingCoins.Count; i++)
                {
                    var data = flyingCoins[i];
                    if (data.rect == null) continue;

                    if (elapsed < data.spawnDelay)
                    {
                        allDone = false;
                        continue;
                    }

                    if (!data.rect.gameObject.activeSelf)
                    {
                        data.rect.gameObject.SetActive(true);
                    }

                    allDone = false;
                    float timeSinceSpawn = elapsed - data.spawnDelay;

                    if (timeSinceSpawn < data.burstDuration)
                    {
                        // Phase 1: Burst Out
                        float t = timeSinceSpawn / data.burstDuration;
                        float smoothT = 1f - (1f - t) * (1f - t); // Ease Out Quad
                        
                        data.rect.localPosition = Vector3.Lerp(data.startPos, data.burstPos, smoothT);
                        data.rect.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f, smoothT);
                        data.rect.localRotation = Quaternion.Euler(0f, 0f, data.initialRotation + elapsed * data.spinSpeed);
                    }
                    else if (timeSinceSpawn < data.burstDuration + data.holdDuration)
                    {
                        // Phase 2: Hold / Hover
                        data.rect.localPosition = data.burstPos;
                        data.rect.localScale = Vector3.one * 1.2f;
                        data.rect.localRotation = Quaternion.Euler(0f, 0f, data.initialRotation + elapsed * data.spinSpeed);
                    }
                    else if (timeSinceSpawn < data.burstDuration + data.holdDuration + data.flightDuration)
                    {
                        // Phase 3: Fly to target (magnetic curved path)
                        float flightElapsed = timeSinceSpawn - data.burstDuration - data.holdDuration;
                        float t = flightElapsed / data.flightDuration;
                        float smoothT = t * t; // Ease In Quad

                        Vector3 control = (data.burstPos + data.endPos) * 0.5f + data.outwardDir * data.curveAmount;
                        
                        // Quadratic bezier
                        float inv = 1f - smoothT;
                        Vector3 pos = inv * inv * data.burstPos
                            + 2f * inv * smoothT * control
                            + smoothT * smoothT * data.endPos;

                        data.rect.localPosition = pos;
                        data.rect.localScale = Vector3.one * 1.2f;
                        data.rect.localRotation = Quaternion.Euler(0f, 0f, data.initialRotation + elapsed * (data.spinSpeed * 1.5f));
                    }
                    else
                    {
                        // Landed
                        data.rect.localPosition = data.endPos;
                        if (data.reward > 0)
                        {
                            GemsSortInventory.AddCoins(data.reward);
                            if (audioService != null) audioService.CoinCollect();
                            if (coinBumpCoroutine != null) StopCoroutine(coinBumpCoroutine);
                            coinBumpCoroutine = StartCoroutine(BumpCoinIcon());
                            data.reward = 0;
                        }
                        Destroy(data.rect.gameObject);
                        data.rect = null;
                        flyingCoins[i] = data;
                    }
                }

                if (allDone) break;
                yield return null;
            }

            // Cleanup any stragglers.
            for (int i = 0; i < flyingCoins.Count; i++)
            {
                var data = flyingCoins[i];
                if (data.rect != null)
                {
                    if (data.reward > 0)
                    {
                        GemsSortInventory.AddCoins(data.reward);
                        if (audioService != null) audioService.CoinCollect();
                    }
                    Destroy(data.rect.gameObject);
                }
            }
        }

        private struct CoinFlyData
        {
            public RectTransform rect;
            public Vector3 startPos;
            public Vector3 burstPos;
            public Vector3 endPos;
            public Vector3 outwardDir;
            public int reward;
            public float spawnDelay;
            public float burstDuration;
            public float holdDuration;
            public float flightDuration;
            public float curveAmount;
            public float spinSpeed;
            public float initialRotation;
        }

        private RectTransform CreateCoinImage(Vector3 localPosition)
        {
            var obj = new GameObject("RewardCoin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(cachedCanvasRect, false);
            obj.transform.SetAsLastSibling();

            var rect = (RectTransform)obj.transform;
            rect.sizeDelta = new Vector2(rewardCoinSize, rewardCoinSize);
            rect.localPosition = localPosition;

            var image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            if (coinSprite != null)
            {
                image.sprite = coinSprite;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(1f, 0.84f, 0f, 1f); // Gold fallback.
            }

            return rect;
        }

        private IEnumerator BumpCoinIcon()
        {
            if (coinIcon == null) yield break;
            if (!coinIconBaseScaleSaved)
            {
                coinIconBaseScale = coinIcon.localScale;
                coinIconBaseScaleSaved = true;
            }
            coinIcon.localScale = coinIconBaseScale;

            float duration = 0.12f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (coinIcon == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                coinIcon.localScale = coinIconBaseScale * (1f + t * 0.3f);
                yield return null;
            }
            coinIcon.localScale = coinIconBaseScale;
        }

        private readonly List<GameObject> hiddenForWin = new List<GameObject>();

        private void ShowGameplayUi(bool visible)
        {
            if (topBarRoot != null) topBarRoot.SetActive(visible);
            if (bottomBarRoot != null) bottomBarRoot.SetActive(visible);

            if (visible)
            {
                foreach (var go in hiddenForWin)
                {
                    if (go != null) go.SetActive(true);
                }
                hiddenForWin.Clear();
                return;
            }

            // Auto-hide any other HUD children directly under the canvas, except
            // anything created for the win sequence (flying coins, next button,
            // purchase dialog).
            if (cachedCanvasRect == null)
            {
                return;
            }

            for (int i = 0; i < cachedCanvasRect.childCount; i++)
            {
                var child = cachedCanvasRect.GetChild(i).gameObject;
                if (!child.activeSelf) continue;
                if (child == winNextLevelButton) continue;
                if (child == coinHudRoot) continue;
                if (child == purchaseDialogRoot) continue;
                if (purchaseDialog != null && child.transform == purchaseDialog.transform) continue;
                if (child.name == "PurchaseDialogHost") continue;
                if (child.name == "Purchase Dialog") continue;
                if (child.name == "RewardCoin") continue;
                if (child.name == "CoinHud") continue;
                if (child.name == "Coin Counter") continue;
                if (coinIcon != null && coinIcon.IsChildOf(child.transform)) continue;
                if (coinsText != null && coinsText.transform.IsChildOf(child.transform)) continue;

                child.SetActive(false);
                hiddenForWin.Add(child);
            }
        }

        private void RefreshInventoryUi()
        {
            if (coinsText != null)
            {
                coinsText.text = GemsSortInventory.Coins.ToString();
            }

            UpdateHintBadge(areaHintCountText, GemsSortInventory.HintType.Area);
            UpdateHintBadge(wandHintCountText, GemsSortInventory.HintType.Wand);
            UpdateHintBadge(magnetHintCountText, GemsSortInventory.HintType.Magnet);
            UpdateHintBadge(shelfHintCountText, GemsSortInventory.HintType.Shelf);

            if (purchaseDialog != null && purchaseDialog.IsOpen)
            {
                purchaseDialog.RefreshState();
            }
        }

        private static void UpdateHintBadge(TextMeshProUGUI label, GemsSortInventory.HintType type)
        {
            if (label != null)
            {
                label.text = GemsSortInventory.GetHintCount(type).ToString();
            }
        }

        private void OnAreaHintClicked()
        {
            HandleHintButton(GemsSortInventory.HintType.Area, () => controller != null ? controller.TryActivateAreaHint() : false);
        }

        private void OnWandHintClicked()
        {
            HandleHintButton(GemsSortInventory.HintType.Wand, () => controller != null ? controller.TryActivateWandHint() : false);
        }

        private void OnMagnetHintClicked()
        {
            HandleHintButton(GemsSortInventory.HintType.Magnet, () => controller != null ? controller.TryActivateMagnetHint() : false);
        }

        private void OnShelfHintClicked()
        {
            HandleHintButton(GemsSortInventory.HintType.Shelf, () => controller != null ? controller.TryActivateShelfHint() : false);
        }

        private void HandleHintButton(GemsSortInventory.HintType type, System.Func<bool> activate)
        {
            if (GemsSortInventory.GetHintCount(type) <= 0)
            {
                OpenPurchaseDialog(type);
                return;
            }

            if (activate != null && activate())
            {
                GemsSortInventory.TrySpendHint(type);
            }
        }

        private Sprite GetHintButtonIcon(GemsSortInventory.HintType type)
        {
            Button btn = null;
            switch (type)
            {
                case GemsSortInventory.HintType.Area: btn = areaHintButton; break;
                case GemsSortInventory.HintType.Wand: btn = wandHintButton; break;
                case GemsSortInventory.HintType.Magnet: btn = magnetHintButton; break;
                case GemsSortInventory.HintType.Shelf: btn = shelfHintButton; break;
            }

            if (btn != null)
            {
                Transform iconTrans = btn.transform.Find("Icon");
                if (iconTrans != null)
                {
                    var img = iconTrans.GetComponent<Image>();
                    if (img != null) return img.sprite;
                }
                
                foreach (var img in btn.GetComponentsInChildren<Image>())
                {
                    if (img.gameObject != btn.gameObject && img.sprite != null)
                    {
                        return img.sprite;
                    }
                }
            }

            return null;
        }

        private void OpenPurchaseDialog(GemsSortInventory.HintType type)
        {
            EnsureRuntimeUi();
            if (purchaseDialog == null)
            {
                return;
            }

            Sprite iconSprite = GetHintButtonIcon(type);
            purchaseDialog.Open(type, iconSprite, () =>
            {
                GemsSortInventory.TryPurchaseHintPack(type);
            });
        }

        private void RestartLevel()
        {
            if (controller != null)
            {
                controller.RestartLevel();
            }
        }

        private void GoToNextLevel()
        {
            if (controller != null)
            {
                controller.LoadNextLevel();
            }
        }

        private void OpenSettingsPanel()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                settingsPanel.transform.SetAsLastSibling();
                RefreshSettingsToggles();
            }
        }

        private void CloseSettingsPanel()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void OpenGalleryPanel()
        {
            CloseSettingsPanel();
            if (galleryPanel != null)
            {
                galleryPanel.SetActive(true);
                galleryPanel.transform.SetAsLastSibling();
                PopulateGallery();
            }
        }

        private void CloseGalleryPanel()
        {
            if (galleryPanel != null)
            {
                galleryPanel.SetActive(false);
            }
            OpenSettingsPanel();
        }

        private void RestartLevelFromPanel()
        {
            CloseSettingsPanel();
            RestartLevel();
        }

        private void ToggleSound()
        {
            if (audioService != null)
            {
                audioService.SoundEnabled = !audioService.SoundEnabled;
                RefreshSettingsToggles();
            }
        }

        private void ToggleMusic()
        {
            if (audioService != null)
            {
                audioService.MusicEnabled = !audioService.MusicEnabled;
                RefreshSettingsToggles();
            }
        }

        private void RefreshSettingsToggles()
        {
            if (audioService == null) return;

            if (soundToggleButton != null)
            {
                bool sEnabled = audioService.SoundEnabled;
                var textObj = soundToggleButton.GetComponentInChildren<TextMeshProUGUI>();
                if (textObj != null)
                {
                    textObj.text = "SOUND: " + (sEnabled ? "ON" : "OFF");
                }
                var imgObj = soundToggleButton.GetComponent<Image>();
                if (imgObj != null)
                {
                    imgObj.color = sEnabled ? new Color(0.27f, 0.78f, 0.46f, 1f) : new Color(0.95f, 0.32f, 0.28f, 1f);
                }
            }

            if (musicToggleButton != null)
            {
                bool mEnabled = audioService.MusicEnabled;
                var textObj = musicToggleButton.GetComponentInChildren<TextMeshProUGUI>();
                if (textObj != null)
                {
                    textObj.text = "MUSIC: " + (mEnabled ? "ON" : "OFF");
                }
                var imgObj = musicToggleButton.GetComponent<Image>();
                if (imgObj != null)
                {
                    imgObj.color = mEnabled ? new Color(0.27f, 0.78f, 0.46f, 1f) : new Color(0.95f, 0.32f, 0.28f, 1f);
                }
            }
        }

        private Sprite CreateSolvedLevelPreview(LevelDefinition level)
        {
            if (level == null) return null;
            int w = level.Width;
            int h = level.Height;
            if (w <= 0 || h <= 0) return null;

            // Target texture size around 256x256 pixels for sharpness
            int blockSize = Mathf.Max(1, 256 / Mathf.Max(w, h));
            int texW = w * blockSize;
            int texH = h * blockSize;

            Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color bg = new Color(0.12f, 0.15f, 0.25f, 1f);

            for (int r = 0; r < h; r++)
            {
                for (int c = 0; c < w; c++)
                {
                    int colorCode = level.Target != null ? level.Target[r, c] : GemColorCode.Blank;
                    Color col = (colorCode == GemColorCode.Blank) ? bg : GemColorCode.Resolve(colorCode, level);

                    // Fill a block of size blockSize x blockSize
                    for (int y = 0; y < blockSize; y++)
                    {
                        for (int x = 0; x < blockSize; x++)
                        {
                            int px = c * blockSize + x;
                            int py = (h - 1 - r) * blockSize + y;
                            tex.SetPixel(px, py, col);
                        }
                    }
                }
            }
            tex.Apply();

            generatedTextures.Add(tex);
            return Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), 100f);
        }

        private void PopulateGallery()
        {
            if (galleryContentGrid == null || controller == null) return;

            // Clear old generated textures to prevent memory leaks
            foreach (var tex in generatedTextures)
            {
                if (tex != null) Destroy(tex);
            }
            generatedTextures.Clear();

            // Clear old buttons
            foreach (Transform child in galleryContentGrid)
            {
                Destroy(child.gameObject);
            }

            int maxSolved = PlayerPrefs.GetInt("GemsSort.MaxSolvedLevel", -1);
            var levels = controller.Levels;
            if (levels == null) return;

            for (int i = 0; i < levels.Length; i++)
            {
                int levelIdx = i;
                bool isSolved = levelIdx <= maxSolved;
                bool isUnlocked = levelIdx <= maxSolved + 1;
                bool isCurrent = levelIdx == controller.CurrentLevelIndex;

                if (levelCardPrefab != null)
                {
                    var cardGo = Instantiate(levelCardPrefab, galleryContentGrid, false);
                    var card = cardGo.GetComponent<GemsSortLevelCard>();
                    if (card != null)
                    {
                        if (card.TitleText != null)
                        {
                            card.TitleText.text = "LEVEL " + (levelIdx + 1);
                        }

                        if (card.PreviewImage != null)
                        {
                            if (isSolved)
                            {
                                card.PreviewImage.sprite = CreateSolvedLevelPreview(levels[levelIdx]);
                                card.PreviewImage.color = Color.white;
                            }
                            else
                            {
                                card.PreviewImage.sprite = lockSprite;
                                card.PreviewImage.color = isUnlocked ? new Color(1f, 1f, 1f, 0.7f) : new Color(1f, 1f, 1f, 0.3f);
                            }
                        }

                        if (card.ActionButton != null)
                        {
                            var cardBtnImage = card.ActionButton.GetComponent<Image>();
                            card.ActionButton.onClick.RemoveAllListeners();

                            if (!isUnlocked)
                            {
                                if (cardBtnImage != null) cardBtnImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
                                if (card.ActionButtonText != null)
                                {
                                    card.ActionButtonText.text = "LOCKED";
                                    card.ActionButtonText.color = new Color(1f, 1f, 1f, 0.35f);
                                }
                                card.ActionButton.interactable = false;
                            }
                            else
                            {
                                card.ActionButton.interactable = true;
                                card.ActionButton.onClick.AddListener(() =>
                                {
                                    PlayClickSound();
                                    controller.LoadLevel(levelIdx);
                                    if (galleryPanel != null) galleryPanel.SetActive(false);
                                });

                                if (isCurrent)
                                {
                                    if (cardBtnImage != null) cardBtnImage.color = new Color(0.95f, 0.65f, 0.08f, 1f);
                                    if (card.ActionButtonText != null)
                                    {
                                        card.ActionButtonText.text = "PLAY";
                                        card.ActionButtonText.color = Color.white;
                                    }
                                }
                                else if (isSolved)
                                {
                                    if (cardBtnImage != null) cardBtnImage.color = new Color(0.27f, 0.78f, 0.46f, 1f);
                                    if (card.ActionButtonText != null)
                                    {
                                        card.ActionButtonText.text = "REPLAY";
                                        card.ActionButtonText.color = Color.white;
                                    }
                                }
                                else
                                {
                                    if (cardBtnImage != null) cardBtnImage.color = new Color(0.36f, 0.62f, 0.95f, 1f);
                                    if (card.ActionButtonText != null)
                                    {
                                        card.ActionButtonText.text = "PLAY";
                                        card.ActionButtonText.color = Color.white;
                                    }
                                }
                            }
                        }
                        continue;
                    }
                }

                // Fallback to dynamic creation
                // Card Container (vertical layout)
                var fallbackCardGo = new GameObject("LevelCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fallbackCardGo.transform.SetParent(galleryContentGrid, false);
                var cardRect = fallbackCardGo.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(280f, 360f);

                var cardImage = fallbackCardGo.GetComponent<Image>();
                cardImage.color = new Color(0.2f, 0.24f, 0.38f, 1f); // Slightly lighter card bg

                // Level text title (top centered)
                var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleGo.transform.SetParent(fallbackCardGo.transform, false);
                var titleRect = titleGo.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.sizeDelta = new Vector2(-20f, 50f);
                titleRect.anchoredPosition = new Vector2(0f, -10f);

                var titleText = titleGo.GetComponent<TextMeshProUGUI>();
                titleText.font = TMP_Settings.defaultFontAsset;
                titleText.text = "LEVEL " + (levelIdx + 1);
                titleText.fontSize = 28;
                titleText.fontStyle = FontStyles.Bold;
                titleText.color = Color.white;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.raycastTarget = false;

                // Image preview box (centered)
                var previewGo = new GameObject("PreviewImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                previewGo.transform.SetParent(fallbackCardGo.transform, false);
                var previewRect = previewGo.GetComponent<RectTransform>();
                previewRect.anchorMin = new Vector2(0.5f, 0.5f);
                previewRect.anchorMax = new Vector2(0.5f, 0.5f);
                previewRect.pivot = new Vector2(0.5f, 0.5f);
                previewRect.sizeDelta = new Vector2(200f, 200f);
                previewRect.anchoredPosition = new Vector2(0f, 10f);

                var previewImage = previewGo.GetComponent<Image>();
                previewImage.preserveAspect = true;

                if (isSolved)
                {
                    previewImage.sprite = CreateSolvedLevelPreview(levels[levelIdx]);
                    previewImage.color = Color.white;
                }
                else
                {
                    previewImage.sprite = lockSprite;
                    previewImage.color = isUnlocked ? new Color(1f, 1f, 1f, 0.7f) : new Color(1f, 1f, 1f, 0.3f);
                }

                // Play/Replay button (bottom centered)
                var btnGo = new GameObject("ActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(fallbackCardGo.transform, false);
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

                if (!isUnlocked)
                {
                    btnImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
                    btnText.text = "LOCKED";
                    btnText.color = new Color(1f, 1f, 1f, 0.35f);
                    btn.interactable = false;
                }
                else
                {
                    btn.interactable = true;
                    btn.onClick.AddListener(() =>
                    {
                        PlayClickSound();
                        controller.LoadLevel(levelIdx);
                        if (galleryPanel != null) galleryPanel.SetActive(false);
                    });

                    if (isCurrent)
                    {
                        btnImage.color = new Color(0.95f, 0.65f, 0.08f, 1f);
                        btnText.text = "PLAY";
                        btnText.color = Color.white;
                    }
                    else if (isSolved)
                    {
                        btnImage.color = new Color(0.27f, 0.78f, 0.46f, 1f);
                        btnText.text = "REPLAY";
                        btnText.color = Color.white;
                    }
                    else
                    {
                        btnImage.color = new Color(0.36f, 0.62f, 0.95f, 1f);
                        btnText.text = "PLAY";
                        btnText.color = Color.white;
                    }
                }
            }
        }

        // ---- Runtime UI auto-build ----

        private void EnsureRuntimeUi()
        {
            if (cachedCanvas == null)
            {
                cachedCanvas = GetComponentInParent<Canvas>();
                if (cachedCanvas != null)
                {
                    cachedCanvasRect = cachedCanvas.transform as RectTransform;
                }
            }

            if (cachedCanvasRect == null)
            {
                return;
            }

            if (purchaseDialog == null)
            {
                BuildPurchaseDialog();
            }

            if (coinsText == null || coinIcon == null)
            {
                BuildCoinHud();
            }

            if (areaHintCountText == null && areaHintButton != null)
            {
                areaHintCountText = BuildHintBadge(areaHintButton.transform);
            }
            if (wandHintCountText == null && wandHintButton != null)
            {
                wandHintCountText = BuildHintBadge(wandHintButton.transform);
            }
            if (magnetHintCountText == null && magnetHintButton != null)
            {
                magnetHintCountText = BuildHintBadge(magnetHintButton.transform);
            }
            if (shelfHintCountText == null && shelfHintButton != null)
            {
                shelfHintCountText = BuildHintBadge(shelfHintButton.transform);
            }
        }

        private IEnumerator PulseLogoRoutine(RectTransform logoRect)
        {
            while (true)
            {
                if (logoRect == null) yield break;
                float scale = 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
                logoRect.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
        }

        public void ShowFirstLoadTransition(int levelNumber)
        {
            EnsureRuntimeUi();
            if (transitionText != null)
            {
                transitionText.text = "LOADING LEVEL " + levelNumber + "...";
            }
            if (transitionCanvasGroup != null)
            {
                transitionCanvasGroup.alpha = 1f;
            }
            if (transitionPanel != null)
            {
                transitionPanel.SetActive(true);
                transitionPanel.transform.SetAsLastSibling();
            }
        }

        public IEnumerator FadeInTransition(int levelNumber)
        {
            EnsureRuntimeUi();

            if (transitionText != null)
            {
                transitionText.text = "LOADING LEVEL " + levelNumber + "...";
            }

            if (transitionPanel != null)
            {
                transitionPanel.SetActive(true);
                transitionPanel.transform.SetAsLastSibling();
            }

            float duration = 0.3f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (transitionCanvasGroup != null)
                {
                    transitionCanvasGroup.alpha = t;
                }
                yield return null;
            }
            if (transitionCanvasGroup != null)
            {
                transitionCanvasGroup.alpha = 1f;
            }
        }

        public IEnumerator FadeOutTransition()
        {
            if (transitionPanel == null || !transitionPanel.activeSelf) yield break;

            float duration = 0.3f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (transitionCanvasGroup != null)
                {
                    transitionCanvasGroup.alpha = 1f - t;
                }
                yield return null;
            }
            if (transitionCanvasGroup != null)
            {
                transitionCanvasGroup.alpha = 0f;
            }
            if (transitionPanel != null)
            {
                transitionPanel.SetActive(false);
            }
        }

        private void BuildPurchaseDialog()
        {
            var go = new GameObject("PurchaseDialogHost", typeof(RectTransform));
            go.transform.SetParent(cachedCanvasRect, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            purchaseDialog = go.AddComponent<PurchaseDialog>();
            purchaseDialogRoot = go;
            purchaseDialog.Build();
        }

        private void BuildCoinHud()
        {
            var coinObj = new GameObject("CoinHud", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            coinObj.transform.SetParent(cachedCanvasRect, false);
            coinHudRoot = coinObj;
            var rect = (RectTransform)coinObj.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(220f, 80f);
            rect.anchoredPosition = new Vector2(40f, -40f);

            var bg = coinObj.GetComponent<Image>();
            bg.color = new Color(0.18f, 0.24f, 0.42f, 0.85f);
            bg.raycastTarget = false;

            // Icon
            var iconGo = new GameObject("CoinIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(coinObj.transform, false);
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(60f, 60f);
            iconRect.anchoredPosition = new Vector2(10f, 0f);
            var iconImage = iconGo.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            if (coinSprite != null)
            {
                iconImage.sprite = coinSprite;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.color = new Color(0.27f, 0.78f, 0.46f, 1f);
            }
            coinIcon = iconRect;

            // Text
            var textGo = new GameObject("CoinText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(coinObj.transform, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = new Vector2(78f, 4f);
            textRect.offsetMax = new Vector2(-12f, -4f);

            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 38;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            text.raycastTarget = false;
            text.text = GemsSortInventory.Coins.ToString();
            coinsText = text;
        }

        private TextMeshProUGUI BuildHintBadge(Transform buttonTransform)
        {
            var go = new GameObject("HintCount", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(buttonTransform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(46f, 46f);
            rect.anchoredPosition = new Vector2(8f, 12f);

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.95f, 0.32f, 0.28f, 1f);
            bg.raycastTarget = false;

            var textGo = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 28;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = "3";
            return text;
        }


    }
}
