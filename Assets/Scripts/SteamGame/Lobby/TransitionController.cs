using System.Collections;
using System.IO;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class LobbyController
{
    private int escaperCount;
    private int trapperCount = 1;

    int deadEscaperCount = 0;
    [HideInInspector] public string previousScenePath = "";

    public int DeadEscaperCount
    {
        get => deadEscaperCount;
        set
        {
            deadEscaperCount = value;
            if (deadEscaperCount >= escaperCount)
            {
                deadEscaperCount = 0;
                TransitionAllPlayersTo1V1(previousScenePath);
            }
        }
    }

    public void TransitionAllPlayersTo1V1(string previousScenePath)
    {
        PlayerObjectController[] allPlayers = FindObjectsOfType<PlayerObjectController>();
        foreach (var player in allPlayers)
        {
            if (player.role == PlayerRole.Trapper)
            {
                CameraController.Instance.gameObject.SetActive(true);
                CameraController.Instance.freeLookCam.Target.TrackingTarget = player.transform;
                player.transform.position = Vector3.zero;
                player.role = PlayerRole.Escaper;
                player.SetPlayerUIState(true);

                if (player.TryGetComponent<PlayerMovement>(out PlayerMovement pm))
                    pm.enabled = false;

                if (player.isServer)
                    StartCoroutine(SendNewPlayerToScene(player.gameObject,
                        SceneManager.GetSceneByName("Scene_3_1v1").path, "SpawnPos", previousScenePath));
            }
            else if (player.role == PlayerRole.Escaper)
            {
                if (player.TryGetComponent<PlayerMovement>(out PlayerMovement pm))
                    pm.enabled = false;

                if (player.isServer)
                    StartCoroutine(SendNewPlayerToScene(player.gameObject,
                        SceneManager.GetSceneByName("Scene_3_1v1").path, "SpawnPos", previousScenePath));
            }
        }
    }

    public void TransitionAllPlayersToScene(string scenePathName, string scenePosToSpawnOn, string previousScenePath)
    {
        PlayerObjectController[] allPlayers = FindObjectsOfType<PlayerObjectController>();
        foreach (var player in allPlayers)
        {
            if (player.TryGetComponent<PlayerMovement>(out PlayerMovement pm))
                pm.enabled = false;

            if (player.isServer)
                StartCoroutine(SendNewPlayerToScene(player.gameObject, scenePathName, scenePosToSpawnOn,
                    previousScenePath));
        }

        if (NetworkServer.active)
            if (scenePathName == SceneManager.GetSceneByName("Scene_4_Terrain").path)
                TerrainController.Instance.CanGenerateTerrain = true;
    }

    [ServerCallback]
    public IEnumerator SendNewPlayerToScene(GameObject player, string transitionToSceneName, string scenePosToSpawnOn,
        string previousScenePath)
    {
        if (player.TryGetComponent<NetworkIdentity>(out NetworkIdentity identity))
        {
            NetworkConnectionToClient conn = identity.connectionToClient;
            if (conn == null)
                yield break;

            conn.Send(new SceneMessage()
            {
                sceneName = previousScenePath,
                sceneOperation = SceneOperation.UnloadAdditive,
                customHandling = true
            });

            yield return new WaitForSeconds(MyNetworkManager.fadeinOutScreen.speed * 0.1f);

            NetworkServer.RemovePlayerForConnection(conn, false);

            NetworkStartPosition[] startPositions = FindObjectsOfType<NetworkStartPosition>();
            Transform startPos = MyNetworkManager.GetStartPosition();
            foreach (var item in startPositions)
            {
                if (item.gameObject.scene.name == Path.GetFileNameWithoutExtension(transitionToSceneName) &&
                    item.name == scenePosToSpawnOn)
                {
                    startPos = item.transform;
                }
            }

            player.transform.position = startPos.position;

            SceneManager.MoveGameObjectToScene(player, SceneManager.GetSceneByPath(transitionToSceneName));
            conn.Send(new SceneMessage()
            {
                sceneName = transitionToSceneName,
                sceneOperation = SceneOperation.LoadAdditive,
                customHandling = true
            });

            NetworkServer.AddPlayerForConnection(conn, player);

            if (NetworkClient.localPlayer != null &&
                player.TryGetComponent<PlayerMovement>(out PlayerMovement playerMove))
            {
                playerMove.enabled = true;
            }


            if (player.GetComponent<PlayerObjectController>().playerID == LocalPlayerObjectController.playerID)
            {
                if (CameraController.Instance.freeLookCam.Target.TrackingTarget == null)
                    CameraController.Instance.freeLookCam.Target.TrackingTarget = player.transform;

                player.GetComponent<PlayerObjectController>().SetPlayerUIState(true);
            }

            if (NetworkServer.active)
                player.GetComponent<PlayerObjectController>().RpcUpdatePlayerParamsAfterTransition();

            // 1v1 Scene Transition
            if (transitionToSceneName == SceneManager.GetSceneByName("Scene_3_1v1").path)
            {
                Show1v1Text();
                if (NetworkServer.active)
                    player.GetComponent<PlayerObjectController>().RpcShow1v1Text();
            }
        }
    }
}