using UnityEngine;
using UnityEditor;
using System.IO;

// Tools > PaintByBlocks > Board Shape Converter se open hota hai.
// Black & white image se board ka outline (playable cells) generate karta hai.
public class BoardShapeConverterWindow : EditorWindow
{
    private Texture2D sourceImage;
    private float darkThreshold = 0.5f; // is se neeche brightness = playable cell
    private string outputFolder = "Assets/BoardLevels";
    private string assetName = "NewBoardLevel";
    private bool invertColors = false;

    [MenuItem("Tools/PaintByBlocks/Board Shape Converter")]
    public static void ShowWindow()
    {
        BoardShapeConverterWindow window = GetWindow<BoardShapeConverterWindow>("Board Shape Converter");
        window.minSize = new Vector2(360, 380);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Image to Board Shape Converter", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        EditorGUI.BeginChangeCheck();
        sourceImage = (Texture2D)EditorGUILayout.ObjectField(
            "Source Image (B&W outline)", sourceImage, typeof(Texture2D), false);

        if (sourceImage != null && EditorGUI.EndChangeCheck())
        {
            EnsureTextureReadable(sourceImage);
        }

        darkThreshold = EditorGUILayout.Slider("Dark Threshold", darkThreshold, 0f, 1f);
        invertColors = EditorGUILayout.Toggle("Invert (white = playable)", invertColors);

        EditorGUILayout.Space(8);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        assetName = EditorGUILayout.TextField("Asset Name", assetName);

        EditorGUILayout.Space(12);

        if (sourceImage != null)
        {
            EditorGUILayout.HelpBox(
                $"Board Size: {sourceImage.width} x {sourceImage.height}\n" +
                "Tip: 6x8 se 10x10 tak boards mobile ke liye achay rehte hain. " +
                "Black shape = playable board area, white background = hole/outside.",
                MessageType.Info);
        }

        EditorGUILayout.Space(8);

        GUI.enabled = sourceImage != null;
        if (GUILayout.Button("Generate Board Asset", GUILayout.Height(36)))
        {
            GenerateBoardAsset();
        }
        GUI.enabled = true;
    }

    private void EnsureTextureReadable(Texture2D tex)
    {
        string path = AssetDatabase.GetAssetPath(tex);
        if (string.IsNullOrEmpty(path)) return;

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }

    private void GenerateBoardAsset()
    {
        int width = sourceImage.width;
        int height = sourceImage.height;

        Color[] pixels;
        try
        {
            pixels = sourceImage.GetPixels();
        }
        catch (System.Exception)
        {
            EditorUtility.DisplayDialog(
                "Texture Not Readable",
                "Is texture ka 'Read/Write Enabled' Inspector mein manually on karke dobara try karein.",
                "OK");
            return;
        }

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }

        BoardLevelData board = ScriptableObject.CreateInstance<BoardLevelData>();
        board.width = width;
        board.height = height;
        board.isPlayable = new bool[width * height];

        // Unity texture pixels bottom-to-top hote hain; grid top-to-bottom banane ke liye y flip karte hain
        for (int y = 0; y < height; y++)
        {
            int flippedY = height - 1 - y;
            for (int x = 0; x < width; x++)
            {
                Color pixel = pixels[flippedY * width + x];
                float brightness = pixel.a < 0.5f ? 1f : (pixel.r + pixel.g + pixel.b) / 3f;
                bool isDark = brightness < darkThreshold;
                if (invertColors) isDark = !isDark;

                board.SetCellPlayable(x, y, isDark);
            }
        }

        string finalName = string.IsNullOrEmpty(assetName) ? "NewBoardLevel" : assetName;
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{outputFolder}/{finalName}.asset");

        AssetDatabase.CreateAsset(board, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(board);
        Selection.activeObject = board;

        EditorUtility.DisplayDialog(
            "Board Generated",
            $"Board asset ban gaya: {assetPath}\nSize: {width} x {height}\nPlayable Cells: {board.GetTotalPlayableCells()}",
            "OK");
    }
}
