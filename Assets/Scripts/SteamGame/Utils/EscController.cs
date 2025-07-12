using UnityEngine;
using UnityEngine.UI;

public class EscController : MonoBehaviour
{
    public GameObject escPanel;

    public Text roleTxt;
    public Button mainMenuBtn;
    public Button quitBtn;

    private void Start()
    {
        mainMenuBtn.onClick.AddListener(OnMainMenuButtonClicked);
        quitBtn.onClick.AddListener(() => Application.Quit());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            roleTxt.text = LobbyController.Instance.LocalPlayerObjectController.role.ToString();
            escPanel.SetActive(!escPanel.activeSelf);
        }
    }

    private void OnMainMenuButtonClicked()
    {
        LobbyController.Instance.ExitGameLobby();
    }
}