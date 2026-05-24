using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GemsSort.Game
{
    public sealed class GemsSortLevelCard : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Image previewImage;
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI actionButtonText;

        public TextMeshProUGUI TitleText
        {
            get => titleText;
            set => titleText = value;
        }

        public Image PreviewImage
        {
            get => previewImage;
            set => previewImage = value;
        }

        public Button ActionButton
        {
            get => actionButton;
            set => actionButton = value;
        }

        public TextMeshProUGUI ActionButtonText
        {
            get => actionButtonText;
            set => actionButtonText = value;
        }
    }
}
