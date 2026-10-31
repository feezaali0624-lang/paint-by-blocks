using UnityEngine;

// Ek level ka POORA data — board + uski sahi pieces ek hi jagah.
// Isse manually board aur pieces match karne ki zaroorat khatam ho jati hai.
[CreateAssetMenu(fileName = "NewLevelConfig", menuName = "PaintByBlocks/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Is Level Ka Board")]
    public BoardLevelData board;

    [Header("Isi Level Ke Liye Sahi Pieces")]
    public PieceShapeData[] pieces;

    [Header("Level Info")]
    public int levelNumber;
}