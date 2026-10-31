using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Saari possible piece shapes ka ek central collection.
// Level Generator isi mein se random shapes uthayega.
[CreateAssetMenu(fileName = "PieceLibrary", menuName = "PaintByBlocks/Piece Library")]
public class PieceLibrary : ScriptableObject
{
    [Tooltip("Saari design ki hui piece shapes yahan drag karo (Square, Plus, L, S, T, etc.)")]
    public PieceShapeData[] allShapes;

    // 'count' random shapes uthata hai, koi duplicate nahi (agar library mein utni shapes hain)
    public PieceShapeData[] GetRandomShapes(int count)
    {
        count = Mathf.Clamp(count, 1, allShapes.Length);

        List<PieceShapeData> pool = new List<PieceShapeData>(allShapes);
        List<PieceShapeData> picked = new List<PieceShapeData>();

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            picked.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return picked.ToArray();
    }
}