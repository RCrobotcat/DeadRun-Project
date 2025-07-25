﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Steamworks;
using UnityEngine.SceneManagement;

public enum LobbySceneTypesEnum
{
    Offline,
    GameLobby,
    GameScene
}

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance; // Singleton

    public Button hostButton;
    public Button lobbiesButton;
    public Button quitBtn;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    private const string HostAddressKey = "HostAddress";

    public ulong currentLobbyID;

    protected Callback<LobbyMatchList_t> lobbyList;
    protected Callback<LobbyDataUpdate_t> lobbyDataUpdated;

    public List<CSteamID> lobbiesIDs = new();

    private MyNetworkManager networkManager;

    public LobbySceneTypesEnum lobbySceneType = LobbySceneTypesEnum.Offline;

    private void Start()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        networkManager = GetComponent<MyNetworkManager>();

        if (!SteamManager.Initialized)
            return;

        hostButton.onClick.AddListener(HostLobby);
        lobbiesButton.onClick.AddListener(GetListOfLobbies);
        quitBtn.onClick.AddListener(() => Application.Quit());

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        lobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbiesList);
        lobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyData);
    }

    private void Update()
    {
        if (SceneManager.GetSceneByName("OfflineScene").isLoaded)
            if (!CameraController.Instance.gameObject.activeSelf)
                CameraController.Instance.gameObject.SetActive(true);
    }

    public void HostLobby()
    {
        if (Instance == null)
            Instance = this;

        // SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, networkManager.maxConnections);

        hostButton.gameObject.SetActive(false);
        lobbiesButton.gameObject.SetActive(false);
        quitBtn.gameObject.SetActive(false);
        lobbySceneType = LobbySceneTypesEnum.GameLobby;
    }

    void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            hostButton.gameObject.SetActive(true);
            Debug.LogError("Failed to create lobby: " + callback.m_eResult);
            return;
        }

        networkManager.StartHost();

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey,
            SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "LobbyName",
            SteamFriends.GetPersonaName() + "'s Lobby");
    }

    void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        hostButton.gameObject.SetActive(false);
        lobbiesButton.gameObject.SetActive(false);
        quitBtn.gameObject.SetActive(false);
        lobbySceneType = LobbySceneTypesEnum.GameLobby;
    }

    void OnLobbyEntered(LobbyEnter_t callback)
    {
        currentLobbyID = callback.m_ulSteamIDLobby;
        networkManager.SetUpClientMsgHandlers();

        if (NetworkServer.active)
            return;

        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();
        networkManager.SetUpClientMsgHandlers();
    }

    public void GetListOfLobbies()
    {
        hostButton.gameObject.SetActive(false);
        lobbiesButton.gameObject.SetActive(false);
        quitBtn.gameObject.SetActive(false);
        LobbiesManager.Instance.lobbiesMenu.SetActive(true);
        GetLobbiesList();
    }

    public void JoinLobby(CSteamID lobbyID)
    {
        SteamMatchmaking.JoinLobby(lobbyID);
        hostButton.gameObject.SetActive(false);
        lobbiesButton.gameObject.SetActive(false);
        quitBtn.gameObject.SetActive(false);
        lobbySceneType = LobbySceneTypesEnum.GameLobby;
    }

    public void ExitLobby(CSteamID lobbyID)
    {
        SteamMatchmaking.LeaveLobby(lobbyID);
        // hostButton.gameObject.SetActive(true);
        // lobbiesButton.gameObject.SetActive(true);
        // lobbySceneType = LobbySceneTypesEnum.Offline;
    }

    private void OnGetLobbiesList(LobbyMatchList_t result)
    {
        if (LobbiesManager.Instance.lobbiesList.Count > 0)
            LobbiesManager.Instance.DestroyAllLobbies();

        for (int i = 0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            if (!lobbiesIDs.Contains(lobbyID))
            {
                lobbiesIDs.Add(lobbyID);
                SteamMatchmaking.RequestLobbyData(lobbyID);
            }
        }
    }

    private void OnGetLobbyData(LobbyDataUpdate_t result)
    {
        LobbiesManager.Instance.DisplayLobbiesList(lobbiesIDs, result);
    }

    public void GetLobbiesList()
    {
        if (lobbiesIDs.Count > 0)
            lobbiesIDs.Clear();

        SteamMatchmaking.AddRequestLobbyListResultCountFilter(60); // Get 60 lobbies max
        SteamMatchmaking.AddRequestLobbyListStringFilter("LobbyName", "",
            ELobbyComparison.k_ELobbyComparisonNotEqual); // Only get lobbies with a name
        SteamMatchmaking.RequestLobbyList();
    }
}