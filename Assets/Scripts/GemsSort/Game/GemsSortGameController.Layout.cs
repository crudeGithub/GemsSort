using GemsSort.Core;
using UnityEngine;

namespace GemsSort.Game
{
    public sealed partial class GemsSortGameController
    {
        private void FitViewport()
        {
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            float shelfWidth = ShelfColumns * CellSize * ShelfCellScale + 1.5f;
            float shelfFitSize = shelfWidth / Mathf.Max(0.1f, mainCamera.aspect * 2f);
            float minimumSize = settings != null ? settings.CameraMinOrthographicSize : 6f;
            mainCamera.orthographicSize = Mathf.Max(minimumSize, shelfFitSize);
        }

        private void LayoutBoardAndShelf()
        {
            float worldHeight = mainCamera.orthographicSize * 2f;
            float worldWidth = worldHeight * mainCamera.aspect;
            float visibleBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
            float visibleTop = mainCamera.transform.position.y + mainCamera.orthographicSize;

            float scaledCell = CellSize * ShelfCellScale;
            shelfY = visibleBottom + ShelfBottomMargin + (currentShelfRows - 1) * scaledCell;
            shelfLeftX = -(ShelfColumns - 1) * scaledCell * 0.5f;

            float boardAreaBottom = shelfY + BoardShelfGap;
            float boardAreaTop = visibleTop - BoardFitPadding;
            float boardAreaHeight = Mathf.Max(CellSize, boardAreaTop - boardAreaBottom);
            float maxBoardWidth = Mathf.Max(CellSize, worldWidth - BoardFitPadding * 2f);
            float maxBoardHeight = Mathf.Max(CellSize, boardAreaHeight);

            float scaleX = maxBoardWidth / Mathf.Max(1f, level.Width * CellSize);
            float scaleY = maxBoardHeight / Mathf.Max(1f, level.Height * CellSize);
            baseBoardScale = Mathf.Clamp(Mathf.Min(scaleX, scaleY, 1f), MinBoardScale, 1f);
            boardScale = Mathf.Clamp(baseBoardScale * boardZoom, MinBoardScale, baseBoardScale * MaxBoardZoom);

            float boardWidth = (level.Width - 1) * CellSize * boardScale;
            float boardHeight = (level.Height - 1) * CellSize * boardScale;
            boardLeftX = -boardWidth * 0.5f + boardPan.x;
            boardTopY = boardAreaBottom + boardAreaHeight * 0.5f + boardHeight * 0.5f + boardPan.y;
        }

        private void ZoomBoard(float factor)
        {
            boardZoom = Mathf.Clamp(boardZoom * factor, 1f, MaxBoardZoom);
            RefreshBoardLayout();
        }

        private void PanBoard(Vector2 world)
        {
            if (!pointerDown)
            {
                previousPointerWorld = world;
                pointerDown = true;
                return;
            }

            Vector2 delta = world - previousPointerWorld;
            if (!panning && delta.sqrMagnitude < PanStartThreshold * PanStartThreshold)
            {
                return;
            }

            panning = true;
            boardPan += delta;
            previousPointerWorld = world;
            RefreshBoardLayout();
        }

        private void RefreshBoardLayout()
        {
            LayoutBoardAndShelf();

            foreach (var pair in cellViews)
            {
                pair.Value.transform.position = BoardPosition(pair.Key.Coord.Row, pair.Key.Coord.Col);
                pair.Value.transform.localScale = BoardCellVisualScale();
            }

            for (int i = 0; i < shelfViews.Length; i++)
            {
                if (shelfViews[i] != null)
                {
                    shelfViews[i].transform.position = ShelfPosition(i);
                    shelfViews[i].transform.localScale = Vector3.one * ShelfCellScale;
                }
            }

            foreach (var pair in diamondViews)
            {
                pair.Value.SetSortingOrder(pair.Key.IsOnShelf);
                if (movingDiamondsSet.Contains(pair.Key))
                {
                    continue;
                }

                pair.Value.transform.position = DiamondPosition(pair.Key);
                pair.Value.transform.localScale = pair.Key.IsOnShelf ? Vector3.one * ShelfCellScale : Vector3.one * boardScale;
            }
        }

        private Vector3 BoardPosition(int row, int col)
        {
            return new Vector3(boardLeftX + col * CellSize * boardScale, boardTopY - row * CellSize * boardScale, 0f);
        }

        private Vector3 ShelfPosition(int index)
        {
            float scaledCell = CellSize * ShelfCellScale;
            int col = index % ShelfColumns;
            int row = index / ShelfColumns;
            float x = shelfLeftX + col * scaledCell;
            float y = shelfY - row * scaledCell;
            return new Vector3(x, y, -0.2f);
        }

        private Vector3 DiamondPosition(DiamondState diamond)
        {
            if (diamond.IsOnShelf)
            {
                return ShelfPosition(diamond.ShelfIndex) + Vector3.back * 0.08f;
            }

            return BoardPosition(diamond.Coord.Row, diamond.Coord.Col) + Vector3.back * 0.08f;
        }

        private Vector3 GetWorldPosition(bool isOnShelf, int shelfIndex, GridCoord coord)
        {
            if (isOnShelf)
            {
                return ShelfPosition(shelfIndex) + Vector3.back * 0.08f;
            }

            return BoardPosition(coord.Row, coord.Col) + Vector3.back * 0.08f;
        }

        private Vector2 WorldFromScreen(Vector3 screenPosition)
        {
            Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition);
            return new Vector2(world.x, world.y);
        }

        private Vector3 BoardCellVisualScale()
        {
            return Vector3.one * boardScale * CellSeamOverlap;
        }

        private void ClearWorld()
        {
            ClearChildren(boardRoot);
            ClearChildren(shelfRoot);
            ClearChildren(diamondRoot);
            diamondViews.Clear();
            cellViews.Clear();
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }
    }
}
