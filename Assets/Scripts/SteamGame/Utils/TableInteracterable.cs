using Mirror;
using UnityEngine;

public class TableInteracterable : NetworkBehaviour
{
    private ItemsManager itemsManager;
    public string desiredItem = "";

    [SyncVar(hook = nameof(OnTableItemChanged))]
    private string tableItem;

    public bool targetTable = false; // 是否是目标桌子 => 逃脱者需要放置目标物品的桌子

    TransitionToScene transitionToScene;

    private void Start()
    {
        itemsManager = FindObjectOfType<ItemsManager>();
        transitionToScene = GetComponent<TransitionToScene>();
        if (isServer)
        {
            if (!targetTable)
                tableItem = desiredItem;
            else
                tableItem = "";
        }

        OnTableItemChanged(null, tableItem);
    }

    private void Update()
    {
        PlayerMovement[] allPlayers = FindObjectsOfType<PlayerMovement>();
        foreach (var player in allPlayers)
        {
            GameObject playerObj = player.gameObject;

            if (playerObj && player.isLocalPlayer && player.ObjPlayerIsNear == gameObject)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    CmdInteractWithTable(playerObj);
                }
            }
        }

        if (targetTable && tableItem != "")
        {
            HandleTargetTablePlaced();
            tableItem = "";
        }
    }

    [Command(requiresAuthority = false)]
    void CmdInteractWithTable(GameObject player)
    {
        if (tableItem.Equals(desiredItem)
            && player.GetComponent<PlayerMovement>().currentEquippedItem == "")
        {
            player.GetComponent<PlayerMovement>().currentEquippedItem = tableItem;

            tableItem = "";
        }
        else if (tableItem == "" &&
                 player.GetComponent<PlayerMovement>().currentEquippedItem == desiredItem)
        {
            tableItem = player.GetComponent<PlayerMovement>().currentEquippedItem;

            player.GetComponent<PlayerMovement>().currentEquippedItem = "";
        }
    }

    private void OnTableItemChanged(string oldTableItem, string newTableItem)
    {
        if (itemsManager)
        {
            foreach (Transform item in transform.Find("InterestParent").transform)
            {
                Destroy(item.gameObject);
            }

            if (newTableItem != "")
            {
                Transform newObj = Instantiate(itemsManager.items.transform.Find(newTableItem),
                    transform.Find("InterestParent").transform);
                newObj.transform.name = newTableItem;
                newObj.transform.localScale = Vector3.one * 0.6f;
                newObj.gameObject.SetActive(true);
                // NetworkServer.Spawn(newObj.gameObject);
            }
        }
    }

    /// <summary>
    /// 目标物品放置到了桌子上 触发逃脱者成功的条件
    /// </summary>
    void HandleTargetTablePlaced()
    {
        SoundController.Instance.PlaySFX(SoundController.Instance.sfxSource_pickup,
            SoundController.Instance.sfxClip_pickup, 0.5f);

        if (tableItem.Equals(desiredItem))
        {
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
                        StartCoroutine(transitionToScene.SendNewPlayerToScene(player.gameObject));
                }
                else if (player.role == PlayerRole.Escaper)
                {
                    // Add Score
                    if (NetworkServer.active)
                        player.CurrentScore++;
                    else
                        player.CmdAddScore();

                    if (player.TryGetComponent<PlayerMovement>(out PlayerMovement pm))
                        pm.enabled = false;

                    if (isServer)
                        StartCoroutine(transitionToScene.SendNewPlayerToScene(player.gameObject));
                }
            }
        }
    }
}