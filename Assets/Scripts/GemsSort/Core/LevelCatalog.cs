using System;
using System.Collections.Generic;
using GemsSort.Game;
using UnityEngine;

namespace GemsSort.Core
{
    public static class LevelCatalog
    {
        private static LevelDefinition[] cachedLevels;

        public static LevelDefinition[] Levels
        {
            get
            {
                if (cachedLevels == null)
                {
                    cachedLevels = LoadFromResources();
                }

                return cachedLevels;
            }
        }

        public static void Reload()
        {
            cachedLevels = LoadFromResources();
        }

        public static LevelDefinition[] Load(GemsSortGameSettings settings)
        {
            if (settings != null && settings.LevelFiles != null && settings.LevelFiles.Length > 0)
            {
                return LoadFromAssets(settings.LevelFiles);
            }

            return Levels;
        }

        private static LevelDefinition[] LoadFromResources()
        {
            TextAsset[] assets = Resources.LoadAll<TextAsset>("Levels");
            Array.Sort(assets, (left, right) => string.CompareOrdinal(left.name, right.name));

            var levels = new List<LevelDefinition>();
            foreach (var asset in assets)
            {
                LevelJson json = JsonUtility.FromJson<LevelJson>(asset.text);
                if (json == null || json.targetRows == null || json.targetRows.Length == 0)
                {
                    Debug.LogError("Invalid level JSON: " + asset.name);
                    continue;
                }

                levels.Add(FromJson(json, asset.name));
            }

            if (levels.Count == 0)
            {
                Debug.LogError("No level JSON files found in Resources/Levels.");
            }

            return levels.ToArray();
        }

        private static LevelDefinition[] LoadFromAssets(TextAsset[] assets)
        {
            var levels = new List<LevelDefinition>();
            for (int i = 0; i < assets.Length; i++)
            {
                var asset = assets[i];
                if (asset == null)
                {
                    Debug.LogError("Level list contains an empty slot at index " + i + ".");
                    continue;
                }

                LevelJson json = JsonUtility.FromJson<LevelJson>(asset.text);
                if (json == null || json.targetRows == null || json.targetRows.Length == 0)
                {
                    Debug.LogError("Invalid level JSON: " + asset.name);
                    continue;
                }

                levels.Add(FromJson(json, asset.name));
            }

            if (levels.Count == 0)
            {
                Debug.LogError("No valid level JSON files are assigned in Gems Sort game settings.");
            }

            return levels.ToArray();
        }

        private static LevelDefinition FromJson(LevelJson json, string assetName)
        {
            var palette = new PaletteColor[json.palette == null ? 0 : json.palette.Length];
            for (int i = 0; i < palette.Length; i++)
            {
                palette[i] = new PaletteColor(json.palette[i].code, json.palette[i].color);
            }

            int[,] target = Grid(json.targetRows);
            int width = json.width > 0 ? json.width : target.GetLength(1);
            int height = json.height > 0 ? json.height : target.GetLength(0);
            if (width != target.GetLength(1) || height != target.GetLength(0))
            {
                Debug.LogError("Level JSON dimensions do not match targetRows: " + assetName);
            }

            return new LevelDefinition(width, height, json.shelfSlots > 0 ? json.shelfSlots : 10, target, palette);
        }

        private static int[,] Grid(string[] rows)
        {
            int height = rows.Length;
            string[] first = rows[0].Split(',');
            int width = first.Length;
            int[,] values = new int[height, width];
            for (int row = 0; row < height; row++)
            {
                string[] parts = rows[row].Split(',');
                for (int col = 0; col < width; col++)
                {
                    values[row, col] = int.Parse(parts[col]);
                }
            }

            return values;
        }
    }
}
