using UnityEngine;
using UnityEngine.EventSystems;

// Har cell ke GameObject pe runtime mein add hota hai (PieceGridRenderer.BuildBoard se).
// Pointer down = drag shuru, pointer enter = drag jaari (finger/mouse move), pointer up = drag khatam.
public class DragCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
{
    private int cellX;
    private int cellY;
    private PieceGridRenderer manager;

    public void Init(int x, int y, PieceGridRenderer gridManager)
    {
        cellX = x;
        cellY = y;
        manager = gridManager;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        manager.BeginDrag(cellX, cellY);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        manager.ContinueDrag(cellX, cellY);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        manager.EndDrag(cellX, cellY);
    }
}
