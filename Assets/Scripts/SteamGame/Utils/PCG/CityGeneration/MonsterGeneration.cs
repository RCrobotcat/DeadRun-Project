using System.Collections.Generic;
using CityGenerator;
using UnityEngine;
using Mirror;

public class MonsterGeneration : MonoBehaviour
{
    public GameObject redMonsterPrefab;

    public List<GameObject> carsPrefabs;

    public int monsterCount = 6;
    public int carCount = 6;

    public Transform monstersParent;

    HashSet<CityMark> usedMarks;
    CityMark[,] cityMarks;

    public void GenerateMonstersAndCars()
    {
        if (!NetworkServer.active)
            return;

        usedMarks = new HashSet<CityMark>();
        cityMarks = CityGroupGenerator.Instance.GetCityMarks();

        GenerateMonsters(usedMarks, cityMarks);
        GenerateCars(usedMarks, cityMarks);
    }

    void GenerateMonsters(HashSet<CityMark> usedMarks, CityMark[,] cityMarks)
    {
        for (int i = 0; i < monsterCount; i++)
        {
            CityMark mark = null;
            int attempts = 0;
            int maxAttempts = 100;
            do
            {
                int val0 = Random.Range(0, cityMarks.GetLength(0));
                int val1 = Random.Range(0, cityMarks.GetLength(1));
                var candidate = cityMarks[val0, val1];
                if ((candidate.markType == CityObjectType.MajorRoad ||
                     candidate.markType == CityObjectType.MinorRoad)
                    && !usedMarks.Contains(candidate))
                {
                    mark = candidate;
                    break;
                }

                attempts++;
            } while (attempts < maxAttempts);

            if (mark == null)
                continue;

            usedMarks.Add(mark);

            var monster = Instantiate(redMonsterPrefab, mark.transform.position + Vector3.up * 0.1f,
                Quaternion.identity,
                monstersParent);
            monster.name = $"Monster {mark.Index}";
            NetworkServer.Spawn(monster);
        }
    }

    private void GenerateCars(HashSet<CityMark> usedMarks, CityMark[,] cityMarks)
    {
        for (int i = 0; i < carCount; i++)
        {
            CityMark mark = null;
            int attempts = 0;
            int maxAttempts = 100;
            do
            {
                int val0 = Random.Range(0, cityMarks.GetLength(0));
                int val1 = Random.Range(0, cityMarks.GetLength(1));
                var candidate = cityMarks[val0, val1];
                if ((candidate.markType == CityObjectType.MajorRoad ||
                     candidate.markType == CityObjectType.MinorRoad)
                    && !usedMarks.Contains(candidate))
                {
                    mark = candidate;
                    break;
                }

                attempts++;
            } while (attempts < maxAttempts);

            if (mark == null)
                continue;

            usedMarks.Add(mark);

            var carPrefab = carsPrefabs[Random.Range(0, carsPrefabs.Count)];
            var car = Instantiate(carPrefab, mark.transform.position + Vector3.up * 1.5f,
                Quaternion.identity,
                monstersParent);
            car.name = $"Car {mark.Index}";
            NetworkServer.Spawn(car);
        }
    }
}