using UnityEngine;

namespace GemsSort.Game
{
    [CreateAssetMenu(menuName = "Gems Sort/Game Settings", fileName = "GemsSortGameSettings")]
    public sealed class GemsSortGameSettings : ScriptableObject
    {
        [Header("Level Source")]
        [Tooltip("Explicit level JSON assets used by the game, in play order. If empty, the game falls back to Resources/Levels.")]
        [SerializeField] private TextAsset[] levelFiles;

        [Header("Board Layout")]
        [Tooltip("Minimum orthographic camera size used when fitting the board and shelf.")]
        [SerializeField] private float cameraMinOrthographicSize = 6f;
        [Tooltip("World-space padding around the board fit area.")]
        [SerializeField] private float boardFitPadding = 0.8f;
        [Tooltip("World-space gap between the board and the shelf.")]
        [SerializeField] private float boardShelfGap = 0.85f;
        [Tooltip("World-space bottom margin below the shelf.")]
        [SerializeField] private float shelfBottomMargin = 0.85f;
        [Tooltip("Smallest allowed board scale after fitting.")]
        [SerializeField] private float minBoardScale = 0.2f;
        [Tooltip("Maximum manual zoom multiplier applied on top of the fitted board scale.")]
        [SerializeField] private float maxBoardZoom = 3f;
        [Tooltip("Mouse wheel zoom multiplier per tick.")]
        [SerializeField] private float mouseZoomStep = 1.1f;
        [Tooltip("Pointer movement needed before a tap becomes a pan.")]
        [SerializeField] private float panStartThreshold = 0.08f;
        [Tooltip("Slightly overlaps cell visuals to avoid visible seams between tiles.")]
        [SerializeField] private float cellSeamOverlap = 1.025f;

        [Header("Shelf Layout")]
        [Tooltip("Number of shelf slots per row.")]
        [SerializeField] private int shelfColumns = 10;
        [Tooltip("Number of shelf rows available for temporary gems.")]
        [SerializeField] private int shelfRows = 3;
        [Tooltip("Maximum shelf rows allowed (shelf hint adds rows up to this cap).")]
        [SerializeField] private int maxShelfRows = 4;
        [Tooltip("Visual scale of shelf slot cells. Lower values fit more slots on screen.")]
        [Range(0.4f, 1f)]
        [SerializeField] private float shelfCellScale = 0.75f;

        [Header("Animation")]
        [Tooltip("Duration for gems moving to or from the board.")]
        [SerializeField] private float boardMoveDuration = 0.16f;
        [Tooltip("Duration for gems entering the shelf.")]
        [SerializeField] private float shelfMoveDuration = 0.12f;
        [Tooltip("Duration for shelf gems compacting after gems leave the shelf.")]
        [SerializeField] private float shelfSettleDuration = 0.09f;
        [Tooltip("Small stagger between selected gems moving to the board. Keep low for snappy play.")]
        [SerializeField] private float boardMoveStaggerDelay = 0.015f;
        [Tooltip("Stagger used when shelf gems reorder. Zero means all shelf gems move together.")]
        [SerializeField] private float shelfMoveStaggerDelay = 0f;

        public TextAsset[] LevelFiles => levelFiles;
        public float CameraMinOrthographicSize => Mathf.Max(0.1f, cameraMinOrthographicSize);
        public float BoardFitPadding => Mathf.Max(0f, boardFitPadding);
        public float BoardShelfGap => Mathf.Max(0f, boardShelfGap);
        public float ShelfBottomMargin => Mathf.Max(0f, shelfBottomMargin);
        public float MinBoardScale => Mathf.Max(0.01f, minBoardScale);
        public float MaxBoardZoom => Mathf.Max(1f, maxBoardZoom);
        public float MouseZoomStep => Mathf.Max(1.01f, mouseZoomStep);
        public float PanStartThreshold => Mathf.Max(0.001f, panStartThreshold);
        public float CellSeamOverlap => Mathf.Max(1f, cellSeamOverlap);
        public int ShelfColumns => Mathf.Max(1, shelfColumns);
        public int ShelfRows => Mathf.Max(1, shelfRows);
        public int MaxShelfRows => Mathf.Max(ShelfRows, maxShelfRows);
        public float ShelfCellScale => Mathf.Clamp(shelfCellScale, 0.4f, 1f);
        public int ShelfSlotCount => ShelfColumns * ShelfRows;
        public float BoardMoveDuration => Mathf.Max(0.01f, boardMoveDuration);
        public float ShelfMoveDuration => Mathf.Max(0.01f, shelfMoveDuration);
        public float ShelfSettleDuration => Mathf.Max(0.01f, shelfSettleDuration);
        public float BoardMoveStaggerDelay => Mathf.Max(0f, boardMoveStaggerDelay);
        public float ShelfMoveStaggerDelay => Mathf.Max(0f, shelfMoveStaggerDelay);

        public void ConfigureLevels(TextAsset[] files)
        {
            levelFiles = files;
        }
    }
}
