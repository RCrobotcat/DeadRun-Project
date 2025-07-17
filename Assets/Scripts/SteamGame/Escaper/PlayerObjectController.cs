using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum PlayerRole
{
    Escaper,
    Trapper
}

public partial class PlayerObjectController : NetworkBehaviour
{
    // Player Data
    [SyncVar] public int connectionID;
    [SyncVar] public int playerID;
    [SyncVar] public ulong playerSteamID;

    [SyncVar(hook = nameof(OnPlayerNameUpdated))]
    public string playerName;

    [SyncVar(hook = nameof(OnReadyStatusChanged))]
    public bool isReady;

    private MyNetworkManager _myNetworkManager;

    private MyNetworkManager MyNetworkManager
    {
        get
        {
            if (_myNetworkManager != null)
            {
                return _myNetworkManager;
            }

            return _myNetworkManager = MyNetworkManager.singleton as MyNetworkManager;
        }
    }

    [HideInInspector] public PlayerRole role = PlayerRole.Escaper;

    private void Update()
    {
        UpdateImportantParams();
    }

    public override void OnStartAuthority()
    {
        SetPlayerName(SteamFriends.GetPersonaName());
        gameObject.name = "LocalPlayer";
        LobbyController.Instance.FindLocalPlayer();
        LobbyController.Instance.UpdateLobbyName();
    }

    public override void OnStartClient()
    {
        MyNetworkManager.GamePlayers.Add(this);
        LobbyController.Instance.UpdateLobbyName();
        LobbyController.Instance.UpdatePlayerList();
    }

    public override void OnStopClient()
    {
        MyNetworkManager.GamePlayers.Remove(this);
        LobbyController.Instance.UpdatePlayerList();
    }

    [Command]
    void SetPlayerName(string playerName)
    {
        OnPlayerNameUpdated(this.playerName, playerName);
    }

    void OnPlayerNameUpdated(string oldName, string newName)
    {
        if (isServer) // Host
        {
            playerName = newName;
        }

        if (isClient) // Client
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    void OnReadyStatusChanged(bool oldVal, bool newVal)
    {
        if (isServer) // Host
        {
            isReady = newVal;
        }

        if (isClient) // Client
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    [Command]
    public void CmdSetReadyStatus()
    {
        OnReadyStatusChanged(isReady, !isReady);
    }

    public void ChangeReadyStatus()
    {
        if (isOwned)
        {
            CmdSetReadyStatus();
        }
    }

    public void SendPlayerExit()
    {
        if (isOwned)
        {
            LocalPlayerExit(); // Client
        }
    }

    public void LocalPlayerExit()
    {
        Debug.Log("Client(Member) is leaving lobby!");
        PlayerExitMsg msg = new PlayerExitMsg(connectionID, playerID, playerSteamID);
        NetworkClient.Send(msg);

        SteamLobby.Instance.ExitLobby(new CSteamID(SteamLobby.Instance.currentLobbyID));
        _myNetworkManager.StopClient();
        _myNetworkManager.GamePlayers.Clear();

        SceneManager.LoadSceneAsync("OfflineScene");
        SteamLobby.Instance.hostButton.gameObject.SetActive(true);
        SteamLobby.Instance.lobbiesButton.gameObject.SetActive(true);
        SteamLobby.Instance.quitBtn.gameObject.SetActive(true);
        SteamLobby.Instance.lobbySceneType = LobbySceneTypesEnum.Offline;
    }

    public void UpdateImportantParams()
    {
        if (SceneManager.GetSceneByName("PersistentScene").isLoaded)
        {
            if (SceneManager.GetSceneByName("Scene_1").isLoaded
                || SceneManager.GetSceneByName("Scene_2").isLoaded)
            {
                LobbyController? lobbyController = FindObjectOfType<LobbyController>();
                SteamLobby? steamLobby = FindObjectOfType<SteamLobby>();

                if (lobbyController != null && steamLobby != null)
                {
                    if (lobbyController.LocalPlayerObjectController != null)
                    {
                        if (lobbyController.LocalPlayerObjectController.playerID > 1 // Not the Host
                            && steamLobby.lobbySceneType != LobbySceneTypesEnum.GameScene)
                        {
                            steamLobby.lobbySceneType = LobbySceneTypesEnum.GameScene;
                        }
                    }
                }
            }
        }

        if (LobbyController.Instance != null)
        {
            if (LobbyController.Instance.LocalPlayerObjectController != null)
            {
                var planeRotation = FindObjectOfType<PlaneRotation>();
                if (LobbyController.Instance.LocalPlayerObjectController.role == PlayerRole.Trapper)
                {
                    if (planeRotation != null)
                    {
                        if (!planeRotation.planeRotationCanvas.activeSelf)
                            planeRotation.planeRotationCanvas.SetActive(true);
                        if (!planeRotation.trapperCamera.activeSelf)
                            planeRotation.trapperCamera.SetActive(true);

                        planeRotation.trapperCamera.GetComponent<Camera>().enabled = true;
                        planeRotation.trapperCamera.GetComponent<AudioListener>().enabled = true;

                        if (!planeRotation.trapperCamera.transform.GetChild(0).GetChild(0)
                                .GetComponent<SkinnedMeshRenderer>().enabled)
                        {
                            planeRotation.trapperCamera.transform.GetChild(0).GetChild(0)
                                .GetComponent<SkinnedMeshRenderer>().enabled = true;
                        }
                    }

                    CameraController.Instance.gameObject.SetActive(false);
                    LobbyController.Instance.LocalPlayerObjectController.transform.position =
                        new Vector3(1000, 1000, 1000); // offscreen
                }
                else if (LobbyController.Instance.LocalPlayerObjectController.role == PlayerRole.Escaper)
                {
                    if (planeRotation != null)
                    {
                        if (planeRotation.planeRotationCanvas.activeSelf)
                            planeRotation.planeRotationCanvas.SetActive(false);
                        if (!planeRotation.trapperCamera.activeSelf)
                            planeRotation.trapperCamera.SetActive(true);

                        planeRotation.trapperCamera.GetComponent<Camera>().enabled = false;
                        planeRotation.trapperCamera.GetComponent<AudioListener>().enabled = false;

                        if (!planeRotation.trapperCamera.transform.GetChild(0).GetChild(0)
                                .GetComponent<SkinnedMeshRenderer>().enabled)
                        {
                            planeRotation.trapperCamera.transform.GetChild(0).GetChild(0)
                                .GetComponent<SkinnedMeshRenderer>().enabled = true;
                        }
                    }

                    CameraController.Instance.gameObject.SetActive(true);
                    if (CameraController.Instance.freeLookCam.Target.TrackingTarget == null)
                    {
                        if (SceneManager.GetSceneByName("Scene_1").isLoaded ||
                            SceneManager.GetSceneByName("Scene_2").isLoaded)
                        {
                            if (LobbyController.Instance.LocalPlayerObjectController.playerID == 1
                                && LobbyController.Instance.localPlayerObject.scene.name != "PersistentScene")
                                CameraController.Instance.freeLookCam.Target.TrackingTarget = LobbyController.Instance
                                    .LocalPlayerObjectController.transform;

                            if (LobbyController.Instance.LocalPlayerObjectController.playerID > 1)
                                CameraController.Instance.freeLookCam.Target.TrackingTarget = LobbyController.Instance
                                    .LocalPlayerObjectController.transform;
                        }
                    }
                }
            }
        }
    }

    [ClientRpc]
    public void RpcUpdatePlayerParams(bool state_allPlayersInGameScene, bool state_playersRolesSet,
        List<PlayerRoles> roles)
    {
        if (!isClientOnly)
            return;
        MyNetworkManager.allPlayersInGameScene = state_allPlayersInGameScene;
        MyNetworkManager.playersRolesSet = state_playersRolesSet;

        foreach (var player in roles)
        {
            if (player.playerID == this.playerID)
                this.role = player.role;

            Debug.Log("player role: " + player.role + " for playerID: " + player.playerID);
        }

        LobbyController.Instance.ShowPlayerRoleText();
    }
}