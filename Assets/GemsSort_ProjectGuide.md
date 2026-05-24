# Gems Sort Project Guide

## Main Scene Object

`Gems Sort Game` is the scene entry point. Select it in the Hierarchy to see:

- `Game Data`: the `GemsSortGameSettings` asset with the ordered level list and layout tuning.
- `Prefab-backed Views`: the board cell, diamond, and shelf prefabs.
- `Scene Roots`: runtime containers for generated board cells, shelf slots, and diamonds.
- `Scene Services`: camera, audio, and visual effects.
- `Scene UI`: the real Canvas HUD used for level text, restart, and next-level flow.

All scene references are normal serialized fields on the `Gems Sort Game` component. Assign the settings asset, prefabs, scene roots, camera, services, and HUD directly in the Inspector.

## Levels

Level files live in `Assets/Resources/Levels`.

The ordered level list and layout tuning live in:

`Assets/GemsSort/Settings/GemsSortGameSettings.asset`

Edit that asset when you want to change level order, camera fitting, board zoom/pan feel, or shelf size.

## Authored Assets

Final prefabs, sprites, and settings live in `Assets/GemsSort`:

- `Prefabs`: cell, diamond, and shelf-slot prefabs with assignable renderer references.
- `Settings`: the main `GemsSortGameSettings` asset.
- `Sprites`: editable source sprites used by the prefabs.

## UI

The HUD is a normal Unity Canvas named `Gems Sort UI`.

It contains:

- top level label
- restart button
- legacy completion panel (hidden during the win flow but still serialized for fallback)
- next-level button (legacy panel button, plus a runtime "Next Level" button shown after the win flow)
- coin counter and coin icon (auto-built at runtime if not pre-wired)
- hint count badges on each hint button (auto-built if not pre-wired)
- universal `PurchaseDialog` shown when a hint type runs out (auto-built if not pre-wired)

The `GemsSortHud` component owns only UI wiring. Game rules stay in `GemsSortGameController`.

## Coins, Hints, And Purchase Dialog

`GemsSortInventory` is a static persistent wallet stored in PlayerPrefs.

- Players start with three uses of each hint type (Area, Wand, Magnet) and zero coins.
- Each level win grants `LevelWinReward` coins (50 by default).
- When a hint type runs out, tapping the hint button opens the universal
  `PurchaseDialog`. The dialog title and description swap based on the hint type.
- Buying a pack costs `HintPackCost` coins (100) and grants `HintPackSize` uses (3).
- The HUD listens to `GemsSortInventory.Changed` to refresh coin and hint badges.

## Editor Tools

The `Tools / Gems Sort` menu (added by `GemsSortUiBuilder`):

- `Rebuild HUD UI` - clears every child of the `Gems Sort UI` canvas and rebuilds a clean HUD: top bar (level label, coin counter, restart), bottom bar (area, wand, magnet hint buttons with count badges), legacy completion panel (kept hidden), and the universal `PurchaseDialog`. Every serialized field on `GemsSortHud` is wired automatically. It also hunts for a confetti prefab in `Lana Studio/Hyper Casual FX/Prefabs/Confetti` and assigns it to `GemsSortEffects.winCelebrationPrefab` if found.
- `Reset Player Inventory (PlayerPrefs)` - resets coins to 0 and each hint to 3.
- `Grant 500 Coins (testing)` - top-up to test purchases.
- `Empty All Hints (testing)` - drain every hint count so the next button click opens the purchase dialog.

## Win Flow

`GemsSortGameController.Win.cs` orchestrates the level-complete sequence:

1. Wait for the in-place shine sweep to finish.
2. Hide every gameplay HUD element (top bar, bottom bar, legacy completion panel) so only coin and dialog UI remain.
3. Switch every board cell into reveal mode (fill hidden, border tinted with target color) and hide all gem renderers, producing a pixelated outline of the finished image.
4. Spawn 10-12 coin sprites at screen center that fly to the coin counter, granting the level reward and bumping the coin icon.
5. Trigger the confetti / celebration effect via `GemsSortEffects.PlayWinCelebration` (assign a confetti prefab on the effects component).
6. Show a runtime "Next Level" button anchored to the bottom of the screen (no completion panel).

## Code Structure

Runtime scripts live in `Assets/Scripts/GemsSort`:

- `Core`: level data, grid coordinates, color lookup, and runtime state objects.
- `Game`: the scene entry point, HUD, settings, and focused `GemsSortGameController` partial files.
- `Services`: level scrambling, audio, and visual effects.
- `Views`: prefab-backed render components for cells, diamonds, and shelf slots.

`GemsSortGameController` is split by responsibility:

- `GemsSortGameController.cs`: serialized references, validation, level lifecycle, board/shelf spawning.
- `GemsSortGameController.Input.cs`: mouse, touch, zoom, pan, tap, and hit detection.
- `GemsSortGameController.Movement.cs`: board/shelf movement, shelf compaction, and diamond animation.
- `GemsSortGameController.Rules.cs`: selection, connected groups, locking, color completion, and win checks.
- `GemsSortGameController.Layout.cs`: camera fit, board/shelf positioning, scaling, and world cleanup.
- `GemsSortGameController.Hints.cs`: area / wand / magnet hint logic plus hint cooldown handling.
- `GemsSortGameController.Win.cs`: win flow orchestration (board reveal, coin animation, celebration, next button).

## Audio And Effects

`GemsSortAudio` and `GemsSortEffects` are separate services on the main scene object.

Select `Gems Sort Game` to assign UI audio clips, tune audio volume, and adjust shine sweep timing and pulse strength. Audio is clip-based; the game does not generate sound effects in code.
