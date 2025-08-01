using System.Collections.Generic;
using UnityEngine;

public class SplatonPlaceGenerator : Singleton<SplatonPlaceGenerator>
{
    public int columns = 10;
    public int rows = 10;

    public float cellSize = 5f;

    public Transform cellMarkParent;
    public SplatonCellMark cellMarkPrefab;
    List<SplatonCellMark> cellMarks = new List<SplatonCellMark>();

    public Transform groundParent;

    //public List<Paintable> grounds;
    public Paintable groundPrefab;
    public List<Paintable> walls;

    [ContextMenu("Instant Create")]
    public void InitializePlace()
    {
        GenerateGridMarks();
        GenerateGround();
        GenerateSurroundingWalls();
    }

    void GenerateGridMarks()
    {
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 position = new Vector3(x * cellSize, 0, y * cellSize);
                SplatonCellMark cellMark = Instantiate(cellMarkPrefab, position, Quaternion.identity, cellMarkParent);
                cellMark.name = $"CellMark_{x}_{y}";
                cellMarks.Add(cellMark);
            }
        }
    }

    void GenerateGround()
    {
        // foreach (var mark in cellMarks)
        // {
        //     Paintable ground = Instantiate(grounds[Random.Range(0, grounds.Count)], mark.transform.position,
        //         Quaternion.identity, groundParent);
        //     ground.name = $"Ground_{mark.name}";
        // }

        if (cellMarks.Count > 0)
        {
            Vector3 center = Vector3.zero;
            foreach (var mark in cellMarks)
            {
                center += mark.transform.position;
            }

            center /= cellMarks.Count;

            Paintable ground = Instantiate(groundPrefab, center, Quaternion.identity, groundParent);
            ground.name = "Ground_Center";
        }
    }

    void GenerateSurroundingWalls()
    {
        // for (int x = 0; x < columns; x++)
        // {
        //     // 下边界
        //     Vector3 posBottom = new Vector3(x * cellSize, 0, -cellSize / 2);
        //     Paintable wallBottom = Instantiate(walls[0], posBottom, Quaternion.identity, groundParent);
        //     wallBottom.name = $"Wall_Bottom_{x}";
        //
        //     // 上边界
        //     Vector3 posTop = new Vector3(x * cellSize, 0, (rows - 1) * cellSize + cellSize / 2);
        //     Paintable wallTop = Instantiate(walls[0], posTop, Quaternion.identity, groundParent);
        //     wallTop.name = $"Wall_Top_{x}";
        // }
        //
        // for (int y = 0; y < rows; y++)
        // {
        //     // 左边界
        //     Vector3 posLeft = new Vector3(-cellSize / 2, 0, y * cellSize);
        //     Paintable wallLeft = Instantiate(walls[0], posLeft, Quaternion.identity, groundParent);
        //     wallLeft.name = $"Wall_Left_{y}";
        //
        //     // 右边界
        //     Vector3 posRight = new Vector3((columns - 1) * cellSize + cellSize / 2, 0, y * cellSize);
        //     Paintable wallRight = Instantiate(walls[0], posRight, Quaternion.identity, groundParent);
        //     wallRight.name = $"Wall_Right_{y}";
        // }

        // 下边界
        Vector3 posBottom = new Vector3((columns * cellSize) / 2 - cellSize / 2, 0, -cellSize / 2 - 2.5f);
        Paintable wallBottom = Instantiate(walls[0], posBottom, Quaternion.identity, groundParent);
        wallBottom.name = "Wall_Bottom";

        // 上边界
        Vector3 posTop = new Vector3((columns * cellSize) / 2 - cellSize / 2, 0,
            (rows - 1) * cellSize + cellSize / 2 + 2.5f);
        Paintable wallTop = Instantiate(walls[0], posTop, Quaternion.identity, groundParent);
        wallTop.name = "Wall_Top";

        // 左边界
        Vector3 posLeft = new Vector3(-cellSize / 2 - 2.5f, 0, (rows * cellSize) / 2 - cellSize / 2);
        Paintable wallLeft = Instantiate(walls[0], posLeft, Quaternion.Euler(0, 90, 0), groundParent);
        wallLeft.name = "Wall_Left";

        // 右边界
        Vector3 posRight = new Vector3((columns - 1) * cellSize + cellSize / 2 + 2.5f, 0,
            (rows * cellSize) / 2 - cellSize / 2);
        Paintable wallRight = Instantiate(walls[0], posRight, Quaternion.Euler(0, 90, 0), groundParent);
        wallRight.name = "Wall_Right";
    }
}