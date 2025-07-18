using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Mirror;
using UnityEngine.SceneManagement;

public partial class PlayerObjectController
{
    [Header("Health")] public GameObject healthBarBase;
    public Image healthBarFillImage;

    public float maxHealth = 100f;
    private float currentHealth;

    public float CurrentHealth
    {
        get => currentHealth;
        set
        {
            currentHealth = Mathf.Clamp(value, 0, maxHealth);
            healthBarFillImage.DOFillAmount(currentHealth / maxHealth, 0.2f);
            if (currentHealth <= 0)
            {
                DieIn1V1();
            }
        }
    }


    int currentScore = 0;

    public int CurrentScore
    {
        get => currentScore;
        set { currentScore = value; }
    }

    [Header("Fell Count")] int fellCount = 0;
    public Text fellCountText;

    public int FellCount
    {
        get => fellCount;
        set
        {
            fellCount = value;
            fellCountText.text = fellCount.ToString() + "/5";
            if (fellCount > 5)
            {
                MissionFailed();
            }
        }
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        healthBarFillImage.fillAmount = currentHealth / maxHealth;
        fellCountText.text = fellCount.ToString() + "/5";
    }

    private void MissionFailed()
    {
        Debug.Log("Mission Failed: Fell too many times.");
        LobbyController.Instance.ShowMissionFailedText();
        fellCount = 0;
        fellCountText.text = "Try to defeat the trapper!";

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

                if (isServer)
                    StartCoroutine(SendNewPlayerToScene(player.gameObject,
                        SceneManager.GetSceneByName("Scene_3_1v1").path, "SpawnPos"));
            }
            else if (player.role == PlayerRole.Escaper)
            {
                if (player.TryGetComponent<PlayerMovement>(out PlayerMovement pm))
                    pm.enabled = false;

                if (isServer)
                    StartCoroutine(SendNewPlayerToScene(player.gameObject,
                        SceneManager.GetSceneByName("Scene_3_1v1").path, "SpawnPos"));
            }
        }
    }

    void DieIn1V1()
    {
        Debug.Log($"Player {playerID} died in 1v1.");
        LobbyController.Instance.ShowMissionFailedText();

        PlayerObjectController[] allPlayers = FindObjectsOfType<PlayerObjectController>();
        foreach (var player in allPlayers)
        {
            if (player.TryGetComponent<PlayerMovement>(out PlayerMovement pm))
                pm.enabled = false;

            if (isServer)
                StartCoroutine(SendNewPlayerToScene(player.gameObject,
                    SceneManager.GetSceneByName("Scene_4_Terrain").path, "SpawnPos"));
        }

        if (NetworkServer.active)
            TerrainController.Instance.CanGenerateTerrain = true;
    }

    public void SetPlayerUIState(bool state)
    {
        healthBarBase.SetActive(state);
        fellCountText.gameObject.SetActive(state);
    }

    [ServerCallback]
    public IEnumerator SendNewPlayerToScene(GameObject player, string transitionToSceneName, string scenePosToSpawnOn)
    {
        if (player.TryGetComponent<NetworkIdentity>(out NetworkIdentity identity))
        {
            NetworkConnectionToClient conn = identity.connectionToClient;
            if (conn == null)
                yield break;

            conn.Send(new SceneMessage()
            {
                sceneName = gameObject.scene.path,
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


            if (player.GetComponent<PlayerObjectController>().playerID ==
                LobbyController.Instance.LocalPlayerObjectController.playerID)
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
                LobbyController.Instance.Show1v1Text();
                if (NetworkServer.active)
                    player.GetComponent<PlayerObjectController>().RpcShow1v1Text();
            }
        }
    }
}