using System.Collections;
using System.Collections.Generic;
using GemsSort.Core;
using GemsSort.Services;
using GemsSort.Views;
using UnityEngine;

namespace GemsSort.Game
{
    public sealed partial class GemsSortGameController : MonoBehaviour
    {
        private const float CellSize = 1f;
        private const int DefaultShelfColumns = 10;
        private const int DefaultShelfRows = 3;

        [Header("Game Data")]
        [Tooltip("Visible project settings asset. Assign level JSON files and tune layout values here.")]
        [SerializeField] private GemsSortGameSettings settings;

        [Header("Prefab-backed Views")]
        [Tooltip("Board cell prefab used for every active level cell.")]
        [SerializeField] private BoardCellView cellPrefab;
        [Tooltip("Movable gem prefab used for all colored gems.")]
        [SerializeField] private DiamondView diamondPrefab;
        [Tooltip("Bottom shelf slot prefab used for temporary gem storage.")]
        [SerializeField] private ShelfSlotView shelfSlotPrefab;

        [Header("Scene Roots")]
        [Tooltip("Runtime board cells are spawned under this transform.")]
        [SerializeField] private Transform boardRoot;
        [Tooltip("Fixed bottom shelf slots are spawned under this transform.")]
        [SerializeField] private Transform shelfRoot;
        [Tooltip("Runtime diamonds are spawned under this transform.")]
        [SerializeField] private Transform diamondRoot;

        [Header("Scene Services")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private GemsSortAudio audioService;
        [SerializeField] private GemsSortEffects effects;

        [Header("Scene UI")]
        [SerializeField] private GemsSortHud hud;

        private readonly List<DiamondState> selected = new List<DiamondState>();
        private readonly Dictionary<DiamondState, DiamondView> diamondViews = new Dictionary<DiamondState, DiamondView>();
        private readonly Dictionary<CellState, BoardCellView> cellViews = new Dictionary<CellState, BoardCellView>();
        private readonly HashSet<int> completedColors = new HashSet<int>();
        private readonly HashSet<DiamondState> movingDiamondsSet = new HashSet<DiamondState>();

        private CellState[,] board;
        private DiamondState[] shelf;
        private ShelfSlotView[] shelfViews;
        private LevelDefinition[] levels;
        private LevelDefinition level;
        private int levelIndex;
        private float boardLeftX;
        private float boardTopY;
        private float shelfLeftX;
        private float shelfY;
        private float baseBoardScale = 1f;
        private float boardZoom = 1f;
        private float boardScale = 1f;
        private Vector2 boardPan;
        private Vector2 previousPointerWorld;
        private Vector2 pointerDownWorld;
        private float previousPinchDistance;
        private int pointerButton = -1;
        private bool pointerDown;
        private bool panning;
        private bool pinching;
        private bool selectedFromShelf;
        private bool moving;
        private bool levelComplete;

        private float BoardFitPadding => settings != null ? settings.BoardFitPadding : 0.8f;
        private float BoardShelfGap => settings != null ? settings.BoardShelfGap : 0.85f;
        private float ShelfBottomMargin => settings != null ? settings.ShelfBottomMargin : 0.85f;
        private float MinBoardScale => settings != null ? settings.MinBoardScale : 0.2f;
        private float MaxBoardZoom => settings != null ? settings.MaxBoardZoom : 3f;
        private float MouseZoomStep => settings != null ? settings.MouseZoomStep : 1.1f;
        private float PanStartThreshold => settings != null ? settings.PanStartThreshold : 0.08f;
        private float CellSeamOverlap => settings != null ? settings.CellSeamOverlap : 1.025f;
        private int ShelfColumns => settings != null ? settings.ShelfColumns : DefaultShelfColumns;
        private int ShelfRows => settings != null ? settings.ShelfRows : DefaultShelfRows;
        private int MaxShelfRows => settings != null ? settings.MaxShelfRows : 4;
        private float ShelfCellScale => settings != null ? settings.ShelfCellScale : 0.75f;
        private int ShelfSlotCount => ShelfColumns * currentShelfRows;
        private int currentShelfRows;
        private float BoardMoveDuration => settings != null ? settings.BoardMoveDuration : 0.16f;
        private float ShelfMoveDuration => settings != null ? settings.ShelfMoveDuration : 0.12f;
        private float ShelfSettleDuration => settings != null ? settings.ShelfSettleDuration : 0.09f;
        private float BoardMoveStaggerDelay => settings != null ? settings.BoardMoveStaggerDelay : 0.015f;
        private float ShelfMoveStaggerDelay => settings != null ? settings.ShelfMoveStaggerDelay : 0f;

        public LevelDefinition[] Levels => levels;
        public int CurrentLevelIndex => levelIndex;
        public GemsSortAudio AudioService => audioService;

        private void Awake()
        {
            if (!ValidateSceneReferences())
            {
                enabled = false;
                return;
            }

            levels = LevelCatalog.Load(settings);
            hud.Bind(this);
            int savedLevelIndex = PlayerPrefs.GetInt("GemsSort.CurrentLevelIndex", 0);
            LoadLevel(savedLevelIndex);
        }

        private void Update()
        {
            HandleViewportInput();
        }

        public void RestartLevel()
        {
            LoadLevel(levelIndex);
        }

        public void LoadNextLevel()
        {
            LoadLevel(levelIndex >= levels.Length - 1 ? 0 : levelIndex + 1);
        }

        private bool ValidateSceneReferences()
        {
            bool valid = true;
            valid &= ReportMissing(cellPrefab, nameof(cellPrefab));
            valid &= ReportMissing(diamondPrefab, nameof(diamondPrefab));
            valid &= ReportMissing(shelfSlotPrefab, nameof(shelfSlotPrefab));
            valid &= ReportMissing(boardRoot, nameof(boardRoot));
            valid &= ReportMissing(shelfRoot, nameof(shelfRoot));
            valid &= ReportMissing(diamondRoot, nameof(diamondRoot));
            valid &= ReportMissing(mainCamera, nameof(mainCamera));
            valid &= ReportMissing(audioService, nameof(audioService));
            valid &= ReportMissing(effects, nameof(effects));
            valid &= ReportMissing(hud, nameof(hud));
            return valid;
        }

        private bool ReportMissing(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError("Gems Sort is missing " + fieldName + ". Assign the scene reference on the Gems Sort Game component.", this);
            return false;
        }

        private bool isTransitioning = false;
        private bool isFirstLoad = true;

        public void LoadLevel(int index)
        {
            if (isTransitioning) return;
            StartCoroutine(LoadLevelRoutine(index));
        }

        private IEnumerator LoadLevelRoutine(int index)
        {
            isTransitioning = true;
            int levelNum = index + 1;

            if (isFirstLoad)
            {
                isFirstLoad = false;
                // Instantly show transition screen overlay fully opaque
                hud.ShowFirstLoadTransition(levelNum);

                // Perform the level generation behind the scene
                PerformLoadLevel(index);

                // Wait a tiny frame for rendering to settle
                yield return null;

                // Fade out transition screen
                yield return hud.FadeOutTransition();
            }
            else
            {
                // Fade in transition screen
                yield return hud.FadeInTransition(levelNum);

                // Perform the level generation behind the scene
                PerformLoadLevel(index);

                // Wait a brief organic moment for the loading feel
                yield return new WaitForSeconds(0.4f);

                // Fade out transition screen
                yield return hud.FadeOutTransition();
            }

            isTransitioning = false;
        }

        private void PerformLoadLevel(int index)
        {
            if (levels == null || levels.Length == 0)
            {
                Debug.LogError("Gems Sort cannot load a level because no levels are available.", this);
                return;
            }

            ClearWorld();
            ResetWinVisuals();
            levelIndex = Mathf.Clamp(index, 0, levels.Length - 1);
            PlayerPrefs.SetInt("GemsSort.CurrentLevelIndex", levelIndex);
            PlayerPrefs.Save();
            level = levels[levelIndex];
            currentShelfRows = ShelfRows;
            board = new CellState[level.Height, level.Width];
            shelf = new DiamondState[ShelfSlotCount];
            shelfViews = new ShelfSlotView[ShelfSlotCount];
            selected.Clear();
            selectedFromShelf = false;
            moving = false;
            levelComplete = false;
            boardZoom = 1f;
            boardPan = Vector2.zero;
            pointerDown = false;
            panning = false;
            pinching = false;
            completedColors.Clear();

            FitViewport();
            LayoutBoardAndShelf();

            BuildBoard(LevelScrambler.CreateStartGrid(level));
            BuildShelf();
            ScanAndLockMatches(false);
            InitializeCompletedColors();
            UpdateSelectionVisuals();
            hud.SetLevel(levelIndex + 1, levels.Length);
        }

        private void BuildBoard(int[,] startGrid)
        {
            for (int row = 0; row < level.Height; row++)
            {
                for (int col = 0; col < level.Width; col++)
                {
                    int target = level.Target[row, col];
                    if (target == GemColorCode.Blank)
                    {
                        continue;
                    }

                    var cell = new CellState(row, col, target);
                    board[row, col] = cell;

                    var cellView = Instantiate(cellPrefab, BoardPosition(row, col), Quaternion.identity, boardRoot);
                    cellView.name = "Cell " + row + "," + col;
                    cellView.gameObject.SetActive(true);
                    cellView.transform.localScale = BoardCellVisualScale();
                    cellView.Bind(cell, level);
                    cellViews[cell] = cellView;

                    var diamond = new DiamondState(startGrid[row, col], cell.Coord);
                    cell.Occupant = diamond;
                    var diamondView = Instantiate(diamondPrefab, DiamondPosition(diamond), Quaternion.identity, diamondRoot);
                    diamondView.name = "Diamond " + diamond.ColorCode;
                    diamondView.gameObject.SetActive(true);
                    diamondView.transform.localScale = Vector3.one * boardScale;
                    diamondView.Bind(diamond, level);
                    diamondViews[diamond] = diamondView;
                }
            }
        }

        private void BuildShelf()
        {
            for (int i = 0; i < shelf.Length; i++)
            {
                var view = Instantiate(shelfSlotPrefab, ShelfPosition(i), Quaternion.identity, shelfRoot);
                view.name = "Shelf Slot " + i;
                view.gameObject.SetActive(true);
                view.transform.localScale = Vector3.one * ShelfCellScale;
                view.Bind(i);
                shelfViews[i] = view;
            }
        }
    }
}
