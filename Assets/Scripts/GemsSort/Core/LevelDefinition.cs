using System;
using UnityEngine;

namespace GemsSort.Core
{
    [Serializable]
    public struct PaletteColor
    {
        public int Code;
        public Color Color;

        public PaletteColor(int code, string htmlColor)
        {
            Code = code;
            ColorUtility.TryParseHtmlString(htmlColor, out Color);
        }
    }

    public sealed class LevelDefinition
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int ShelfSlots;
        public readonly int[,] Target;
        public readonly PaletteColor[] Palette;

        public LevelDefinition(int width, int height, int shelfSlots, int[,] target, params PaletteColor[] palette)
        {
            Width = width;
            Height = height;
            ShelfSlots = shelfSlots;
            Target = target;
            Palette = palette ?? Array.Empty<PaletteColor>();
        }
    }
}
