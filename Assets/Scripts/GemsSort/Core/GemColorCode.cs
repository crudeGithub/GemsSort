using UnityEngine;

namespace GemsSort.Core
{
    public static class GemColorCode
    {
        public const int Blank = -1;

        public static readonly Color DefaultTile = new Color(0.82f, 0.85f, 0.88f);

        public static Color Resolve(int colorCode, LevelDefinition level)
        {
            if (level != null)
            {
                for (int i = 0; i < level.Palette.Length; i++)
                {
                    if (level.Palette[i].Code == colorCode)
                    {
                        return level.Palette[i].Color;
                    }
                }
            }

            switch (colorCode)
            {
                case 1:
                    return new Color(1f, 0.3f, 0.3f);
                case 2:
                    return new Color(1f, 0.6f, 0f);
                case 3:
                    return new Color(1f, 0.8f, 0f);
                case 4:
                    return new Color(0.2f, 0.8f, 0.2f);
                case 5:
                    return new Color(0.2f, 0.6f, 1f);
                case 6:
                    return new Color(0.48f, 0.55f, 0.62f);
                case 7:
                    return new Color(0.6f, 0.4f, 0.2f);
                case 8:
                    return Color.white;
                case 12:
                    return new Color(1f, 0.14f, 0.14f);
                case 13:
                    return new Color(0.9f, 1f, 0.18f);
                default:
                    return DefaultTile;
            }
        }
    }
}
