using Mirror;

public class SpawnPosPainting : NetworkBehaviour
{
    private int spawnedPlayerID = -1;

    public int SpawnedPlayerID
    {
        get => spawnedPlayerID;
        set
        {
            spawnedPlayerID = value;
            if (NetworkServer.active)
            {
                RpcSetSpawnedPlayerID(value);
            }
            else
            {
                CmdSetSpawnedPlayerID(value);
            }
        }
    }

    [ClientRpc]
    public void RpcSetSpawnedPlayerID(int playerID)
    {
        if (!isClientOnly)
            return;

        spawnedPlayerID = playerID;
    }

    [Command(requiresAuthority = false)]
    public void CmdSetSpawnedPlayerID(int playerID)
    {
        spawnedPlayerID = playerID;
    }
}