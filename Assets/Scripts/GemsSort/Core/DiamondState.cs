namespace GemsSort.Core
{
    public sealed class DiamondState
    {
        public int ColorCode;
        public GridCoord Coord;
        public int ShelfIndex = -1;
        public bool Locked;
        public bool IsOnShelf => ShelfIndex >= 0;

        public DiamondState(int colorCode, GridCoord coord)
        {
            ColorCode = colorCode;
            Coord = coord;
        }
    }
}
