using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

// Cellular Automata
public class PlaneRotationTrapGeneration : MonoBehaviour
{
    public int width = 100; // 迷宫宽度
    private int lastWidth;
    public int height = 100; // 迷宫高度
    int lastHeight;

    public GameObject planeRotationTrapPrefab;
    public GameObject startPlanePrefab;
    public GameObject endPlanePrefab;
    public GameObject tablePrefab;

    private List<Vector2Int> mainPathPoints = new();
    private int[,] maze; // 0 = 路径，1 = 墙壁
    private int[,] newMap;

    public Transform planeTransformParent;

    private void Update()
    {
        if (lastWidth != width || lastHeight != height)
        {
            lastWidth = width;
            lastHeight = height;
            GenerateMaze();
        }
    }

    void GenerateMaze()
    {
        if (!NetworkServer.active) return;

        mainPathPoints.Clear();
        maze = null;
        foreach (Transform child in planeTransformParent)
            Destroy(child.gameObject);

        InitializeMaze();
        RunCellularAutomata();
        PlacePlanesOnMaze();
    }

    void InitializeMaze()
    {
        maze = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                maze[x, y] = 1;
                var r = LCGRandom(CantorPair(x, y)) % 50 + 50;
                if (r < 55)
                    maze[x, y] = 0;
                else
                    maze[x, y] = 1;
            }
        }
    }

    #region LCG Random Number Generator

    // 线性同余生成器 (LCG) 随机数生成器
    // out = (in * a + c) mod m
    public static int LCGRandom(int v)
    {
        return (1140671485 * v + 12820163) % 16777216;
    }

    // 康托配对函数
    public static int CantorPair(int a, int b)
    {
        return (a + b) * (a + b + 1) / 2 + b;
    }

    #endregion

    void RunCellularAutomata()
    {
        newMap = new int[width, height];

        // Cellular Automata:
        // 遍历整片地图,如果以当前遍历点为中心周围9个方格内执行如下判断:
        // 如果中心点为墙, 判断周围8个点是否存在4个及以上的墙壁,
        // 如果满足则中心为墙壁, 否则为空洞;
        // 如果中心点为空洞, 判断周围8个点是否存在5个及以上的墙壁;
        // 如果存在则中心为墙壁, 否则为空洞.
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                var wallCount = 0;
                for (int i = x - 1; i <= x + 1; i++)
                {
                    for (int j = y - 1; j <= y + 1; j++)
                    {
                        if (maze[i, j] == 1)
                            wallCount++;
                    }
                }

                if (maze[x, y] == 1)
                {
                    if (wallCount > 4)
                        newMap[x, y] = 1;
                    else
                        newMap[x, y] = 0;
                }
                else
                {
                    if (wallCount > 4)
                        newMap[x, y] = 1;
                    else
                        newMap[x, y] = 0;
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                maze[x, y] = newMap[x, y];
            }
        }
    }

    void PlacePlanesOnMaze()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float spacing = 5f;
                float minOffset = 0.5f;
                float maxOffset = 1f;

                if (maze[x, y] == 0)
                {
                    float offsetX = Random.Range(minOffset, maxOffset) * (Random.value < 0.5f ? 1 : -1);
                    float offsetZ = Random.Range(minOffset, maxOffset) * (Random.value < 0.5f ? 1 : -1);
                    Vector3 position = new Vector3(x * spacing + offsetX, 0, y * spacing + offsetZ);

                    GameObject plane = Instantiate(planeRotationTrapPrefab, position, Quaternion.identity);
                    NetworkServer.Spawn(plane);
                    plane.transform.parent = planeTransformParent;
                    mainPathPoints.Add(new Vector2Int((int)position.x, (int)position.z));
                }
            }
        }

        GenerateTableAndStartEnd();
    }

    void GenerateTableAndStartEnd()
    {
        // Start and End Points:
        Vector3 startPos = new Vector3(0, 0, 0);
        GameObject startPlane = Instantiate(startPlanePrefab, startPos, Quaternion.identity);
        NetworkServer.Spawn(startPlane);
        startPlane.transform.parent = planeTransformParent;
        mainPathPoints.Add(new Vector2Int(0, 0));

        var randomIndex = Random.Range(25, mainPathPoints.Count + 1);
        Vector3 endPos = new Vector3(mainPathPoints[randomIndex].x, 0, mainPathPoints[randomIndex].y);
        GameObject endPlane = Instantiate(endPlanePrefab, endPos, Quaternion.identity);
        NetworkServer.Spawn(endPlane);
        endPlane.transform.parent = planeTransformParent;
        mainPathPoints.Add(new Vector2Int(width - 1, height - 1));

        // Table
        var randomIndex_1 = Random.Range(10, mainPathPoints.Count + 1);
        Vector2Int randomPoint = new Vector2Int(mainPathPoints[randomIndex_1].x, mainPathPoints[randomIndex_1].y);
        Vector3 tablePosition = new Vector3(randomPoint.x, 0.65f, randomPoint.y);
        GameObject table = Instantiate(tablePrefab, tablePosition, Quaternion.identity);
        NetworkServer.Spawn(table);
        table.transform.parent = planeTransformParent;
    }
}