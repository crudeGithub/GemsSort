using System;
using System.Collections.Generic;
using GemsSort.Core;
using UnityEngine;

namespace GemsSort.Services
{
    public static class LevelScrambler
    {
        private struct ActiveCell
        {
            public int Row;
            public int Col;
            public int Color;
        }

        public static int[,] CreateStartGrid(LevelDefinition level)
        {
            var activeCells = GetActiveCells(level);
            var colorCounts = CountColors(activeCells);
            var start = new int[level.Height, level.Width];
            for (int row = 0; row < level.Height; row++)
            {
                for (int col = 0; col < level.Width; col++)
                {
                    start[row, col] = GemColorCode.Blank;
                }
            }

            if (colorCounts.Count <= 1)
            {
                foreach (var cell in activeCells)
                {
                    start[cell.Row, cell.Col] = cell.Color;
                }

                return start;
            }

            List<int> gemColors = null;
            bool solved = true;
            int attempts = 0;
            while (solved && attempts < 50)
            {
                gemColors = BuildClusteredColors(colorCounts);
                var sortedIndices = BuildSpatialOrder(activeCells, UnityEngine.Random.Range(0, 4));
                var mapped = new int[activeCells.Count];
                for (int i = 0; i < gemColors.Count; i++)
                {
                    mapped[sortedIndices[i]] = gemColors[i];
                }

                gemColors = new List<int>(mapped);
                solved = IsSolved(activeCells, gemColors);
                attempts++;
            }

            if (gemColors == null || solved)
            {
                gemColors = BuildClusteredColors(colorCounts);
                Shuffle(gemColors);
            }

            for (int i = 0; i < activeCells.Count; i++)
            {
                start[activeCells[i].Row, activeCells[i].Col] = gemColors[i];
            }

            return start;
        }

        private static List<ActiveCell> GetActiveCells(LevelDefinition level)
        {
            var cells = new List<ActiveCell>();
            for (int row = 0; row < level.Height; row++)
            {
                for (int col = 0; col < level.Width; col++)
                {
                    int color = level.Target[row, col];
                    if (color != GemColorCode.Blank)
                    {
                        cells.Add(new ActiveCell { Row = row, Col = col, Color = color });
                    }
                }
            }

            return cells;
        }

        private static Dictionary<int, int> CountColors(List<ActiveCell> cells)
        {
            var counts = new Dictionary<int, int>();
            foreach (var cell in cells)
            {
                counts.TryGetValue(cell.Color, out int count);
                counts[cell.Color] = count + 1;
            }

            return counts;
        }

        private static List<int> BuildClusteredColors(Dictionary<int, int> colorCounts)
        {
            var colors = new List<int>(colorCounts.Keys);
            Shuffle(colors);
            var result = new List<int>();
            foreach (int color in colors)
            {
                for (int i = 0; i < colorCounts[color]; i++)
                {
                    result.Add(color);
                }
            }

            return result;
        }

        private static List<int> BuildSpatialOrder(List<ActiveCell> cells, int sortMethod)
        {
            var indices = new List<int>();
            for (int i = 0; i < cells.Count; i++)
            {
                indices.Add(i);
            }

            indices.Sort((left, right) => CompareCells(cells[left], cells[right], sortMethod));
            return indices;
        }

        private static int CompareCells(ActiveCell a, ActiveCell b, int sortMethod)
        {
            switch (sortMethod)
            {
                case 0:
                    return ComparePair(a.Row, b.Row, a.Col, b.Col);
                case 1:
                    return ComparePair(a.Col, b.Col, a.Row, b.Row);
                case 2:
                    return ComparePair(a.Row + a.Col, b.Row + b.Col, a.Row, b.Row);
                default:
                    return ComparePair(a.Row - a.Col, b.Row - b.Col, a.Row, b.Row);
            }
        }

        private static int ComparePair(int primaryA, int primaryB, int secondaryA, int secondaryB)
        {
            int primary = primaryA.CompareTo(primaryB);
            return primary != 0 ? primary : secondaryA.CompareTo(secondaryB);
        }

        private static bool IsSolved(List<ActiveCell> cells, List<int> gemColors)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].Color != gemColors[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void Shuffle<T>(IList<T> values)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }
        }
    }
}
