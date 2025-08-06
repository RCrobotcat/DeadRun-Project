using System.Collections;
using System.Collections.Generic;
using System.IO;
using CityGenerator;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class LobbyController
{
    int currentGameSceneIndex = 1; // Start from 1 for the first scene

    public int CurrentGameSceneIndex
    {
        get
        {
            currentGameSceneIndex++;
            Debug.Log("Current Game Scene Index: " + currentGameSceneIndex);
            return currentGameSceneIndex;
        }
    }

    Dictionary<int, string> mainGameScenes = new Dictionary<int, string>
    {
        { 1, "Assets/Scenes/DemoScene/Scene_1.unity" }, // Level 1
        { 2, "Assets/Scenes/DemoScene/Scene_4.unity" }, // Level 2
        { 3, "Assets/Scenes/DemoScene/Scene_5_Painting.unity" } // Level 3
    };

    private int escaperCount;
    private int trapperCount = 1;

    [HideInInspector] public string previousScenePath = "";

    int deadEscaperCount = 0;

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

    int requiredCountReachedEscaperCount = 0;

    public int RequiredCountReachedEscaperCount
    {
        get => requiredCountReachedEscaperCount;
        set
        {
            requiredCountReachedEscaperCount = value;
            if (requiredCountReachedEscaperCount >= 1)
            {
                requiredCountReachedEscaperCount = 0;

                string previousScenePathTemp = previousScenePath;
                TransitionAllPlayersTo1V1(previousScenePathTemp);
            }
        }
    }

    int deadEscaperCountIn1V1 = 0;

    public int DeadEscaperCountIn1V1
    {
        get => deadEscaperCountIn1V1;
        set
        {
            deadEscaperCountIn1V1 = value;
            if (deadEscaperCountIn1V1 >= 1)
            {
                deadEscaperCountIn1V1 = 0;

                SendingAllPlayersToScene(mainGameScenes[CurrentGameSceneIndex],
                    SceneManager.GetSceneByName("Scene_3_1v1").path);
            }
        }
    }

    void SendingAllPlayersToScene(string nextScenePathName, string previousScenePathName)
    {
        ClearBullets();

        nextScenePath = nextScenePathName;
        previousScenePath = previousScenePathName;
        NeedTransitionToOtherScene = true;
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

    public void TransitionAllPlayersTo1V1(string previousScenePathName)
    {
        PlayerObjectController[] allPlayers = FindObjectsOfType<PlayerObjectController>();
        ClearBullets();

        MyNetworkManager.isNextSceneSet = false;

        foreach (var player in allPlayers)
        {
            ClearBullets();
            if (player.role == PlayerRole.Trapper)
            {
                CameraController.Instance.gameObject.SetActive(true);
                CameraController.Instance.freeLookCam.Target.TrackingTarget = player.transform;
                player.role = PlayerRole.Escaper;
                player.SetPlayerUIState(true);

                if (player.TryGetComponent<Collider>(out Collider collider))
                    collider.enabled = false;

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
                if (player.TryGetComponent<Collider>(out Collider collider))
                    collider.enabled = false;

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

        MyNetworkManager.isNextSceneSet = false;

        foreach (var player in allPlayers)
        {
            if (player.role == PlayerRole.Trapper)
            {
                CameraController.Instance.gameObject.SetActive(true);
                CameraController.Instance.freeLookCam.Target.TrackingTarget = player.transform;
                player.transform.position = Vector3.zero;
                player.role = PlayerRole.Escaper;
                player.SetPlayerUIState(true);

                if (player.TryGetComponent<Collider>(out Collider collider))
                    collider.enabled = false;

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

                if (player.TryGetComponent<Collider>(out Collider collider))
                    collider.enabled = false;

                if (player.isServer)
                    StartCoroutine(SendNewPlayerToScene(player.gameObject, scenePathName, scenePosToSpawnOn,
                        previousScenePathName));
            }
        }
    }

    public void TransitionAllPlayersToEndScene(string previousPathName)
    {
        PlayerObjectController[] allPlayers = FindObjectsOfType<PlayerObjectController>();

        MyNetworkManager.isNextSceneSet = false;

        foreach (var player in allPlayers)
        {
            if (player.TryGetComponent<Collider>(out Collider collider))
                collider.enabled = false;

            if (player.TryGetComponent<PlayerMovement>(out PlayerMovement pm))
                pm.enabled = false;
            if (player.transform.GetChild(2).TryGetComponent<GunShooting>(out GunShooting gunShooting))
                gunShooting.enabled = false;

            if (player.isServer)
                StartCoroutine(SendNewPlayerToScene(player.gameObject,
                    SceneManager.GetSceneByName("Scene_6_End").path, "SpawnPos", previousPathName));
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

            if (NetworkServer.active)
            {
                PlayerObjectController p = player.GetComponent<PlayerObjectController>();
                p.RpcUpdatePlayerParamsAfterTransition(transitionToSceneName);
            }

            if (player.GetComponent<PlayerObjectController>().playerID ==
                LocalPlayerObjectController.playerID) // playerID == 1
            {
                if (CameraController.Instance.freeLookCam.Target.TrackingTarget == null)
                    CameraController.Instance.freeLookCam.Target.TrackingTarget = player.transform;

                player.GetComponent<PlayerObjectController>().SetPlayerUIState(true);
                player.GetComponent<PlayerObjectController>().scoreText.text = player
                    .GetComponent<PlayerObjectController>()
                    .CurrentScore.ToString();
            }

            if (NetworkClient.localPlayer != null &&
                player.TryGetComponent<PlayerMovement>(out PlayerMovement playerMove))
                playerMove.enabled = true;
            if (player.transform.GetChild(2).TryGetComponent<GunShooting>(out GunShooting gunShooting))
                gunShooting.enabled = true;

            if (player.GetComponent<PlayerObjectController>().playerID == 1)
            {
                if (player.TryGetComponent<Collider>(out Collider collider))
                    collider.enabled = true;
            }

            yield return new WaitForSeconds(0.3f);

            // Ensure again some settings are applied successfully
            NextSceneSettingsSecondary(transitionToSceneName, player);
        }

        nextScenePath = "";
        previousScenePath = "";
    }

    public void NextSceneSettings(string transitionToSceneName, GameObject player)
    {
        PlayerObjectController playerObjectController = player.GetComponent<PlayerObjectController>();
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();

        // 1v1 Scene Transition
        if (transitionToSceneName == SceneManager.GetSceneByName("Scene_3_1v1").path)
        {
            playerMovement.currentEquippedItem = "";
            if (playerObjectController.playerID == 1) // Host
            {
                Show1v1Text();
                playerObjectController.fellCountText.gameObject.SetActive(false);
            }
            else
            {
                playerObjectController.RpcShow1v1Text();
            }
        }

        // Scene 4 Transition
        if (transitionToSceneName == SceneManager.GetSceneByName("Scene_4").path)
        {
            if (playerObjectController.playerID == 1) // Host
            {
                Debug.Log("Host instant generating city.");

                playerMovement.currentEquippedItem = "";
                playerObjectController.fellCountText.gameObject.SetActive(false);
                playerMovement.isAiming = false;
                CameraController.Instance.freeLookCam.Lens.FieldOfView = 70f;
                CameraController.Instance.freeLookCam.Lens.FarClipPlane = 500f;
                //player.GetComponent<PlayerMovement>().gun.gameObject.SetActive(false);
                if (!CityGroupGenerator.Instance.IsInitial)
                    CityGroupGenerator.Instance.InstantGenerating();
            }
            else
            {
                Debug.Log("Client " + playerObjectController.playerID + " instant generating city.");
                playerObjectController.RpcCityInstantGenerating();
            }
        }

        // Scene 5 Transition
        if (transitionToSceneName == SceneManager.GetSceneByName("Scene_5_Painting").path)
        {
            if (playerObjectController.playerID == 1) // Host
            {
                playerMovement.currentEquippedItem = "";
                playerObjectController.fellCountText.gameObject.SetActive(false);
                CameraController.Instance.freeLookCam.Lens.FieldOfView = 80f;
                CameraController.Instance.freeLookCam.Lens.FarClipPlane = 500f;
                if (!SplatonPlaceGenerator.Instance.IsInitialized)
                    SplatonPlaceGenerator.Instance.InitializePlace();
            }
            else
            {
                playerObjectController.RpcSplatonGenerating();
            }
        }

        if (NetworkServer.active)
            playerObjectController.CurrentHealth = playerObjectController.maxHealth;
    }

    void NextSceneSettingsSecondary(string transitionToSceneName, GameObject player)
    {
        PlayerObjectController playerObjectController = player.GetComponent<PlayerObjectController>();
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();

        if (NetworkServer.active)
        {
            PlayerObjectController playerValues = player.GetComponent<PlayerObjectController>();
            playerValues.RpcSyncPlayerScoreToClient(playerValues.CurrentScore);
        }

        // 1v1 Scene Transition
        if (transitionToSceneName == SceneManager.GetSceneByName("Scene_3_1v1").path)
        {
            playerMovement.currentEquippedItem = "";
            if (playerObjectController.playerID == 1) // Host
            {
                playerObjectController.fellCountText.gameObject.SetActive(false);

                SoundController.Instance.PlayMusic(Random.Range(0, 3), true);
            }
            else
            {
                playerObjectController.RpcSetPlayerFellCountUIState(false);
            }
        }

        // Scene 4 Transition
        if (transitionToSceneName == SceneManager.GetSceneByName("Scene_4").path)
        {
            if (playerObjectController.playerID == 1) // Host
            {
                playerObjectController.fellCountText.gameObject.SetActive(false);
                ShowPopupText("Level 2: Fight for Collections!");
                playerObjectController.collectionUIBase.SetActive(true);
                playerMovement.moveSpeed = 10f;

                SoundController.Instance.PlayMusic(Random.Range(0, 3), true);
            }
            else
            {
                playerObjectController.RpcSetPlayerFellCountUIState(false);
                playerObjectController.RpcShowPopupText("Level 2: Fight for Collections!");
                playerObjectController.RpcSetCollectionUIState(true);
            }
        }

        // Scene 5 Transition
        if (transitionToSceneName == SceneManager.GetSceneByName("Scene_5_Painting").path)
        {
            if (playerObjectController.playerID == 1) // Host
            {
                playerObjectController.collectionUIBase.SetActive(false);

                playerMovement.currentEquippedItem = "";
                playerObjectController.fellCountText.gameObject.SetActive(false);

                if (SplatonPlaceGenerator.Instance.IsInitialized)
                {
                    Debug.Log("Host is setting player position in Splaton Place Generator.");
                    SpawnPosPainting[] startPos = FindObjectsOfType<SpawnPosPainting>();
                    foreach (var pos in startPos)
                    {
                        if (pos.spawnedPlayerID == playerObjectController.playerID)
                        {
                            player.transform.position = pos.transform.position;
                            player.GetComponent<PlayerSplatonPainting>().SelfPaintingColor = pos.paintingColor;
                            break;
                        }
                    }
                }

                ShowPopupText("Level 3: Paint Paint Paint!");

                ShowCountDownText(); // start countdown for painting
                player.GetComponent<PlayerSplatonPainting>().currentPaintedAreasPanel.SetActive(true);

                SoundController.Instance.PlayMusic(Random.Range(0, 3), true);
            }
            else
            {
                playerObjectController.RpcSetCollectionUIState(false);
                playerObjectController.RpcSplatonGenerating();
            }
        }

        // Scene 6 (End Scene) Transition
        if (transitionToSceneName == SceneManager.GetSceneByName("Scene_6_End").path)
        {
            PlayerObjectController winner = null;
            int maxScore = -1;
            foreach (var p in MyNetworkManager.GamePlayers)
            {
                p.isEnd = true;
                p.GetComponent<PlayerMovement>().isEnd = true;

                if (p.CurrentScore > maxScore)
                {
                    maxScore = p.CurrentScore;
                    winner = p;
                }
            }

            if (playerObjectController.playerID == 1) // Host
            {
                CameraController.Instance.gameObject.SetActive(false);
                countDownPanel.gameObject.SetActive(false);

                ResultCanvas resultCanvas = FindObjectOfType<ResultCanvas>();
                if (resultCanvas != null)
                    resultCanvas.ShowResult(winner.playerName, winner.CurrentScore,
                        playerObjectController.CurrentScore);
            }
            else
            {
                CameraController.Instance.gameObject.SetActive(false);
                countDownPanel.gameObject.SetActive(false);

                playerObjectController.RpcShowResultCanvas(winner.playerName, winner.CurrentScore,
                    playerObjectController.CurrentScore);
            }
        }

        if (NetworkServer.active)
            playerObjectController.CurrentHealth = playerObjectController.maxHealth;
    }

    public void ClearBullets()
    {
        Bullet[] allBullets = FindObjectsOfType<Bullet>();
        foreach (var bullet in allBullets)
        {
            if (bullet.TryGetComponent<NetworkIdentity>(out NetworkIdentity identity))
            {
                Destroy(bullet.gameObject);
            }
        }
    }
}