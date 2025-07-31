using System.Collections.Generic;
using CityGenerator;
using UnityEngine;
using Mirror;

public class MonsterGeneration : MonoBehaviour
{
    public GameObject redMonsterPrefab;
    
    public int monsterCount = 6;
    
    public Transform monstersParent;

    public void GenerateMonsters()
    {
        if (!NetworkServer.active)
            return;

        var usedMarks = new HashSet<CityMark>();
        var cityMarks = CityGroupGenerator.Instance.GetCityMarks();

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
}