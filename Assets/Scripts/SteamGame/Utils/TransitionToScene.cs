﻿using System.Collections;
using System.IO;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionToScene : NetworkBehaviour
{
    MyNetworkManager _networkManager;
    FadeInOutScreen fadeInOutScreen;

    [Scene] public string transitionToSceneName;

    public string scenePosToSpawnOn;

    void Awake()
    {
        if (_networkManager == null)
        {
            _networkManager = FindObjectOfType<MyNetworkManager>();
            fadeInOutScreen = FindObjectOfType<FadeInOutScreen>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerMovement>())
        {
            if (other.TryGetComponent<PlayerMovement>(out PlayerMovement pm))
            {
                pm.enabled = false;
            }

            if (isServer)
            {
                StartCoroutine(SendNewPlayerToScene(other.gameObject));
            }
        }
    }

    [ServerCallback]
    public IEnumerator SendNewPlayerToScene(GameObject player)
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

            yield return new WaitForSeconds(fadeInOutScreen.speed * 0.1f);

            NetworkServer.RemovePlayerForConnection(conn, false);

            NetworkStartPosition[] startPositions = FindObjectsOfType<NetworkStartPosition>();
            Transform startPos = _networkManager.GetStartPosition();
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
                player.GetComponent<PlayerObjectController>().RpcUpdatePlayerParamsAfterTransition(transitionToSceneName);

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