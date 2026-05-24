using GemsSort.Core;
using UnityEngine;

namespace GemsSort.Views
{
    public sealed class BoardCellView : MonoBehaviour
    {
        [Header("Renderer References")]
        [Tooltip("Renderer tinted to the target color for this board cell.")]
        [SerializeField] private SpriteRenderer fillRenderer;
        [Tooltip("Renderer used as the cell outline or backing shape.")]
        [SerializeField] private SpriteRenderer borderRenderer;

        [Header("Style")]
        [Tooltip("Outline color applied when the cell is bound.")]
        [SerializeField] private Color borderColor = new Color(0f, 0f, 0f, 0.42f);

        public CellState State { get; private set; }

        private Color targetColor = Color.white;

        public void Bind(CellState state, LevelDefinition level)
        {
            State = state;
            targetColor = GemColorCode.Resolve(state.TargetColor, level);
            if (fillRenderer != null)
            {
                fillRenderer.color = targetColor;
                fillRenderer.enabled = true;
            }

            if (borderRenderer != null)
            {
                borderRenderer.color = borderColor;
            }
        }

        /// <summary>
        /// Switches the cell into the win-reveal style. The fill is hidden and the
        /// border sprite is tinted with the target color so the board reads as a
        /// pixelated outline of the finished image.
        /// </summary>
        public void SetRevealMode(bool revealing)
        {
            if (fillRenderer != null)
            {
                fillRenderer.enabled = !revealing;
            }

            if (borderRenderer != null)
            {
                borderRenderer.color = revealing ? targetColor : borderColor;
            }
        }

        public void ConfigureRenderers(SpriteRenderer fill, SpriteRenderer border)
        {
            fillRenderer = fill;
            borderRenderer = border;
        }
    }
}
