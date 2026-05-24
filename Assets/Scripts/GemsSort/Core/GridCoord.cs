using System;

namespace GemsSort.Core
{
    [Serializable]
    public struct GridCoord
    {
        public int Row;
        public int Col;

        public GridCoord(int row, int col)
        {
            Row = row;
            Col = col;
        }
    }
}
