using System;

namespace GemsSort.Core
{
    [Serializable]
    public sealed class LevelJson
    {
        public int width;
        public int height;
        public int shelfSlots = 10;
        public PaletteJson[] palette;
        public string[] targetRows;
    }

    [Serializable]
    public sealed class PaletteJson
    {
        public int code;
        public string color;
    }
}
