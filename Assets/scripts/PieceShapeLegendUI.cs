using UnityEngine;
using UnityEngine.UI;
using System.Linq;

// Level ke start mein "yeh shapes banani hain" wala info box banata hai.
// Har piece ka chota icon uske cellOffsets se automatically generate hota hai.
public class PieceShapeLegendUI : MonoBehaviour
{
    [Header("Pieces Jo Is Level Mein Available Hain")]
    public PieceShapeData[] pieces;

    [Header("Container (Horizontal Layout Group wala box, Canvas ke top mein)")]
    public RectTransform iconRowContainer;

    [Header("Icon Cell Ka Chota Square Prefab")]
    public GameObject iconCellPrefab;

    [Header("Icon Sizing")]
    public float iconCellSize = 14f;
    public float iconCellSpacing = 2f;

    private void Start()
    {
        BuildLegend();
    }

    public void BuildLegend()
    {
        foreach (Transform child in iconRowContainer)
            Destroy(child.gameObject);

        foreach (PieceShapeData piece in pieces)
        {
            CreateIconForPiece(piece);
        }
    }

    private void CreateIconForPiece(PieceShapeData piece)
    {
        // Piece ki bounding box nikalo taake icon box sahi size ka bane
        int minX = piece.cellOffsets.Min(c => c.x);
        int maxX = piece.cellOffsets.Max(c => c.x);
        int minY = piece.cellOffsets.Min(c => c.y);
        int maxY = piece.cellOffsets.Max(c => c.y);

        int widthCells = (maxX - minX) + 1;
        int heightCells = (maxY - minY) + 1;

        float step = iconCellSize + iconCellSpacing;

        // Ek empty box banao jo is piece ke saare mini-cells ko hold karega
        GameObject iconBox = new GameObject($"Icon_{piece.pieceName}", typeof(RectTransform));
        iconBox.transform.SetParent(iconRowContainer, false);

        RectTransform boxRt = iconBox.GetComponent<RectTransform>();
        boxRt.sizeDelta = new Vector2(widthCells * step, heightCells * step);

        foreach (Vector2Int offset in piece.cellOffsets)
        {
            int normX = offset.x - minX;
            int normY = offset.y - minY;

            GameObject cellObj = Instantiate(iconCellPrefab, boxRt);
            RectTransform cellRt = cellObj.GetComponent<RectTransform>();
            cellRt.sizeDelta = new Vector2(iconCellSize, iconCellSize);
            cellRt.anchoredPosition = new Vector2(normX * step, -normY * step);
        }
    }
}
