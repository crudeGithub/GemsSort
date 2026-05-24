using System.Collections.Generic;
using GemsSort.Core;
using GemsSort.Views;

namespace GemsSort.Game
{
    public sealed partial class GemsSortGameController
    {
        private void SelectBoardGroup(DiamondState origin)
        {
            ClearSelection();
            selectedFromShelf = false;

            var visited = new bool[level.Height, level.Width];
            var queue = new Queue<CellState>();
            queue.Enqueue(GetCell(origin.Coord));
            visited[origin.Coord.Row, origin.Coord.Col] = true;

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var diamond = cell.Occupant;
                if (diamond == null || diamond.Locked || diamond.ColorCode != origin.ColorCode)
                {
                    continue;
                }

                selected.Add(diamond);
                foreach (var neighbor in Neighbors(cell.Coord))
                {
                    if (neighbor == null || visited[neighbor.Coord.Row, neighbor.Coord.Col])
                    {
                        continue;
                    }

                    visited[neighbor.Coord.Row, neighbor.Coord.Col] = true;
                    if (neighbor.Occupant != null && !neighbor.Occupant.Locked && neighbor.Occupant.ColorCode == origin.ColorCode)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            UpdateSelectionVisuals();
        }

        private void SelectShelfColor(int colorCode)
        {
            ClearSelection();
            selectedFromShelf = true;
            for (int i = 0; i < shelf.Length; i++)
            {
                if (shelf[i] != null && shelf[i].ColorCode == colorCode)
                {
                    selected.Add(shelf[i]);
                }
            }

            UpdateSelectionVisuals();
        }

        private List<CellState> FindConnectedEmptyTargets(CellState start, int colorCode)
        {
            var result = new List<CellState>();
            var visited = new bool[level.Height, level.Width];
            var queue = new Queue<CellState>();
            queue.Enqueue(start);
            visited[start.Coord.Row, start.Coord.Col] = true;

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (cell.Occupant != null || cell.TargetColor != colorCode)
                {
                    continue;
                }

                result.Add(cell);
                foreach (var neighbor in Neighbors(cell.Coord))
                {
                    if (neighbor == null || visited[neighbor.Coord.Row, neighbor.Coord.Col])
                    {
                        continue;
                    }

                    visited[neighbor.Coord.Row, neighbor.Coord.Col] = true;
                    if (neighbor.Occupant == null && neighbor.TargetColor == colorCode)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return result;
        }

        private IEnumerable<CellState> Neighbors(GridCoord coord)
        {
            yield return TryCell(coord.Row - 1, coord.Col);
            yield return TryCell(coord.Row + 1, coord.Col);
            yield return TryCell(coord.Row, coord.Col - 1);
            yield return TryCell(coord.Row, coord.Col + 1);
            yield return TryCell(coord.Row - 1, coord.Col - 1);
            yield return TryCell(coord.Row - 1, coord.Col + 1);
            yield return TryCell(coord.Row + 1, coord.Col - 1);
            yield return TryCell(coord.Row + 1, coord.Col + 1);
        }

        private CellState TryCell(int row, int col)
        {
            if (row < 0 || row >= level.Height || col < 0 || col >= level.Width)
            {
                return null;
            }

            return board[row, col];
        }

        private CellState GetCell(GridCoord coord)
        {
            return board[coord.Row, coord.Col];
        }

        private void ScanAndLockMatches(bool playShine = true)
        {
            foreach (var pair in diamondViews)
            {
                var diamond = pair.Key;
                if (diamond.IsOnShelf || diamond.Locked)
                {
                    continue;
                }

                var cell = GetCell(diamond.Coord);
                if (cell.TargetColor != diamond.ColorCode)
                {
                    continue;
                }

                diamond.Locked = true;
                if (playShine)
                {
                    pair.Value.PlayCrossShine();
                }
            }
        }

        private void FinishAfterMove()
        {
            selected.RemoveAll(diamond => diamond.Locked);
            if (selected.Count == 0)
            {
                selectedFromShelf = false;
            }

            UpdateSelectionVisuals();
            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            var colorTotals = new Dictionary<int, int>();
            var colorCorrect = new Dictionary<int, List<DiamondView>>();
            bool win = true;

            for (int row = 0; row < level.Height; row++)
            {
                for (int col = 0; col < level.Width; col++)
                {
                    var cell = board[row, col];
                    if (cell == null)
                    {
                        continue;
                    }

                    if (!colorTotals.ContainsKey(cell.TargetColor))
                    {
                        colorTotals[cell.TargetColor] = 0;
                        colorCorrect[cell.TargetColor] = new List<DiamondView>();
                    }

                    colorTotals[cell.TargetColor]++;
                    if (cell.Occupant != null && cell.Occupant.ColorCode == cell.TargetColor)
                    {
                        colorCorrect[cell.TargetColor].Add(diamondViews[cell.Occupant]);
                    }
                    else
                    {
                        win = false;
                    }
                }
            }

            if (ShelfHasDiamonds())
            {
                win = false;
            }

            if (!win)
            {
                ShineNewlyCompletedColors(colorTotals, colorCorrect);
                return;
            }

            levelComplete = true;
            ClearSelection();
            audioService.Win();
            effects.ShineSweep(diamondViews.Values);
            BeginWinFlow();
        }

        private bool ShelfHasDiamonds()
        {
            for (int i = 0; i < shelf.Length; i++)
            {
                if (shelf[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void ShineNewlyCompletedColors(Dictionary<int, int> colorTotals, Dictionary<int, List<DiamondView>> colorCorrect)
        {
            foreach (var pair in colorTotals)
            {
                int color = pair.Key;
                if (completedColors.Contains(color) || colorCorrect[color].Count != pair.Value)
                {
                    continue;
                }

                completedColors.Add(color);
                effects.ShineSweep(colorCorrect[color]);
                audioService.ColorSorted();
            }
        }

        public void InitializeCompletedColors()
        {
            completedColors.Clear();
            var colorTotals = new Dictionary<int, int>();
            var colorCorrect = new Dictionary<int, int>();

            for (int row = 0; row < level.Height; row++)
            {
                for (int col = 0; col < level.Width; col++)
                {
                    var cell = board[row, col];
                    if (cell == null)
                    {
                        continue;
                    }

                    if (!colorTotals.ContainsKey(cell.TargetColor))
                    {
                        colorTotals[cell.TargetColor] = 0;
                        colorCorrect[cell.TargetColor] = 0;
                    }

                    colorTotals[cell.TargetColor]++;
                    if (cell.Occupant != null && cell.Occupant.ColorCode == cell.TargetColor)
                    {
                        colorCorrect[cell.TargetColor]++;
                    }
                }
            }

            foreach (var pair in colorTotals)
            {
                int color = pair.Key;
                if (colorCorrect[color] == pair.Value)
                {
                    completedColors.Add(color);
                }
            }
        }

        private void UpdateSelectionVisuals()
        {
            foreach (var pair in diamondViews)
            {
                pair.Value.SetSelected(selected.Contains(pair.Key));
                pair.Value.SetLocked(pair.Key.Locked);

                if (!movingDiamondsSet.Contains(pair.Key))
                {
                    pair.Value.SetSortingOrder(pair.Key.IsOnShelf);
                }
            }
        }

        private void ClearSelection()
        {
            selected.Clear();
            selectedFromShelf = false;
            UpdateSelectionVisuals();
        }
    }
}
