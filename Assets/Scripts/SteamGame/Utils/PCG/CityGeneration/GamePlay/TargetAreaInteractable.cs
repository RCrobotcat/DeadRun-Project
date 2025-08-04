using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TargetAreaInteractable : NetworkBehaviour
{
    public int requiredCollectableItemCount = 20;

    [SyncVar(hook = nameof(OnAreaItemsCountChanged))]
    private int currentCollectableItemCount = 0;

    public string desiredItem = "";

    private bool isRequiredItemCountReached = false;

    private ItemsManager itemsManager;

    public int possessivePlayerId;

    public Text progressText;

    public GameObject flag;

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

    private void Start()
    {
        itemsManager = FindObjectOfType<ItemsManager>();
        OnAreaItemsCountChanged(0, 0);
        progressText.text = currentCollectableItemCount + "/" + requiredCollectableItemCount;
    }

    private void Update()
    {
        PlayerMovement[] allPlayers = FindObjectsOfType<PlayerMovement>();
        foreach (var player in allPlayers)
        {
            GameObject playerObj = player.gameObject;

            if (playerObj && player.isLocalPlayer && player.ObjPlayerIsNear == gameObject &&
                player.playerObjectController.playerID == possessivePlayerId)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    CmdInteractWithArea(playerObj);
                }
            }
        }

        if (possessivePlayerId != LobbyController.Instance.LocalPlayerObjectController.playerID)
        {
            if (gameObject.layer == LayerMask.NameToLayer("Interactable"))
            {
                gameObject.layer = LayerMask.NameToLayer("Default");
                foreach (Transform child in flag.transform)
                    child.gameObject.layer = LayerMask.NameToLayer("Default");
            }
        }
    }

    [Command(requiresAuthority = false)]
    void CmdInteractWithArea(GameObject player)
    {
        if (currentCollectableItemCount < requiredCollectableItemCount
            && player.GetComponent<PlayerMovement>().currentEquippedItem == desiredItem)
        {
            currentCollectableItemCount++;
            player.GetComponent<PlayerMovement>().currentEquippedItem = "";
            progressText.text = currentCollectableItemCount + "/" + requiredCollectableItemCount;
            if (currentCollectableItemCount >= requiredCollectableItemCount)
            {
                progressText.text = "Done!";
                progressText.color = Color.green;
                if (player.GetComponent<PlayerObjectController>().playerID ==
                    LobbyController.Instance.LocalPlayerObjectController.playerID)
                    LobbyController.Instance.ShowMissionSuccessText("Required item count reached in target area!");
                else
                    player.GetComponent<PlayerObjectController>()
                        .RpcShowMissionSuccessText(player.GetComponent<PlayerObjectController>().playerID,
                            "Required item count reached in target area!");
            }

            RpcUpdateProgressText(currentCollectableItemCount);
        }
    }

    [ClientRpc]
    private void RpcUpdateProgressText(int val)
    {
        if (!isClientOnly)
            return;

        currentCollectableItemCount = val;
        progressText.text = currentCollectableItemCount + "/" + requiredCollectableItemCount;
        if (currentCollectableItemCount >= requiredCollectableItemCount)
        {
            progressText.text = "Done!";
            progressText.color = Color.green;
        }
    }

    private void OnAreaItemsCountChanged(int oldCount, int newCount)
    {
        Debug.Log($"Target Area Items Count Changed: {newCount}/{requiredCollectableItemCount}");
        if (currentCollectableItemCount >= requiredCollectableItemCount && !isRequiredItemCountReached)
        {
            isRequiredItemCountReached = true;
            Debug.Log("Required item count reached in target area.");

            // Add Score
            PlayerObjectController winPlayer = MyNetworkManager.GamePlayers.Find(
                p => p.playerID == possessivePlayerId);
            if (NetworkServer.active)
            {
                if (winPlayer != null)
                {
                    winPlayer.CurrentScore++;
                }
            }
            else
            {
                if (winPlayer != null)
                {
                    winPlayer.CmdAddScore();
                }
            }

            if (!NetworkServer.active)
            {
                CmdAddRequiredCountReachedEscaperCount(SceneManager.GetSceneByName("Scene_4").path);
            }
            else
            {
                LobbyController.Instance.previousScenePath = SceneManager.GetSceneByName("Scene_4").path;
                LobbyController.Instance.RequiredCountReachedEscaperCount++;
            }
        }
    }

    [ClientRpc]
    public void RpcUpdateTargetAreaPossessivePlayerId(int playerPlayerID)
    {
        if (!isClientOnly)
            return;
        possessivePlayerId = playerPlayerID;
    }

    [Command(requiresAuthority = false)]
    void CmdAddRequiredCountReachedEscaperCount(string previousScenePath)
    {
        LobbyController.Instance.previousScenePath = previousScenePath;
        LobbyController.Instance.RequiredCountReachedEscaperCount++;
    }
}