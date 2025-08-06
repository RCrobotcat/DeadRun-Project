using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class LobbyController : Singleton<LobbyController>
{
    public Text lobbyNameText;
    public GameObject playerListViewContent;
    public GameObject playerListItemPrefab;
    public GameObject localPlayerObject;

    public ulong CurrentLobbyID;
    public bool PlayerItemCreated = false;
    public List<PlayerListItem> PlayerListItems = new();
    public PlayerObjectController LocalPlayerObjectController;

    public Button readyBtn;
    public Text readyBtnText;
    public Button startGameBtn;
    public Button exitLobbyBtn;

    public GameObject lobbyCanvas;

    private List<PlayerRoles> roles; // all players roles set in host server

    [Header("Splaton 1v1 Settings")] public float countDownTime = 180f; // seconds
    [HideInInspector] public float countDownTimer = 0;
    public GameObject countDownPanel;
    public Text countDownText;

    MyNetworkManager _myNetworkManager;

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

    protected override void Awake()
    {
        base.Awake();
        readyBtn.onClick.AddListener(ReadyPlayer);
        startGameBtn.onClick.AddListener(StartGame);
        exitLobbyBtn.onClick.AddListener(ExitGameLobby);
    }

    private void Update()
    {
        SetLobbyCanvasState();

        // Host Set Players Roles
        if (MyNetworkManager.allPlayersInGameScene_server && !MyNetworkManager.playersRolesSet)
        {
            if (NetworkServer.active)
            {
                trapperCount = 1;
                escaperCount = MyNetworkManager.GamePlayers.Count - trapperCount;

                roles = MyNetworkManager.SetPlayersRoles();

                if (roles != null)
                    LocalPlayerObjectController.RpcUpdatePlayerParams(MyNetworkManager.allPlayersInGameScene_server,
                        MyNetworkManager.playersRolesSet, roles);

                ShowPlayerRoleText();
                if (LocalPlayerObjectController.role == PlayerRole.Escaper)
                    LocalPlayerObjectController.SetPlayerUIState(true);
            }
        }
    }

    public void UpdateLobbyName()
    {
        CurrentLobbyID = MyNetworkManager.GetComponent<SteamLobby>().currentLobbyID;
        lobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyID), "LobbyName");
    }

    public void UpdatePlayerList()
    {
        if (!PlayerItemCreated)
            CreateHostPlayerItem(); // Host

        if (PlayerListItems.Count < MyNetworkManager.GamePlayers.Count)
            CreateClientPlayerItem();

        if (PlayerListItems.Count > MyNetworkManager.GamePlayers.Count)
            RemovePlayerItem();

        if (PlayerListItems.Count == MyNetworkManager.GamePlayers.Count)
            UpdatePlayerItem();
    }

    public void FindLocalPlayer()
    {
        localPlayerObject = GameObject.Find("LocalPlayer");
        LocalPlayerObjectController = localPlayerObject.GetComponent<PlayerObjectController>();
    }

    public void CreateHostPlayerItem()
    {
        foreach (PlayerObjectController player in MyNetworkManager.GamePlayers)
        {
            GameObject newPlayerItem = Instantiate(playerListItemPrefab);
            PlayerListItem newPlayerListItem = newPlayerItem.GetComponent<PlayerListItem>();

            newPlayerListItem.playerName = player.playerName;
            newPlayerListItem.playerSteamID = player.playerSteamID;
            newPlayerListItem.connectionID = player.connectionID;
            newPlayerListItem.isReady = player.isReady;
            newPlayerListItem.SetPlayerValues();

            newPlayerListItem.transform.SetParent(playerListViewContent.transform);
            newPlayerListItem.transform.localScale = Vector3.one;
            newPlayerListItem.transform.localPosition = Vector3.zero;

            PlayerListItems.Add(newPlayerListItem);
        }

        PlayerItemCreated = true;
    }

    public void CreateClientPlayerItem()
    {
        foreach (PlayerObjectController player in MyNetworkManager.GamePlayers)
        {
            if (PlayerListItems.All(b => b.connectionID != player.connectionID))
            {
                GameObject newPlayerItem = Instantiate(playerListItemPrefab);
                PlayerListItem newPlayerListItem = newPlayerItem.GetComponent<PlayerListItem>();

                newPlayerListItem.playerName = player.playerName;
                newPlayerListItem.playerSteamID = player.playerSteamID;
                newPlayerListItem.connectionID = player.connectionID;
                newPlayerListItem.isReady = player.isReady;
                newPlayerListItem.SetPlayerValues();

                newPlayerListItem.transform.SetParent(playerListViewContent.transform);
                newPlayerListItem.transform.localScale = Vector3.one;
                newPlayerListItem.transform.localPosition = Vector3.zero;

                PlayerListItems.Add(newPlayerListItem);
            }
        }
    }

    public void UpdatePlayerItem()
    {
        foreach (PlayerObjectController player in MyNetworkManager.GamePlayers)
        {
            foreach (PlayerListItem item in PlayerListItems)
            {
                if (item.connectionID == player.connectionID)
                {
                    item.playerName = player.playerName;
                    item.isReady = player.isReady;
                    item.SetPlayerValues();
                    if (player == LocalPlayerObjectController)
                        UpdateReadyBtn();
                }
            }
        }

        CheckIfAllReady();
    }

    public void RemovePlayerItem()
    {
        List<PlayerListItem> playerListItemToRemove = new();
        foreach (PlayerListItem item in PlayerListItems)
        {
            if (MyNetworkManager.GamePlayers.All(b => b.connectionID != item.connectionID))
            {
                playerListItemToRemove.Add(item);
            }
        }

        if (playerListItemToRemove.Count > 0)
        {
            foreach (PlayerListItem removeItem in playerListItemToRemove)
            {
                GameObject objectToRemove = removeItem.gameObject;
                PlayerListItems.Remove(removeItem);
                Destroy(objectToRemove);
            }
        }
    }

    public void ReadyPlayer()
    {
        LocalPlayerObjectController.ChangeReadyStatus();
    }

    public void StartGame()
    {
        string scenePath = SceneManager.GetSceneByName("Scene_1").name;
        lobbyCanvas.SetActive(false);
        MyNetworkManager.HandleSendPlayerToNewScene(scenePath, "SpawnPos");
    }

    public void UpdateReadyBtn()
    {
        if (LocalPlayerObjectController.isReady)
        {
            readyBtnText.text = "Unready";
            readyBtnText.color = Color.red;
        }
        else
        {
            readyBtnText.text = "Ready";
            readyBtnText.color = Color.green;
        }
    }

    public void ExitGameLobby()
    {
        // SteamLobby.Instance.ExitLobby(new CSteamID(CurrentLobbyID));
        // LocalPlayerObjectController.SendPlayerExit();
        int playerID = LocalPlayerObjectController.playerID;
        if (playerID == 1) // Host
        {
            Debug.Log("Host is exiting lobby!");
            PlayerExitMsg msg = new PlayerExitMsg(LocalPlayerObjectController.connectionID,
                LocalPlayerObjectController.playerID, LocalPlayerObjectController.playerSteamID);
            NetworkServer.SendToAll(msg);
            SteamLobby.Instance.ExitLobby(new CSteamID(SteamLobby.Instance.currentLobbyID));
        }
        else // Lobby Member
        {
            LocalPlayerObjectController.SendPlayerExit();
        }
    }

    public void CheckIfAllReady()
    {
        bool AllReady = false;
        foreach (PlayerObjectController player in MyNetworkManager.GamePlayers)
        {
            if (player.isReady)
            {
                AllReady = true;
            }
            else
            {
                AllReady = false;
                break;
            }
        }

        if (AllReady)
        {
            if (LocalPlayerObjectController.playerID == 1) // Host
            {
                startGameBtn.interactable = true;
            }
            else
            {
                startGameBtn.interactable = false;
            }
        }
        else
        {
            startGameBtn.interactable = false;
        }
    }

    public void SetLobbyCanvasState()
    {
        SteamLobby steamLobby = FindObjectOfType<SteamLobby>();
        if (steamLobby != null)
        {
            if (steamLobby.lobbySceneType == LobbySceneTypesEnum.GameLobby)
            {
                if (!lobbyCanvas.activeSelf)
                    lobbyCanvas.SetActive(true);
            }
            else
            {
                if (lobbyCanvas.activeSelf)
                    lobbyCanvas.SetActive(false);
            }
        }
    }

    #region Text Popups

    [Header("Player Role Text")] public Text playerRoleText;
    public GameObject roleTxtPanel;

    public void ShowPlayerRoleText()
    {
        roleTxtPanel.SetActive(true);
        playerRoleText.text = LocalPlayerObjectController.role.ToString();
        roleTxtPanel.transform.localScale.To(Vector3.one * 1.5f, 2.5f,
            (a) => { roleTxtPanel.transform.localScale = a; }, () => { roleTxtPanel.SetActive(false); });
    }

    [Header("1v1 Text")] public Text oneVOneText;
    public GameObject oneVOnePanel;

    public void Show1v1Text()
    {
        oneVOnePanel.SetActive(true);
        oneVOnePanel.transform.localScale.To(Vector3.one * 1.5f, 2.5f,
            (a) => { oneVOnePanel.transform.localScale = a; }, () =>
            {
                oneVOnePanel.transform.localScale = Vector3.one;
                oneVOnePanel.SetActive(false);
            });
    }

    [Header("Mission Status Text")] public GameObject missionSuccessPanel;
    public GameObject missionFailedPanel;
    public GameObject popupTextPanel;

    public void ShowMissionSuccessText(string text)
    {
        missionSuccessPanel.SetActive(true);
        missionSuccessPanel.transform.localScale.To(Vector3.one * 1.5f, 2.5f,
            (a) => { missionSuccessPanel.transform.localScale = a; }, () =>
            {
                missionSuccessPanel.transform.localScale = Vector3.one;
                missionSuccessPanel.SetActive(false);
            });
        missionSuccessPanel.GetComponentInChildren<Text>().text = text;
    }

    public void ShowMissionFailedText(string text)
    {
        missionFailedPanel.SetActive(true);
        missionFailedPanel.transform.localScale.To(Vector3.one * 1.5f, 2.5f,
            (a) => { missionFailedPanel.transform.localScale = a; }, () =>
            {
                missionFailedPanel.transform.localScale = Vector3.one;
                missionFailedPanel.SetActive(false);
            });
        missionFailedPanel.GetComponentInChildren<Text>().text = text;
    }

    public void ShowPopupText(string text)
    {
        popupTextPanel.SetActive(true);
        popupTextPanel.transform.localScale.To(Vector3.one * 1.5f, 2.5f,
            (a) => { popupTextPanel.transform.localScale = a; }, () =>
            {
                popupTextPanel.transform.localScale = Vector3.one;
                popupTextPanel.SetActive(false);
            });
        popupTextPanel.GetComponentInChildren<Text>().text = text;
    }

    public void ShowCountDownText()
    {
        if (!NetworkServer.active)
            return;

        countDownPanel.SetActive(true);
        countDownTimer = countDownTime;
        StartCoroutine(CountDownCoroutine());
    }

    private IEnumerator CountDownCoroutine()
    {
        while (countDownTimer > 0)
        {
            int minutes = Mathf.FloorToInt(countDownTimer / 60f);
            int seconds = Mathf.FloorToInt(countDownTimer % 60f);
            countDownText.text = $"{minutes:D2}:{seconds:D2}";
            UpdateOtherPlayersCountdown(countDownTimer);
            yield return new WaitForSeconds(1f);
            countDownTimer -= 1f;
        }

        countDownText.text = "00:00";
        UpdateOtherPlayersCountdown(0f);

        yield return new WaitForSeconds(0.2f);

        // Show Match Results
        countDownTimer = 10f; // show match results for 10 seconds
        FindObjectOfType<MatchResultsList>().ShowMatchResults();
        yield return new WaitForSeconds(countDownTimer);

        FindObjectOfType<MatchResultsList>().HideMatchResults();
        HideOtherPlayersResultsPanel();

        // go to end scene
        TransitionAllPlayersToEndScene(SceneManager.GetSceneByName("Scene_5_Painting").path);
    }

    void UpdateOtherPlayersCountdown(float currentTime)
    {
        foreach (var player in MyNetworkManager.GamePlayers)
        {
            if (player.playerID != 1) // Not the host
            {
                player.RpcUpdateCountdown(currentTime);
            }
        }
    }

    void HideOtherPlayersResultsPanel()
    {
        foreach (var player in MyNetworkManager.GamePlayers)
        {
            if (player.playerID != 1) // Not the host
            {
                player.RpcHideOtherPlayersResultsPanel();
            }
        }
    }

    #endregion
}