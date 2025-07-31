using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Monster_1v1_Controller : MonoBehaviour
{
    public List<Vector3> monsterPositions;

    bool isInitialized = false;

    private void Update()
    {
        if (NetworkServer.active)
        {
            if (LobbyController.Instance.localPlayerObject.scene.name != "Scene_3_1v1")
            {
                isInitialized = false;
                return;
            }

            if (!isInitialized)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform monster = transform.GetChild(i);
                    monster.position = monsterPositions[i];
                }

                isInitialized = true;
            }
        }
    }
}