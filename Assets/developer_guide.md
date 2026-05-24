# Gems Sort Developer Guide & Architecture Tutorial

Welcome to the developer integration guide for the **Gems Sort** codebase. This document outlines the project's structural architecture, data flow, and provides step-by-step tutorials to help developers build features, author levels, and extend the game mechanics.

---

## 1. Architectural Blueprint (Overview)

The codebase is organized using a structured Model-View-Controller (MVC) pattern adapted for Unity:

```
        Model (State & Data)  <--  Controller (Game Logic)  -->  Views (Renderers)
        - LevelDefinition          - GemsSortGameController      - BoardCellView
        - CellState                - GemsSortHud                 - DiamondView
        - DiamondState                                           - ShelfSlotView
        - GemsSortInventory                                      - GemsSortLevelCard
```

### Component Breakdown
- **Core (Model)**: Pure C# objects storing game definitions and state.
  - `LevelDefinition`: Read-only specifications loaded from JSON files.
  - `CellState`: Tracks individual grid coordinates, whether they are blank, target colors, and current occupant gems.
  - `DiamondState`: Tracks a gem's current coordinate and color code.
  - `GemsSortInventory`: Persistent wallet state using `PlayerPrefs` (owns coin counts, hint uses).
- **Game/Services (Controller)**: Handles user interaction, camera placement, rule execution, and transition flow.
  - `GemsSortGameController`: A partial class coordinating viewport interaction, level loading, scrambling, selection constraints, and compaction.
  - `GemsSortHud`: Wires up UI buttons (Settings, Restart, Gallery, Hints), updates badges, and handles the coin collection animation.
- **Views (Visual Layer)**: Prefab-backed Unity components that sync visual properties with the Model.
  - `BoardCellView`, `DiamondView`, `ShelfSlotView`: Listen to board updates and trigger visual pulses, translations, and color changes.
  - `GemsSortLevelCard`: Exposes title text, lock/preview images, and load buttons on the gallery card.

---

## 2. Step-by-Step Level Creation Tutorial

Developers can create levels either by hand-painting on a grid or by auto-converting pixel-art images.

### Step 1: Open the Level Editor
1. In the Unity Editor, select **Tools -> Gems Sort -> Level Editor** from the top menu bar.
2. An interactive `Gems Sort Level Editor` window will open.

### Step 2: Option A - Hand-Painting a Level
1. Select the **Paint Editor** tab.
2. Adjust the **Width** and **Height** in the **Grid Settings** box to resize your canvas.
3. In the **Palette Manager** box:
   - Click **Add New Color** to define a color entry.
   - Set the unique **Color Code** (e.g., `1`, `2`, `3`) and choose a custom RGB value.
4. Select a tool from the **Painting Tools** box:
   - **Pen**: Paint the selected color onto cell grids.
   - **Eraser**: Turn painted cells back to blank.
   - **Eyedropper**: Sample a color from the canvas.
   - **Flood Fill**: Fill adjacent connected grids with the active color.
5. Paint your shape on the right visualizer grid.

### Step 3: Option B - Converting an Image to a Level
1. Select the **Image to Level** tab.
2. Drag your image asset (ideally pixel art or simple shapes) into the **Source Image** field.
   - *Note*: If the texture isn't readable, click the **Auto-Fix Import Settings** button.
3. Set **Cell Size (Pixels)**: If your pixel art uses `8x8` pixels per block, set cell size to `8`.
4. Adjust **Max Colors** and **Color Merge Threshold** to group close RGB tones into unified color codes.
5. Click **Regenerate Now** to see a preview of the grid.

### Step 4: Save & Integrate the Level JSON
1. Type a unique name in the **Level Name** field (e.g., `level_11`).
2. Click **Save Level**. The JSON specification will be exported to `Assets/Resources/Levels/level_11.json`.
3. Locate the settings asset: `Assets/GemsSort/Settings/GemsSortGameSettings.asset`.
4. Select it in the Inspector and drag your new JSON asset into the **Level Files** array to add it to the game's progression.

---

## 3. Extending the Codebase: Adding a Custom Hint Type

Let's walk through how to add a custom hint type called **Double-Coins Hint** that doubles the coins rewarded on level completion.

### Part A: Update the Inventory Wallet
First, define the enum value and keys in `GemsSortInventory.cs`:

```csharp
// 1. Add to the HintType enum:
public enum HintType
{
    Area,
    Wand,
    Magnet,
    Shelf,
    DoubleCoins // New hint type!
}

// 2. Add player prefs key in GemsSortInventory:
private const string DoubleCoinsKey = "GemsSort.Hints.DoubleCoins";

// 3. Update the fields and load fallback counts:
private static int doubleCoinsHints;

private static void EnsureLoaded()
{
    // ... Inside if (PlayerPrefs.GetInt(InitFlagKey, 0) == 0) block:
    doubleCoinsHints = InitialFreeHints;
    
    // ... Inside loading block:
    doubleCoinsHints = PlayerPrefs.GetInt(DoubleCoinsKey, InitialFreeHints);
}

// 4. Update helper getters/setters:
public static int GetHintCount(HintType type)
{
    // Add inside switch (type):
    case HintType.DoubleCoins: return doubleCoinsHints;
}

private static void SetHintCount(HintType type, int value)
{
    // Add inside switch (type):
    case HintType.DoubleCoins: doubleCoinsHints = value; break;
}
```

### Part B: Integrate Hint button on the Canvas
1. Find your Canvas in the scene hierarchy and copy one of the existing hint buttons (e.g. `Shelf`).
2. Rename the new button game object to `DoubleCoinsHintButton`.
3. Drag `DoubleCoinsHintButton` to your target layout position.
4. Open `GemsSortHud.cs`:
   - Expose serialized fields:
     ```csharp
     [SerializeField] private Button doubleCoinsHintButton;
     [SerializeField] private TextMeshProUGUI doubleCoinsHintCountText;
     ```
   - Wire them inside `Bind()`:
     ```csharp
     if (doubleCoinsHintButton != null)
     {
         doubleCoinsHintButton.onClick.RemoveListener(OnDoubleCoinsHintClicked);
         doubleCoinsHintButton.onClick.AddListener(OnDoubleCoinsHintClicked);
     }
     ```
   - Bind badge UI refresh:
     ```csharp
     UpdateHintBadge(doubleCoinsHintCountText, GemsSortInventory.HintType.DoubleCoins);
     ```
   - Add click listener:
     ```csharp
     private void OnDoubleCoinsHintClicked()
     {
         PlayClickSound();
         HandleHintButton(GemsSortInventory.HintType.DoubleCoins, () => {
             // Activate double coin reward mode
             if (controller != null)
             {
                 controller.ActivateDoubleCoinsMultiplier();
                 return true;
             }
             return false;
         });
     }
     ```

### Part C: Update the Game Controller
In `GemsSortGameController.cs` (or via a partial class):
- Add a boolean flag tracking if the coin reward is doubled:
  ```csharp
  private bool doubleCoinsActive = false;

  public void ActivateDoubleCoinsMultiplier()
  {
      doubleCoinsActive = true;
      Debug.Log("Double Coins Multiplier Active!");
  }
  ```
- Reset the multiplier inside `PerformLoadLevel()`:
  ```csharp
  doubleCoinsActive = false;
  ```
- Apply multiplier when granting rewards in `GemsSortGameController.Win.cs`:
  ```csharp
  int baseReward = settings != null ? settings.LevelWinReward : 50;
  int finalReward = doubleCoinsActive ? baseReward * 2 : baseReward;
  // Pass finalReward to HUD's reward presentation routine.
  ```

---

## 4. Best Practices & Performance Checklist

- **Cleanup Generated Textures**: When creating runtime images (like solved preview sprites in the gallery), keep track of generated `Texture2D` instances and destroy them using `Object.Destroy(texture)` before clearing lists to prevent graphics memory leak.
- **Event Unbinding**: When writing HUD components, always subscribe to wallet changes (`GemsSortInventory.Changed`) in `OnEnable()` and unsubscribe in `OnDisable()` to prevent memory retention bugs.
- **Ignore UI Elements in Input**: Ensure `EventSystem.current.IsPointerOverGameObject()` is checked when starting clicks/touches to block tap-through commands to the grid under the HUD.
- **Dynamic Layout Adjustments**: Check `FitViewport()` and `LayoutBoardAndShelf()` inside `GemsSortGameController.Layout.cs` to adjust cell scales depending on aspect ratios, ensuring layouts do not overflow off-screen on tablets or wide screens.
