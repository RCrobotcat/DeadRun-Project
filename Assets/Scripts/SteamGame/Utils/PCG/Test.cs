﻿using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Cellular Automata
public class Test : MonoBehaviour
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

    public Transform planeTransformParent;

    private void Update()
    {
        if (lastWidth != width || lastHeight != height)
        {
            ClearPlanes();
            lastWidth = width;
            lastHeight = height;
            GenerateMaze();
        }
    }

    private void ClearPlanes()
    {
        //if (!NetworkServer.active) return;

        foreach (Transform child in planeTransformParent)
            Destroy(child.gameObject);
    }

    void GenerateMaze()
    {
        //if (!NetworkServer.active) return;

        mainPathPoints.Clear();
        maze = null;

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
                maze[x, y] = Random.Range(0f, 1f) < 0.5f ? 1 : 0;
            }
        }
    }

    void RunCellularAutomata()
    {
        // Cellular Automata:
        // 一个活细胞有两个或三个邻居时继续存活;
        // 一个死细胞有三个邻居时变为活细胞;
        // 其他情况下, 细胞保持当前状态不变;
        int iterations = 5;
        for (int i = 0; i < iterations; i++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int neighbors = CountAliveNeighbors(x, y);
                    if (neighbors > 4) // 4个以上邻居为墙，保持墙壁 => 活细胞
                        maze[x, y] = 1;
                    else if (neighbors < 4) // 4个以下邻居为墙，变为路径 => 死细胞
                        maze[x, y] = 0;
                }
            }
        }
    }

    int CountAliveNeighbors(int x, int y)
    {
        int aliveCount = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    aliveCount += maze[nx, ny]; // 墙壁 = 1
                }
            }
        }

        return aliveCount;
    }

    void PlacePlanesOnMaze()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float spacing = 3f;
                float minOffset = -2f;
                float maxOffset = 2f;

                if (maze[x, y] == 0)
                {
                    float offsetX = Random.Range(minOffset, maxOffset);
                    float offsetZ = Random.Range(minOffset, maxOffset);
                    Vector3 position = new Vector3(x * spacing + offsetX, 0, y * spacing + offsetZ);

                    GameObject plane = Instantiate(planeRotationTrapPrefab, position, Quaternion.identity);
                    //NetworkServer.Spawn(plane);
                    //QuadTreeCulling.Instance.tree.InsertData(plane.transform);
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
        //NetworkServer.Spawn(startPlane);
        //QuadTreeCulling.Instance.tree.InsertData(startPlane.transform);
        startPlane.transform.parent = planeTransformParent;
        mainPathPoints.Add(new Vector2Int(0, 0));

        Vector3 endPos = new Vector3(width - 1, 0, height - 1);
        GameObject endPlane = Instantiate(endPlanePrefab, endPos, Quaternion.identity);
        //NetworkServer.Spawn(endPlane);
        //QuadTreeCulling.Instance.tree.InsertData(endPlane.transform);
        endPlane.transform.parent = planeTransformParent;
        mainPathPoints.Add(new Vector2Int(width - 1, height - 1));

        // Table
        Vector2Int randomPoint = new Vector2Int(mainPathPoints[Random.Range(0, mainPathPoints.Count + 1)].x,
            mainPathPoints[Random.Range(0, mainPathPoints.Count + 1)].y);
        Vector3 tablePosition = new Vector3(randomPoint.x, 0.65f, randomPoint.y);
        GameObject table = Instantiate(tablePrefab, tablePosition, Quaternion.identity);
        //NetworkServer.Spawn(table);
        //QuadTreeCulling.Instance.tree.InsertData(table.transform);
        table.transform.parent = planeTransformParent;
    }
}