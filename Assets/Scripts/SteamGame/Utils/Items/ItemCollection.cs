using Mirror;
using UnityEngine;

public class ItemCollection : NetworkBehaviour
{
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
                    CmdPickupItem(playerObj);
                }
            }
        }
    }

    [Command(requiresAuthority = false)]
    void CmdPickupItem(GameObject player)
    {
        PlayerMovement playerControl = player.GetComponent<PlayerMovement>();
        if (playerControl.currentEquippedItem == "")
        {
            playerControl.currentEquippedItem = gameObject.name.Replace("(Clone)", "");
            NetworkServer.Destroy(gameObject);
        }
    }
}