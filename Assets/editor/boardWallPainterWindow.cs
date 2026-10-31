using UnityEngine;
using UnityEditor;

// Tools > PaintByBlocks > Wall Painter se open hota hai.
// Cells ke beech ki patli lines pe click karke wall on/off karo.
public class BoardWallPainterWindow : EditorWindow
{
    private BoardLevelData board;
    private float cellDisplaySize = 32f;
    private float wallStripSize = 12f;

    [MenuItem("Tools/PaintByBlocks/Wall Painter")]
    public static void ShowWindow()
    {
        GetWindow<BoardWallPainterWindow>("Wall Painter");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Board Wall Painter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Cells ke beech ki patli line pe click karo — wall on ho jayegi (laal dikhegi). " +
            "Player us line ko cross karke shape nahi bana sakega. Dobara click se wall hat jayegi.",
            MessageType.Info);

        board = (BoardLevelData)EditorGUILayout.ObjectField("Board", board, typeof(BoardLevelData), false);

        if (board == null) return;

        board.EnsureWallArrays();
        EditorGUILayout.HelpBox(
    "Cells ke beech ki patli line pe click karo = wall (laal). " +
    "Cell ke andar (beech) click karo = prefilled cell (brown) — player wahan interact nahi kar sakega.",
    MessageType.Info);
        EditorGUILayout.Space(10);

        Rect area = GUILayoutUtility.GetRect(
            board.width * cellDisplaySize + (board.width - 1) * wallStripSize + 40,
            board.height * cellDisplaySize + (board.height - 1) * wallStripSize + 40);

        DrawGrid(area);

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Clear All Walls"))
        {
            System.Array.Clear(board.wallRight, 0, board.wallRight.Length);
            System.Array.Clear(board.wallDown, 0, board.wallDown.Length);
            EditorUtility.SetDirty(board);
        }
    }

    private void DrawGrid(Rect area)
    {
        float startX = area.x + 20;
        float startY = area.y + 20;
        float step = cellDisplaySize + wallStripSize;

        Event e = Event.current;

        for (int y = 0; y < board.height; y++)
        {
            for (int x = 0; x < board.width; x++)
            {
                Rect cellRect = new Rect(startX + x * step, startY + y * step, cellDisplaySize, cellDisplaySize);
                bool playable = board.IsCellPlayable(x, y);
                bool prefilled = board.IsCellPrefilled(x, y);

                Color cellColor;
                if (!playable) cellColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
                else if (prefilled) cellColor = new Color(0.6f, 0.45f, 0.3f); // brown — prefilled indicate karne ke liye
                else cellColor = new Color(0.85f, 0.85f, 0.8f);

                EditorGUI.DrawRect(cellRect, cellColor);

                // Cell body pe click = prefilled toggle (sirf playable cells ke liye)
                if (playable && e.type == EventType.MouseDown && cellRect.Contains(e.mousePosition))
                {
                    int cellIdx = y * board.width + x;
                    board.isPrefilled[cellIdx] = !board.isPrefilled[cellIdx];
                    EditorUtility.SetDirty(board);
                    e.Use();
                    Repaint();
                }

                // Right wall strip
                if (x < board.width - 1)
                {
                    Rect rightStrip = new Rect(cellRect.xMax, cellRect.y, wallStripSize, cellDisplaySize);
                    int idx = y * board.width + x;
                    bool hasWall = idx < board.wallRight.Length && board.wallRight[idx];
                    EditorGUI.DrawRect(rightStrip, hasWall ? Color.red : new Color(0, 0, 0, 0.08f));

                    if (e.type == EventType.MouseDown && rightStrip.Contains(e.mousePosition))
                    {
                        board.wallRight[idx] = !board.wallRight[idx];
                        EditorUtility.SetDirty(board);
                        e.Use();
                        Repaint();
                    }
                }

                // Down wall strip
                if (y < board.height - 1)
                {
                    Rect downStrip = new Rect(cellRect.x, cellRect.yMax, cellDisplaySize, wallStripSize);
                    int idx = y * board.width + x;
                    bool hasWall = idx < board.wallDown.Length && board.wallDown[idx];
                    EditorGUI.DrawRect(downStrip, hasWall ? Color.red : new Color(0, 0, 0, 0.08f));

                    if (e.type == EventType.MouseDown && downStrip.Contains(e.mousePosition))
                    {
                        board.wallDown[idx] = !board.wallDown[idx];
                        EditorUtility.SetDirty(board);
                        e.Use();
                        Repaint();
                    }
                }
            }
        }
    }
}