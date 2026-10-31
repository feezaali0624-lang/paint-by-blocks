using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

// Tools > PaintByBlocks > Level Generator se open hota hai.
// Piece Library mein se random (ya manually chuni) shapes leta hai,
// unko baar-baar canvas pe place karta hai, aur unka union hi board bana deta hai.
// Isse board HAMESHA solvable hota hai kyunke wo khud unhi placements se bana hai.
// Ab board ke saath saath ek LevelConfig asset bhi banata hai jisme sahi pieces already jodi hui hoti hain.
public class LevelGeneratorWindow : EditorWindow
{
    private PieceLibrary library;

    private int minShapeTypes = 1;
    private int maxShapeTypes = 2;
    private int placementCount = 6;     
    private int canvasWidth = 5;        
    private int canvasHeight =6;
    private int maxAttemptsPerPlacement = 60;

    private bool useManualSelection = false;
    private bool[] manualMask;

    private int levelNumber = 1;
    private string outputFolder = "Assets/PaintByBlocksLevels";
    private string assetName = "NewLevel";

    [MenuItem("Tools/PaintByBlocks/Level Generator")]
    public static void ShowWindow()
    {
        GetWindow<LevelGeneratorWindow>("Level Generator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Random Solvable Level Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        library = (PieceLibrary)EditorGUILayout.ObjectField("Piece Library", library, typeof(PieceLibrary), false);

        if (library == null)
        {
            EditorGUILayout.HelpBox("Pehle ek PieceLibrary asset assign karo.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(6);
        useManualSelection = EditorGUILayout.Toggle("Manually Choose Shapes", useManualSelection);

        if (useManualSelection)
        {
            if (manualMask == null || manualMask.Length != library.allShapes.Length)
                manualMask = new bool[library.allShapes.Length];

            EditorGUILayout.LabelField("Is level ke liye shapes chuno:");
            for (int i = 0; i < library.allShapes.Length; i++)
            {
                if (library.allShapes[i] == null) continue;
                manualMask[i] = EditorGUILayout.ToggleLeft(library.allShapes[i].name, manualMask[i]);
            }
        }
        else
        {
            minShapeTypes = EditorGUILayout.IntSlider("Min Shape Types", minShapeTypes, 1, library.allShapes.Length);
            maxShapeTypes = EditorGUILayout.IntSlider("Max Shape Types", maxShapeTypes, minShapeTypes, library.allShapes.Length);
        }

        EditorGUILayout.Space(8);
        placementCount = EditorGUILayout.IntSlider("Piece Placements (difficulty/size)", placementCount, 2, 30);
        canvasWidth = EditorGUILayout.IntField("Working Canvas Width", canvasWidth);
        canvasHeight = EditorGUILayout.IntField("Working Canvas Height", canvasHeight);

        EditorGUILayout.Space(8);
        levelNumber = EditorGUILayout.IntField("Level Number", levelNumber);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        assetName = EditorGUILayout.TextField("Level Name", assetName);

        EditorGUILayout.Space(12);
        if (GUILayout.Button("Generate Level", GUILayout.Height(36)))
        {
            GenerateLevel();
        }
    }

    private void GenerateLevel()
    {
        // Step 1: is level ke liye kaunsi shapes use hongi, decide karo
        List<PieceShapeData> chosenShapes = new List<PieceShapeData>();

        if (useManualSelection)
        {
            for (int i = 0; i < library.allShapes.Length; i++)
                if (manualMask[i]) chosenShapes.Add(library.allShapes[i]);

            if (chosenShapes.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "Kam se kam ek shape select karo.", "OK");
                return;
            }
        }
        else
        {
            int count = Random.Range(minShapeTypes, maxShapeTypes + 1);
            chosenShapes = library.GetRandomShapes(count).ToList();
        }

        // Step 2: empty working canvas banao
        bool[] used = new bool[canvasWidth * canvasHeight];
        int successfulPlacements = 0;

        for (int p = 0; p < placementCount; p++)
        {
            bool placed = TryPlaceOnePiece(chosenShapes, used, successfulPlacements == 0);
            if (placed) successfulPlacements++;
        }

        if (successfulPlacements == 0)
        {
            EditorUtility.DisplayDialog("Failed", "Koi piece place nahi ho saki — canvas size barhao ya placements kam karo.", "OK");
            return;
        }

        // Step 3: used cells ka tight bounding box nikaalo (khali border cells hata do)
        CropToBounds(used, canvasWidth, canvasHeight,
            out bool[] croppedCells, out int croppedWidth, out int croppedHeight);

        // Step 4: BoardLevelData asset banao
        BoardLevelData board = ScriptableObject.CreateInstance<BoardLevelData>();
        board.width = croppedWidth;
        board.height = croppedHeight;
        board.isPlayable = croppedCells;

        if (!System.IO.Directory.Exists(outputFolder))
        {
            System.IO.Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }

        string finalName = string.IsNullOrEmpty(assetName) ? "NewLevel" : assetName;

        string boardPath = AssetDatabase.GenerateUniqueAssetPath($"{outputFolder}/{finalName}_Board.asset");
        AssetDatabase.CreateAsset(board, boardPath);

        // Step 5: LevelConfig asset banao — board + isi ki sahi pieces ek sath
        LevelConfig config = ScriptableObject.CreateInstance<LevelConfig>();
        config.board = board;
        config.pieces = chosenShapes.ToArray();
        config.levelNumber = levelNumber;

        string configPath = AssetDatabase.GenerateUniqueAssetPath($"{outputFolder}/{finalName}_Config.asset");
        AssetDatabase.CreateAsset(config, configPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(config);
        Selection.activeObject = config;

        string shapeNames = string.Join(", ", chosenShapes.Select(s => s.name));
        EditorUtility.DisplayDialog(
            "Level Generated",
            $"Board: {boardPath}\nConfig: {configPath}\nSize: {croppedWidth}x{croppedHeight}\n\n" +
            $"Is level ki pieces: {shapeNames}\n\n" +
            $"Ab sirf '_Config' asset LevelManager ki 'All Levels' list mein daalo — " +
            $"kuch bhi manually match karne ki zaroorat nahi.",
            "OK");
    }

    // Ek shape ko random position pe place karne ki koshish karta hai.
    private bool TryPlaceOnePiece(List<PieceShapeData> shapes, bool[] used, bool isFirstPlacement)
    {
        for (int attempt = 0; attempt < maxAttemptsPerPlacement; attempt++)
        {
            PieceShapeData shape = shapes[Random.Range(0, shapes.Count)];
            int originX = Random.Range(0, canvasWidth);
            int originY = Random.Range(0, canvasHeight);

            List<Vector2Int> targetCells = new List<Vector2Int>();
            bool valid = true;

            foreach (Vector2Int offset in shape.cellOffsets)
            {
                int x = originX + offset.x;
                int y = originY + offset.y;

                if (x < 0 || x >= canvasWidth || y < 0 || y >= canvasHeight) { valid = false; break; }
                if (used[y * canvasWidth + x]) { valid = false; break; }

                targetCells.Add(new Vector2Int(x, y));
            }

            if (!valid) continue;

            if (!isFirstPlacement)
            {
                bool touchesExisting = false;
                foreach (Vector2Int cell in targetCells)
                {
                    if (IsAdjacentToFilled(cell, used)) { touchesExisting = true; break; }
                }
                if (!touchesExisting) continue;
            }

            foreach (Vector2Int cell in targetCells)
                used[cell.y * canvasWidth + cell.x] = true;

            return true;
        }

        return false;
    }

    private bool IsAdjacentToFilled(Vector2Int cell, bool[] used)
    {
        Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        foreach (Vector2Int d in dirs)
        {
            int nx = cell.x + d.x;
            int ny = cell.y + d.y;
            if (nx < 0 || nx >= canvasWidth || ny < 0 || ny >= canvasHeight) continue;
            if (used[ny * canvasWidth + nx]) return true;
        }
        return false;
    }

    private void CropToBounds(bool[] used, int width, int height,
        out bool[] cropped, out int croppedWidth, out int croppedHeight)
    {
        int minX = width, minY = height, maxX = -1, maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (used[y * width + x])
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        croppedWidth = maxX - minX + 1;
        croppedHeight = maxY - minY + 1;
        cropped = new bool[croppedWidth * croppedHeight];

        for (int y = 0; y < croppedHeight; y++)
            for (int x = 0; x < croppedWidth; x++)
                cropped[y * croppedWidth + x] = used[(y + minY) * width + (x + minX)];
    }
}