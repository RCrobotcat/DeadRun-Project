using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class PlaneRotationTrapGeneration : MonoBehaviour
{
    public Texture2D noiseTexture;
    public GameObject planeRotationTrapPrefab;
    public GameObject startPlanePrefab;
    public GameObject endPlanePrefab;

    public GameObject tablePrefab;

    private List<Vector2Int> mainPathPoints = new();

    public Vector3 startPos;
    Vector3 lastStartPos = Vector3.zero;
    public Vector3 endPos;
    Vector3 lastEndPos = Vector3.zero;

    public int maxJumpDistance = 5;
    int lastMaxJumpDistance;
    public int minJumpDistance = 3;
    int lastMinJumpDistance;

    public int maxSteps = 30;
    int lastMaxSteps;

    public Transform planeTransformParent;

    private void Update()
    {
        UpdatePlaneRotationTraps();
    }

    public void UpdatePlaneRotationTraps()
    {
        if (SceneManager.GetSceneByName("Scene_1").isLoaded)
        {
            if (lastStartPos != startPos || lastEndPos != endPos || lastMaxSteps != maxSteps
                || lastMaxJumpDistance != maxJumpDistance || lastMinJumpDistance != minJumpDistance)
            {
                ClearAllTraps();

                lastStartPos = startPos;
                lastEndPos = endPos;
                lastMaxSteps = maxSteps;

                lastMaxJumpDistance = maxJumpDistance;
                lastMinJumpDistance = minJumpDistance;

                GenerateTrapsAlongPath();
            }
        }
    }

    void GenerateTrapsAlongPath()
    {
        GetSimplePath();

        foreach (Vector2Int point in mainPathPoints)
        {
            int x = point.x;
            int y = point.y;

            if (x < 0 || x >= noiseTexture.width || y < 0 || y >= noiseTexture.height)
                continue;

            Color pixelColor = noiseTexture.GetPixel(x, y);
            float probability = pixelColor.r;

            if (Random.Range(0, 1) <= probability)
            {
                Vector3 position = new Vector3(x, 0, y);
                if (point == mainPathPoints[^1])
                {
                    GameObject endPlane = Instantiate(endPlanePrefab, position, Quaternion.identity);
                    NetworkServer.Spawn(endPlane);
                    endPlane.name = "EndPlane";
                    endPlane.transform.parent = planeTransformParent;
                }
                else if (point == mainPathPoints[0])
                {
                    GameObject startPlane = Instantiate(startPlanePrefab, position, Quaternion.identity);
                    NetworkServer.Spawn(startPlane);
                    startPlane.name = "StartPlane";
                    startPlane.transform.parent = planeTransformParent;
                }
                else
                {
                    GameObject plane = Instantiate(planeRotationTrapPrefab, position, Quaternion.identity);
                    NetworkServer.Spawn(plane);
                    plane.transform.parent = planeTransformParent;
                }
            }
        }

        if (mainPathPoints.Count > 0)
        {
            Vector2Int randomPoint = mainPathPoints[Random.Range(0, mainPathPoints.Count)];
            Vector3 tablePosition = new Vector3(randomPoint.x, 0.65f, randomPoint.y);
            GameObject table = Instantiate(tablePrefab, tablePosition, Quaternion.identity);
            NetworkServer.Spawn(table);
            table.transform.parent = planeTransformParent;
        }
    }

    void GetSimplePath()
    {
        mainPathPoints.Clear();

        Vector2Int current = new Vector2Int(Mathf.RoundToInt(startPos.x), Mathf.RoundToInt(startPos.z));
        Vector2Int end = new Vector2Int(Mathf.RoundToInt(endPos.x), Mathf.RoundToInt(endPos.z));

        int stepCount = 0;

        while (Vector2Int.Distance(current, end) >= 3f && stepCount < maxSteps)
        {
            stepCount++;

            Vector2Int direction = end - current;
            int jumpDistance = Random.Range(minJumpDistance, maxJumpDistance);

            Vector2Int mainDir = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
                ? new Vector2Int((int)Mathf.Sign(direction.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(direction.y));

            Vector2Int sideDir = mainDir.x != 0
                ? new Vector2Int(0, Random.Range(-1, 2))
                : new Vector2Int(Random.Range(-1, 2), 0);

            Vector2Int offset = mainDir * jumpDistance + sideDir;

            Vector2Int next = current + offset;

            mainPathPoints.Add(current);
            current = next;
        }

        mainPathPoints.Add(end); // 添加终点
    }

    private void ClearAllTraps()
    {
        foreach (Transform child in planeTransformParent)
        {
            Destroy(child.gameObject);
        }

        mainPathPoints.Clear();
    }
}