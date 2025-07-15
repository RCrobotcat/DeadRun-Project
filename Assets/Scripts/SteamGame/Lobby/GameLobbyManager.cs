using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;

public partial class MyNetworkManager
{
    public List<PlayerObjectController> GamePlayers { get; } = new();

    public void SetUpClientMsgHandlers()
    {
        NetworkClient.RegisterHandler<PlayerExitMsg>(OnPlayerExit);
        NetworkServer.RegisterHandler<PlayerExitMsg>(OnPlayerExit_ServerHost);
    }

    private void OnPlayerExit(PlayerExitMsg msg)
    {
        Debug.Log("PlayerExitMsg received: " + msg.connectionID + " - " + msg.playerID);

        StopHost();

        SteamLobby.Instance.hostButton.gameObject.SetActive(true);
        SteamLobby.Instance.lobbiesButton.gameObject.SetActive(true);
        SteamLobby.Instance.quitBtn.gameObject.SetActive(true);
        SteamLobby.Instance.lobbySceneType = LobbySceneTypesEnum.Offline;
    }

    private void OnPlayerExit_ServerHost(NetworkConnectionToClient conn, PlayerExitMsg msg)
    {
        Debug.Log("PlayerExitMsg received on server: " + msg.connectionID + " - " + msg.playerID);

        if (msg.playerID == 1)
            return;

        PlayerObjectController player =
            GamePlayers.Find(p => p.connectionID == msg.connectionID && p.playerID == msg.playerID);
        if (player != null)
        {
            GamePlayers.Remove(player);
            NetworkServer.Destroy(player.gameObject);
        }
    }

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        if (conn.identity == null)
        {
            StartCoroutine(AddPlayerDelayed(conn));
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        StartCoroutine(AddPlayerDelayed(conn));
    }

    IEnumerator AddPlayerDelayed(NetworkConnectionToClient conn)
    {
        while (!subScenesLoaded)
            yield return null;

        foreach (var id in FindObjectsOfType<NetworkIdentity>())
            id.enabled = true;

        firstSceneLoaded = false;

        conn.Send(new SceneMessage
        {
            sceneName = firstSceneToLoad,
            sceneOperation = SceneOperation.LoadAdditive,
            customHandling = true
        });

        // Transform startPos = GetStartPosition();
        // GameObject player = Instantiate(playerPrefab, startPos);

        GameObject player = Instantiate(playerPrefab, null);

        var poc = player.GetComponent<PlayerObjectController>();
        poc.connectionID = conn.connectionId;
        poc.playerID = GamePlayers.Count + 1;
        poc.playerSteamID =
            (ulong)SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)SteamLobby.Instance.currentLobbyID,
                GamePlayers.Count);

        yield return null;

        SceneManager.MoveGameObjectToScene(player, SceneManager.GetSceneByName(firstSceneToLoad));
        NetworkServer.AddPlayerForConnection(conn, player);

        player.transform.position = new Vector3(1000, 1000, 1000); // offscreen
        CameraController.Instance.freeLookCam.Target.TrackingTarget = null;
    }

    public void HandleSendPlayerToNewScene(string sceneName, string spawnPos)
    {
        StartCoroutine(SendPlayerToNewScene(sceneName, spawnPos));
    }

    IEnumerator SendPlayerToNewScene(string transitionToSceneName, string scenePosToSpawnOn)
    {
        SteamLobby.Instance.lobbySceneType = LobbySceneTypesEnum.GameScene;

        var players = GamePlayers.ToArray();
        foreach (var player in players)
        {
            var identity = player.GetComponent<NetworkIdentity>();
            if (identity == null) continue;

            var conn = identity.connectionToClient;
            if (conn == null) continue;

            string current = player.gameObject.scene.name;
            conn.Send(new SceneMessage
            {
                sceneName = current,
                sceneOperation = SceneOperation.UnloadAdditive,
                customHandling = true
            });

            yield return new WaitForSeconds(fadeinOutScreen.speed * 0.1f);

            NetworkServer.RemovePlayerForConnection(conn, false);

            Transform startPos = GetStartPosition();
            foreach (var sp in FindObjectsOfType<NetworkStartPosition>())
                if (sp.gameObject.scene.name == transitionToSceneName && sp.name == scenePosToSpawnOn)
                    startPos = sp.transform;

            player.transform.position = startPos.position;
            SceneManager.MoveGameObjectToScene(player.gameObject, SceneManager.GetSceneByName(transitionToSceneName));

            conn.Send(new SceneMessage
            {
                sceneName = transitionToSceneName,
                sceneOperation = SceneOperation.LoadAdditive,
                customHandling = true
            });

            NetworkServer.AddPlayerForConnection(conn, player.gameObject);
            if (NetworkClient.localPlayer.TryGetComponent<PlayerMovement>(out var pm))
                pm.enabled = true;
        }
    }

    public void SetPlayersRoles()
    {
        int playerCount = GamePlayers.Count;
        var shuffledPlayers = GamePlayers.OrderBy(_ => Random.value).ToList();
        int half = playerCount / 2;
        for (int i = 0; i < playerCount; i++)
        {
            if (i < half)
                shuffledPlayers[i].role = PlayerRole.Escaper;
            else
                shuffledPlayers[i].role = PlayerRole.Escaper;
        }
    }
}