using System.Collections.Generic;
using Mirror;

public struct PlayerExitMsg : NetworkMessage
{
    public int connectionID;
    public int playerID;
    public ulong playerSteamID;

    public PlayerExitMsg(int connectionID, int playerID, ulong playerSteamID)
    {
        this.connectionID = connectionID;
        this.playerID = playerID;
        this.playerSteamID = playerSteamID;
    }
}

public struct PlayerLoadSceneSuccessMsg : NetworkMessage
{
    public int connectionID;
    public int playerID;
    public ulong playerSteamID;

    public PlayerLoadSceneSuccessMsg(int connectionID, int playerID, ulong playerSteamID)
    {
        this.connectionID = connectionID;
        this.playerID = playerID;
        this.playerSteamID = playerSteamID;
    }
}