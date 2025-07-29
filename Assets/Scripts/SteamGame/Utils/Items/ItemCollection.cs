using Mirror;
using UnityEngine;

public class ItemCollection : NetworkBehaviour
{
    public float dropForce = 3f;

    private void Update()
    {
        PlayerMovement[] allPlayers = FindObjectsOfType<PlayerMovement>();
        foreach (var player in allPlayers)
        {
            GameObject playerObj = player.gameObject;

            if (playerObj && player.isLocalPlayer && player.ObjPlayerIsNear == gameObject)
            {
                //Debug.Log("Player is near the item: " + gameObject.name);
                if (Input.GetKeyDown(KeyCode.E))
                {
                    CmdPickupItem();
                }
            }
        }
    }

    [Command(requiresAuthority = false)]
    void CmdPickupItem()
    {
        PlayerMovement player = LobbyController.Instance.localPlayerObject.GetComponent<PlayerMovement>();
        if (player.currentEquippedItem == "")
        {
            player.currentEquippedItem = gameObject.name.Replace("(Clone)", "");
            NetworkServer.Destroy(gameObject);
        }
    }
}