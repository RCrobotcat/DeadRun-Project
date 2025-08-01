using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using CityGenerator;

public partial class MyNetworkManager : NetworkManager
{
    public FadeInOutScreen fadeinOutScreen;
    public string firstSceneToLoad;
    private string[] scenesToLoad;
    private bool subScenesLoaded;
    private bool isInTransition;
    private bool firstSceneLoaded;

    void Start()
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings - 2;
        scenesToLoad = new string[sceneCount];
        for (int i = 0; i < sceneCount; i++)
            scenesToLoad[i] = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i + 2));
    }

    void Update()
    {
        if (SceneManager.GetSceneByName("OfflineScene").isLoaded)
        {
            if (!Cursor.visible)
                Cursor.visible = true;
        }

        var loadedScenes = SceneManager.GetAllScenes();

        // 如果场景中有 firstSceneToLoad，并且加载了多个 firstSceneToLoad，则卸载最后一个加载的 firstSceneToLoad
        if (loadedScenes.Length > 0)
        {
            int firstSceneCount = 0;
            Scene lastLoadedFirstScene = default;

            foreach (var loadedScene in loadedScenes)
            {
                if (loadedScene.name == firstSceneToLoad)
                {
                    firstSceneCount++;
                    lastLoadedFirstScene = loadedScene;
                }
            }

            if (firstSceneCount > 1)
            {
                UnloadAdditiveScene(lastLoadedFirstScene);
            }
        }

        if (fadeinOutScreen == null)
            fadeinOutScreen = FindObjectOfType<FadeInOutScreen>();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("Time out, Disconnected from server.");
        // TODO: Do reconnection or show disconnection screen
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        fadeinOutScreen.ShowScreenNoDelay();
        if (sceneName == onlineScene)
            StartCoroutine(ServerLoadSubScenes());
    }

    IEnumerator ServerLoadSubScenes()
    {
        foreach (var additiveScene in scenesToLoad)
            yield return SceneManager.LoadSceneAsync(additiveScene,
                new LoadSceneParameters
                {
                    loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D
                });
        subScenesLoaded = true;
    }

    public override void OnClientChangeScene(string sceneName, SceneOperation sceneOperation, bool customHandling)
    {
        base.OnClientChangeScene(sceneName, sceneOperation, customHandling);
        if (sceneOperation == SceneOperation.UnloadAdditive)
            StartCoroutine(UnloadAdditive(sceneName));
        else if (sceneOperation == SceneOperation.LoadAdditive)
            StartCoroutine(LoadAdditiveScene(sceneName));
    }

    public bool isNextSceneSet = false;

    IEnumerator LoadAdditiveScene(string sceneName)
    {
        isInTransition = true;
        yield return fadeinOutScreen.FadeIn();
        if (mode == NetworkManagerMode.ClientOnly)
        {
            Debug.Log("Loading scene: " + sceneName);
            loadingSceneAsync = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (loadingSceneAsync != null && !loadingSceneAsync.isDone)
                yield return null;
        }

        NetworkClient.isLoadingScene = false;
        isInTransition = false;

        if (NetworkServer.active && !isNextSceneSet)
        {
            foreach (var player in GamePlayers)
            {
                LobbyController.Instance.NextSceneSettings(sceneName, player.gameObject);
            }

            isNextSceneSet = true;
        }

        OnClientSceneChanged();
        if (!firstSceneLoaded)
        {
            firstSceneLoaded = true;
            yield return new WaitForSeconds(0.6f);
        }
        else
        {
            firstSceneLoaded = false;
            yield return new WaitForSeconds(0.5f);
        }

        yield return fadeinOutScreen.FadeOut();

        if (!NetworkServer.active)
        {
            PlayerObjectController localPlayerController =
                LobbyController.Instance.LocalPlayerObjectController;
            PlayerLoadSceneSuccessMsg msg = new PlayerLoadSceneSuccessMsg
            {
                connectionID = localPlayerController.connectionID,
                playerID = localPlayerController.playerID
            };
            NetworkClient.Send(msg);
        }
    }

    IEnumerator UnloadAdditive(string sceneName)
    {
        isInTransition = true;
        yield return fadeinOutScreen.FadeIn();
        if (mode == NetworkManagerMode.ClientOnly)
        {
            if (SceneManager.GetSceneByPath(sceneName).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(sceneName);
                yield return Resources.UnloadUnusedAssets();
            }
        }

        NetworkClient.isLoadingScene = false;
        isInTransition = false;
        OnClientSceneChanged();
    }

    public void UnloadAdditiveScene(Scene sceneToUnload)
    {
        SceneManager.UnloadSceneAsync(sceneToUnload);
    }
}