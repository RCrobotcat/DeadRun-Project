using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EscController : MonoBehaviour
{
    public GameObject escPanel;

    public Text roleTxt;
    public Button mainMenuBtn;
    public Button quitBtn;

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
        mainMenuBtn.onClick.AddListener(OnMainMenuButtonClicked);
        quitBtn.onClick.AddListener(() => Application.Quit());
    }

    private void Update()
    {
        bool inRoomScene = MyNetworkManager.allPlayersInGameScene_server;
        bool escActive = escPanel.activeSelf;

        Cursor.visible = !inRoomScene || escActive || LobbyController.Instance.LocalPlayerObjectController.isEnd;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            escPanel.SetActive(!escActive);
            Cursor.visible = !inRoomScene || !escActive;
            roleTxt.text = LobbyController.Instance.LocalPlayerObjectController.role.ToString();
        }
    }

    private void OnMainMenuButtonClicked()
    {
        LobbyController.Instance.ExitGameLobby();
    }
}