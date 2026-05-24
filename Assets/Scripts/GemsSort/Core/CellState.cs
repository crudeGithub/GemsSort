namespace GemsSort.Core
{
    public sealed class CellState
    {
        public GridCoord Coord;
        public int TargetColor;
        public DiamondState Occupant;
        public bool IsBlank => TargetColor == GemColorCode.Blank;

        public CellState(int row, int col, int targetColor)
        {
            Coord = new GridCoord(row, col);
            TargetColor = targetColor;
        }
    }
}
