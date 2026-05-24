using System;
using UnityEngine;

namespace GemsSort.Game
{
    /// <summary>
    /// Persistent player wallet and hint counts.
    /// Each hint type starts with three free uses. When a type runs out the player can
    /// purchase another pack for a fixed coin cost. Levels grant coins on win.
    /// </summary>
    public static class GemsSortInventory
    {
        public enum HintType
        {
            Area,
            Wand,
            Magnet,
            Shelf
        }

        public const int InitialFreeHints = 3;
        public const int HintPackSize = 3;
        public const int HintPackCost = 100;
        public const int LevelWinReward = 50;

        private const string CoinsKey = "GemsSort.Coins";
        private const string AreaKey = "GemsSort.Hints.Area";
        private const string WandKey = "GemsSort.Hints.Wand";
        private const string MagnetKey = "GemsSort.Hints.Magnet";
        private const string ShelfKey = "GemsSort.Hints.Shelf";
        private const string InitFlagKey = "GemsSort.Inventory.Init";

        public static event Action Changed;

        private static bool loaded;
        private static int coins;
        private static int areaHints;
        private static int wandHints;
        private static int magnetHints;
        private static int shelfHints;

        public static int Coins
        {
            get
            {
                EnsureLoaded();
                return coins;
            }
        }

        public static int GetHintCount(HintType type)
        {
            EnsureLoaded();
            switch (type)
            {
                case HintType.Area: return areaHints;
                case HintType.Wand: return wandHints;
                case HintType.Magnet: return magnetHints;
                case HintType.Shelf: return shelfHints;
                default: return 0;
            }
        }

        public static bool TrySpendHint(HintType type)
        {
            EnsureLoaded();
            int current = GetHintCount(type);
            if (current <= 0)
            {
                return false;
            }

            SetHintCount(type, current - 1);
            Save();
            RaiseChanged();
            return true;
        }

        public static bool CanPurchaseHints()
        {
            EnsureLoaded();
            return coins >= HintPackCost;
        }

        public static bool TryPurchaseHintPack(HintType type)
        {
            EnsureLoaded();
            if (coins < HintPackCost)
            {
                return false;
            }

            coins -= HintPackCost;
            SetHintCount(type, GetHintCount(type) + HintPackSize);
            Save();
            RaiseChanged();
            return true;
        }

        public static void AddCoins(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            EnsureLoaded();
            coins = Mathf.Max(0, coins + amount);
            Save();
            RaiseChanged();
        }

        public static void ResetForTesting()
        {
            PlayerPrefs.DeleteKey(CoinsKey);
            PlayerPrefs.DeleteKey(AreaKey);
            PlayerPrefs.DeleteKey(WandKey);
            PlayerPrefs.DeleteKey(MagnetKey);
            PlayerPrefs.DeleteKey(ShelfKey);
            PlayerPrefs.DeleteKey(InitFlagKey);
            loaded = false;
            EnsureLoaded();
            RaiseChanged();
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            if (PlayerPrefs.GetInt(InitFlagKey, 0) == 0)
            {
                coins = 0;
                areaHints = InitialFreeHints;
                wandHints = InitialFreeHints;
                magnetHints = InitialFreeHints;
                shelfHints = InitialFreeHints;
                PlayerPrefs.SetInt(InitFlagKey, 1);
                Save();
                return;
            }

            coins = PlayerPrefs.GetInt(CoinsKey, 0);
            areaHints = PlayerPrefs.GetInt(AreaKey, InitialFreeHints);
            wandHints = PlayerPrefs.GetInt(WandKey, InitialFreeHints);
            magnetHints = PlayerPrefs.GetInt(MagnetKey, InitialFreeHints);
            shelfHints = PlayerPrefs.GetInt(ShelfKey, InitialFreeHints);
        }

        private static void SetHintCount(HintType type, int value)
        {
            value = Mathf.Max(0, value);
            switch (type)
            {
                case HintType.Area: areaHints = value; break;
                case HintType.Wand: wandHints = value; break;
                case HintType.Magnet: magnetHints = value; break;
                case HintType.Shelf: shelfHints = value; break;
            }
        }

        private static void Save()
        {
            PlayerPrefs.SetInt(CoinsKey, coins);
            PlayerPrefs.SetInt(AreaKey, areaHints);
            PlayerPrefs.SetInt(WandKey, wandHints);
            PlayerPrefs.SetInt(MagnetKey, magnetHints);
            PlayerPrefs.SetInt(ShelfKey, shelfHints);
            PlayerPrefs.Save();
        }

        private static void RaiseChanged()
        {
            Changed?.Invoke();
        }
    }
}
