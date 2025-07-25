using Mirror;
using UnityEngine;
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
            if (gameObject.layer == LayerMask.GetMask("Interactable"))
                gameObject.layer = LayerMask.GetMask("Default");
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
            }
        }
    }

    private void OnAreaItemsCountChanged(int oldCount, int newCount)
    {
        Debug.Log($"Target Area Items Count Changed: {newCount}/{requiredCollectableItemCount}");
        if (currentCollectableItemCount >= requiredCollectableItemCount && !isRequiredItemCountReached)
        {
            isRequiredItemCountReached = true;
            Debug.Log("Required item count reached in target area.");
            // TODO 
        }
    }

    [ClientRpc]
    public void RpcUpdateTargetAreaPossessivePlayerId(int playerPlayerID)
    {
        if (!isClientOnly)
            return;
        possessivePlayerId = playerPlayerID;
    }
}