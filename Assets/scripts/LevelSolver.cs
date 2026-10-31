using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Backtracking algorithm: check karta hai ke di gayi pieces se
// board POORA fill ho sakta hai ya nahi (bina overlap, bina gap).
// Koi rotation try nahi karta — kyunke game mein bhi pieces sirf
// waise hi place hoti hain jaisi PieceShapeData mein defined hain.
public static class LevelSolver
{
    // Ek piece ki ek placement ka record — hint dikhane ke kaam aata hai
    public class PlacementInfo
    {
        public PieceShapeData piece;
        public List<int> cellIndices;
    }

    public static bool IsSolvable(BoardLevelData board, PieceShapeData[] pieces)
    {
        if (board == null || pieces == null || pieces.Length == 0) return false;

        bool[] filled = BuildInitialFilledState(board);
        return SolveRecursive(board, pieces, filled, null);
    }

    // Current game state (currentFilledState) se aage poora board solve karne ki koshish karta hai.
    // Agar solve ho sake, to placements ki poori list return karta hai (order mein — pehli
    // placement hint ke liye use karo). Agar solve na ho sake, null return karta hai.
    public static List<PlacementInfo> SolveAndGetPlacements(BoardLevelData board, PieceShapeData[] pieces, bool[] currentFilledState)
    {
        if (board == null || pieces == null || pieces.Length == 0 || currentFilledState == null) return null;

        board.EnsureWallArrays();

        bool[] filled = (bool[])currentFilledState.Clone();
        List<PlacementInfo> placements = new List<PlacementInfo>();

        bool solved = SolveRecursive(board, pieces, filled, placements);
        return solved ? placements : null;
    }

    private static bool[] BuildInitialFilledState(BoardLevelData board)
    {
        board.EnsureWallArrays(); // isPrefilled array bhi ensure ho jayega

        bool[] filled = new bool[board.width * board.height];

        // Holes (non-playable) aur Prefilled cells dono ko "already filled/blocked" maante hain —
        // solver in dono jagah pe koi piece place karne ki koshish nahi karega
        for (int y = 0; y < board.height; y++)
            for (int x = 0; x < board.width; x++)
                if (!board.IsCellPlayable(x, y) || board.IsCellPrefilled(x, y))
                    filled[y * board.width + x] = true;

        return filled;
    }

    private static bool SolveRecursive(BoardLevelData board, PieceShapeData[] pieces, bool[] filled, List<PlacementInfo> placements)
    {
        int firstEmptyIndex = FindFirstEmptyCell(filled);

        if (firstEmptyIndex == -1) return true; // poora fill ho chuka

        int fx = firstEmptyIndex % board.width;
        int fy = firstEmptyIndex / board.width;

        foreach (PieceShapeData piece in pieces)
        {
            if (piece == null || piece.cellOffsets == null || piece.cellOffsets.Length == 0) continue;

            // Har piece ki 4 rotations try karo — koi bhi orientation valid hai
            foreach (List<Vector2Int> rotatedOffsets in GetUniqueRotations(piece.cellOffsets))
            {
                foreach (Vector2Int anchorOffset in rotatedOffsets)
                {
                    int originX = fx - anchorOffset.x;
                    int originY = fy - anchorOffset.y;

                    if (TryPlacePiece(board, rotatedOffsets, originX, originY, filled, out List<int> placedIndices))
                    {
                        foreach (int idx in placedIndices) filled[idx] = true;
                        placements?.Add(new PlacementInfo { piece = piece, cellIndices = placedIndices });

                        if (SolveRecursive(board, pieces, filled, placements))
                            return true;

                        // Backtrack
                        foreach (int idx in placedIndices) filled[idx] = false;
                        placements?.RemoveAt(placements.Count - 1);
                    }
                }
            }
        }

        return false;
    }

    // Piece ki 4 rotations nikalta hai, duplicate shapes (jaise symmetric pieces) hata deta hai
    private static List<List<Vector2Int>> GetUniqueRotations(Vector2Int[] baseOffsets)
    {
        List<List<Vector2Int>> uniqueRotations = new List<List<Vector2Int>>();
        HashSet<string> seenSignatures = new HashSet<string>();
        Vector2Int[] current = baseOffsets;

        for (int i = 0; i < 4; i++)
        {
            List<Vector2Int> normalized = NormalizeToList(current);
            string signature = string.Join(",", normalized.OrderBy(v => v.x).ThenBy(v => v.y));

            if (!seenSignatures.Contains(signature))
            {
                seenSignatures.Add(signature);
                uniqueRotations.Add(normalized);
            }

            current = RotateOffsets90(current);
        }

        return uniqueRotations;
    }

    private static Vector2Int[] RotateOffsets90(Vector2Int[] offsets)
    {
        Vector2Int[] rotated = new Vector2Int[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
            rotated[i] = new Vector2Int(-offsets[i].y, offsets[i].x);
        return rotated;
    }

    private static List<Vector2Int> NormalizeToList(Vector2Int[] offsets)
    {
        int minX = offsets.Min(o => o.x);
        int minY = offsets.Min(o => o.y);

        List<Vector2Int> result = new List<Vector2Int>();
        foreach (Vector2Int o in offsets)
            result.Add(new Vector2Int(o.x - minX, o.y - minY));

        return result;
    }

    private static bool TryPlacePiece(BoardLevelData board, List<Vector2Int> offsets, int originX, int originY,
    bool[] filled, out List<int> placedIndices)
    {
        placedIndices = new List<int>();
        Vector2Int? previousCell = null;

        foreach (Vector2Int offset in offsets)
        {
            int x = originX + offset.x;
            int y = originY + offset.y;

            if (x < 0 || x >= board.width || y < 0 || y >= board.height) return false;
            if (!board.IsCellPlayable(x, y)) return false;

            int index = y * board.width + x;
            if (filled[index]) return false;

            if (previousCell.HasValue &&
                IsAdjacent(previousCell.Value, new Vector2Int(x, y)) &&
                board.HasWallBetween(previousCell.Value.x, previousCell.Value.y, x, y))
            {
                return false;
            }

            placedIndices.Add(index);
            previousCell = new Vector2Int(x, y);
        }

        return true;
    }
    private static bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) == 1;
    }

    private static int FindFirstEmptyCell(bool[] filled)
    {
        for (int i = 0; i < filled.Length; i++)
            if (!filled[i]) return i;
        return -1;
    }
}