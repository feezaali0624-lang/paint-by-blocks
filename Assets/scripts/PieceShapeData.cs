using UnityEngine;

// Ek piece shape define karta hai (jaise 2x2 square, ya plus-shape) — relative offsets ki list.
// Anchor cell hamesha (0,0) maana jata hai.
[CreateAssetMenu(fileName = "NewPieceShape", menuName = "PaintByBlocks/Piece Shape Data")]
public class PieceShapeData : ScriptableObject
{
    [Header("Display")]
    public string pieceName;

    [Header("Cell Offsets (anchor = 0,0)")]
    public Vector2Int[] cellOffsets;

    // Diya gaya anchor position pe, is piece ke actual world/grid cells kya hongi
    public Vector2Int[] GetOccupiedCells(Vector2Int anchorPosition)
    {
        Vector2Int[] result = new Vector2Int[cellOffsets.Length];
        for (int i = 0; i < cellOffsets.Length; i++)
            result[i] = anchorPosition + cellOffsets[i];
        return result;
    }

    // Editor mein quick-setup ke liye helper (2x2 square)
    [ContextMenu("Setup As 2x2 Square")]
    private void SetupSquare()
    {
        cellOffsets = new Vector2Int[]
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(0, 1), new Vector2Int(1, 1)
        };
        pieceName = "Square2x2";
    }
    [ContextMenu("Setup As 3x3 Square")]
    private void SetupSquare3x3()
    {
        cellOffsets = new Vector2Int[]
        {
        new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
        new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)
        };
        pieceName = "Square3x3";
    }
    // Editor mein quick-setup ke liye helper (plus/cross shape)
    [ContextMenu("Setup As Plus Shape")]
    private void SetupPlus()
    {
        cellOffsets = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
            new Vector2Int(1, 2)
        };
        pieceName = "PlusShape";
    }
    [ContextMenu("Setup As L-Tromino")]
    private void SetupAsLTromino()
    {
        cellOffsets = new Vector2Int[] {
        new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1)
    };
    }

    [ContextMenu("Setup As S-Shape")]
    private void SetupAsSShape()
    {
        cellOffsets = new Vector2Int[] {
        new Vector2Int(1,0), new Vector2Int(2,0),
        new Vector2Int(0,1), new Vector2Int(1,1)
    };
    }

    [ContextMenu("Setup As Straight Line 3")]
    private void SetupAsLine3()
    {
        cellOffsets = new Vector2Int[] {
        new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0)
    };
    }

    [ContextMenu("Setup As T-Shape")]
    private void SetupAsTShape()
    {
        cellOffsets = new Vector2Int[] {
        new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1)
    };
    }
    [ContextMenu("Setup As Zigzag")]
    private void SetupAsZigzag()
    {
        cellOffsets = new Vector2Int[] {
        new Vector2Int(0,0), new Vector2Int(1,0),
        new Vector2Int(1,1), new Vector2Int(2,1)
    };
    }
    [ContextMenu("Setup As U-Shape")]
    private void SetupAsUShape()
    {
        cellOffsets = new Vector2Int[] {
        new Vector2Int(0,0), new Vector2Int(0,1),
        new Vector2Int(1,1),
        new Vector2Int(2,0), new Vector2Int(2,1)
    };
    }
    [ContextMenu("Setup As P-Pentomino")]
    private void SetupAsPPentomino()
    {
        cellOffsets = new Vector2Int[] {
        new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
        new Vector2Int(0,1), new Vector2Int(1,1)
    };
    }

    [ContextMenu("Setup As Step-Shape")]
    private void SetupAsStepShape()
    {
        cellOffsets = new Vector2Int[] {
        new Vector2Int(0,0), new Vector2Int(1,0),
        new Vector2Int(1,1), new Vector2Int(2,1)
    };
    }
    [ContextMenu("Setup As Straight Line 4")]
    private void SetupAsLine4()
    {
        cellOffsets = new Vector2Int[] {
        new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0)
    };
    }
}
