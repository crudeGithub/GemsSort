using GemsSort.Core;
using System.Collections;
using UnityEngine;

namespace GemsSort.Views
{
    public sealed class DiamondView : MonoBehaviour
    {
        [Header("Renderer References")]
        [Tooltip("Main gem sprite renderer tinted from the active level palette.")]
        [SerializeField] private SpriteRenderer gemRenderer;
        [Tooltip("Optional selected/highlight overlay renderer.")]
        [SerializeField] private SpriteRenderer highlightRenderer;
        [Tooltip("Optional solved/locked checkmark renderer.")]
        [SerializeField] private SpriteRenderer lockRenderer;
        [Tooltip("Optional colored shadow renderer behind the gem.")]
        [SerializeField] private SpriteRenderer shadowRenderer;
        [Tooltip("Optional shine renderer used for solve feedback.")]
        [SerializeField] private SpriteRenderer shineRenderer;

        [Header("Selection Style")]
        [Tooltip("Local-space lift applied to selected gems.")]
        [SerializeField] private float selectedLift = 0.16f;

        [Header("Locked Style")]
        [Tooltip("Alpha used for the lock/check mark after a gem lands on its target.")]
        [Range(0f, 1f)]
        [SerializeField] private float lockedMarkAlpha = 0.35f;
        [Tooltip("How much the shadow color is darkened from the gem color.")]
        [Range(0f, 1f)]
        [SerializeField] private float shadowDarkenAmount = 0.62f;

        [Header("Solve Shine")]
        [SerializeField] private float crossShineDuration = 0.45f;
        [SerializeField] private float crossShineStartScale = 0.4f;
        [SerializeField] private float crossShineEndScale = 1.3f;
        [SerializeField] private float crossShineRotation = 120f;
        [Range(0f, 1f)]
        [SerializeField] private float solveGemWhitenAmount = 0.22f;
        [Range(0f, 1f)]
        [SerializeField] private float solveLockAlpha = 0.92f;

        public DiamondState State { get; private set; }
        private Color baseColor;
        private Vector3 gemRestLocalPosition;
        private Vector3 highlightRestLocalPosition;
        private Vector3 lockRestLocalPosition;
        private Vector3 shadowRestLocalPosition;
        private Vector3 shineRestLocalPosition;
        private Vector3 shineRestScale;
        private bool cachedRendererPositions;

        public void Bind(DiamondState state, LevelDefinition level)
        {
            State = state;
            CacheRendererPositions();
            if (gemRenderer != null)
            {
                baseColor = GemColorCode.Resolve(state.ColorCode, level);
                gemRenderer.color = baseColor;
            }

            SetSelected(false);
            SetLocked(state.Locked);
        }

        public void SetSelected(bool selected)
        {
            CacheRendererPositions();
            var offset = selected ? Vector3.up * selectedLift : Vector3.zero;

            if (gemRenderer != null)
            {
                gemRenderer.transform.localPosition = gemRestLocalPosition + offset;
            }

            if (highlightRenderer != null)
            {
                highlightRenderer.color = new Color(1f, 1f, 1f, 0f);
                highlightRenderer.transform.localPosition = highlightRestLocalPosition + offset;
            }

            if (lockRenderer != null)
            {
                lockRenderer.transform.localPosition = lockRestLocalPosition + offset;
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.transform.localPosition = shadowRestLocalPosition + offset;
            }

            if (shineRenderer != null)
            {
                shineRenderer.transform.localPosition = shineRestLocalPosition + offset;
            }
        }

        public void SetLocked(bool locked)
        {
            if (lockRenderer != null)
            {
                lockRenderer.color = locked ? new Color(1f, 1f, 1f, lockedMarkAlpha) : new Color(1f, 1f, 1f, 0f);
            }

            if (gemRenderer != null && State != null)
            {
                gemRenderer.color = baseColor;
            }

            if (shadowRenderer != null)
            {
                if (locked)
                {
                    // Remove shadow when locked (on correct color)
                    shadowRenderer.color = new Color(0f, 0f, 0f, 0f);
                }
                else
                {
                    Color shadowColor = Color.Lerp(baseColor, Color.black, shadowDarkenAmount);
                    shadowColor.a = 1f;
                    shadowRenderer.color = shadowColor;
                }
            }
        }

        public void SetSortingOrder(bool isOnShelf)
        {
            int baseOrder = isOnShelf ? 20 : 6;
            ApplySortingOrder(baseOrder);
        }

        public void SetVisible(bool visible)
        {
            if (gemRenderer != null) gemRenderer.enabled = visible;
            if (highlightRenderer != null) highlightRenderer.enabled = visible;
            if (lockRenderer != null) lockRenderer.enabled = visible;
            if (shadowRenderer != null) shadowRenderer.enabled = visible;
            if (shineRenderer != null) shineRenderer.enabled = visible;
        }

        public void SetFlightSortingOrder()
        {
            ApplySortingOrder(40);
        }

        private void ApplySortingOrder(int baseOrder)
        {
            if (gemRenderer != null) gemRenderer.sortingOrder = baseOrder;
            if (highlightRenderer != null) highlightRenderer.sortingOrder = baseOrder - 1;
            if (lockRenderer != null) lockRenderer.sortingOrder = baseOrder + 1;
            if (shadowRenderer != null) shadowRenderer.sortingOrder = baseOrder - 1;
            if (shineRenderer != null) shineRenderer.sortingOrder = baseOrder + 2;
        }

        public void PlayCrossShine()
        {
            if (shineRenderer != null)
            {
                StartCoroutine(CrossShineCoroutine());
            }
        }

        private IEnumerator CrossShineCoroutine()
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, crossShineDuration);
            shineRenderer.transform.localRotation = Quaternion.identity;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                float alpha = Mathf.Sin(progress * Mathf.PI);
                shineRenderer.color = new Color(1f, 1f, 1f, alpha);

                float scaleMultiplier = Mathf.Lerp(crossShineStartScale, crossShineEndScale, progress);
                shineRenderer.transform.localScale = shineRestScale * scaleMultiplier;

                shineRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, progress * crossShineRotation);

                yield return null;
            }

            shineRenderer.color = new Color(1f, 1f, 1f, 0f);
            shineRenderer.transform.localScale = shineRestScale;
            shineRenderer.transform.localRotation = Quaternion.identity;
        }

        public void SetSolveAnimationVisuals(bool active)
        {
            if (active)
            {
                if (gemRenderer != null)
                {
                    gemRenderer.color = Color.Lerp(baseColor, Color.white, solveGemWhitenAmount);
                }
                if (lockRenderer != null)
                {
                    lockRenderer.color = new Color(1f, 1f, 1f, solveLockAlpha);
                }
            }
            else
            {
                SetLocked(State != null && State.Locked);
            }
        }

        public void ConfigureRenderers(SpriteRenderer gem, SpriteRenderer highlight, SpriteRenderer lockMark, SpriteRenderer shadow, SpriteRenderer shine)
        {
            gemRenderer = gem;
            highlightRenderer = highlight;
            lockRenderer = lockMark;
            shadowRenderer = shadow;
            shineRenderer = shine;
            cachedRendererPositions = false;
            CacheRendererPositions();
        }

        private void CacheRendererPositions()
        {
            if (cachedRendererPositions)
            {
                return;
            }

            gemRestLocalPosition = gemRenderer != null ? gemRenderer.transform.localPosition : Vector3.zero;
            highlightRestLocalPosition = highlightRenderer != null ? highlightRenderer.transform.localPosition : Vector3.zero;
            lockRestLocalPosition = lockRenderer != null ? lockRenderer.transform.localPosition : Vector3.zero;
            shadowRestLocalPosition = shadowRenderer != null ? shadowRenderer.transform.localPosition : Vector3.zero;
            shineRestLocalPosition = shineRenderer != null ? shineRenderer.transform.localPosition : Vector3.zero;
            shineRestScale = shineRenderer != null ? shineRenderer.transform.localScale : Vector3.one;
            cachedRendererPositions = true;
        }
    }
}
