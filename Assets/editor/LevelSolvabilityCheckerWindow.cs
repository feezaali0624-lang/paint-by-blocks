using UnityEngine;
using UnityEditor;
using System.Diagnostics;

// Tools > PaintByBlocks > Level Solvability Checker se open hota hai.
// Board + pieces assign karo, "Check Solvability" dabao — turant pata chal jayega
// ke ye level completely fill ho sakta hai ya nahi, bina khud test kiye.
public class LevelSolvabilityCheckerWindow : EditorWindow
{
    private BoardLevelData boardToCheck;
    [SerializeField] private PieceShapeData[] piecesToCheck = new PieceShapeData[0];
    private SerializedObject serializedWindow;
    private SerializedProperty piecesProperty;

    [MenuItem("Tools/PaintByBlocks/Level Solvability Checker")]
    public static void ShowWindow()
    {
        GetWindow<LevelSolvabilityCheckerWindow>("Solvability Checker");
    }

    private void OnEnable()
    {
        serializedWindow = new SerializedObject(this);
        piecesProperty = serializedWindow.FindProperty("piecesToCheck");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Level Solvability Checker", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Check karta hai ke diya gaya board un piece shapes se poora fill ho sakta hai ya nahi " +
            "(rotation allowed nahi, translation allowed, har piece unlimited baar use ho sakti hai).",
            MessageType.Info);

        EditorGUILayout.Space(8);

        boardToCheck = (BoardLevelData)EditorGUILayout.ObjectField(
            "Board To Check", boardToCheck, typeof(BoardLevelData), false);

        serializedWindow.Update();
        EditorGUILayout.PropertyField(piecesProperty, new GUIContent("Pieces To Check"), true);
        serializedWindow.ApplyModifiedProperties();

        EditorGUILayout.Space(12);

        GUI.enabled = boardToCheck != null && piecesToCheck.Length > 0;
        if (GUILayout.Button("Check Solvability", GUILayout.Height(36)))
        {
            RunCheck();
        }
        GUI.enabled = true;
    }

    private void RunCheck()
    {
        Stopwatch timer = Stopwatch.StartNew();
        bool solvable = LevelSolver.IsSolvable(boardToCheck, piecesToCheck);
        timer.Stop();

        int totalCells = boardToCheck.GetTotalPlayableCells();

        if (solvable)
        {
            EditorUtility.DisplayDialog(
                "Solvable ✔",
                $"Ye board in pieces se poora fill ho sakta hai.\n\n" +
                $"Total Playable Cells: {totalCells}\n" +
                $"Check Time: {timer.ElapsedMilliseconds} ms",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "NAHI Solvable ✘",
                $"Ye board in pieces se POORA fill NAHI ho sakta — kahin na kahin cells reh jayengi.\n\n" +
                $"Total Playable Cells: {totalCells}\n\n" +
                $"Suggestion: Board shape change karo, ya extra pieces add karo is level ke liye.",
                "OK");
        }
    }
}