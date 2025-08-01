using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class SplatonPlaceGenerator : Singleton<SplatonPlaceGenerator>
{
    public int columns = 10;
    public int rows = 10;

    int lastColumns;
    int lastRows;

    public float cellSize = 5f;

    public Transform cellMarkParent;
    public SplatonCellMark cellMarkPrefab;
    List<SplatonCellMark> cellMarks = new List<SplatonCellMark>();

    public Transform groundParent;

    //public List<Paintable> grounds;
    public Paintable groundPrefab;
    public List<Paintable> walls;

    public Transform respawnPlaceParent;
    public GameObject respawnPlacePrefab;

    bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    MyNetworkManager _myNetworkManager;

    private MyNetworkManager MyNetworkManager
    {
        get
        {
            if (_myNetworkManager != null)
            {
                return _myNetworkManager;
            }

            return _myNetworkManager = MyNetworkManager.singleton as MyNetworkManager;
        }
    }

    [ContextMenu("Instant Create")]
    public void InitializePlace()
    {
        ClearPlace();

        GenerateGridMarks();
        GenerateGround();
        GenerateSurroundingWalls();

        CreateRespawnPlace();

        isInitialized = true;
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

            PaintablesManager.Instance.RegisterPaintable(ground);
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
        PaintablesManager.Instance.RegisterPaintable(wallBottom);

        // 上边界
        Vector3 posTop = new Vector3((columns * cellSize) / 2 - cellSize / 2, 0,
            (rows - 1) * cellSize + cellSize / 2 + 2.5f);
        Paintable wallTop = Instantiate(walls[0], posTop, Quaternion.identity, groundParent);
        wallTop.name = "Wall_Top";
        PaintablesManager.Instance.RegisterPaintable(wallTop);

        // 左边界
        Vector3 posLeft = new Vector3(-cellSize / 2 - 2.5f, 0, (rows * cellSize) / 2 - cellSize / 2);
        Paintable wallLeft = Instantiate(walls[0], posLeft, Quaternion.Euler(0, 90, 0), groundParent);
        wallLeft.name = "Wall_Left";
        PaintablesManager.Instance.RegisterPaintable(wallLeft);

        // 右边界
        Vector3 posRight = new Vector3((columns - 1) * cellSize + cellSize / 2 + 2.5f, 0,
            (rows * cellSize) / 2 - cellSize / 2);
        Paintable wallRight = Instantiate(walls[0], posRight, Quaternion.Euler(0, 90, 0), groundParent);
        wallRight.name = "Wall_Right";
        PaintablesManager.Instance.RegisterPaintable(wallRight);
    }

    void CreateRespawnPlace()
    {
        if (!NetworkServer.active) return;

        int respawnWidth = 5;
        int respawnHeight = 3;
        int minX = 1;
        int maxX = columns - respawnWidth - 1;
        int minY = 1;
        int maxY = rows - respawnHeight - 1;

        List<RectInt> usedAreas = new List<RectInt>();

        int maxAttempts = 10;
        for (int j = 0; j < maxAttempts; j++)
        {
            int x = Random.Range(minX, maxX + 1);
            int y = Random.Range(minY, maxY + 1);
            RectInt area = new RectInt(x, y, respawnWidth, respawnHeight);

            bool overlap = false;
            foreach (var used in usedAreas)
            {
                if (used.Overlaps(area))
                {
                    overlap = true;
                    break;
                }
            }

            if (!overlap)
            {
                usedAreas.Add(area);
                Vector3 pos = new Vector3(x * cellSize + (respawnWidth * cellSize) / 2 - cellSize / 2, 1,
                    y * cellSize + (respawnHeight * cellSize) / 2 - cellSize / 2);
                GameObject res = Instantiate(respawnPlacePrefab, pos, Quaternion.identity, respawnPlaceParent);
                res.name = $"RespawnPlace_{x}_{y}";
                NetworkServer.Spawn(res);

                for (int i = 0; i < res.transform.childCount - 1; i++)
                {
                    PaintablesManager.Instance.RegisterPaintable(res.transform.GetChild(i).GetComponent<Paintable>());
                }

                if (usedAreas.Count == MyNetworkManager.GamePlayers.Count) break;
            }
        }
    }

    [ContextMenu("Clear Place")]
    public void ClearPlace()
    {
        foreach (var cellMark in cellMarks)
        {
            Destroy(cellMark.gameObject);
        }

        cellMarks.Clear();

        foreach (Transform child in groundParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in respawnPlaceParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in cellMarkParent)
        {
            Destroy(child.gameObject);
        }
    }
}