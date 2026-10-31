using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class PieceGridRenderer : MonoBehaviour
{
    [Header("Board Data")]
    public BoardLevelData boardData;
    [Header("Sound Effects")]
    public AudioClip actionSound; // place, remove, aur complete — teeno ke liye same sound
    [Header("Known Piece Shapes (jo bhi drag se banayi ja sakti hain)")]
    public PieceShapeData[] availablePieces; [Header("Fill Texture (watercolor overlay)")]
    public Sprite fillTextureOverlay;
    [Header("Grid Container (RectTransform under Canvas)")]
    public RectTransform gridContainer;
    public float cellSize = 70f;
    public float cellSpacing = 0f;
    public float cellSpacingRatio = 0.04f;
    [Header("Position Adjustment")]
    public float horizontalOffset = 0f; // positive = right, negative = left
    public float verticalOffset = 0f;   // positive = upar, negative = neeche
    [Header("Board Background (optional decorative panel)")]
    public RectTransform boardBackgroundPanel;
    public float backgroundPadding = 20f;
    [Header("Auto-Fit Settings")]
    public bool autoFitToContainer = true;
    public RectTransform fitAreaBounds; // ek RectTransform jo batata hai kitni jagah available hai (jaise poora blue area)
    public float maxCellSize = 200f;
    public float minCellSize = 100f;
    [Header("Prefab")]
    public GameObject cellButtonPrefab;
    [Header("Wall Visuals")]
    public Color wallColor = new Color(0.15f, 0.1f, 0.05f);
    public Sprite wallSprite; // agar assign ho to color ki jagah ye image use hogi
    public float wallThickness = 4f;
    public Color prefilledCellColor = new Color(0.6f, 0.45f, 0.3f);
    [Header("Hint Settings")]
    public Color hintColor = new Color(0.3f, 0.8f, 0.75f);
    public float hintBlinkInterval = 0.35f;
    public int hintBlinkCount = 4;
    [Header("Board Border")]
    public Color borderColor = new Color(0.55f, 0.45f, 0.35f);
    public float borderThickness = 8f;
    public Sprite borderSprite;
    private Coroutine hintCoroutine;
    private List<GameObject> borderObjects = new List<GameObject>();
    [Header("Seam Smoothing (filled shapes ko ek solid block jaisa dikhata hai)")]
    public float seamPatchOverlap = 6f; // border sprite ki thickness se thoda zyada rakhna
    private float currentTotalCellStep;
    private float cachedFitWidth = -1f;
    private float cachedFitHeight = -1f;
    [Header("UI Buttons (state ke hisaab se show/hide)")]
    public GameObject nextButtonObject;
    public GameObject[] gameplayOnlyButtons; // Hint, Reset, Undo
    public Sprite cornerSprite; // sirf ek corner rounded wali sprite
    public float cornerPatchSize = 24f;
    [Header("Drag Wave Effect")]
    public float dragWaveDuration = 0.18f; // har cell ko highlight hote waqt kitni der mein smoothly fade-in ho
    [Header("Colors")]
    public Color emptyCellColor = new Color(0.95f, 0.95f, 0.9f);
    public Color holeCellColor = new Color(0, 0, 0, 0);
    public Color dragHighlightColor = new Color(0.7f, 0.7f, 0.9f);
    public Color[] placementColorPalette = new Color[]
    {
        new Color(1f, 0.6f, 0.2f), new Color(0.6f, 0.4f, 0.9f),
        new Color(0.4f, 0.8f, 0.5f), new Color(0.3f, 0.8f, 0.9f),
        new Color(0.95f, 0.5f, 0.7f), new Color(0.9f, 0.8f, 0.2f)
    };
    [Header("Shape Info Display (usage flash feedback)")]
    public ShapeInfoDisplay shapeInfoDisplay;
    [Header("Events")]
    public UnityEngine.Events.UnityEvent onBoardComplete;

    private Image[] cellImages;
    private bool[] isFilled;
    private bool isSolved = false;
    // Agli baar commit hone par jo color use hoga, wahi peek/preview kar leta hai
    private Color GetUpcomingPlacementColor()
    {
        return placementColorPalette[colorCycleIndex % placementColorPalette.Length];
    }
    // Drag state
    private bool isDragging = false;
    private List<Vector2Int> currentDragCells = new List<Vector2Int>();

    private List<PlacementRecord> placementHistory = new List<PlacementRecord>();
    private int colorCycleIndex = 0;
    private Dictionary<int, PlacementRecord> cellToPlacement = new Dictionary<int, PlacementRecord>();
    private int tapCandidateIndex = -1;
    private class PlacementRecord
    {

        public Vector2Int[] cells;
        public PieceShapeData piece;
        public List<GameObject> seamPatches = new List<GameObject>();
    }

    private void Start()
    {
        // Agar LevelManager use ho raha hai, wo khud RebuildLevel() call karega.
        // Agar LevelManager nahi hai (single-level testing), yahan se bhi chal jayega.
        if (boardData != null)
        {
            RebuildLevel();
        }
    }

    // ---------------- PUBLIC REBUILD (LevelManager isay call karta hai) ----------------
    public void RebuildLevel()
    {
        if (boardData == null)
        {
            Debug.LogError("PieceGridRenderer: boardData assign nahi hui.");
            return;
        }

        if (hintCoroutine != null)
        {
            StopCoroutine(hintCoroutine);
            hintCoroutine = null;
        }

        placementHistory.Clear();
        colorCycleIndex = 0;
        isSolved = false;
        isDragging = false;
        currentDragCells.Clear();
        cellToPlacement.Clear();
        tapCandidateIndex = -1;

        StartCoroutine(RebuildLevelDelayed());
    }

    // Unity ka UI layout system ek frame delay se RectTransform ki rect values update karta hai
    // (jaise fitAreaBounds.rect.width/height). Agar hum turant BuildBoard() call karein bina
    // wait kiye, purani/stale values mil sakti hain — is liye 2 frames wait karte hain taake
    // layout poora settle ho jaye, phir sahi size calculate hoga.
    private IEnumerator RebuildLevelDelayed()
    {
        yield return null; // ek frame wait
        yield return null; // safety ke liye doosra frame bhi wait (kabhi kabhi ek kaafi nahi hota)

        BuildBoard();
        UpdateButtonVisibility();
    }
    private void UpdateButtonVisibility()
    {
        if (nextButtonObject != null)
            nextButtonObject.SetActive(isSolved);

        if (gameplayOnlyButtons != null)
        {
            foreach (GameObject btn in gameplayOnlyButtons)
            {
                if (btn != null) btn.SetActive(!isSolved);
            }
        }
    }
    // ---------------- BOARD BUILD ----------------

    private void BuildBoard()
    {

        int w = boardData.width;
        int h = boardData.height;
        boardData.EnsureWallArrays();
        // Auto-fit: available area dekh kar best cell size nikaalo
        // Auto-fit: available area dekh kar best cell size nikaalo.
        // IMPORTANT: fitAreaBounds ka size sirf PEHLI baar measure karte hain aur cache kar lete hain.
        // Agar hum har level pe dobara measure karein, aur fitAreaBounds ka size kisi Layout Group/
        // Content Size Fitter ki wajah se board ke apne content (gridContainer/boardBackgroundPanel)
        // se indirectly influence ho raha ho, to ek feedback loop ban jata hai — har "Next" pe board
        // thoda thoda chhota hota jata hai. Cache karne se hamesha original sahi size use hoga.
        if (autoFitToContainer && fitAreaBounds != null)
        {
            if (cachedFitWidth < 0f || cachedFitHeight < 0f)
            {
                cachedFitWidth = fitAreaBounds.rect.width;
                cachedFitHeight = fitAreaBounds.rect.height;
            }

            float availableWidth = cachedFitWidth;
            float availableHeight = cachedFitHeight;

            float stepIfWidthLimited = availableWidth / w;
            float stepIfHeightLimited = availableHeight / h;
            float bestStep = Mathf.Min(stepIfWidthLimited, stepIfHeightLimited);

            cellSpacing = bestStep * cellSpacingRatio;

            float safeMinCellSize = Mathf.Min(minCellSize, bestStep - cellSpacing);
            cellSize = Mathf.Clamp(bestStep - cellSpacing, safeMinCellSize, maxCellSize);
        }
        cellImages = new Image[w * h];
        isFilled = new bool[w * h];

        foreach (Transform child in gridContainer)
            Destroy(child.gameObject);

        float totalCellStep = cellSize + cellSpacing;
        currentTotalCellStep = totalCellStep;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int index = y * w + x;
                bool playable = boardData.IsCellPlayable(x, y);

                GameObject cellObj = Instantiate(cellButtonPrefab, gridContainer);
                RectTransform rt = cellObj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x * totalCellStep, -y * totalCellStep);
                rt.sizeDelta = new Vector2(cellSize, cellSize);

                Image img = cellObj.GetComponent<Image>();
                bool prefilled = boardData.IsCellPrefilled(x, y);

                if (prefilled)
                {
                    img.color = prefilledCellColor;
                    img.raycastTarget = false; // player is cell ko touch nahi kar sakta

                    // Prefilled cells par bhi wahi watercolor texture overlay karo
                    if (fillTextureOverlay != null)
                    {
                        GameObject overlay = new GameObject("PrefilledTextureOverlay", typeof(RectTransform), typeof(Image));
                        RectTransform ort = overlay.GetComponent<RectTransform>();
                        ort.SetParent(cellObj.transform, false);
                        ort.anchorMin = Vector2.zero;
                        ort.anchorMax = Vector2.one;
                        ort.sizeDelta = Vector2.zero;
                        ort.anchoredPosition = Vector2.zero;

                        Image oimg = overlay.GetComponent<Image>();
                        oimg.sprite = fillTextureOverlay;
                        oimg.type = Image.Type.Simple;
                        oimg.raycastTarget = false;
                    }
                }
                else
                {
                    img.color = playable ? emptyCellColor : holeCellColor;
                    img.raycastTarget = playable;

                    if (playable)
                    {
                        DragCell dragCell = cellObj.AddComponent<DragCell>();
                        dragCell.Init(x, y, this);
                    }
                }

                cellImages[index] = img;
                //  isFilled[index] = prefilled; // prefilled cells shuru se hi "filled" mani jayengi
                isFilled[index] = prefilled || !playable;
            }
        }

        gridContainer.sizeDelta = new Vector2(w * totalCellStep, h * totalCellStep);
        

        // Background panel ko board ke exact size + halka padding ke hisaab se resize karo
        if (boardBackgroundPanel != null)
        {
            boardBackgroundPanel.sizeDelta = new Vector2(
                w * totalCellStep + backgroundPadding * 2f,
                h * totalCellStep + backgroundPadding * 2f
            );
        }
        gridLines.Clear();
        DrawWalls(w, h, totalCellStep);
        DrawBoardBorder(w, h, totalCellStep);
        DrawGridLines(w, h, totalCellStep);
        RefreshGridLineVisibility();

        // Board ka poora bounding box (0..w, 0..h) center karne ki bajaye, sirf ACTUAL
        // playable cells ki bounding box nikal ke usay center karte hain — isse har
        // board shape (chhota, bara, asymmetric) apne aap sahi center hoga, koi manual
        // per-level offset tune karne ki zaroorat nahi rahegi.
        int minPlayX = w, maxPlayX = -1, minPlayY = h, maxPlayY = -1;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (boardData.IsCellPlayable(x, y))
                {
                    if (x < minPlayX) minPlayX = x;
                    if (x > maxPlayX) maxPlayX = x;
                    if (y < minPlayY) minPlayY = y;
                    if (y > maxPlayY) maxPlayY = y;
                }
            }
        }

        if (maxPlayX < 0) { minPlayX = 0; maxPlayX = w - 1; minPlayY = 0; maxPlayY = h - 1; } // safety fallback

        float playableCenterX = (minPlayX + maxPlayX) / 2f * totalCellStep;
        float playableCenterY = -(minPlayY + maxPlayY) / 2f * totalCellStep;

        gridContainer.anchoredPosition = new Vector2(
            -playableCenterX + horizontalOffset,
            -playableCenterY + verticalOffset
        );
        gridContainer.anchoredPosition = new Vector2(
    -playableCenterX + horizontalOffset,
    -playableCenterY + verticalOffset
);

        if (boardBackgroundPanel != null)
        {
            boardBackgroundPanel.anchoredPosition = gridContainer.anchoredPosition;
        }
    }
    // Har playable-playable adjacent pair (jahan wall nahi hai) ke beech ek thin subtle line banata hai.
    // Ye line sirf tab dikhegi jab dono taraf ki cells khali hon — fill hote hi chhup jayegi.
    private void DrawGridLines(int w, int h, float totalCellStep)
    {
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!boardData.IsCellPlayable(x, y)) continue;
                int indexA = y * w + x;

                // Right neighbor ke sath line
                if (x < w - 1 && boardData.IsCellPlayable(x + 1, y) && !boardData.HasWallBetween(x, y, x + 1, y))
                {
                    int indexB = y * w + (x + 1);
                    GameObject line = new GameObject("GridLine_R_" + x + "_" + y, typeof(RectTransform), typeof(Image));
                    RectTransform rt = line.GetComponent<RectTransform>();
                    rt.SetParent(gridContainer, false);
                    rt.sizeDelta = new Vector2(gridLineThickness, cellSize * 0.9f);
                    rt.anchoredPosition = new Vector2(x * totalCellStep + totalCellStep / 2f, -y * totalCellStep);
                    Image img = line.GetComponent<Image>();
                    img.color = gridLineColor;
                    img.raycastTarget = false;

                    gridLines.Add(new GridLineEntry { obj = line, cellA = indexA, cellB = indexB });
                }

                // Down neighbor ke sath line
                if (y < h - 1 && boardData.IsCellPlayable(x, y + 1) && !boardData.HasWallBetween(x, y, x, y + 1))
                {
                    int indexB = (y + 1) * w + x;
                    GameObject line = new GameObject("GridLine_D_" + x + "_" + y, typeof(RectTransform), typeof(Image));
                    RectTransform rt = line.GetComponent<RectTransform>();
                    rt.SetParent(gridContainer, false);
                    rt.sizeDelta = new Vector2(cellSize * 0.9f, gridLineThickness);
                    rt.anchoredPosition = new Vector2(x * totalCellStep, -y * totalCellStep - totalCellStep / 2f);
                    Image img = line.GetComponent<Image>();
                    img.color = gridLineColor;
                    img.raycastTarget = false;

                    gridLines.Add(new GridLineEntry { obj = line, cellA = indexA, cellB = indexB });
                }
            }
        }
    }

    // Line sirf tab dikhegi jab dono taraf ki cells khali (unfilled) hon
    private void RefreshGridLineVisibility()
    {
        foreach (GridLineEntry entry in gridLines)
        {
            if (entry.obj == null) continue;
            bool bothEmpty = !isFilled[entry.cellA] && !isFilled[entry.cellB];
            entry.obj.SetActive(bothEmpty);
        }
    }
    // Board data mein jo walls store hain, unhe chhoti dark strips ke tor pe cells ke beech draw karta hai
    // Board data mein jo walls store hain, unhe chhoti dark strips ke tor pe cells ke exact boundary pe draw karta hai
    [Header("Grid Lines (sirf khali cells ke beech dikhengi)")]
    public Color gridLineColor = new Color(0.75f, 0.68f, 0.6f, 0.6f);
    public float gridLineThickness = 2f;

    private class GridLineEntry
    {
        public GameObject obj;
        public int cellA;
        public int cellB;
    }
    private List<GridLineEntry> gridLines = new List<GridLineEntry>();
    private void DrawWalls(int w, int h, float totalCellStep)
    {
        boardData.EnsureWallArrays();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int index = y * w + x;

                // Right wall — cell x aur cell x+1 ke EXACT beech mein (dono cell centers ka midpoint)
                if (x < w - 1 && index < boardData.wallRight.Length && boardData.wallRight[index])
                {
                    GameObject wallObj = new GameObject("Wall_R_" + x + "_" + y, typeof(RectTransform), typeof(Image));
                    RectTransform rt = wallObj.GetComponent<RectTransform>();
                    rt.SetParent(gridContainer, false);
                    rt.anchoredPosition = new Vector2(x * totalCellStep + totalCellStep / 2f, -y * totalCellStep);

                    Image img = wallObj.GetComponent<Image>();
                    if (wallSprite != null)
                    {
                        // Sprite horizontal-line ke liye bani hai, isliye vertical (Right) wall ke
                        // liye ise 90 degree rotate karte hain, aur size bhi swap kar dete hain
                        rt.sizeDelta = new Vector2(cellSize, wallThickness);
                        rt.localEulerAngles = new Vector3(0, 0, 90);
                        img.sprite = wallSprite;
                        img.type = Image.Type.Simple;
                        img.preserveAspect = false;
                        img.color = Color.white;
                    }
                    else
                    {
                        rt.sizeDelta = new Vector2(wallThickness, cellSize);
                        img.color = wallColor;
                    }
                }

                // Down wall — cell y aur cell y+1 ke EXACT beech mein
                if (y < h - 1 && index < boardData.wallDown.Length && boardData.wallDown[index])
                {
                    GameObject wallObj = new GameObject("Wall_D_" + x + "_" + y, typeof(RectTransform), typeof(Image));
                    RectTransform rt = wallObj.GetComponent<RectTransform>();
                    rt.SetParent(gridContainer, false);
                    rt.sizeDelta = new Vector2(cellSize, wallThickness);
                    rt.anchoredPosition = new Vector2(x * totalCellStep, -y * totalCellStep - totalCellStep / 2f);

                    Image img = wallObj.GetComponent<Image>();
                    if (wallSprite != null)
                    {
                        img.sprite = wallSprite;
                        img.type = Image.Type.Simple;
                        img.preserveAspect = false;
                        img.color = Color.white;
                    }
                    else
                    {
                        img.color = wallColor;
                    }
                }
            }
        }
    }
    // Playable area ke bahar ki taraf (jahan neighbor non-playable/bahar hai) border segments draw karta hai
    private void DrawBoardBorder(int w, int h, float totalCellStep)
    {
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!boardData.IsCellPlayable(x, y)) continue;

                float cx = x * totalCellStep;
                float cy = -y * totalCellStep;

                bool blockedUp = !boardData.IsCellPlayable(x, y - 1);
                bool blockedDown = !boardData.IsCellPlayable(x, y + 1);
                bool blockedLeft = !boardData.IsCellPlayable(x - 1, y);
                bool blockedRight = !boardData.IsCellPlayable(x + 1, y);

                if (blockedUp)
                    CreateBorderSegment(new Vector2(cx, cy + cellSize / 2f), new Vector2(cellSize + borderThickness, borderThickness));

                if (blockedDown)
                    CreateBorderSegment(new Vector2(cx, cy - cellSize / 2f), new Vector2(cellSize + borderThickness, borderThickness));

                if (blockedLeft)
                    CreateBorderSegment(new Vector2(cx - cellSize / 2f, cy), new Vector2(borderThickness, cellSize + borderThickness));

                if (blockedRight)
                    CreateBorderSegment(new Vector2(cx + cellSize / 2f, cy), new Vector2(borderThickness, cellSize + borderThickness));

                // Outer convex corners — jahan 2 blocked sides mil rahi hain, wahan rounded corner patch lagao
                if (blockedUp && blockedLeft)
                    CreateCornerBorderPatch(new Vector2(cx - cellSize / 2f, cy + cellSize / 2f), 0f);

                if (blockedUp && blockedRight)
                    CreateCornerBorderPatch(new Vector2(cx + cellSize / 2f, cy + cellSize / 2f), -90f);

                if (blockedDown && blockedLeft)
                    CreateCornerBorderPatch(new Vector2(cx - cellSize / 2f, cy - cellSize / 2f), 90f);

                if (blockedDown && blockedRight)
                    CreateCornerBorderPatch(new Vector2(cx + cellSize / 2f, cy - cellSize / 2f), 180f);
            }
        }
    }

    private void CreateCornerBorderPatch(Vector2 position, float rotationZ)
    {
        if (cornerSprite == null) return;

        GameObject seg = new GameObject("BorderCorner", typeof(RectTransform), typeof(Image));
        RectTransform rt = seg.GetComponent<RectTransform>();
        rt.SetParent(gridContainer, false);
        rt.sizeDelta = new Vector2(cornerPatchSize, cornerPatchSize);
        rt.anchoredPosition = position;
        rt.localEulerAngles = new Vector3(0, 0, rotationZ);

        Image img = seg.GetComponent<Image>();
        img.sprite = cornerSprite;
        img.color = borderColor;
        img.raycastTarget = false;

        borderObjects.Add(seg);
    }
    private void RaiseBorderToFront()
    {
        foreach (GameObject obj in borderObjects)
        {
            if (obj != null) obj.transform.SetAsLastSibling();
        }
    }
    private void CreateBorderSegment(Vector2 position, Vector2 size)
    {
        GameObject seg = new GameObject("BorderSegment", typeof(RectTransform), typeof(Image));
        RectTransform rt = seg.GetComponent<RectTransform>();
        rt.SetParent(gridContainer, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;

        Image img = seg.GetComponent<Image>();
        if (borderSprite != null)
        {
            img.sprite = borderSprite;
            img.type = Image.Type.Sliced;
        }
        img.color = Color.white; // sprite ke apne gradient colors use hon, tint na ho
        img.raycastTarget = false;

        borderObjects.Add(seg);
    }
    // ---------------- DRAG INPUT (DragCell se call hota hai) ----------------

    //public void BeginDrag(int x, int y)
    //{
    //    if (isSolved) return;
    //    if (!boardData.IsCellPlayable(x, y)) return;

    //    int index = y * boardData.width + x;
    //    if (isFilled[index]) return;

    //    isDragging = true;
    //    currentDragCells.Clear();
    //    currentDragCells.Add(new Vector2Int(x, y));
    //    cellImages[index].color = dragHighlightColor;
    //}
    public void BeginDrag(int x, int y)
    {
        if (isSolved) return;
        if (!boardData.IsCellPlayable(x, y)) return;

        int index = y * boardData.width + x;

        // Agar cell already filled hai, ye "tap to remove" ka candidate hai — drag shuru nahi karte
        if (isFilled[index])
        {
            tapCandidateIndex = index;
            isDragging = false;
            currentDragCells.Clear();
            return;
        }

        tapCandidateIndex = -1;
        isDragging = true;
        currentDragCells.Clear();
        currentDragCells.Add(new Vector2Int(x, y));
        AnimateCellTo(index, GetUpcomingPlacementColor(), dragWaveDuration);
    }
    public void ContinueDrag(int x, int y)
    {
        if (!isDragging) return;
        if (!boardData.IsCellPlayable(x, y)) return;

        Vector2Int cell = new Vector2Int(x, y);
        int index = y * boardData.width + x;

        if (isFilled[index]) return;
        if (currentDragCells.Contains(cell)) return;

        // Pichli dragged cell se is cell tak wall hai to yahan drag rok do
        Vector2Int lastCell = currentDragCells[currentDragCells.Count - 1];
        if (boardData.HasWallBetween(lastCell.x, lastCell.y, x, y)) return;

        currentDragCells.Add(cell);
        AnimateCellTo(index, GetUpcomingPlacementColor(), dragWaveDuration);
    }
    //public void EndDrag()
    //{
    //    if (!isDragging) return;
    //    isDragging = false;

    //    if (currentDragCells.Count == 0) return;

    //    HashSet<Vector2Int> drawnShape = NormalizeCells(currentDragCells);
    //    PieceShapeData matchedPiece = FindMatchingPiece(drawnShape);

    //    if (matchedPiece != null)
    //    {
    //        CommitPlacement(currentDragCells);
    //    }
    //    else
    //    {
    //        RevertDragHighlight();
    //    }

    //    currentDragCells.Clear();
    //}
    public void EndDrag(int x, int y)
    {
        int index = y * boardData.width + x;

        // Tap-to-remove: pointer isi filled cell pe down aur up hua, bina drag kiye
        if (!isDragging && tapCandidateIndex == index && isFilled[index])
        {
            RemovePlacementAtCell(index);
            tapCandidateIndex = -1;
            return;
        }

        tapCandidateIndex = -1;

        if (!isDragging) return;
        isDragging = false;

        if (currentDragCells.Count == 0) return;

        HashSet<Vector2Int> drawnShape = NormalizeCells(currentDragCells);
        PieceShapeData matchedPiece = FindMatchingPiece(drawnShape);

        if (matchedPiece != null)
        {
            CommitPlacement(currentDragCells, matchedPiece);
        }
        else
        {
            RevertDragHighlight();
        }

        currentDragCells.Clear();
    }

    // ---------------- SHAPE MATCHING ----------------

    private HashSet<Vector2Int> NormalizeCells(List<Vector2Int> cells)
    {
        int minX = cells.Min(c => c.x);
        int minY = cells.Min(c => c.y);

        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        foreach (Vector2Int c in cells)
            result.Add(new Vector2Int(c.x - minX, c.y - minY));

        return result;
    }

    private PieceShapeData FindMatchingPiece(HashSet<Vector2Int> drawnShape)
    {
        foreach (PieceShapeData piece in availablePieces)
        {
            foreach (HashSet<Vector2Int> rotatedShape in GetAllRotations(piece.cellOffsets))
            {
                if (drawnShape.SetEquals(rotatedShape))
                    return piece;
            }
        }
        return null;
    }

    // Ek shape ke 4 possible rotations (0, 90, 180, 270 degree) return karta hai,
    // har ek normalized (0,0) se shuru hota hua. Isse player shape ko kisi bhi
    // orientation mein draw kare, match ho jayegi.
    public static List<HashSet<Vector2Int>> GetAllRotations(Vector2Int[] baseOffsets)
    {
        List<HashSet<Vector2Int>> rotations = new List<HashSet<Vector2Int>>();
        Vector2Int[] current = baseOffsets;

        for (int i = 0; i < 4; i++)
        {
            rotations.Add(NormalizeOffsets(current));
            current = RotateOffsets90(current);
        }

        return rotations;
    }

    private static Vector2Int[] RotateOffsets90(Vector2Int[] offsets)
    {
        Vector2Int[] rotated = new Vector2Int[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
        {
            // 90 degree clockwise rotation: (x, y) -> (-y, x)
            rotated[i] = new Vector2Int(-offsets[i].y, offsets[i].x);
        }
        return rotated;
    }

    private static HashSet<Vector2Int> NormalizeOffsets(Vector2Int[] offsets)
    {
        int minX = offsets.Min(o => o.x);
        int minY = offsets.Min(o => o.y);

        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        foreach (Vector2Int o in offsets)
            result.Add(new Vector2Int(o.x - minX, o.y - minY));

        return result;
    }

    // ---------------- COMMIT / REVERT ----------------

    //private void CommitPlacement(List<Vector2Int> cells)
    //{
    //    Color placementColor = placementColorPalette[colorCycleIndex % placementColorPalette.Length];
    //    colorCycleIndex++;

    //    foreach (Vector2Int cell in cells)
    //    {
    //        int index = cell.y * boardData.width + cell.x;
    //        isFilled[index] = true;
    //        cellImages[index].color = placementColor;
    //    }

    //    placementHistory.Add(new PlacementRecord { cells = cells.ToArray() });
    //    CheckWinCondition();
    // Diye gaye cell index ko smoothly target color tak animate karta hai (wave/ripple feel).
    // Agar us cell ki koi purani animation chal rahi ho to usay rok kar naya start karta hai.

    private void AnimateCellTo(int index, Color targetColor, float duration)
{
    if (cellAnimCoroutines.TryGetValue(index, out Coroutine existing) && existing != null)
        StopCoroutine(existing);

    Coroutine c = StartCoroutine(AnimateSingleCellColor(cellImages[index], targetColor, duration));
    cellAnimCoroutines[index] = c;
}
    private Dictionary<int, Coroutine> cellAnimCoroutines = new Dictionary<int, Coroutine>();
    private void CommitPlacement(List<Vector2Int> cells, PieceShapeData piece)
    {
        Color placementColor = placementColorPalette[colorCycleIndex % placementColorPalette.Length];
        colorCycleIndex++;

        PlacementRecord record = new PlacementRecord { cells = cells.ToArray(), piece = piece };

        List<Image> imagesToAnimate = new List<Image>();
        foreach (Vector2Int cell in cells)
        {
            int index = cell.y * boardData.width + cell.x;
            isFilled[index] = true;
            cellToPlacement[index] = record;
            imagesToAnimate.Add(cellImages[index]);
        }
        StartCoroutine(AnimateFillSequential(imagesToAnimate, placementColor, 0.20f, 0.05f));

        CreateSeamPatches(cells, placementColor, record);
        CreateShapeTextureOverlay(cells, record);
        RaiseBorderToFront();

        placementHistory.Add(record);

        if (shapeInfoDisplay != null && piece != null)
            shapeInfoDisplay.FlashPieceUsed(piece, placementColor);

        RefreshGridLineVisibility();
        TriggerHaptic();
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySfx(actionSound);
        CheckWinCondition();
    }

    // Cells ka color instant badalne ki bajaye smoothly lerp karta hai — chunky/pop feel ke sath
    // Har cell ko thodi thodi der ke gap se ek-ek karke fill karta hai (cascade/wave effect)
    private IEnumerator AnimateFillSequential(List<Image> images, Color targetColor, float perCellDuration, float staggerDelay)
    {
        foreach (Image img in images)
        {
            if (img != null)
                StartCoroutine(AnimateSingleCellColor(img, targetColor, perCellDuration));

            yield return new WaitForSeconds(staggerDelay);
        }
    }

    // Ek single cell ka color smoothly lerp karta hai
    private IEnumerator AnimateSingleCellColor(Image img, Color targetColor, float duration)
    {
        Color startColor = img.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out

            if (img != null)
                img.color = Color.Lerp(startColor, targetColor, eased);
            else
                yield break;

            yield return null;
        }

        if (img != null) img.color = targetColor;
    }
    // Poori shape ki bounding box par EK hi texture overlay lagata hai — isse cells ke
    // beech koi seam/line nahi dikhti, texture continuously flow karti hai
    private void CreateShapeTextureOverlay(List<Vector2Int> cells, PlacementRecord record)
    {
        if (fillTextureOverlay == null) return;

        int minX = cells.Min(c => c.x);
        int maxX = cells.Max(c => c.x);
        int minY = cells.Min(c => c.y);
        int maxY = cells.Max(c => c.y);

        int cols = maxX - minX + 1;
        int rows = maxY - minY + 1;

        float centerX = (minX + maxX) / 2f * currentTotalCellStep;
        float centerY = -(minY + maxY) / 2f * currentTotalCellStep;
        float width = cols * currentTotalCellStep;
        float height = rows * currentTotalCellStep;

        // Runtime mask texture banao — sirf wahi cells white(opaque) hongi jo asal shape ka hissa hain,
        // baaki (jaise L-shape ke gaps) transparent rahengi
        HashSet<Vector2Int> cellSet = new HashSet<Vector2Int>(cells);
        int cellRes = 16; // per-cell resolution, sharp edges ke liye kaafi hai
        Texture2D maskTex = new Texture2D(cols * cellRes, rows * cellRes, TextureFormat.RGBA32, false);
        maskTex.filterMode = FilterMode.Bilinear;
        maskTex.wrapMode = TextureWrapMode.Clamp;

        Color32 opaque = new Color32(255, 255, 255, 255);
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32[] pixels = new Color32[maskTex.width * maskTex.height];

        for (int gy = 0; gy < rows; gy++)
        {
            for (int gx = 0; gx < cols; gx++)
            {
                bool filled = cellSet.Contains(new Vector2Int(minX + gx, minY + gy));
                Color32 col = filled ? opaque : clear;

                // Texture ka Y axis neeche se upar hota hai, isliye row ko flip karte hain
                int texRow = rows - 1 - gy;

                for (int py = 0; py < cellRes; py++)
                {
                    int rowStart = (texRow * cellRes + py) * maskTex.width;
                    for (int px = 0; px < cellRes; px++)
                    {
                        pixels[rowStart + gx * cellRes + px] = col;
                    }
                }
            }
        }

        maskTex.SetPixels32(pixels);
        maskTex.Apply();

        Sprite maskSprite = Sprite.Create(maskTex, new Rect(0, 0, maskTex.width, maskTex.height), new Vector2(0.5f, 0.5f));

        // Mask container — isi ke andar wali texture sirf actual shape ke andar hi dikhegi
        GameObject maskContainer = new GameObject("ShapeMaskContainer", typeof(RectTransform), typeof(Image), typeof(Mask));
        RectTransform mrt = maskContainer.GetComponent<RectTransform>();
        mrt.SetParent(gridContainer, false);
        mrt.sizeDelta = new Vector2(width, height);
        mrt.anchoredPosition = new Vector2(centerX, centerY);

        Image maskImg = maskContainer.GetComponent<Image>();
        maskImg.sprite = maskSprite;
        maskImg.raycastTarget = false;

        Mask mask = maskContainer.GetComponent<Mask>();
        mask.showMaskGraphic = false; // sirf clipping ke liye, khud dikhega nahi

        // Texture overlay, mask ke andar (child) — sirf shape ke cells mein hi visible hoga
        GameObject overlay = new GameObject("TextureOverlay", typeof(RectTransform), typeof(Image));
        RectTransform ort = overlay.GetComponent<RectTransform>();
        ort.SetParent(maskContainer.transform, false);
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.sizeDelta = Vector2.zero;
        ort.anchoredPosition = Vector2.zero;

        Image oimg = overlay.GetComponent<Image>();
        oimg.sprite = fillTextureOverlay;
        oimg.type = Image.Type.Simple;
        oimg.raycastTarget = false;

        record.seamPatches.Add(maskContainer);
    }
    // Shape ke andar jo cells aapas mein adjacent hain, unke beech ki seam (border line)
    // ko plain-color patch se cover kar deta hai — isse poori shape ek solid block jaisi lagti hai
    private void CreateSeamPatches(List<Vector2Int> cells, Color color, PlacementRecord record)
    {
        HashSet<Vector2Int> cellSet = new HashSet<Vector2Int>(cells);

        foreach (Vector2Int cell in cells)
        {
            float cx = cell.x * currentTotalCellStep;
            float cy = -cell.y * currentTotalCellStep;

            Vector2Int rightNeighbor = new Vector2Int(cell.x + 1, cell.y);
            if (cellSet.Contains(rightNeighbor))
            {
                GameObject patch = CreatePlainPatch(
                    new Vector2(cx + currentTotalCellStep / 2f, cy),
                    new Vector2(cellSpacing + seamPatchOverlap, cellSize),
                    color);
                record.seamPatches.Add(patch);
            }

            Vector2Int downNeighbor = new Vector2Int(cell.x, cell.y + 1);
            if (cellSet.Contains(downNeighbor))
            {
                GameObject patch = CreatePlainPatch(
                    new Vector2(cx, cy - currentTotalCellStep / 2f),
                    new Vector2(cellSpacing + seamPatchOverlap, cellSpacing + seamPatchOverlap),
                            color);
                record.seamPatches.Add(patch);
            }
        }

        CreateCornerPatches(cellSet, color, record);
    }

    // Jahan L-corner ya T/Plus junction banta hai (4 cells ke beech ka vertex point),
    // wahan seedhi patches ke corner par ek chhota sa border-stub reh jata hai — ye function usay bhi dhak deta hai
    private void CreateCornerPatches(HashSet<Vector2Int> cellSet, Color color, PlacementRecord record)
    {
        HashSet<Vector2Int> processedVertices = new HashSet<Vector2Int>();

        foreach (Vector2Int cell in cellSet)
        {
            int[] dxs = { 0, 1 };
            int[] dys = { 0, 1 };

            foreach (int dx in dxs)
            {
                foreach (int dy in dys)
                {
                    Vector2Int vertex = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (processedVertices.Contains(vertex)) continue;
                    processedVertices.Add(vertex);

                    Vector2Int topLeft = new Vector2Int(vertex.x - 1, vertex.y - 1);
                    Vector2Int topRight = new Vector2Int(vertex.x, vertex.y - 1);
                    Vector2Int bottomLeft = new Vector2Int(vertex.x - 1, vertex.y);
                    Vector2Int bottomRight = new Vector2Int(vertex.x, vertex.y);

                    // Ab kisi bhi placement ki filled cell count hogi, sirf isi shape ki nahi
                    int count = 0;


                    if (cellSet.Contains(topLeft)) count++;
                    if (cellSet.Contains(topRight)) count++;
                    if (cellSet.Contains(bottomLeft)) count++;
                    if (cellSet.Contains(bottomRight)) count++;


                    if (count >= 3)
                    {
                        float vx = vertex.x * currentTotalCellStep - currentTotalCellStep / 2f;
                        float vy = -vertex.y * currentTotalCellStep + currentTotalCellStep / 2f;
                        GameObject patch = CreatePlainPatch(
                            new Vector2(vx, vy),
                           new Vector2(cellSpacing + seamPatchOverlap, cellSpacing + seamPatchOverlap),
                            color);
                        record.seamPatches.Add(patch);
                    }
                }
            }
        }
    }

    // Helper — batata hai ke di gayi cell (kisi bhi color/placement ki ho) currently filled hai ya nahi

    private GameObject CreatePlainPatch(Vector2 position, Vector2 size, Color color)
    {
        GameObject patch = new GameObject("SeamPatch", typeof(RectTransform), typeof(Image));
        RectTransform rt = patch.GetComponent<RectTransform>();
        rt.SetParent(gridContainer, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;

        Image img = patch.GetComponent<Image>();
        img.sprite = null; // plain color, koi border sprite nahi
        img.color = color;
        img.raycastTarget = false; // patch kisi click/drag ko block na kare

        rt.SetAsLastSibling(); // hamesha sabse upar render ho, cell borders ke neeche na chhupe

        return patch;
    }
    // Filled cell pe tap karne se wahi poori shape (jis placement se ye cell bani thi) remove ho jati hai
    private void RemovePlacementAtCell(int cellIndex)
    {
        if (isSolved) return; // solve hone ke baad lock

        if (!cellToPlacement.TryGetValue(cellIndex, out PlacementRecord record)) return;

        placementHistory.Remove(record);

        foreach (Vector2Int cell in record.cells)
        {
            int idx = cell.y * boardData.width + cell.x;
            isFilled[idx] = false;
            cellImages[idx].color = emptyCellColor;
            cellToPlacement.Remove(idx);
        }
        foreach (GameObject patch in record.seamPatches)
            if (patch != null) Destroy(patch);
        record.seamPatches.Clear();
        RefreshGridLineVisibility();
        SimpleHaptics.LightTick(showDebugToast: true);
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySfx(actionSound);
    }
    // Mobile pe halka vibration — Android/iOS dono pe kaam karta hai
    // Mobile pe halka vibration — Android pe intensity/duration control ke sath,
    // iOS pe fallback default vibration (iOS se duration control possible nahi bina extra plugin ke)
    private void TriggerHaptic()
    {
        if (SettingsManager.Instance != null && !SettingsManager.Instance.HapticsEnabled) return;

        // showDebugToast: true rakho testing ke liye (phone pe halka "Haptic fired OK" message dikhega),
        // final release se pehle false kar dena
        SimpleHaptics.LightTick(showDebugToast: true);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
private void TriggerAndroidNativeHaptic()
{
    try
    {
        AndroidJavaObject activity = null;

        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        if (activity == null) return;

        // UI thread par run karna zaroori hai — warna performHapticFeedback
        // silently fail ho jata hai (koi error nahi deta, bas kaam nahi karta).
        // 'activity' yahan 'using' mein NAHI hai — kyunki ye runnable baad mein
        // async chalta hai, aur 'using' turant dispose kar deta jo bug create kar raha tha.
        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            using (AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow"))
            using (AndroidJavaObject decorView = window.Call<AndroidJavaObject>("getDecorView"))
            {
                // HapticFeedbackConstants.KEYBOARD_TAP = 3 — halka "tick" click feel
                int hapticConstant = 3;
                // FLAG_IGNORE_VIEW_SETTING (0x02) taake ye system settings pe depend na kare
                int flagIgnoreViewSetting = 2;
                decorView.Call<bool>("performHapticFeedback", hapticConstant, flagIgnoreViewSetting);
            }

            activity.Dispose(); // ab runnable poora ho chuka hai, ab safe hai dispose karna
        }));
    }
    catch (System.Exception e)
    {
        Debug.LogError("Native haptic failed: " + e.Message);
    }
}
#endif


    private void RevertDragHighlight()
    {
        foreach (Vector2Int cell in currentDragCells)
        {
            int index = cell.y * boardData.width + cell.x;
            if (!isFilled[index])
                AnimateCellTo(index, emptyCellColor, dragWaveDuration);
        }
    }

    // ---------------- UNDO / RESET ----------------
    public void UndoLastPlacement()
    {
        if (isSolved || placementHistory.Count == 0) return;

        PlacementRecord last = placementHistory[placementHistory.Count - 1];
        placementHistory.RemoveAt(placementHistory.Count - 1);

        foreach (Vector2Int cell in last.cells)
        {
            int index = cell.y * boardData.width + cell.x;
            isFilled[index] = false;
            cellImages[index].color = emptyCellColor;
            cellToPlacement.Remove(index);
        }
        foreach (GameObject patch in last.seamPatches)
            if (patch != null) Destroy(patch);
        last.seamPatches.Clear();
        RefreshGridLineVisibility();
    }
    //public void UndoLastPlacement()
    //{
    //    if (isSolved || placementHistory.Count == 0) return;

    //    PlacementRecord last = placementHistory[placementHistory.Count - 1];
    //    placementHistory.RemoveAt(placementHistory.Count - 1);

    //    foreach (Vector2Int cell in last.cells)
    //    {
    //        int index = cell.y * boardData.width + cell.x;
    //        isFilled[index] = false;
    //        cellImages[index].color = emptyCellColor;
    //    }
    //}

    public void ResetBoard()
    {
        RebuildLevel();
    }

    // ---------------- WIN CHECK ----------------

    private void CheckWinCondition()
    {
        for (int i = 0; i < isFilled.Length; i++)
        {
            if (boardData.isPlayable[i] && !isFilled[i]) return;
        }

        isSolved = true;
        Debug.Log("Board Complete! Picture Perfect!");
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySfx(actionSound);
        UpdateButtonVisibility();
        onBoardComplete?.Invoke();
    }
    // ---------------- HINT ----------------


    // ---------------- HINT ----------------
    public Color wrongPlacementColor = new Color(0.9f, 0.3f, 0.3f);
    public void ShowHint()
    {
        if (isSolved || boardData == null || availablePieces == null) return;

        if (hintCoroutine != null) StopCoroutine(hintCoroutine);
        hintCoroutine = StartCoroutine(HintRoutine());
    }

    private IEnumerator HintRoutine()
    {
        List<LevelSolver.PlacementInfo> solution = LevelSolver.SolveAndGetPlacements(boardData, availablePieces, isFilled);

        // Jab tak current state se solve nahi ho raha, sabse recent placement hatate jao
        int safetyLimit = placementHistory.Count + 1; // infinite loop se bachne ke liye
        while ((solution == null || solution.Count == 0) && placementHistory.Count > 0 && safetyLimit > 0)
        {
            safetyLimit--;

            PlacementRecord last = placementHistory[placementHistory.Count - 1];
            int firstIdx = last.cells[0].y * boardData.width + last.cells[0].x;
            RemovePlacementAtCell(firstIdx);

            solution = LevelSolver.SolveAndGetPlacements(boardData, availablePieces, isFilled);
        }

        if (solution == null || solution.Count == 0)
        {
            Debug.Log("Hint: is state se board solve nahi ho sakta.");
            hintCoroutine = null;
            yield break;
        }

        LevelSolver.PlacementInfo hintPlacement = solution[0];
        yield return StartCoroutine(FlashHintCells(hintPlacement.cellIndices));

        hintCoroutine = null;
    }

    // Existing placements mein se dhoondta hai ke kaunsi ek placement hataane se
    // board dobara solvable ho jata hai — wahi "galat" placement maani jati hai
    private PlacementRecord FindBlockingPlacement()
    {
        foreach (PlacementRecord record in placementHistory)
        {
            bool[] testFilled = (bool[])isFilled.Clone();
            foreach (Vector2Int cell in record.cells)
            {
                int idx = cell.y * boardData.width + cell.x;
                testFilled[idx] = false;
            }

            List<LevelSolver.PlacementInfo> testSolution = LevelSolver.SolveAndGetPlacements(boardData, availablePieces, testFilled);
            if (testSolution != null && testSolution.Count > 0)
            {
                return record;
            }
        }

        return null;
    }

    // Diye gaye cells ko ek color mein N baar blink karta hai (hint aur wrong-placement dono ke liye reusable)
    private IEnumerator FlashCellsColor(Vector2Int[] cells, Color flashColor, int blinkCount)
    {
        int[] indices = new int[cells.Length];
        Color[] originalColors = new Color[cells.Length];
        for (int i = 0; i < cells.Length; i++)
        {
            indices[i] = cells[i].y * boardData.width + cells[i].x;
            originalColors[i] = cellImages[indices[i]].color;
        }

        for (int blink = 0; blink < blinkCount; blink++)
        {
            foreach (int idx in indices)
                cellImages[idx].color = flashColor;

            yield return new WaitForSeconds(hintBlinkInterval);

            for (int i = 0; i < indices.Length; i++)
                cellImages[indices[i]].color = originalColors[i];

            yield return new WaitForSeconds(hintBlinkInterval);
        }
    }

    private IEnumerator FlashHintCells(List<int> cellIndices)
    {
        // Original colors yaad rakho taake wapis restore ho sakein
        Color[] originalColors = new Color[cellIndices.Count];
        for (int i = 0; i < cellIndices.Count; i++)
            originalColors[i] = cellImages[cellIndices[i]].color;

        for (int blink = 0; blink < hintBlinkCount; blink++)
        {
            foreach (int idx in cellIndices)
                cellImages[idx].color = hintColor;

            yield return new WaitForSeconds(hintBlinkInterval);

            for (int i = 0; i < cellIndices.Count; i++)
            {
                // Agar is dauran player ne khud fill kar diya ho to us cell ka original color na thopo
                if (!isFilled[cellIndices[i]])
                    cellImages[cellIndices[i]].color = originalColors[i];
            }

            yield return new WaitForSeconds(hintBlinkInterval);
        }
    }
}