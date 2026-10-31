using UnityEngine;

// Har level ka board "outline" store karta hai — kaunse cells playable hain,
// kaunse bahar/hole hain, aur kaunse cells ke beech "wall" hai (jo drag cross nahi kar sakta)
[CreateAssetMenu(fileName = "NewBoardLevel", menuName = "PaintByBlocks/Board Level Data")]
public class BoardLevelData : ScriptableObject
{
    [Header("Grid Size (bounding box, board isi ke andar fit hoga)")]
    public int width;
    public int height;

    [Header("Playable Cells (flattened, index = y * width + x)")]
    public bool[] isPlayable; // true = board ka hissa, false = outside/hole

    [Header("Walls (jo shape ko cross nahi karne deti)")]
    public bool[] wallRight; // is cell ke RIGHT neighbor ke beech wall hai ya nahi
    public bool[] wallDown;  // is cell ke NEECHE wale neighbor ke beech wall hai ya nahi
    [Header("Prefilled Cells (level shuru se hi filled, player edit nahi kar sakta)")]
    public bool[] isPrefilled;
    public bool IsCellPlayable(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;

        int idx = y * width + x;
        if (isPlayable == null || idx < 0 || idx >= isPlayable.Length) return false; // array size mismatch se crash na ho

        return isPlayable[idx];
    }

    public void SetCellPlayable(int x, int y, bool value)
    {
        isPlayable[y * width + x] = value;
    }

    public int GetTotalPlayableCells()
    {
        int count = 0;
        foreach (bool b in isPlayable)
            if (b) count++;
        return count;
    }

    // wallRight/wallDown arrays ko sahi size mein bana deta hai agar khali/purani size ki hon
    public void EnsureWallArrays()
    {
        int size = width * height;
        if (wallRight == null || wallRight.Length != size) wallRight = new bool[size];
        if (wallDown == null || wallDown.Length != size) wallDown = new bool[size];
        if (isPrefilled == null || isPrefilled.Length != size) isPrefilled = new bool[size];
    }

    public bool IsCellPrefilled(int x, int y)
    {
        if (isPrefilled == null) return false;
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        return isPrefilled[y * width + x];
    }

    // Check karta hai (x1,y1) aur (x2,y2) — jo ADJACENT hone chahiye — ke beech wall hai ya nahi
    public bool HasWallBetween(int x1, int y1, int x2, int y2)
    {
        if (wallRight == null || wallDown == null) return false;

        // Horizontal neighbor (left-right)
        if (y1 == y2 && Mathf.Abs(x1 - x2) == 1)
        {
            int leftX = Mathf.Min(x1, x2);
            int idx = y1 * width + leftX;
            if (idx < 0 || idx >= wallRight.Length) return false;
            return wallRight[idx];
        }

        // Vertical neighbor (top-bottom)
        if (x1 == x2 && Mathf.Abs(y1 - y2) == 1)
        {
            int topY = Mathf.Min(y1, y2);
            int idx = topY * width + x1;
            if (idx < 0 || idx >= wallDown.Length) return false;
            return wallDown[idx];
        }

        return false; // adjacent nahi hain — wall irrelevant
    }
}