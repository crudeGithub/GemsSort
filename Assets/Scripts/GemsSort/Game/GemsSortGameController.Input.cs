using GemsSort.Core;
using UnityEngine;

namespace GemsSort.Game
{
    public sealed partial class GemsSortGameController
    {
        private void HandleViewportInput()
        {
            if (level == null || mainCamera == null || hintRunning)
            {
                return;
            }

            if (hud != null && hud.IsPanelActive)
            {
                pointerDown = false;
                panning = false;
                pinching = false;
                return;
            }

            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > Mathf.Epsilon)
            {
                ZoomBoard(wheel > 0f ? MouseZoomStep : 1f / MouseZoomStep);
            }

            if (Input.touchCount >= 2)
            {
                HandlePinchInput();
                return;
            }

            if (Input.touchCount == 1)
            {
                HandleSingleTouch(Input.GetTouch(0));
                return;
            }

            if (pinching)
            {
                pinching = false;
                previousPinchDistance = 0f;
                return;
            }

            HandleMouseInput();
        }

        private void HandlePinchInput()
        {
            var first = Input.GetTouch(0);
            var second = Input.GetTouch(1);
            float distance = Vector2.Distance(first.position, second.position);

            if (!pinching || previousPinchDistance <= Mathf.Epsilon)
            {
                previousPinchDistance = distance;
                pinching = true;
                pointerDown = false;
                panning = false;
                return;
            }

            ZoomBoard(distance / previousPinchDistance);
            previousPinchDistance = distance;
        }

        private bool IsPointerOverUi()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null) return false;
            
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return true;
            
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return true;
            }
            
            return false;
        }

        private void HandleMouseInput()
        {
            if (areaHintActive)
            {
                Vector2 world = WorldFromScreen(Input.mousePosition);
                UpdateAreaHintBox(world);
                
                if (!areaHintJustActivated && (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2)))
                {
                    StartCoroutine(ExecuteAreaHint());
                }
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUi()) return;
                BeginPointerDrag(0, WorldFromScreen(Input.mousePosition), false);
                return;
            }

            if (Input.GetMouseButton(0))
            {
                PanBoard(WorldFromScreen(Input.mousePosition));
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                EndPointerDrag(WorldFromScreen(Input.mousePosition), true);
                return;
            }

            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                if (IsPointerOverUi()) return;
                BeginPointerDrag(Input.GetMouseButtonDown(1) ? 1 : 2, WorldFromScreen(Input.mousePosition), true);
                return;
            }

            if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {
                PanBoard(WorldFromScreen(Input.mousePosition));
                return;
            }

            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
            {
                EndPointerDrag(WorldFromScreen(Input.mousePosition), false);
            }
        }

        private void HandleSingleTouch(Touch touch)
        {
            Vector2 world = WorldFromScreen(touch.position);

            if (areaHintActive)
            {
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    UpdateAreaHintBox(world);
                }
                else if (!areaHintJustActivated && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
                {
                    StartCoroutine(ExecuteAreaHint());
                }
                return;
            }

            if (touch.phase == TouchPhase.Began)
            {
                if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
                BeginPointerDrag(0, world, false);
                return;
            }

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                PanBoard(world);
                return;
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                EndPointerDrag(world, touch.phase == TouchPhase.Ended);
            }
        }

        private void BeginPointerDrag(int button, Vector2 world, bool startPanning)
        {
            pointerButton = button;
            pointerDown = true;
            panning = startPanning;
            pointerDownWorld = world;
            previousPointerWorld = world;
        }

        private void EndPointerDrag(Vector2 world, bool allowTap)
        {
            bool wasTap = allowTap && pointerButton == 0 && !panning && Vector2.Distance(pointerDownWorld, world) < PanStartThreshold;
            pointerButton = -1;
            pointerDown = false;
            panning = false;

            if (wasTap && !moving && !levelComplete)
            {
                HandlePointer(world);
            }
        }

        private void HandlePointer(Vector2 world)
        {
            bool boardHit = TryGetBoardCell(world, out CellState cell);
            bool shelfHit = TryGetShelfSlot(world, out int shelfIndex);
            bool shelfAreaHit = IsInShelfArea(world);

            if (selected.Count == 0)
            {
                if (boardHit && cell.Occupant != null && !cell.Occupant.Locked)
                {
                    SelectBoardGroup(cell.Occupant);
                    audioService.Select(selected.Count);
                    return;
                }

                if (shelfHit && shelf[shelfIndex] != null)
                {
                    SelectShelfColor(shelf[shelfIndex].ColorCode);
                    audioService.Select(selected.Count);
                }

                return;
            }

            if (boardHit)
            {
                HandleBoardTap(cell);
                return;
            }

            if (shelfHit && shelf[shelfIndex] != null)
            {
                if (selected.Contains(shelf[shelfIndex]))
                {
                    ClearSelection();
                    audioService.Deselect();
                    return;
                }

                SelectShelfColor(shelf[shelfIndex].ColorCode);
                audioService.Select(selected.Count);
                return;
            }

            if (shelfAreaHit)
            {
                if (!selectedFromShelf)
                {
                    StartCoroutine(MoveSelectedToShelf());
                }

                return;
            }

            audioService.Error();
        }

        private void HandleBoardTap(CellState cell)
        {
            if (cell.Occupant != null)
            {
                if (selected.Contains(cell.Occupant))
                {
                    ClearSelection();
                    audioService.Deselect();
                    return;
                }

                if (!cell.Occupant.Locked)
                {
                    SelectBoardGroup(cell.Occupant);
                    audioService.Select(selected.Count);
                }
                else
                {
                    audioService.Error();
                }

                return;
            }

            int color = selected[0].ColorCode;
            if (cell.TargetColor != color)
            {
                audioService.Error();
                return;
            }

            var targets = FindConnectedEmptyTargets(cell, color);
            if (targets.Count == 0)
            {
                audioService.Error();
                return;
            }

            StartCoroutine(MoveSelectedToBoard(targets));
        }

        private bool TryGetBoardCell(Vector2 world, out CellState cell)
        {
            float scaledCellSize = CellSize * boardScale;
            int col = Mathf.RoundToInt((world.x - boardLeftX) / scaledCellSize);
            int row = Mathf.RoundToInt((boardTopY - world.y) / scaledCellSize);
            cell = TryCell(row, col);
            if (cell == null)
            {
                return false;
            }

            Vector3 center = BoardPosition(row, col);
            float halfSize = boardScale * 0.5f;
            return Mathf.Abs(world.x - center.x) <= halfSize && Mathf.Abs(world.y - center.y) <= halfSize;
        }

        private bool TryGetShelfSlot(Vector2 world, out int index)
        {
            index = -1;
            float scaledCell = CellSize * ShelfCellScale;
            int col = Mathf.RoundToInt((world.x - shelfLeftX) / scaledCell);
            int row = Mathf.RoundToInt((shelfY - world.y) / scaledCell);
            if (col < 0 || col >= ShelfColumns || row < 0 || row >= currentShelfRows)
            {
                return false;
            }

            int potentialIndex = row * ShelfColumns + col;
            if (potentialIndex >= shelf.Length)
            {
                return false;
            }

            Vector3 center = ShelfPosition(potentialIndex);
            float halfCell = scaledCell * 0.5f;
            if (Mathf.Abs(world.x - center.x) <= halfCell && Mathf.Abs(world.y - center.y) <= halfCell)
            {
                index = potentialIndex;
                return true;
            }

            return false;
        }

        private bool IsInShelfArea(Vector2 world)
        {
            float scaledCell = CellSize * ShelfCellScale;
            float minX = shelfLeftX - scaledCell * 0.55f;
            float maxX = shelfLeftX + (ShelfColumns - 1) * scaledCell + scaledCell * 0.55f;
            float minY = shelfY - (currentShelfRows - 1) * scaledCell - scaledCell * 0.55f;
            float maxY = shelfY + scaledCell * 0.55f;
            return world.x >= minX && world.x <= maxX && world.y >= minY && world.y <= maxY;
        }
    }
}
