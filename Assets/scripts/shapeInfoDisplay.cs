using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// Level shuru hote hi 'Available Pieces' ki chhoti mini-preview icons banata hai,
// taake player ko pata chale is level mein kaunsi shapes milne wali hain.
// Jab bhi koi shape board par place hoti hai, uska icon usi color mein
// thodi der flash ho jata hai — visual confirmation ke tor par.
public class ShapeInfoDisplay : MonoBehaviour
{
    [Header("References")]
    public PieceGridRenderer gridRenderer;   // wahi GameManager wali script, jahan se Available Pieces milengi
    public RectTransform shapeInfoContainer; // ShapeInfoBox ke andar wala RectTransform jahan icons banenge

    [Header("Icon Settings")]
    public GameObject miniCellPrefab; // ek chhota Image (jaise 8x8), plain color
    public float miniCellSize = 8f;
    public float miniCellSpacing = 1f;
    public float iconBoxSize = 50f;   // har piece ke liye total icon area
    public float iconSpacing = 20f;   // do pieces ke icons ke beech gap
    public Color iconColor = new Color(0.6f, 0.55f, 0.5f);

    [Header("Flash Feedback")]
    public float flashDuration = 0.4f; // kitni der colored rehna hai

    // Har piece ke mini-cells ki Image list, taake flash karte waqt inhe access kar sakein
    private Dictionary<PieceShapeData, List<Image>> pieceIconImages = new Dictionary<PieceShapeData, List<Image>>();
    private Dictionary<PieceShapeData, Coroutine> activeFlashes = new Dictionary<PieceShapeData, Coroutine>();

    private void Start()
    {
        BuildShapeIcons();
    }

    public void BuildShapeIcons()
    {
        if (gridRenderer == null || gridRenderer.availablePieces == null) return;

        foreach (Transform child in shapeInfoContainer)
            Destroy(child.gameObject);

        pieceIconImages.Clear();
        activeFlashes.Clear();

        float xCursor = 0f;

        foreach (PieceShapeData piece in gridRenderer.availablePieces)
        {
            GameObject iconHolder = new GameObject(piece.name + "_Icon", typeof(RectTransform));
            RectTransform holderRT = iconHolder.GetComponent<RectTransform>();
            holderRT.SetParent(shapeInfoContainer, false);
            holderRT.sizeDelta = new Vector2(iconBoxSize, iconBoxSize);
            holderRT.anchoredPosition = new Vector2(xCursor, 0);

            DrawMiniShape(piece, holderRT);

            xCursor += iconBoxSize + iconSpacing;
        }

        shapeInfoContainer.sizeDelta = new Vector2(xCursor, iconBoxSize);
    }

    private void DrawMiniShape(PieceShapeData piece, RectTransform parent)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (Vector2Int offset in piece.cellOffsets)
        {
            minX = Mathf.Min(minX, offset.x);
            maxX = Mathf.Max(maxX, offset.x);
            minY = Mathf.Min(minY, offset.y);
            maxY = Mathf.Max(maxY, offset.y);
        }

        int shapeWidth = maxX - minX + 1;
        int shapeHeight = maxY - minY + 1;
        float step = miniCellSize + miniCellSpacing;

        float totalWidth = shapeWidth * step;
        float totalHeight = shapeHeight * step;
        float startX = -totalWidth / 2f + miniCellSize / 2f;
        float startY = totalHeight / 2f - miniCellSize / 2f;

        List<Image> images = new List<Image>();

        foreach (Vector2Int offset in piece.cellOffsets)
        {
            int localX = offset.x - minX;
            int localY = offset.y - minY;

            GameObject cell = Instantiate(miniCellPrefab, parent);
            RectTransform cellRT = cell.GetComponent<RectTransform>();
            cellRT.sizeDelta = new Vector2(miniCellSize, miniCellSize);
            cellRT.anchoredPosition = new Vector2(startX + localX * step, startY - localY * step);

            Image img = cell.GetComponent<Image>();
            if (img != null)
            {
                img.color = iconColor;
                images.Add(img);
            }
        }

        pieceIconImages[piece] = images;
    }

    // PieceGridRenderer se call hota hai jab bhi wo piece board par place hoti hai —
    // icon ki cells usi placement color mein thodi der flash ho jati hain
    public void FlashPieceUsed(PieceShapeData piece, Color flashColor)
    {
        if (piece == null || !pieceIconImages.ContainsKey(piece)) return;

        if (activeFlashes.TryGetValue(piece, out Coroutine existing) && existing != null)
            StopCoroutine(existing);

        activeFlashes[piece] = StartCoroutine(FlashRoutine(piece, flashColor));
    }

    private IEnumerator FlashRoutine(PieceShapeData piece, Color flashColor)
    {
        List<Image> images = pieceIconImages[piece];

        foreach (Image img in images)
            if (img != null) img.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        foreach (Image img in images)
            if (img != null) img.color = iconColor;

        activeFlashes[piece] = null;
    }
}