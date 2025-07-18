using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Mirror;
using UnityEngine.SceneManagement;

public partial class PlayerObjectController
{
    [Header("Health")] public GameObject healthBarBase;
    public Image healthBarFillImage;

    public float maxHealth = 100f;
    private float currentHealth;

    public float CurrentHealth
    {
        get => currentHealth;
        set
        {
            currentHealth = Mathf.Clamp(value, 0, maxHealth);
            healthBarFillImage.DOFillAmount(currentHealth / maxHealth, 0.2f);
            if (currentHealth <= 0)
            {
                DieIn1V1();
            }
        }
    }


    int currentScore = 0;

    public int CurrentScore
    {
        get => currentScore;
        set { currentScore = value; }
    }

    [Header("Fell Count")] int fellCount = 0;
    public Text fellCountText;

    public int FellCount
    {
        get => fellCount;
        set
        {
            fellCount = value;
            fellCountText.text = fellCount.ToString() + "/5";
            if (fellCount > 5)
            {
                MissionFailed();
            }
        }
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        healthBarFillImage.fillAmount = currentHealth / maxHealth;
        fellCountText.text = fellCount.ToString() + "/5";
    }

    private void MissionFailed()
    {
        Debug.Log("Mission Failed: Fell too many times.");
        LobbyController.Instance.ShowMissionFailedText("Mission Failed:" + "\n" + " Fell too many times!");
        fellCount = 0;
        fellCountText.text = "Try not to die!";

        if (!NetworkServer.active)
            CmdSetDeadEscaperCount(SceneManager.GetSceneByName("Scene_1").path);
        else
        {
            LobbyController.Instance.previousScenePath = SceneManager.GetSceneByName("Scene_1").path;
            LobbyController.Instance.DeadEscaperCount++;
        }
    }

    void DieIn1V1()
    {
        Debug.Log($"Player {playerID} died in 1v1.");
        if (NetworkServer.active)
            RpcTransitionToNextLevelScene();
        else
        {
            LobbyController.Instance.ShowMissionFailedText("Mission Failed: " + "\n" + "You died in 1v1!");
            fellCountText.gameObject.SetActive(false);
            LobbyController.Instance.TransitionAllPlayersToScene(SceneManager.GetSceneByName("Scene_4_Terrain").path,
                "SpawnPos", SceneManager.GetSceneByName("Scene_3_1v1").path);
        }
    }

    public void SetPlayerUIState(bool state)
    {
        healthBarBase.SetActive(state);
        fellCountText.gameObject.SetActive(state);
    }

    [Command(requiresAuthority = false)]
    void CmdSetDeadEscaperCount(string previousScenePath)
    {
        LobbyController.Instance.previousScenePath = previousScenePath;
        LobbyController.Instance.DeadEscaperCount++;
    }

    [ClientRpc]
    private void RpcTransitionToNextLevelScene()
    {
        if (!isClientOnly)
            return;

        LobbyController.Instance.ShowMissionFailedText("Mission Failed: " + "\n" + "You died in 1v1!");
        fellCountText.gameObject.SetActive(false);
    }
}