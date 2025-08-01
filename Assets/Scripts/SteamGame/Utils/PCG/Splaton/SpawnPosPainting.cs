using Mirror;
using UnityEngine;

public class SpawnPosPainting : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnSpawnedPlayerIDChanged))]
    public int spawnedPlayerID = -1;

    void OnSpawnedPlayerIDChanged(int oldValue, int newValue)
    {
        Debug.Log("SpawnPosPainting: SpawnedPlayerID changed from " + oldValue + " to " + newValue);
    }

    public void SetSpawnedPlayerID(int playerID)
    {
        spawnedPlayerID = playerID;
    }
}