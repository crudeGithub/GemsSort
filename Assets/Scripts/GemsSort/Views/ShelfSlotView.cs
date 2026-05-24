using UnityEngine;

namespace GemsSort.Views
{
    public sealed class ShelfSlotView : MonoBehaviour
    {
        [Header("Renderer References")]
        [Tooltip("Renderer for the shelf slot background.")]
        [SerializeField] private SpriteRenderer fillRenderer;

        [Header("Style")]
        [Tooltip("Color applied to an empty shelf slot.")]
        [SerializeField] private Color slotColor = new Color(0.18f, 0.19f, 0.21f, 1f);

        public int Index { get; private set; }

        public void Bind(int index)
        {
            Index = index;
            if (fillRenderer != null)
            {
                fillRenderer.color = slotColor;
            }
        }

        public void ConfigureRenderer(SpriteRenderer fill)
        {
            fillRenderer = fill;
        }
    }
}
