using UnityEngine;

public class SplatonPlaceGenerator : Singleton<SplatonPlaceGenerator>
{
    public int columns = 10;
    public int rows = 10;

    public float cellSize = 5f;

    public SplatonCellMark cellMarkPrefab;

    protected override void Awake()
    {
        base.Awake();

        GenerateGrid();
    }

    public void GenerateGrid()
    {
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 position = new Vector3(x * cellSize, 0, y * cellSize);
                SplatonCellMark cellMark = Instantiate(cellMarkPrefab, position, Quaternion.identity);
                cellMark.transform.SetParent(transform);
            }
        }
    }
}