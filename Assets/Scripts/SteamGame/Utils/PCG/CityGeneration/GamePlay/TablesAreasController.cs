using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TablesAreasController : MonoBehaviour
{
    public GameObject tables;
    public GameObject areas;
    public GameObject monsters;

    private void Update()
    {
        if (NetworkServer.active)
        {
            if (LobbyController.Instance.localPlayerObject.scene.name == "Scene_4")
            {
                if (tables.activeSelf && areas.activeSelf && monsters.activeSelf)
                    return;

                tables.SetActive(true);
                areas.SetActive(true);
                monsters.SetActive(true);
            }
            else
            {
                if (!tables.activeSelf && !areas.activeSelf && !monsters.activeSelf)
                    return;

                tables.SetActive(false);
                areas.SetActive(false);
                monsters.SetActive(false);
            }
        }
        else
        {
            if (SceneManager.GetSceneByName("Scene_4").isLoaded)
            {
                if (tables.activeSelf && areas.activeSelf && monsters.activeSelf)
                    return;

                tables.SetActive(true);
                areas.SetActive(true);
                monsters.SetActive(true);
            }
            else
            {
                if (!tables.activeSelf && !areas.activeSelf && !monsters.activeSelf)
                    return;

                tables.SetActive(false);
                areas.SetActive(false);
                monsters.SetActive(false);
            }
        }
    }
}