using UnityEngine;

public class SplatonCellMark : MonoBehaviour
{
    Vector2 cellPosition;

    public Vector2 CellPosition
    {
        get => cellPosition;
        set => cellPosition = value;
    }

    [SerializeField] bool isMarked = false;

    public bool IsMarked
    {
        get => isMarked;
        set => isMarked = value;
    }
}