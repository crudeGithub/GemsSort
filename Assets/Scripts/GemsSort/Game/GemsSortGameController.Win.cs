using System.Collections;
using GemsSort.Views;
using UnityEngine;

namespace GemsSort.Game
{
    public sealed partial class GemsSortGameController
    {
        private bool winFlowRunning;

        private void BeginWinFlow()
        {
            if (winFlowRunning)
            {
                return;
            }

            winFlowRunning = true;
            StartCoroutine(WinFlowRoutine());
        }

        private IEnumerator WinFlowRoutine()
        {
            ResetViewport();

            // Hide gameplay UI while the win flow plays.
            hud.HideGameplayUiForWin();

            // Let the shine sweep that fired in CheckWinCondition finish before reveal.
            yield return new WaitForSeconds(WinShineHoldDuration);

            // Hide shelf cells.
            HideShelf();

            // Switch the board into the pixel-reveal look: hide gems, hide cell fill,
            // and tint each cell's border with its target color.
            foreach (var pair in diamondViews)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetVisible(false);
                }
            }

            foreach (var pair in cellViews)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetRevealMode(true);
                }
            }

            yield return new WaitForSeconds(WinRevealHoldDuration);

            // Coin animation - spawn a few coins at screen center that fly to the
            // coin counter, granting the level reward and refreshing the HUD.
            yield return hud.PlayCoinRewardAnimation(GemsSortInventory.LevelWinReward);

            // Confetti / celebrate effect.
            effects.PlayWinCelebration();

            // Save level completion progress!
            int maxSolved = PlayerPrefs.GetInt("GemsSort.MaxSolvedLevel", -1);
            if (levelIndex > maxSolved)
            {
                PlayerPrefs.SetInt("GemsSort.MaxSolvedLevel", levelIndex);
                PlayerPrefs.Save();
            }

            // Final "Next Level" button takes over.
            hud.ShowWinNextButton(levelIndex >= levels.Length - 1);

            winFlowRunning = false;
        }

        private void HideShelf()
        {
            if (shelfRoot != null)
            {
                shelfRoot.gameObject.SetActive(false);
            }
        }

        private void ShowShelf()
        {
            if (shelfRoot != null)
            {
                shelfRoot.gameObject.SetActive(true);
            }
        }

        private void ResetWinVisuals()
        {
            winFlowRunning = false;
            ShowShelf();

            foreach (var pair in cellViews)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetRevealMode(false);
                }
            }

            foreach (var pair in diamondViews)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetVisible(true);
                }
            }
        }

        private const float WinShineHoldDuration = 0.55f;
        private const float WinRevealHoldDuration = 0.45f;
    }
}
