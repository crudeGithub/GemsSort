using System.Collections;
using System.Collections.Generic;
using GemsSort.Core;
using GemsSort.Views;
using UnityEngine;

namespace GemsSort.Game
{
    public sealed partial class GemsSortGameController
    {
        private IEnumerator MoveSelectedToShelf()
        {
            var freeSlots = new List<int>();
            for (int i = 0; i < shelf.Length; i++)
            {
                if (shelf[i] == null)
                {
                    freeSlots.Add(i);
                }
            }

            int moveCount = Mathf.Min(freeSlots.Count, selected.Count);
            if (moveCount == 0)
            {
                audioService.Error();
                yield break;
            }

            moving = true;
            var movingDiamonds = selected.GetRange(0, moveCount);

            var originalCoords = new Dictionary<DiamondState, GridCoord>();
            for (int i = 0; i < movingDiamonds.Count; i++)
            {
                originalCoords[movingDiamonds[i]] = movingDiamonds[i].Coord;
            }

            var originalShelfIndices = new Dictionary<DiamondState, int>();
            var shelfDiamonds = new List<DiamondState>();
            for (int i = 0; i < shelf.Length; i++)
            {
                if (shelf[i] != null)
                {
                    var diamond = shelf[i];
                    originalShelfIndices[diamond] = diamond.ShelfIndex;
                    shelfDiamonds.Add(diamond);
                    shelf[i] = null;
                }
            }

            for (int i = 0; i < movingDiamonds.Count; i++)
            {
                var diamond = movingDiamonds[i];
                GetCell(diamond.Coord).Occupant = null;
                diamond.Coord = new GridCoord(-1, -1);
                diamond.ShelfIndex = -1;
            }

            int insertIndex = FindShelfInsertIndex(shelfDiamonds, movingDiamonds[0].ColorCode);
            shelfDiamonds.InsertRange(insertIndex, movingDiamonds);

            var animInfos = new List<MovingDiamondInfo>();
            for (int i = 0; i < shelfDiamonds.Count; i++)
            {
                var diamond = shelfDiamonds[i];
                bool isNewToShelf = originalCoords.ContainsKey(diamond);
                int startShelfIndex = originalShelfIndices.TryGetValue(diamond, out int originalShelfIndex) ? originalShelfIndex : -1;

                diamond.Coord = new GridCoord(-1, -1);
                diamond.ShelfIndex = i;
                shelf[i] = diamond;

                if (diamondViews.TryGetValue(diamond, out var view))
                {
                    view.SetSortingOrder(true);
                }

                if (!isNewToShelf && startShelfIndex == i)
                {
                    continue;
                }

                animInfos.Add(new MovingDiamondInfo
                {
                    Diamond = diamond,
                    StartIsOnShelf = !isNewToShelf,
                    StartShelfIndex = startShelfIndex,
                    StartCoord = isNewToShelf ? originalCoords[diamond] : new GridCoord(-1, -1),
                    EndIsOnShelf = true,
                    EndShelfIndex = i
                });
            }

            selected.RemoveRange(0, moveCount);
            selectedFromShelf = false;
            UpdateSelectionVisuals();

            yield return AnimateDiamonds(animInfos, ShelfMoveDuration, ShelfMoveStaggerDelay, true);

            moving = false;
            ScanAndLockMatches(false);
            FinishAfterMove();
        }

        private IEnumerator MoveSelectedToBoard(List<CellState> targets)
        {
            int moveCount = Mathf.Min(targets.Count, selected.Count);
            if (moveCount == 0)
            {
                audioService.Error();
                yield break;
            }

            moving = true;
            var movingDiamonds = selected.GetRange(0, moveCount);
            var animInfos = new List<MovingDiamondInfo>();

            bool shelfChanged = false;
            for (int i = 0; i < movingDiamonds.Count; i++)
            {
                var diamond = movingDiamonds[i];
                animInfos.Add(new MovingDiamondInfo
                {
                    Diamond = diamond,
                    StartIsOnShelf = diamond.IsOnShelf,
                    StartShelfIndex = diamond.ShelfIndex,
                    StartCoord = diamond.Coord,
                    EndIsOnShelf = false,
                    EndCoord = targets[i].Coord
                });

                if (diamond.IsOnShelf)
                {
                    shelf[diamond.ShelfIndex] = null;
                    diamond.ShelfIndex = -1;
                    shelfChanged = true;
                }
                else
                {
                    GetCell(diamond.Coord).Occupant = null;
                }

                diamond.Coord = targets[i].Coord;
                targets[i].Occupant = diamond;

                if (diamondViews.TryGetValue(diamond, out var view))
                {
                    view.SetSortingOrder(false);
                }
            }

            selected.RemoveRange(0, moveCount);
            if (selected.Count == 0)
            {
                selectedFromShelf = false;
            }

            UpdateSelectionVisuals();
            yield return AnimateDiamonds(animInfos, BoardMoveDuration, BoardMoveStaggerDelay, true);

            if (shelfChanged)
            {
                var settleInfos = CompactShelfAndCreateSettleAnimations();
                if (settleInfos.Count > 0)
                {
                    yield return AnimateDiamonds(settleInfos, ShelfSettleDuration, 0f, false);
                }
            }

            moving = false;
            ScanAndLockMatches(false);
            FinishAfterMove();
        }

        private List<MovingDiamondInfo> CompactShelfAndCreateSettleAnimations()
        {
            var originalShelfIndices = new Dictionary<DiamondState, int>();
            var shelfDiamonds = new List<DiamondState>();
            for (int i = 0; i < shelf.Length; i++)
            {
                if (shelf[i] != null)
                {
                    var diamond = shelf[i];
                    originalShelfIndices[diamond] = diamond.ShelfIndex;
                    shelfDiamonds.Add(diamond);
                    shelf[i] = null;
                }
            }

            var animInfos = new List<MovingDiamondInfo>();
            for (int i = 0; i < shelfDiamonds.Count; i++)
            {
                var diamond = shelfDiamonds[i];
                int startIndex = originalShelfIndices[diamond];

                diamond.Coord = new GridCoord(-1, -1);
                diamond.ShelfIndex = i;
                shelf[i] = diamond;

                if (diamondViews.TryGetValue(diamond, out var view))
                {
                    view.SetSortingOrder(true);
                }

                if (startIndex == i)
                {
                    continue;
                }

                animInfos.Add(new MovingDiamondInfo
                {
                    Diamond = diamond,
                    StartIsOnShelf = true,
                    StartShelfIndex = startIndex,
                    StartCoord = new GridCoord(-1, -1),
                    EndIsOnShelf = true,
                    EndShelfIndex = i
                });
            }

            return animInfos;
        }

        private static int FindShelfInsertIndex(List<DiamondState> shelfDiamonds, int colorCode)
        {
            int insertIndex = shelfDiamonds.Count;
            for (int i = 0; i < shelfDiamonds.Count; i++)
            {
                if (shelfDiamonds[i].ColorCode == colorCode)
                {
                    insertIndex = i + 1;
                }
            }

            return insertIndex;
        }

        private IEnumerator AnimateDiamonds(List<MovingDiamondInfo> animInfos, float duration, float staggerDelay, bool playSound = true)
        {
            if (animInfos.Count == 0)
            {
                yield break;
            }

            duration = Mathf.Max(0.01f, duration);
            staggerDelay = Mathf.Max(0f, staggerDelay);

            var views = new DiamondView[animInfos.Count];
            var hasLanded = new bool[animInfos.Count];
            var hasPlayedSound = new bool[animInfos.Count];

            for (int i = 0; i < animInfos.Count; i++)
            {
                var info = animInfos[i];
                movingDiamondsSet.Add(info.Diamond);
                views[i] = diamondViews[info.Diamond];
                views[i].SetFlightSortingOrder();
            }

            float totalDuration = duration + (animInfos.Count - 1) * staggerDelay;
            float elapsed = 0f;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < animInfos.Count; i++)
                {
                    float tStart = i * staggerDelay;
                    if (playSound && elapsed >= tStart && !hasPlayedSound[i])
                    {
                        audioService.Move();
                        hasPlayedSound[i] = true;
                    }

                    float progress = Mathf.Clamp01((elapsed - tStart) / duration);
                    float t = Mathf.SmoothStep(0f, 1f, progress);

                    var info = animInfos[i];
                    var view = views[i];

                    Vector3 currentStart = GetWorldPosition(info.StartIsOnShelf, info.StartShelfIndex, info.StartCoord);
                    Vector3 currentEnd = GetWorldPosition(info.EndIsOnShelf, info.EndShelfIndex, info.EndCoord);
                    Vector3 currentStartScale = info.StartIsOnShelf ? Vector3.one * ShelfCellScale : Vector3.one * boardScale;
                    Vector3 currentEndScale = info.EndIsOnShelf ? Vector3.one * ShelfCellScale : Vector3.one * boardScale;

                    view.transform.position = Vector3.Lerp(currentStart, currentEnd, t);
                    view.transform.localScale = Vector3.Lerp(currentStartScale, currentEndScale, t);

                    if (progress >= 1f && !hasLanded[i])
                    {
                        LandDiamond(info, view);
                        hasLanded[i] = true;
                    }
                }

                yield return null;
            }

            for (int i = 0; i < animInfos.Count; i++)
            {
                var info = animInfos[i];
                var view = views[i];
                view.transform.position = GetWorldPosition(info.EndIsOnShelf, info.EndShelfIndex, info.EndCoord);
                view.transform.localScale = info.EndIsOnShelf ? Vector3.one * ShelfCellScale : Vector3.one * boardScale;

                if (!hasLanded[i])
                {
                    LandDiamond(info, view);
                }
                else
                {
                    view.SetSortingOrder(info.EndIsOnShelf);
                }
            }

            foreach (var info in animInfos)
            {
                movingDiamondsSet.Remove(info.Diamond);
            }
        }

        private void LandDiamond(MovingDiamondInfo info, DiamondView view)
        {
            view.SetSortingOrder(info.EndIsOnShelf);
            var diamond = info.Diamond;
            if (info.EndIsOnShelf || diamond.Locked)
            {
                return;
            }

            var cell = GetCell(info.EndCoord);
            if (cell.TargetColor != diamond.ColorCode)
            {
                return;
            }

            diamond.Locked = true;
            view.SetLocked(true);
            view.PlayCrossShine();
        }

        private struct MovingDiamondInfo
        {
            public DiamondState Diamond;
            public bool StartIsOnShelf;
            public int StartShelfIndex;
            public GridCoord StartCoord;
            public bool EndIsOnShelf;
            public int EndShelfIndex;
            public GridCoord EndCoord;
        }
    }
}
