using Grass_RC_14;
using Mirror;
using UnityEngine.SceneManagement;

public class GrassController : Singleton<GrassController>
{
    public Grass grass;

    private void Update()
    {
        if (NetworkServer.active)
        {
            if (LobbyController.Instance.localPlayerObject != null)
            {
                if (LobbyController.Instance.localPlayerObject.scene.name == "Scene_3_1v1")
                {
                    grass.gameObject.SetActive(true);
                }
                else
                {
                    grass.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            if (SceneManager.GetSceneByName("Scene_3_1v1").isLoaded)
            {
                grass.gameObject.SetActive(true);
            }
            else
            {
                grass.gameObject.SetActive(false);
            }
        }
    }
}