using System.Collections;
using System.Collections.Generic;
using GemsSort.Core;
using GemsSort.Views;
using UnityEngine;

namespace GemsSort.Game
{
    public sealed partial class GemsSortGameController
    {
        private bool areaHintActive;
        private bool areaHintJustActivated;
        private Rect areaHintBounds;
        private LineRenderer areaHintLineRenderer;
        private bool hintRunning;

        public void ActivateAreaHint()
        {
            TryActivateAreaHint();
        }

        public bool TryActivateAreaHint()
        {
            if (moving || levelComplete || areaHintActive || hintRunning) return false;
            ResetViewport();
            areaHintActive = true;
            StartCoroutine(AreaHintCooldown());
            CreateAreaHintVisuals();
            UpdateAreaHintBox(new Vector2(boardLeftX + (level.Width * CellSize * boardScale * 0.5f), boardTopY - (level.Height * CellSize * boardScale * 0.5f)));
            return true;
        }

        private IEnumerator AreaHintCooldown()
        {
            areaHintJustActivated = true;
            yield return null;
            yield return new WaitUntil(() => !Input.GetMouseButton(0) && Input.touchCount == 0);
            areaHintJustActivated = false;
        }

        public void ActivateWandHint()
        {
            TryActivateWandHint();
        }

        public bool TryActivateWandHint()
        {
            if (moving || levelComplete || hintRunning) return false;
            if (!CanActivateWandHint())
            {
                audioService.Error();
                return false;
            }
            ResetViewport();
            StartCoroutine(ExecuteWandHint());
            return true;
        }

        public void ActivateMagnetHint()
        {
            TryActivateMagnetHint();
        }

        public bool TryActivateMagnetHint()
        {
            if (moving || levelComplete || hintRunning) return false;
            if (!CanActivateMagnetHint())
            {
                audioService.Error();
                return false;
            }
            ResetViewport();
            StartCoroutine(ExecuteMagnetHint());
            return true;
        }

        public void ActivateShelfHint()
        {
            TryActivateShelfHint();
        }

        public bool TryActivateShelfHint()
        {
            if (moving || levelComplete || hintRunning) return false;
            if (currentShelfRows >= MaxShelfRows) return false;
            ExpandShelf();
            return true;
        }

        private bool CanActivateWandHint()
        {
            if (board == null || level == null || shelf == null) return false;

            var shelfGems = new List<DiamondState>();
            for (int i = 0; i < shelf.Length; i++)
            {
                if (shelf[i] != null) shelfGems.Add(shelf[i]);
            }

            var cellsToSolve = new List<CellState>();
            foreach (var gem in shelfGems)
            {
                bool found = false;
                for (int r = 0; r < level.Height && !found; r++)
                {
                    for (int c = 0; c < level.Width; c++)
                    {
                        var cell = board[r, c];
                        if (cell != null && cell.TargetColor == gem.ColorCode)
                        {
                            if (!cellsToSolve.Contains(cell) && (cell.Occupant == null || cell.Occupant.ColorCode != cell.TargetColor))
                            {
                                cellsToSolve.Add(cell);
                                found = true;
                                break;
                            }
                        }
                    }
                }
            }
            return cellsToSolve.Count > 0;
        }

        private bool CanActivateMagnetHint()
        {
            if (board == null || level == null) return false;

            for (int r = 0; r < level.Height; r++)
            {
                for (int c = 0; c < level.Width; c++)
                {
                    var cell = board[r, c];
                    if (cell != null && cell.TargetColor != GemColorCode.Blank)
                    {
                        if (cell.Occupant == null || cell.Occupant.ColorCode != cell.TargetColor)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void ExpandShelf()
        {
            int oldSlotCount = shelf.Length;
            currentShelfRows++;
            int newSlotCount = ShelfSlotCount;

            // Expand arrays.
            var newShelf = new DiamondState[newSlotCount];
            var newShelfViews = new ShelfSlotView[newSlotCount];
            for (int i = 0; i < oldSlotCount; i++)
            {
                newShelf[i] = shelf[i];
                newShelfViews[i] = shelfViews[i];
            }
            shelf = newShelf;
            shelfViews = newShelfViews;

            // Spawn new shelf slot views.
            for (int i = oldSlotCount; i < newSlotCount; i++)
            {
                var view = Instantiate(shelfSlotPrefab, ShelfPosition(i), Quaternion.identity, shelfRoot);
                view.name = "Shelf Slot " + i;
                view.gameObject.SetActive(true);
                view.transform.localScale = Vector3.one * ShelfCellScale;
                view.Bind(i);
                shelfViews[i] = view;
            }

            // Re-layout everything to account for the new row.
            FitViewport();
            RefreshBoardLayout();
        }

        private void ResetViewport()
        {
            boardZoom = 1f;
            boardPan = Vector2.zero;
            pointerDown = false;
            panning = false;
            pinching = false;
            RefreshBoardLayout();
        }

        private IEnumerator ExecuteAreaHint()
        {
            areaHintActive = false;
            if (areaHintLineRenderer != null)
            {
                areaHintLineRenderer.enabled = false;
            }

            hintRunning = true;
            moving = true;
            var cellsToSolve = new List<CellState>();

            for (int r = 0; r < level.Height; r++)
            {
                for (int c = 0; c < level.Width; c++)
                {
                    var cell = board[r, c];
                    if (cell == null || cell.TargetColor == GemColorCode.Blank) continue;

                    Vector3 pos = BoardPosition(r, c);
                    if (areaHintBounds.Contains(pos))
                    {
                        if (cell.Occupant == null || cell.Occupant.ColorCode != cell.TargetColor)
                        {
                            cellsToSolve.Add(cell);
                        }
                    }
                }
            }

            yield return PerformHintSwaps(cellsToSolve, true, false);
            hintRunning = false;
        }

        private IEnumerator ExecuteWandHint()
        {
            hintRunning = true;
            moving = true;
            var cellsToSolve = new List<CellState>();

            var shelfGems = new List<DiamondState>();
            for (int i = 0; i < shelf.Length; i++)
            {
                if (shelf[i] != null) shelfGems.Add(shelf[i]);
            }

            foreach (var gem in shelfGems)
            {
                bool found = false;
                for (int r = 0; r < level.Height && !found; r++)
                {
                    for (int c = 0; c < level.Width; c++)
                    {
                        var cell = board[r, c];
                        if (cell != null && cell.TargetColor == gem.ColorCode)
                        {
                            if (!cellsToSolve.Contains(cell) && (cell.Occupant == null || cell.Occupant.ColorCode != cell.TargetColor))
                            {
                                cellsToSolve.Add(cell);
                                found = true;
                                break;
                            }
                        }
                    }
                }
            }

            yield return PerformHintSwaps(cellsToSolve, false, true, true);
            hintRunning = false;
        }

        private IEnumerator ExecuteMagnetHint()
        {
            hintRunning = true;
            moving = true;
            var cellsToSolve = new List<CellState>();
            var potentialCells = new List<CellState>();

            for (int r = 0; r < level.Height; r++)
            {
                for (int c = 0; c < level.Width; c++)
                {
                    var cell = board[r, c];
                    if (cell != null && cell.TargetColor != GemColorCode.Blank)
                    {
                        if (cell.Occupant == null || cell.Occupant.ColorCode != cell.TargetColor)
                        {
                            potentialCells.Add(cell);
                        }
                    }
                }
            }

            // Sort potential cells top-to-bottom, left-to-right
            potentialCells.Sort((a, b) =>
            {
                int rowCmp = a.Coord.Row.CompareTo(b.Coord.Row);
                if (rowCmp != 0) return rowCmp;
                return a.Coord.Col.CompareTo(b.Coord.Col);
            });

            int count = Mathf.Min(Random.Range(10, 13), potentialCells.Count);
            for (int i = 0; i < count; i++)
            {
                cellsToSolve.Add(potentialCells[i]);
            }

            yield return PerformHintSwaps(cellsToSolve, true, false);
            hintRunning = false;
        }

        private IEnumerator PerformHintSwaps(List<CellState> targetsToSolve, bool allowShelfDisplacement, bool prioritizeShelf = false, bool prioritizeShelfOnly = false)
        {
            var initialStates = new Dictionary<DiamondState, (bool isOnShelf, int shelfIdx, GridCoord coord)>();
            foreach (var diamond in diamondViews.Keys)
            {
                initialStates[diamond] = (diamond.IsOnShelf, diamond.ShelfIndex, diamond.Coord);
            }

            foreach (var target in targetsToSolve)
            {
                if (target.Occupant != null && target.Occupant.ColorCode == target.TargetColor)
                    continue;

                DiamondState candidate = FindUnsolvedGem(target.TargetColor, target, prioritizeShelf, prioritizeShelfOnly, initialStates);
                if (candidate == null) continue;

                var occupant = target.Occupant;

                bool candidateWasOnShelf = candidate.IsOnShelf;
                int candidateOldShelfIdx = candidate.ShelfIndex;
                GridCoord candidateOldCoord = candidate.Coord;

                if (candidateWasOnShelf)
                {
                    shelf[candidateOldShelfIdx] = null;
                    candidate.ShelfIndex = -1;
                }
                else
                {
                    GetCell(candidateOldCoord).Occupant = null;
                }
                
                candidate.Coord = target.Coord;
                target.Occupant = candidate;

                if (occupant != null)
                {
                    if (!candidateWasOnShelf)
                    {
                        var emptyCell = GetCell(candidateOldCoord);
                        occupant.Coord = emptyCell.Coord;
                        emptyCell.Occupant = occupant;
                    }
                    else
                    {
                        if (allowShelfDisplacement)
                        {
                            occupant.Coord = new GridCoord(-1, -1);
                            occupant.ShelfIndex = candidateOldShelfIdx;
                            shelf[candidateOldShelfIdx] = occupant;
                        }
                        else
                        {
                            var emptyCell = FindEmptyBoardCell();
                            if (emptyCell != null)
                            {
                                occupant.Coord = emptyCell.Coord;
                                emptyCell.Occupant = occupant;
                            }
                            else
                            {
                                occupant.Coord = new GridCoord(-1, -1);
                                occupant.ShelfIndex = candidateOldShelfIdx;
                                shelf[candidateOldShelfIdx] = occupant;
                            }
                        }
                    }
                }
            }

            var animInfos = new List<MovingDiamondInfo>();
            bool shelfChanged = false;

            foreach (var pair in initialStates)
            {
                var diamond = pair.Key;
                var initial = pair.Value;
                
                bool moved = false;
                if (initial.isOnShelf != diamond.IsOnShelf) moved = true;
                else if (initial.isOnShelf && initial.shelfIdx != diamond.ShelfIndex) moved = true;
                else if (!initial.isOnShelf && !initial.coord.Equals(diamond.Coord)) moved = true;

                if (moved)
                {
                    animInfos.Add(new MovingDiamondInfo
                    {
                        Diamond = diamond,
                        StartIsOnShelf = initial.isOnShelf,
                        StartShelfIndex = initial.shelfIdx,
                        StartCoord = initial.coord,
                        EndIsOnShelf = diamond.IsOnShelf,
                        EndShelfIndex = diamond.ShelfIndex,
                        EndCoord = diamond.Coord
                    });

                    if (initial.isOnShelf || diamond.IsOnShelf)
                    {
                        shelfChanged = true;
                    }
                    
                    if (diamondViews.TryGetValue(diamond, out var view))
                    {
                        view.SetSortingOrder(initial.isOnShelf);
                    }
                }
            }

            if (animInfos.Count > 0)
            {
                ClearSelection();
                
                var groups = GroupAnimInfos(animInfos);
                groups.Sort((a, b) => 
                {
                    var aFirst = a[0];
                    var bFirst = b[0];
                    if (aFirst.EndIsOnShelf != bFirst.EndIsOnShelf) return aFirst.EndIsOnShelf ? 1 : -1;
                    if (aFirst.EndIsOnShelf) return aFirst.EndShelfIndex.CompareTo(bFirst.EndShelfIndex);
                    int rowCmp = aFirst.EndCoord.Row.CompareTo(bFirst.EndCoord.Row);
                    if (rowCmp != 0) return rowCmp;
                    return aFirst.EndCoord.Col.CompareTo(bFirst.EndCoord.Col);
                });

                yield return AnimateHintGroups(groups, BoardMoveDuration, BoardMoveStaggerDelay);

                if (shelfChanged)
                {
                    var settleInfos = CompactShelfAndCreateSettleAnimations();
                    if (settleInfos.Count > 0)
                    {
                        yield return AnimateDiamonds(settleInfos, ShelfSettleDuration, 0f, false);
                    }
                }
            }

            moving = false;
            ScanAndLockMatches(false);
            FinishAfterMove();
        }

        private List<List<MovingDiamondInfo>> GroupAnimInfos(List<MovingDiamondInfo> animInfos)
        {
            var groups = new List<List<MovingDiamondInfo>>();
            var unassigned = new List<MovingDiamondInfo>(animInfos);

            while (unassigned.Count > 0)
            {
                var currentGroup = new List<MovingDiamondInfo>();
                var queue = new Queue<MovingDiamondInfo>();
                
                var first = unassigned[0];
                queue.Enqueue(first);
                unassigned.RemoveAt(0);

                while (queue.Count > 0)
                {
                    var info = queue.Dequeue();
                    currentGroup.Add(info);

                    for (int i = unassigned.Count - 1; i >= 0; i--)
                    {
                        var other = unassigned[i];
                        if (SharesLocation(info, other))
                        {
                            queue.Enqueue(other);
                            unassigned.RemoveAt(i);
                        }
                    }
                }
                groups.Add(currentGroup);
            }

            return groups;
        }

        private bool SharesLocation(MovingDiamondInfo a, MovingDiamondInfo b)
        {
            if (a.StartIsOnShelf == b.EndIsOnShelf)
            {
                if (a.StartIsOnShelf && a.StartShelfIndex == b.EndShelfIndex) return true;
                if (!a.StartIsOnShelf && a.StartCoord.Equals(b.EndCoord)) return true;
            }
            if (a.EndIsOnShelf == b.StartIsOnShelf)
            {
                if (a.EndIsOnShelf && a.EndShelfIndex == b.StartShelfIndex) return true;
                if (!a.EndIsOnShelf && a.EndCoord.Equals(b.StartCoord)) return true;
            }
            return false;
        }

        private IEnumerator AnimateHintGroups(List<List<MovingDiamondInfo>> groups, float duration, float staggerDelay)
        {
            if (groups.Count == 0) yield break;

            float totalDuration = duration + (groups.Count - 1) * staggerDelay;
            float elapsed = 0f;

            var allViews = new Dictionary<DiamondState, DiamondView>();
            var hasPlayedSound = new bool[groups.Count];
            var hasLanded = new Dictionary<DiamondState, bool>();

            foreach (var group in groups)
            {
                foreach (var info in group)
                {
                    movingDiamondsSet.Add(info.Diamond);
                    var view = diamondViews[info.Diamond];
                    allViews[info.Diamond] = view;
                    view.SetFlightSortingOrder();
                    hasLanded[info.Diamond] = false;
                }
            }

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < groups.Count; i++)
                {
                    float tStart = i * staggerDelay;
                    if (elapsed < tStart) continue;

                    if (!hasPlayedSound[i])
                    {
                        audioService.Move();
                        hasPlayedSound[i] = true;
                    }

                    float progress = Mathf.Clamp01((elapsed - tStart) / duration);
                    float t = Mathf.SmoothStep(0f, 1f, progress);

                    foreach (var info in groups[i])
                    {
                        var view = allViews[info.Diamond];
                        
                        Vector3 currentStart = GetWorldPosition(info.StartIsOnShelf, info.StartShelfIndex, info.StartCoord);
                        Vector3 currentEnd = GetWorldPosition(info.EndIsOnShelf, info.EndShelfIndex, info.EndCoord);
                        Vector3 currentStartScale = info.StartIsOnShelf ? Vector3.one * ShelfCellScale : Vector3.one * boardScale;
                        Vector3 currentEndScale = info.EndIsOnShelf ? Vector3.one * ShelfCellScale : Vector3.one * boardScale;

                        if (progress > 0f && progress < 1f)
                        {
                            view.transform.position = Vector3.Lerp(currentStart, currentEnd, t);
                            view.transform.localScale = Vector3.Lerp(currentStartScale, currentEndScale, t);
                        }
                        else if (progress >= 1f && !hasLanded[info.Diamond])
                        {
                            view.transform.position = currentEnd;
                            view.transform.localScale = currentEndScale;
                            LandDiamond(info, view);
                            hasLanded[info.Diamond] = true;
                        }
                    }
                }

                yield return null;
            }

            foreach (var group in groups)
            {
                foreach (var info in group)
                {
                    var view = allViews[info.Diamond];
                    view.transform.position = GetWorldPosition(info.EndIsOnShelf, info.EndShelfIndex, info.EndCoord);
                    view.transform.localScale = info.EndIsOnShelf ? Vector3.one * ShelfCellScale : Vector3.one * boardScale;

                    if (!hasLanded[info.Diamond])
                    {
                        LandDiamond(info, view);
                        hasLanded[info.Diamond] = true;
                    }
                    else
                    {
                        view.SetSortingOrder(info.EndIsOnShelf);
                    }
                    
                    movingDiamondsSet.Remove(info.Diamond);
                }
            }
        }

        private DiamondState FindUnsolvedGem(
            int colorCode, 
            CellState excludeTargetCell, 
            bool prioritizeShelf, 
            bool shelfOnly = false,
            Dictionary<DiamondState, (bool isOnShelf, int shelfIdx, GridCoord coord)> initialStates = null)
        {
            if (prioritizeShelf || shelfOnly)
            {
                for (int i = 0; i < shelf.Length; i++)
                {
                    if (shelf[i] != null && shelf[i].ColorCode == colorCode)
                    {
                        if (shelfOnly && initialStates != null)
                        {
                            if (initialStates.TryGetValue(shelf[i], out var initial) && initial.isOnShelf)
                            {
                                return shelf[i];
                            }
                        }
                        else
                        {
                            return shelf[i];
                        }
                    }
                }
            }

            if (shelfOnly)
            {
                return null;
            }

            for (int r = 0; r < level.Height; r++)
            {
                for (int c = 0; c < level.Width; c++)
                {
                    var cell = board[r, c];
                    if (cell == null || cell == excludeTargetCell) continue;
                    
                    var occ = cell.Occupant;
                    if (occ != null && occ.ColorCode == colorCode)
                    {
                        if (cell.TargetColor != colorCode)
                        {
                            return occ;
                        }
                    }
                }
            }

            if (!prioritizeShelf)
            {
                for (int i = 0; i < shelf.Length; i++)
                {
                    if (shelf[i] != null && shelf[i].ColorCode == colorCode)
                    {
                        return shelf[i];
                    }
                }
            }

            return null;
        }

        private CellState FindEmptyBoardCell()
        {
            for (int r = 0; r < level.Height; r++)
            {
                for (int c = 0; c < level.Width; c++)
                {
                    var cell = board[r, c];
                    if (cell != null && cell.TargetColor != GemColorCode.Blank && cell.Occupant == null)
                    {
                        return cell;
                    }
                }
            }
            return null;
        }

        private void CreateAreaHintVisuals()
        {
            if (areaHintLineRenderer != null) return;
            var go = new GameObject("AreaHintBox");
            go.transform.SetParent(transform);
            areaHintLineRenderer = go.AddComponent<LineRenderer>();
            areaHintLineRenderer.positionCount = 5;
            areaHintLineRenderer.loop = true;
            areaHintLineRenderer.startWidth = 0.1f;
            areaHintLineRenderer.endWidth = 0.1f;
            areaHintLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            areaHintLineRenderer.startColor = new Color(0f, 1f, 1f, 0.8f);
            areaHintLineRenderer.endColor = new Color(0f, 1f, 1f, 0.8f);
            areaHintLineRenderer.sortingOrder = 100;
            areaHintLineRenderer.enabled = false;
        }

        private void UpdateAreaHintBox(Vector2 worldPos)
        {
            if (areaHintLineRenderer == null) return;
            
            float scaledCellSize = CellSize * boardScale;
            int col = Mathf.RoundToInt((worldPos.x - boardLeftX) / scaledCellSize);
            int row = Mathf.RoundToInt((boardTopY - worldPos.y) / scaledCellSize);
            
            int boxSize = 10;
            int halfSize = boxSize / 2;
            
            int startCol = Mathf.Clamp(col - halfSize, 0, Mathf.Max(0, level.Width - boxSize));
            int startRow = Mathf.Clamp(row - halfSize, 0, Mathf.Max(0, level.Height - boxSize));
            int endCol = Mathf.Min(startCol + boxSize, level.Width);
            int endRow = Mathf.Min(startRow + boxSize, level.Height);
            
            Vector3 topLeft = BoardPosition(startRow, startCol) + new Vector3(-scaledCellSize/2, scaledCellSize/2, 0);
            Vector3 bottomRight = BoardPosition(endRow - 1, endCol - 1) + new Vector3(scaledCellSize/2, -scaledCellSize/2, 0);
            
            areaHintLineRenderer.SetPosition(0, new Vector3(topLeft.x, topLeft.y, 0));
            areaHintLineRenderer.SetPosition(1, new Vector3(bottomRight.x, topLeft.y, 0));
            areaHintLineRenderer.SetPosition(2, new Vector3(bottomRight.x, bottomRight.y, 0));
            areaHintLineRenderer.SetPosition(3, new Vector3(topLeft.x, bottomRight.y, 0));
            areaHintLineRenderer.SetPosition(4, new Vector3(topLeft.x, topLeft.y, 0));
            areaHintLineRenderer.enabled = true;
            
            areaHintBounds = new Rect(topLeft.x, bottomRight.y, bottomRight.x - topLeft.x, topLeft.y - bottomRight.y);
        }
    }
}
