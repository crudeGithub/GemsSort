using System;
using UnityEngine;
using UnityEngine.UI;
using GemsSort.Services;
using TMPro;

namespace GemsSort.Game
{
    /// <summary>
    /// Universal purchase dialog used for all hint types. The same panel is reused;
    /// the title, body and icon swap based on which hint button opened it.
    /// </summary>
    public sealed class PurchaseDialog : MonoBehaviour
    {
        [Header("Optional Authoring References")]
        [Tooltip("Optional pre-built panel root. If empty the dialog auto-builds itself.")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button closeButton;

        [Header("Auto Build")]
        [Tooltip("Sprite used for the powerup icon when auto-building. Diamond is fine as a fallback.")]
        [SerializeField] private Sprite hintIconSprite;
        [SerializeField] private Sprite coinIconSprite;
        [Tooltip("Optional radiant background sprite that sits behind the icon (e.g. a sun-burst).")]
        [SerializeField] private Sprite iconBackdropSprite;
        [Tooltip("Solid background color for the dialog body.")]
        [SerializeField] private Color panelColor = new Color(0.36f, 0.49f, 0.78f, 1f);
        [SerializeField] private Color headerColor = new Color(0.55f, 0.7f, 0.95f, 1f);
        [SerializeField] private Color buyButtonColor = new Color(0.55f, 0.7f, 0.95f, 1f);
        [SerializeField] private Color closeButtonColor = new Color(0.95f, 0.32f, 0.28f, 1f);

        private Action onBuy;
        private bool autoBuilt;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void Build()
        {
            EnsureBuilt();
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void Open(GemsSortInventory.HintType hintType, Sprite iconSprite, Action onPurchase)
        {
            EnsureBuilt();
            onBuy = onPurchase;

            if (headerText != null)
            {
                headerText.text = "BUY?";
            }

            if (titleText != null)
            {
                titleText.text = TitleFor(hintType);
            }

            if (descriptionText != null)
            {
                descriptionText.text = DescriptionFor(hintType);
            }

            if (costText != null)
            {
                costText.text = GemsSortInventory.HintPackCost.ToString();
            }

            if (iconImage != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.color = iconSprite != null ? Color.white : new Color(1f, 0.85f, 0.18f, 1f);
            }

            UpdateBuyInteractable();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                panelRoot.transform.SetAsLastSibling();
            }
        }

        public void Close()
        {
            CloseInternal(true);
        }

        private void CloseInternal(bool playClickSound)
        {
            if (playClickSound && GemsSortAudio.Instance != null)
            {
                GemsSortAudio.Instance.ButtonClick();
            }
            onBuy = null;
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void RefreshState()
        {
            UpdateBuyInteractable();
        }

        private void UpdateBuyInteractable()
        {
            if (buyButton != null)
            {
                buyButton.interactable = GemsSortInventory.CanPurchaseHints();
            }
        }

        private static string TitleFor(GemsSortInventory.HintType hintType)
        {
            switch (hintType)
            {
                case GemsSortInventory.HintType.Area: return "GET AREA HINTS";
                case GemsSortInventory.HintType.Wand: return "GET WAND HINTS";
                case GemsSortInventory.HintType.Magnet: return "GET MAGNET HINTS";
                case GemsSortInventory.HintType.Shelf: return "GET SHELF HINTS";
                default: return "GET HINTS";
            }
        }

        private static string DescriptionFor(GemsSortInventory.HintType hintType)
        {
            switch (hintType)
            {
                case GemsSortInventory.HintType.Area:
                    return "AREA HINT SOLVES EVERY GEM INSIDE A 10x10 BOX YOU CHOOSE.\nGET 3 USES";
                case GemsSortInventory.HintType.Wand:
                    return "WAND HINT PLACES SHELF GEMS BACK ONTO MATCHING TARGETS.\nGET 3 USES";
                case GemsSortInventory.HintType.Magnet:
                    return "MAGNET HINT PULLS THE NEXT 10-12 GEMS TO THEIR TARGET CELLS.\nGET 3 USES";
                case GemsSortInventory.HintType.Shelf:
                    return "SHELF HINT ADDS AN EXTRA ROW TO YOUR SHELF (MAX 4 ROWS).\nGET 3 USES";
                default:
                    return "CHOOSE HOW TO\nGET 3 HINT POWERUPS";
            }
        }

        private void EnsureBuilt()
        {
            if (panelRoot != null)
            {
                if (buyButton != null)
                {
                    buyButton.onClick.RemoveAllListeners();
                    buyButton.onClick.AddListener(HandleBuy);
                }
                if (closeButton != null)
                {
                    closeButton.onClick.RemoveAllListeners();
                    closeButton.onClick.AddListener(Close);
                }
                return;
            }

            BuildAtRuntime();
        }

        private void BuildAtRuntime()
        {
            if (autoBuilt)
            {
                return;
            }

            autoBuilt = true;

            // Root - full screen overlay
            panelRoot = new GameObject("PurchaseDialog", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelRoot.transform.SetParent(transform, false);

            var rootRect = panelRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.localScale = Vector3.one;

            var dim = panelRoot.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            dim.raycastTarget = true;

            // Card
            var card = CreateChild(panelRoot.transform, "Card", panelColor);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(720f, 980f);
            cardRect.anchoredPosition = Vector2.zero;
            card.GetComponent<Image>().raycastTarget = true;

            // Header chip
            var header = CreateChild(card.transform, "Header", headerColor);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(360f, 100f);
            headerRect.anchoredPosition = new Vector2(0f, 60f);

            headerText = CreateText(header.transform, "BUY?", 56, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            StretchToParent(headerText.rectTransform, 24f);

            // Decorative X marks on either side of the header chip.
            CreateHeaderChipMark(header.transform, true);
            CreateHeaderChipMark(header.transform, false);

            // Title
            titleText = CreateText(card.transform, "GET HINTS", 64, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-40f, 120f);
            titleRect.anchoredPosition = new Vector2(0f, -110f);

            // Icon backdrop (radial rays behind the icon).
            if (iconBackdropSprite != null)
            {
                var backdrop = CreateChild(card.transform, "IconBackdrop", new Color(1f, 1f, 1f, 0.35f));
                var backdropRect = backdrop.GetComponent<RectTransform>();
                backdropRect.anchorMin = new Vector2(0.5f, 0.5f);
                backdropRect.anchorMax = new Vector2(0.5f, 0.5f);
                backdropRect.pivot = new Vector2(0.5f, 0.5f);
                backdropRect.sizeDelta = new Vector2(560f, 560f);
                backdropRect.anchoredPosition = new Vector2(0f, 60f);
                var backdropImage = backdrop.GetComponent<Image>();
                backdropImage.sprite = iconBackdropSprite;
                backdropImage.preserveAspect = true;
                backdropImage.raycastTarget = false;
            }

            // Icon
            var iconObj = CreateChild(card.transform, "Icon", Color.white);
            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(260f, 260f);
            iconRect.anchoredPosition = new Vector2(0f, 60f);
            iconImage = iconObj.GetComponent<Image>();
            iconImage.color = new Color(1f, 0.85f, 0.18f, 1f);
            iconImage.preserveAspect = true;
            if (hintIconSprite != null)
            {
                iconImage.sprite = hintIconSprite;
            }

            // Description
            descriptionText = CreateText(card.transform, "CHOOSE HOW TO\nGET 3 HINT POWERUPS", 38, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            var descRect = descriptionText.rectTransform;
            descRect.anchorMin = new Vector2(0f, 0f);
            descRect.anchorMax = new Vector2(1f, 0f);
            descRect.pivot = new Vector2(0.5f, 0f);
            descRect.sizeDelta = new Vector2(-60f, 200f);
            descRect.anchoredPosition = new Vector2(0f, 220f);

            // Buy button (cost chip)
            var buyObj = CreateChild(card.transform, "BuyButton", buyButtonColor);
            var buyRect = buyObj.GetComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(0.5f, 0f);
            buyRect.anchorMax = new Vector2(0.5f, 0f);
            buyRect.pivot = new Vector2(0.5f, 0f);
            buyRect.sizeDelta = new Vector2(280f, 110f);
            buyRect.anchoredPosition = new Vector2(0f, 70f);
            buyButton = buyObj.AddComponent<Button>();
            buyButton.targetGraphic = buyObj.GetComponent<Image>();
            buyButton.onClick.AddListener(HandleBuy);

            var coinIconObj = CreateChild(buyObj.transform, "CoinIcon", new Color(0.27f, 0.78f, 0.46f, 1f));
            var coinIconRect = coinIconObj.GetComponent<RectTransform>();
            coinIconRect.anchorMin = new Vector2(0f, 0.5f);
            coinIconRect.anchorMax = new Vector2(0f, 0.5f);
            coinIconRect.pivot = new Vector2(0f, 0.5f);
            coinIconRect.sizeDelta = new Vector2(60f, 60f);
            coinIconRect.anchoredPosition = new Vector2(28f, 0f);
            var coinIconImage = coinIconObj.GetComponent<Image>();
            coinIconImage.preserveAspect = true;
            if (coinIconSprite != null)
            {
                coinIconImage.sprite = coinIconSprite;
                coinIconImage.color = Color.white;
            }

            costText = CreateText(buyObj.transform, GemsSortInventory.HintPackCost.ToString(), 48, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            var costRect = costText.rectTransform;
            costRect.anchorMin = new Vector2(0f, 0f);
            costRect.anchorMax = new Vector2(1f, 1f);
            costRect.pivot = new Vector2(0.5f, 0.5f);
            costRect.sizeDelta = new Vector2(-30f, 0f);
            costRect.anchoredPosition = new Vector2(40f, 0f);

            // Close X button
            var closeObj = CreateChild(card.transform, "Close", closeButtonColor);
            var closeRect = closeObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(96f, 96f);
            closeRect.anchoredPosition = new Vector2(40f, 60f);
            closeButton = closeObj.AddComponent<Button>();
            closeButton.targetGraphic = closeObj.GetComponent<Image>();
            closeButton.onClick.AddListener(Close);

            var closeText = CreateText(closeObj.transform, "X", 60, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            StretchToParent(closeText.rectTransform, 0f);
        }

        private void HandleBuy()
        {
            if (!GemsSortInventory.CanPurchaseHints())
            {
                UpdateBuyInteractable();
                return;
            }

            if (GemsSortAudio.Instance != null)
            {
                GemsSortAudio.Instance.Buy();
            }

            var callback = onBuy;
            onBuy = null;
            callback?.Invoke();
            CloseInternal(false);
        }

        private static void CreateHeaderChipMark(Transform parent, bool left)
        {
            var dot = new GameObject(left ? "MarkLeft" : "MarkRight", typeof(RectTransform), typeof(TextMeshProUGUI));
            dot.transform.SetParent(parent, false);
            var rect = (RectTransform)dot.transform;
            rect.anchorMin = new Vector2(left ? 0f : 1f, 0.5f);
            rect.anchorMax = new Vector2(left ? 0f : 1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(56f, 56f);
            rect.anchoredPosition = new Vector2(left ? 36f : -36f, 0f);

            var text = dot.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = "x";
            text.fontSize = 38;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 1f, 1f, 0.55f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        private static GameObject CreateChild(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return obj;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string content, int size, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static void StretchToParent(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
