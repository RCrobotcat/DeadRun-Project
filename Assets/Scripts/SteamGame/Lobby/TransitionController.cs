using System.Collections;
using System.IO;
using CityGenerator;
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

                string previousScenePathTemp = previousScenePath;
                TransitionAllPlayersTo1V1(previousScenePathTemp);
            }
        }
    }

    private bool needTransitionToOtherScene = false;
    [HideInInspector] public string nextScenePath = "";

    public bool NeedTransitionToOtherScene
    {
        get => needTransitionToOtherScene;
        set
        {
            needTransitionToOtherScene = value;
            if (needTransitionToOtherScene)
            {
                needTransitionToOtherScene = false;

                string nextScenePathTemp = nextScenePath;
                string previousScenePathTemp = previousScenePath;
                TransitionAllPlayersToScene(nextScenePathTemp, "SpawnPos", previousScenePathTemp);
            }
        }
    }

    void TransitionAllPlayersTo1V1(string previousScenePathName)
    {
        PlayerObjectController[] allPlayers = FindObjectsOfType<PlayerObjectController>();
        ClearBullets();

        foreach (var player in allPlayers)
        {
            ClearBullets();
            if (player.role == PlayerRole.Trapper)
            {
                CameraController.Instance.gameObject.SetActive(true);
                CameraController.Instance.freeLookCam.Target.TrackingTarget = player.transform;
                player.transform.position = Vector3.zero;
                player.role = PlayerRole.Escaper;
                player.SetPlayerUIState(true);

                if (player.TryGetComponent<PlayerMovement>(out PlayerMovement pm))
                    pm.enabled = false;
                if (player.transform.GetChild(2).TryGetComponent<GunShooting>(out GunShooting gunShooting))
                    gunShooting.enabled = false;

                if (player.isServer)
                    StartCoroutine(SendNewPlayerToScene(player.gameObject,
                        SceneManager.GetSceneByName("Scene_3_1v1").path, "SpawnPos", previousScenePathName));
            }
            else if (player.role == PlayerRole.Escaper)
            {
                if (player.TryGetComponent<PlayerMovement>(out PlayerMovement pm))
                    pm.enabled = false;
                if (player.transform.GetChild(2).TryGetComponent<GunShooting>(out GunShooting gunShooting))
                    gunShooting.enabled = false;

                if (player.isServer)
                    StartCoroutine(SendNewPlayerToScene(player.gameObject,
                        SceneManager.GetSceneByName("Scene_3_1v1").path, "SpawnPos", previousScenePathName));
            }
        }
    }

    public void TransitionAllPlayersToScene(string scenePathName, string scenePosToSpawnOn,
        string previousScenePathName)
    {
        PlayerObjectController[] allPlayers = FindObjectsOfType<PlayerObjectController>();
        ClearBullets();

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
                if (player.transform.GetChild(2).TryGetComponent<GunShooting>(out GunShooting gunShooting))
                    gunShooting.enabled = false;

                if (player.isServer)
                    StartCoroutine(SendNewPlayerToScene(player.gameObject, scenePathName, scenePosToSpawnOn,
                        previousScenePathName));
            }
            else if (player.role == PlayerRole.Escaper || player.role == PlayerRole.None)
            {
                if (player.TryGetComponent<PlayerMovement>(out PlayerMovement pm))
                    pm.enabled = false;
                if (player.transform.GetChild(2).TryGetComponent<GunShooting>(out GunShooting gunShooting))
                    gunShooting.enabled = false;

                if (player.isServer)
                    StartCoroutine(SendNewPlayerToScene(player.gameObject, scenePathName, scenePosToSpawnOn,
                        previousScenePathName));
            }

            player.CurrentHealth = player.maxHealth;
        }
    }

    [ServerCallback]
    public IEnumerator SendNewPlayerToScene(GameObject player, string transitionToSceneName, string scenePosToSpawnOn,
        string previousScenePathName)
    {
        if (player.TryGetComponent<NetworkIdentity>(out NetworkIdentity identity))
        {
            NetworkConnectionToClient conn = identity.connectionToClient;
            if (conn == null)
                yield break;

            conn.Send(new SceneMessage()
            {
                sceneName = previousScenePathName,
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

            conn.Send(new SceneMessage()
            {
                sceneName = transitionToSceneName,
                sceneOperation = SceneOperation.LoadAdditive,
                customHandling = true
            });
            SceneManager.MoveGameObjectToScene(player, SceneManager.GetSceneByPath(transitionToSceneName));

            NetworkServer.AddPlayerForConnection(conn, player);

            if (NetworkClient.localPlayer != null &&
                player.TryGetComponent<PlayerMovement>(out PlayerMovement playerMove))
                playerMove.enabled = true;
            if (player.transform.GetChild(2).TryGetComponent<GunShooting>(out GunShooting gunShooting))
                gunShooting.enabled = true;

            if (player.GetComponent<PlayerObjectController>().playerID == LocalPlayerObjectController.playerID)
            {
                if (CameraController.Instance.freeLookCam.Target.TrackingTarget == null)
                    CameraController.Instance.freeLookCam.Target.TrackingTarget = player.transform;

                player.GetComponent<PlayerObjectController>().SetPlayerUIState(true);
            }

            if (NetworkServer.active)
                player.GetComponent<PlayerObjectController>().RpcUpdatePlayerParamsAfterTransition();

            NextSceneSettings(transitionToSceneName, player);
        }

        nextScenePath = "";
        previousScenePath = "";
    }

    void NextSceneSettings(string transitionToSceneName, GameObject player)
    {
        // 1v1 Scene Transition
        if (transitionToSceneName == SceneManager.GetSceneByName("Scene_3_1v1").path)
        {
            Show1v1Text();
            player.GetComponent<PlayerObjectController>().fellCountText.gameObject.SetActive(false);
            if (NetworkServer.active)
                player.GetComponent<PlayerObjectController>().RpcShow1v1Text();
        }

        // Scene 4 Transition
        if (transitionToSceneName == SceneManager.GetSceneByName("Scene_4").path)
        {
            CityGroupGenerator.Instance.InstantGenerating();
            player.GetComponent<PlayerObjectController>().fellCountText.gameObject.SetActive(false);
            if (NetworkServer.active)
                player.GetComponent<PlayerObjectController>().RpcCityInstantGenerating();
        }
    }

    public void ClearBullets()
    {
        Bullet[] allBullets = FindObjectsOfType<Bullet>();
        foreach (var bullet in allBullets)
        {
            if (bullet.TryGetComponent<NetworkIdentity>(out NetworkIdentity identity))
            {
                DestroyImmediate(bullet.gameObject);
            }
        }
    }
}