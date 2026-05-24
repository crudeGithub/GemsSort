#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GemsSort.Core;
using GemsSort.Game;
using UnityEditor;
using UnityEngine;

namespace GemsSort.EditorTools
{
    public sealed class GemsSortLevelEditor : EditorWindow
    {
        public enum ToolType
        {
            Pen,
            Eraser,
            Eyedropper,
            FloodFill
        }

        public enum SamplingMode
        {
            CenterPixel,
            AverageColor,
            MostFrequentColor
        }

        [Serializable]
        public class PaletteColorEntry
        {
            public int code;
            public Color color;
        }

        // --- Editor State ---
        private int activeTab = 0; // 0 = Paint Editor, 1 = Image to Level
        private Vector2 sidebarScrollPos;
        private Vector2 paletteScrollPos;
        private Vector2 fileScrollPos;
        private Vector2 gridScrollPos;

        // --- Level Properties ---
        private string levelName = "level_new";
        private int width = 10;
        private int height = 10;
        private int shelfSlots = 10;
        private int[,] grid;
        private List<PaletteColorEntry> paletteList = new List<PaletteColorEntry>();
        private int selectedPaletteIndex = 0;

        // --- Paint Tool State ---
        private ToolType activeTool = ToolType.Pen;
        private int tileSize = 32;

        // --- File Manager State ---
        private List<string> levelsList = new List<string>();

        // --- Image Generator State ---
        private Texture2D sourceImage;
        private SamplingMode samplingMode = SamplingMode.CenterPixel;
        private int cellSize = 8;
        private int maxColors = 8;
        private float colorMergeThreshold = 0.15f;
        private float alphaThreshold = 0.1f;
        private bool filterBackground = false;
        private Color backgroundColor = Color.black;
        private float bgThreshold = 0.15f;

        // --- Styles ---
        private GUIStyle cellTextStyle;
        private bool stylesInitialized = false;

        [MenuItem("Tools/Gems Sort/Level Editor", false, 1)]
        public static void ShowWindow()
        {
            GemsSortLevelEditor window = GetWindow<GemsSortLevelEditor>("Gems Sort Level Editor");
            window.minSize = new Vector2(850, 500);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshLevelsList();
            if (grid == null)
            {
                InitializeDefaultGrid();
            }
        }

        private void InitializeDefaultGrid()
        {
            width = 10;
            height = 10;
            shelfSlots = 10;
            grid = new int[height, width];
            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    grid[r, c] = -1; // -1 is Blank
                }
            }

            paletteList.Clear();
            paletteList.Add(new PaletteColorEntry { code = 1, color = new Color(1f, 0.3f, 0.3f) }); // Red-ish
            paletteList.Add(new PaletteColorEntry { code = 2, color = new Color(0.2f, 0.6f, 1f) }); // Blue-ish
            selectedPaletteIndex = 0;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;
            cellTextStyle = new GUIStyle();
            cellTextStyle.alignment = TextAnchor.MiddleCenter;
            cellTextStyle.fontStyle = FontStyle.Bold;
            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.BeginHorizontal();

            // ==========================================
            // LEFT PANEL: CONTROLS & SETTINGS (Fixed Width)
            // ==========================================
            EditorGUILayout.BeginVertical(GUILayout.Width(330));
            sidebarScrollPos = EditorGUILayout.BeginScrollView(sidebarScrollPos, GUILayout.Width(330));

            GUILayout.Label("Gems Sort Level Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Tab Bar
            string[] tabs = { "Paint Editor", "Image to Level" };
            int newTab = GUILayout.Toolbar(activeTab, tabs);
            if (newTab != activeTab)
            {
                activeTab = newTab;
                if (activeTab == 1 && sourceImage != null)
                {
                    GenerateFromImage();
                }
            }
            EditorGUILayout.Space();

            if (activeTab == 0)
            {
                DrawPaintEditorTab();
            }
            else
            {
                DrawImageToLevelTab();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Splitter Line
            EditorGUILayout.BeginVertical(GUILayout.Width(2));
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, GUILayout.Width(2), GUILayout.ExpandHeight(true)), new Color(0.15f, 0.15f, 0.15f));
            EditorGUILayout.EndVertical();

            // ==========================================
            // RIGHT PANEL: INTERACTIVE GRID
            // ==========================================
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawGridViewHeader();
            DrawGridVisualizer();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        // --- Left Panel tabs ---

        private void DrawPaintEditorTab()
        {
            // 1. File Manager
            EditorGUILayout.LabelField("File Manager", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            levelName = EditorGUILayout.TextField("Level Name", levelName);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Level", GUILayout.Height(25)))
            {
                SaveLevel(levelName);
            }
            if (GUILayout.Button("New Level", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Confirm Reset", "Create a new blank level? Any unsaved progress will be lost.", "Yes", "No"))
                {
                    InitializeDefaultGrid();
                    levelName = "level_new";
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Available Levels:", EditorStyles.miniLabel);

            fileScrollPos = EditorGUILayout.BeginScrollView(fileScrollPos, GUILayout.Height(100));
            if (levelsList.Count == 0)
            {
                EditorGUILayout.LabelField("No level JSON files found.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                for (int i = 0; i < levelsList.Count; i++)
                {
                    string filename = levelsList[i];
                    string dispName = Path.GetFileNameWithoutExtension(filename);
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button(dispName, EditorStyles.label, GUILayout.Width(180)))
                    {
                        LoadLevelFromFile(filename);
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Load", GUILayout.Width(45)))
                    {
                        LoadLevelFromFile(filename);
                    }
                    if (GUILayout.Button("X", GUILayout.Width(22)))
                    {
                        DeleteLevel(filename);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 2. Grid Size controls
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            int newWidth = Mathf.Clamp(EditorGUILayout.IntField("Width", width), 1, 100);
            int newHeight = Mathf.Clamp(EditorGUILayout.IntField("Height", height), 1, 100);
            if (newWidth != width || newHeight != height)
            {
                ResizeGrid(newWidth, newHeight);
            }

            shelfSlots = Mathf.Clamp(EditorGUILayout.IntField("Shelf Slots", shelfSlots), 1, 30);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 3. Painting Tools
            EditorGUILayout.LabelField("Painting Tools", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            DrawToolButton(ToolType.Pen, "Pen");
            DrawToolButton(ToolType.Eraser, "Eraser");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            DrawToolButton(ToolType.Eyedropper, "Eyedropper");
            DrawToolButton(ToolType.FloodFill, "Flood Fill");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 4. Palette Editor
            EditorGUILayout.LabelField("Palette Manager", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            paletteScrollPos = EditorGUILayout.BeginScrollView(paletteScrollPos, GUILayout.Height(150));
            if (paletteList.Count == 0)
            {
                EditorGUILayout.LabelField("Palette is empty. Add a color to start painting.", EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                for (int i = 0; i < paletteList.Count; i++)
                {
                    var entry = paletteList[i];
                    EditorGUILayout.BeginHorizontal();

                    // Active selection highlight
                    bool isActive = (selectedPaletteIndex == i);
                    Color oldBg = GUI.backgroundColor;
                    if (isActive)
                    {
                        GUI.backgroundColor = new Color(0.4f, 1f, 0.4f); // Green outline-like tint
                    }
                    if (GUILayout.Button(isActive ? "★" : "Select", GUILayout.Width(50)))
                    {
                        selectedPaletteIndex = i;
                    }
                    GUI.backgroundColor = oldBg;

                    // Code (ID)
                    EditorGUI.BeginChangeCheck();
                    int enteredCode = EditorGUILayout.IntField(entry.code, GUILayout.Width(35));
                    if (EditorGUI.EndChangeCheck() && enteredCode != entry.code && enteredCode != -1)
                    {
                        // Ensure unique code
                        if (!paletteList.Any(p => p.code == enteredCode))
                        {
                            int oldCode = entry.code;
                            entry.code = enteredCode;
                            // Sync grid
                            for (int r = 0; r < height; r++)
                            {
                                for (int c = 0; c < width; c++)
                                {
                                    if (grid[r, c] == oldCode) grid[r, c] = enteredCode;
                                }
                            }
                        }
                    }

                    // Color picker
                    entry.color = EditorGUILayout.ColorField(entry.color);

                    // Delete color
                    if (GUILayout.Button("X", GUILayout.Width(22)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Color", $"Are you sure you want to delete color code {entry.code}? Tiles painted with this color will become blank.", "Yes", "No"))
                        {
                            int code = entry.code;
                            paletteList.RemoveAt(i);
                            // Clear matching grid values
                            for (int r = 0; r < height; r++)
                            {
                                for (int c = 0; c < width; c++)
                                {
                                    if (grid[r, c] == code) grid[r, c] = -1;
                                }
                            }
                            if (selectedPaletteIndex >= paletteList.Count)
                            {
                                selectedPaletteIndex = paletteList.Count - 1;
                            }
                            i--;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Add New Color"))
            {
                int nextCode = 1;
                if (paletteList.Count > 0)
                {
                    nextCode = paletteList.Max(p => p.code) + 1;
                }
                if (nextCode == -1) nextCode = 0; // Avoid blank ID
                paletteList.Add(new PaletteColorEntry { code = nextCode, color = Color.white });
                selectedPaletteIndex = paletteList.Count - 1;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawToolButton(ToolType tool, string label)
        {
            Color oldBg = GUI.backgroundColor;
            if (activeTool == tool)
            {
                GUI.backgroundColor = new Color(0.3f, 0.7f, 1f); // Sleek active blue
            }

            if (GUILayout.Button(label, GUILayout.Height(25), GUILayout.ExpandWidth(true)))
            {
                activeTool = tool;
            }

            GUI.backgroundColor = oldBg;
        }

        private void DrawImageToLevelTab()
        {
            EditorGUILayout.LabelField("Image to Level Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginChangeCheck();

            sourceImage = (Texture2D)EditorGUILayout.ObjectField("Source Image", sourceImage, typeof(Texture2D), false);
            
            if (sourceImage != null)
            {
                // Verify read/write is enabled
                string path = AssetDatabase.GetAssetPath(sourceImage);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && !importer.isReadable)
                {
                    EditorGUILayout.HelpBox("This texture is not marked as Read/Write Enabled.", MessageType.Warning);
                    if (GUILayout.Button("Auto-Fix Import Settings"))
                    {
                        importer.isReadable = true;
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        importer.SaveAndReimport();
                        GenerateFromImage();
                    }
                }
                
                EditorGUILayout.LabelField($"Size: {sourceImage.width} x {sourceImage.height} pixels");
            }

            samplingMode = (SamplingMode)EditorGUILayout.EnumPopup("Sampling Mode", samplingMode);
            cellSize = EditorGUILayout.IntSlider(new GUIContent("Cell Size (Pixels)", "Width/height of pixel block to sample/average into a single tile."), cellSize, 1, 64);
            maxColors = EditorGUILayout.IntSlider(new GUIContent("Max Colors", "Limits how many colors are kept in the final palette."), maxColors, 2, 32);
            colorMergeThreshold = EditorGUILayout.Slider(new GUIContent("Color Merge Threshold", "Aggression of combining close shades. High value avoids many shades."), colorMergeThreshold, 0f, 0.5f);
            alphaThreshold = EditorGUILayout.Slider(new GUIContent("Alpha Threshold", "Pixels with alpha below this are ignored."), alphaThreshold, 0f, 1f);

            filterBackground = EditorGUILayout.Toggle("Filter Background Color", filterBackground);
            if (filterBackground)
            {
                backgroundColor = EditorGUILayout.ColorField("BG Color to Remove", backgroundColor);
                bgThreshold = EditorGUILayout.Slider("Distance Threshold", bgThreshold, 0f, 1f);
            }

            bool settingsChanged = EditorGUI.EndChangeCheck();

            if (sourceImage != null)
            {
                int calculatedW = Mathf.Max(1, sourceImage.width / cellSize);
                int calculatedH = Mathf.Max(1, sourceImage.height / cellSize);
                EditorGUILayout.LabelField($"Calculated Grid: {calculatedW} x {calculatedH} cells");

                if (settingsChanged)
                {
                    GenerateFromImage();
                }

                EditorGUILayout.Space();
                if (GUILayout.Button("Regenerate Now", GUILayout.Height(30)))
                {
                    GenerateFromImage();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Drag a pixel-art or simple image above to generate a grid automatically. Preview updates live on the right.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        // --- Right Panel ---

        private void DrawGridViewHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Interactive Grid View", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            // Zoom Slider
            GUILayout.Label("Tile Size:", EditorStyles.miniLabel);
            tileSize = EditorGUILayout.IntSlider(tileSize, 8, 64, GUILayout.Width(150));
            
            EditorGUILayout.EndHorizontal();
            
            // Brief usage hint
            EditorGUILayout.LabelField("Left-Click: Paint/Apply Tool  |  Right-Click: Erase  |  Drag: Paint/Erase Continuously", EditorStyles.miniLabel);
            EditorGUILayout.Space();
        }

        private void DrawGridVisualizer()
        {
            gridScrollPos = EditorGUILayout.BeginScrollView(gridScrollPos);

            int gridW = width * tileSize;
            int gridH = height * tileSize;

            // Reserve layout space for grid
            Rect gridRect = GUILayoutUtility.GetRect(gridW, gridH, GUILayout.Width(gridW), GUILayout.Height(gridH));

            // Background boundary border
            EditorGUI.DrawRect(new Rect(gridRect.x - 2, gridRect.y - 2, gridRect.width + 4, gridRect.height + 4), new Color(0.12f, 0.12f, 0.12f));

            // Draw grid cells
            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    Rect cellRect = new Rect(gridRect.x + c * tileSize, gridRect.y + r * tileSize, tileSize, tileSize);
                    int code = grid[r, c];

                    Color cellColor;
                    bool isBlank = (code == -1);

                    if (isBlank)
                    {
                        // Checkerboard colors
                        bool alt = ((r + c) % 2 == 0);
                        cellColor = alt ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.24f, 0.24f, 0.24f);
                    }
                    else
                    {
                        cellColor = GetColorFromCode(code);
                    }

                    // 1px Border drawing
                    EditorGUI.DrawRect(cellRect, new Color(0.1f, 0.1f, 0.1f));
                    Rect innerRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.width - 2, cellRect.height - 2);
                    EditorGUI.DrawRect(innerRect, cellColor);

                    // Overlay code label if painted
                    if (!isBlank)
                    {
                        // Calculate text color by luminance
                        float luminance = (cellColor.r * 0.299f + cellColor.g * 0.587f + cellColor.b * 0.114f);
                        Color textColor = luminance > 0.5f ? Color.black : Color.white;

                        cellTextStyle.fontSize = Mathf.Clamp(tileSize / 2, 8, 24);
                        DrawShadowLabel(innerRect, code.ToString(), cellTextStyle, textColor);
                    }
                }
            }

            // Mouse handling inside grid
            HandleGridInput(gridRect, tileSize);

            EditorGUILayout.EndScrollView();
        }

        private void DrawShadowLabel(Rect rect, string text, GUIStyle style, Color textColor)
        {
            GUIStyle shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.65f);

            // Shift down-right by 1px
            Rect shadowRect = new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height);
            GUI.Label(shadowRect, text, shadowStyle);

            style.normal.textColor = textColor;
            GUI.Label(rect, text, style);
        }

        private void HandleGridInput(Rect gridRect, int size)
        {
            Event e = Event.current;
            if (e.rawType == EventType.MouseDown || e.rawType == EventType.MouseDrag)
            {
                Vector2 mousePos = e.mousePosition;
                if (gridRect.Contains(mousePos))
                {
                    int col = (int)((mousePos.x - gridRect.x) / size);
                    int row = (int)((mousePos.y - gridRect.y) / size);

                    if (row >= 0 && row < height && col >= 0 && col < width)
                    {
                        bool isLeftClick = (e.button == 0);
                        bool isRightClick = (e.button == 1);

                        if (isLeftClick)
                        {
                            switch (activeTool)
                            {
                                case ToolType.Pen:
                                    if (paletteList.Count > 0 && selectedPaletteIndex >= 0 && selectedPaletteIndex < paletteList.Count)
                                    {
                                        grid[row, col] = paletteList[selectedPaletteIndex].code;
                                    }
                                    break;
                                case ToolType.Eraser:
                                    grid[row, col] = -1;
                                    break;
                                case ToolType.Eyedropper:
                                    int code = grid[row, col];
                                    if (code != -1)
                                    {
                                        int idx = paletteList.FindIndex(p => p.code == code);
                                        if (idx != -1)
                                        {
                                            selectedPaletteIndex = idx;
                                        }
                                    }
                                    break;
                                case ToolType.FloodFill:
                                    if (paletteList.Count > 0 && selectedPaletteIndex >= 0 && selectedPaletteIndex < paletteList.Count)
                                    {
                                        // Flood fill should only run once per mouse press to prevent drag-filling
                                        if (e.type == EventType.MouseDown)
                                        {
                                            FloodFill(row, col, paletteList[selectedPaletteIndex].code);
                                        }
                                    }
                                    break;
                            }
                        }
                        else if (isRightClick)
                        {
                            grid[row, col] = -1; // Right-click erases on any tool
                        }

                        e.Use();
                        Repaint();
                    }
                }
            }
        }

        // --- Helpers & Logic ---

        private Color GetColorFromCode(int code)
        {
            var entry = paletteList.Find(p => p.code == code);
            if (entry != null) return entry.color;
            return Color.magenta; // Missing color indicator
        }

        private void ResizeGrid(int newWidth, int newHeight)
        {
            int[,] newGrid = new int[newHeight, newWidth];
            for (int r = 0; r < newHeight; r++)
            {
                for (int c = 0; c < newWidth; c++)
                {
                    if (r < height && c < width)
                    {
                        newGrid[r, c] = grid[r, c];
                    }
                    else
                    {
                        newGrid[r, c] = -1; // Default to Blank
                    }
                }
            }
            grid = newGrid;
            width = newWidth;
            height = newHeight;
        }

        private void FloodFill(int startRow, int startCol, int targetCode)
        {
            int originalCode = grid[startRow, startCol];
            if (originalCode == targetCode) return;

            int H = grid.GetLength(0);
            int W = grid.GetLength(1);

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startCol, startRow));

            while (queue.Count > 0)
            {
                Vector2Int curr = queue.Dequeue();
                int c = curr.x;
                int r = curr.y;

                if (grid[r, c] == originalCode)
                {
                    grid[r, c] = targetCode;

                    if (r > 0 && grid[r - 1, c] == originalCode) queue.Enqueue(new Vector2Int(c, r - 1));
                    if (r < H - 1 && grid[r + 1, c] == originalCode) queue.Enqueue(new Vector2Int(c, r + 1));
                    if (c > 0 && grid[r, c - 1] == originalCode) queue.Enqueue(new Vector2Int(c - 1, r));
                    if (c < W - 1 && grid[r, c + 1] == originalCode) queue.Enqueue(new Vector2Int(c + 1, r));
                }
            }
        }

        // --- File Operations ---

        private void RefreshLevelsList()
        {
            levelsList.Clear();
            string folderPath = "Assets/Resources/Levels";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            string[] files = Directory.GetFiles(folderPath, "*.json");
            foreach (var f in files)
            {
                levelsList.Add(Path.GetFileName(f));
            }
            // Sort files numerically if possible, otherwise alphabetically
            levelsList.Sort((a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadLevelFromFile(string filename)
        {
            string folderPath = "Assets/Resources/Levels";
            string fullPath = Path.Combine(folderPath, filename);
            if (!File.Exists(fullPath)) return;

            string jsonText = File.ReadAllText(fullPath);
            LevelJson levelData = null;
            try
            {
                levelData = JsonUtility.FromJson<LevelJson>(jsonText);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error Loading", "JSON parsing failed:\n" + ex.Message, "OK");
                return;
            }

            if (levelData == null || levelData.targetRows == null || levelData.targetRows.Length == 0)
            {
                EditorUtility.DisplayDialog("Error Loading", "Invalid Level JSON structure.", "OK");
                return;
            }

            width = levelData.width;
            height = levelData.height;
            shelfSlots = levelData.shelfSlots > 0 ? levelData.shelfSlots : 10;

            grid = new int[height, width];
            for (int r = 0; r < height; r++)
            {
                if (r < levelData.targetRows.Length)
                {
                    string[] parts = levelData.targetRows[r].Split(',');
                    for (int c = 0; c < width; c++)
                    {
                        if (c < parts.Length)
                        {
                            grid[r, c] = int.Parse(parts[c]);
                        }
                        else
                        {
                            grid[r, c] = -1;
                        }
                    }
                }
                else
                {
                    for (int c = 0; c < width; c++)
                    {
                        grid[r, c] = -1;
                    }
                }
            }

            paletteList.Clear();
            if (levelData.palette != null)
            {
                foreach (var p in levelData.palette)
                {
                    Color col;
                    ColorUtility.TryParseHtmlString(p.color, out col);
                    paletteList.Add(new PaletteColorEntry { code = p.code, color = col });
                }
            }

            selectedPaletteIndex = paletteList.Count > 0 ? 0 : -1;
            levelName = Path.GetFileNameWithoutExtension(filename);
            
            Debug.Log($"Loaded Gems Sort Level: '{filename}'");
        }

        private void SaveLevel(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a level name.", "OK");
                return;
            }

            string folderPath = "Assets/Resources/Levels";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filename = name.EndsWith(".json") ? name : name + ".json";
            string fullPath = Path.Combine(folderPath, filename);

            LevelJson json = new LevelJson();
            json.width = width;
            json.height = height;
            json.shelfSlots = shelfSlots;

            json.palette = new PaletteJson[paletteList.Count];
            for (int i = 0; i < paletteList.Count; i++)
            {
                json.palette[i] = new PaletteJson
                {
                    code = paletteList[i].code,
                    color = "#" + ColorUtility.ToHtmlStringRGB(paletteList[i].color).ToLower()
                };
            }

            json.targetRows = new string[height];
            for (int r = 0; r < height; r++)
            {
                string[] rowParts = new string[width];
                for (int c = 0; c < width; c++)
                {
                    rowParts[c] = grid[r, c].ToString();
                }
                json.targetRows[r] = string.Join(",", rowParts);
            }

            string jsonString = JsonUtility.ToJson(json, true);
            File.WriteAllText(fullPath, jsonString);
            AssetDatabase.Refresh();

            Debug.Log($"Saved Level to '{fullPath}'");
            RefreshLevelsList();

            // Auto Register in Settings
            RegisterLevelInSettings(fullPath);
        }

        private void DeleteLevel(string filename)
        {
            if (EditorUtility.DisplayDialog("Confirm Delete", $"Delete level {filename}? This action cannot be undone.", "Yes", "No"))
            {
                string folderPath = "Assets/Resources/Levels";
                string fullPath = Path.Combine(folderPath, filename);

                // Load TextAsset before delete to clean it out from settings
                TextAsset levelAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(fullPath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    string metaPath = fullPath + ".meta";
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }
                    AssetDatabase.Refresh();

                    if (levelAsset != null)
                    {
                        UnregisterLevelFromSettings(levelAsset);
                    }

                    RefreshLevelsList();
                    Debug.Log($"Deleted Level '{filename}'");
                }
            }
        }

        private void RegisterLevelInSettings(string assetPath)
        {
            string settingsPath = "Assets/GemsSort/Settings/GemsSortGameSettings.asset";
            GemsSortGameSettings settings = AssetDatabase.LoadAssetAtPath<GemsSortGameSettings>(settingsPath);
            if (settings == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:GemsSortGameSettings");
                if (guids != null && guids.Length > 0)
                {
                    settings = AssetDatabase.LoadAssetAtPath<GemsSortGameSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            if (settings == null)
            {
                Debug.LogWarning("Could not find GemsSortGameSettings asset. Skipping automatic level registration.");
                return;
            }

            TextAsset levelAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (levelAsset == null) return;

            SerializedObject so = new SerializedObject(settings);
            SerializedProperty levelFilesProp = so.FindProperty("levelFiles");
            if (levelFilesProp == null) return;

            // Check if level already registered
            bool alreadyRegistered = false;
            for (int i = 0; i < levelFilesProp.arraySize; i++)
            {
                if (levelFilesProp.GetArrayElementAtIndex(i).objectReferenceValue == levelAsset)
                {
                    alreadyRegistered = true;
                    break;
                }
            }

            if (!alreadyRegistered)
            {
                int nextIndex = levelFilesProp.arraySize;
                levelFilesProp.arraySize++;
                levelFilesProp.GetArrayElementAtIndex(nextIndex).objectReferenceValue = levelAsset;
                
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log($"Level '{levelAsset.name}' auto-registered in GemsSortGameSettings at index {nextIndex}.");
            }
        }

        private void UnregisterLevelFromSettings(TextAsset levelAsset)
        {
            string settingsPath = "Assets/GemsSort/Settings/GemsSortGameSettings.asset";
            GemsSortGameSettings settings = AssetDatabase.LoadAssetAtPath<GemsSortGameSettings>(settingsPath);
            if (settings == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:GemsSortGameSettings");
                if (guids != null && guids.Length > 0)
                {
                    settings = AssetDatabase.LoadAssetAtPath<GemsSortGameSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            if (settings == null) return;

            var activeList = new List<TextAsset>();
            if (settings.LevelFiles != null)
            {
                foreach (var file in settings.LevelFiles)
                {
                    if (file != null && file != levelAsset)
                    {
                        activeList.Add(file);
                    }
                }
            }

            settings.ConfigureLevels(activeList.ToArray());
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"Level '{levelAsset.name}' removed from GemsSortGameSettings levelFiles list.");
        }

        // --- Image to Level Generation Logic ---

        private void GenerateFromImage()
        {
            if (sourceImage == null) return;

            string assetPath = AssetDatabase.GetAssetPath(sourceImage);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            int imgW = sourceImage.width;
            int imgH = sourceImage.height;

            int W = Mathf.Max(1, imgW / cellSize);
            int H = Mathf.Max(1, imgH / cellSize);

            var colorFrequencies = new Dictionary<string, int>();
            string[,] cellHexColors = new string[H, W];

            for (int r = 0; r < H; r++)
            {
                for (int c = 0; c < W; c++)
                {
                    int startX = c * cellSize;
                    int startY = (H - 1 - r) * cellSize; // row 0 is top, Y is bottom-up

                    if (samplingMode == SamplingMode.CenterPixel)
                    {
                        int x = Mathf.Clamp(startX + cellSize / 2, 0, imgW - 1);
                        int y = Mathf.Clamp(startY + cellSize / 2, 0, imgH - 1);
                        Color pixel = sourceImage.GetPixel(x, y);

                        if (pixel.a < alphaThreshold || (filterBackground && ColorDistance(pixel, backgroundColor) < bgThreshold))
                        {
                            cellHexColors[r, c] = null;
                        }
                        else
                        {
                            string hex = "#" + ColorUtility.ToHtmlStringRGB(pixel).ToLower();
                            cellHexColors[r, c] = hex;

                            if (colorFrequencies.ContainsKey(hex))
                            {
                                colorFrequencies[hex]++;
                            }
                            else
                            {
                                colorFrequencies[hex] = 1;
                            }
                        }
                    }
                    else if (samplingMode == SamplingMode.AverageColor)
                    {
                        float sumR = 0, sumG = 0, sumB = 0, sumA = 0;
                        int validCount = 0;

                        for (int py = 0; py < cellSize; py++)
                        {
                            for (int px = 0; px < cellSize; px++)
                            {
                                int x = Mathf.Clamp(startX + px, 0, imgW - 1);
                                int y = Mathf.Clamp(startY + py, 0, imgH - 1);

                                Color pixel = sourceImage.GetPixel(x, y);

                                if (pixel.a < alphaThreshold) continue;

                                if (filterBackground)
                                {
                                    float dist = ColorDistance(pixel, backgroundColor);
                                    if (dist < bgThreshold) continue;
                                }

                                sumR += pixel.r;
                                sumG += pixel.g;
                                sumB += pixel.b;
                                sumA += pixel.a;
                                validCount++;
                            }
                        }

                        if (validCount > 0 && (float)validCount / (cellSize * cellSize) >= 0.25f)
                        {
                            Color avg = new Color(sumR / validCount, sumG / validCount, sumB / validCount, 1f);
                            string hex = "#" + ColorUtility.ToHtmlStringRGB(avg).ToLower();
                            cellHexColors[r, c] = hex;

                            if (colorFrequencies.ContainsKey(hex))
                            {
                                colorFrequencies[hex]++;
                            }
                            else
                            {
                                colorFrequencies[hex] = 1;
                            }
                        }
                        else
                        {
                            cellHexColors[r, c] = null;
                        }
                    }
                    else if (samplingMode == SamplingMode.MostFrequentColor)
                    {
                        var blockFreqs = new Dictionary<string, int>();

                        for (int py = 0; py < cellSize; py++)
                        {
                            for (int px = 0; px < cellSize; px++)
                            {
                                int x = Mathf.Clamp(startX + px, 0, imgW - 1);
                                int y = Mathf.Clamp(startY + py, 0, imgH - 1);

                                Color pixel = sourceImage.GetPixel(x, y);

                                if (pixel.a < alphaThreshold) continue;

                                if (filterBackground)
                                {
                                    float dist = ColorDistance(pixel, backgroundColor);
                                    if (dist < bgThreshold) continue;
                                }

                                string hex = "#" + ColorUtility.ToHtmlStringRGB(pixel).ToLower();
                                if (blockFreqs.ContainsKey(hex))
                                {
                                    blockFreqs[hex]++;
                                }
                                else
                                {
                                    blockFreqs[hex] = 1;
                                }
                            }
                        }

                        if (blockFreqs.Count > 0)
                        {
                            string bestHex = blockFreqs.OrderByDescending(kv => kv.Value).First().Key;
                            cellHexColors[r, c] = bestHex;

                            if (colorFrequencies.ContainsKey(bestHex))
                            {
                                colorFrequencies[bestHex]++;
                            }
                            else
                            {
                                colorFrequencies[bestHex] = 1;
                            }
                        }
                        else
                        {
                            cellHexColors[r, c] = null;
                        }
                    }
                }
            }

            // 1. Merge similar colors
            if (colorMergeThreshold > 0.01f && colorFrequencies.Count > 1)
            {
                MergeSimilarHexColors(colorFrequencies, cellHexColors, colorMergeThreshold);
            }

            // 2. Reduce palette to limit size (merges rare into closest common color)
            HashSet<string> finalPaletteHex = LimitHexPalette(colorFrequencies, cellHexColors, maxColors);

            // Save variables to active level grid
            width = W;
            height = H;
            grid = new int[H, W];

            paletteList.Clear();
            var hexToCode = new Dictionary<string, int>();
            int codeCounter = 1;

            foreach (var hex in finalPaletteHex)
            {
                Color col;
                ColorUtility.TryParseHtmlString(hex, out col);
                paletteList.Add(new PaletteColorEntry { code = codeCounter, color = col });
                hexToCode[hex] = codeCounter;
                codeCounter++;
            }

            // Fill actual grid
            for (int r = 0; r < H; r++)
            {
                for (int c = 0; c < W; c++)
                {
                    string hex = cellHexColors[r, c];
                    if (hex != null && hexToCode.ContainsKey(hex))
                    {
                        grid[r, c] = hexToCode[hex];
                    }
                    else
                    {
                        grid[r, c] = -1; // Blank
                    }
                }
            }

            selectedPaletteIndex = paletteList.Count > 0 ? 0 : -1;
            Repaint();
        }

        private void MergeSimilarHexColors(Dictionary<string, int> frequencies, string[,] cellHexColors, float threshold)
        {
            var hexColors = new List<string>(frequencies.Keys);
            hexColors.Sort((a, b) => frequencies[b].CompareTo(frequencies[a])); // Sort descending frequency

            var mergeMap = new Dictionary<string, string>(); // rare -> common

            for (int i = 0; i < hexColors.Count; i++)
            {
                string hexA = hexColors[i];
                if (mergeMap.ContainsKey(hexA)) continue;

                Color cA;
                ColorUtility.TryParseHtmlString(hexA, out cA);

                for (int j = i + 1; j < hexColors.Count; j++)
                {
                    string hexB = hexColors[j];
                    if (mergeMap.ContainsKey(hexB)) continue;

                    Color cB;
                    ColorUtility.TryParseHtmlString(hexB, out cB);

                    if (ColorDistance(cA, cB) < threshold)
                    {
                        mergeMap[hexB] = hexA;
                    }
                }
            }

            // Apply mergers
            foreach (var kvp in mergeMap)
            {
                string rare = kvp.Key;
                string common = kvp.Value;

                frequencies[common] += frequencies[rare];
                frequencies.Remove(rare);

                int H = cellHexColors.GetLength(0);
                int W = cellHexColors.GetLength(1);
                for (int r = 0; r < H; r++)
                {
                    for (int c = 0; c < W; c++)
                    {
                        if (cellHexColors[r, c] == rare)
                        {
                            cellHexColors[r, c] = common;
                        }
                    }
                }
            }
        }

        private HashSet<string> LimitHexPalette(Dictionary<string, int> frequencies, string[,] cellHexColors, int limit)
        {
            var sortedList = new List<string>(frequencies.Keys);
            sortedList.Sort((a, b) => frequencies[b].CompareTo(frequencies[a]));

            var allowed = new HashSet<string>();
            var rare = new List<string>();

            for (int i = 0; i < sortedList.Count; i++)
            {
                if (i < limit)
                {
                    allowed.Add(sortedList[i]);
                }
                else
                {
                    rare.Add(sortedList[i]);
                }
            }

            if (rare.Count > 0 && allowed.Count > 0)
            {
                var allowedList = new List<string>(allowed);
                var allowedColors = new List<Color>();
                foreach (var hex in allowedList)
                {
                    Color col;
                    ColorUtility.TryParseHtmlString(hex, out col);
                    allowedColors.Add(col);
                }

                var remapping = new Dictionary<string, string>();
                foreach (var rareHex in rare)
                {
                    Color rColor;
                    ColorUtility.TryParseHtmlString(rareHex, out rColor);

                    float minDist = float.MaxValue;
                    string closestAllowed = allowedList[0];

                    for (int i = 0; i < allowedList.Count; i++)
                    {
                        float dist = ColorDistance(rColor, allowedColors[i]);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestAllowed = allowedList[i];
                        }
                    }
                    remapping[rareHex] = closestAllowed;
                }

                // Rewrite grid
                int H = cellHexColors.GetLength(0);
                int W = cellHexColors.GetLength(1);
                for (int r = 0; r < H; r++)
                {
                    for (int c = 0; c < W; c++)
                    {
                        string hex = cellHexColors[r, c];
                        if (hex != null && remapping.ContainsKey(hex))
                        {
                            cellHexColors[r, c] = remapping[hex];
                        }
                    }
                }
            }

            return allowed;
        }

        private float ColorDistance(Color c1, Color c2)
        {
            return Mathf.Sqrt(
                Mathf.Pow(c1.r - c2.r, 2) +
                Mathf.Pow(c1.g - c2.g, 2) +
                Mathf.Pow(c1.b - c2.b, 2)
            );
        }
    }
}
#endif
