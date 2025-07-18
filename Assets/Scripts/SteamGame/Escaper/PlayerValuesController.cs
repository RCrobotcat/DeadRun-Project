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
        LobbyController.Instance.ShowMissionFailedText();
        fellCount = 0;
        fellCountText.text = "Try not to die!";

        if (!NetworkServer.active)
            CmdSetDeadEscaperCount();
        else
            LobbyController.Instance.DeadEscaperCount++;
    }

    void DieIn1V1()
    {
        Debug.Log($"Player {playerID} died in 1v1.");
        LobbyController.Instance.ShowMissionFailedText();
    }

    public void SetPlayerUIState(bool state)
    {
        healthBarBase.SetActive(state);
        fellCountText.gameObject.SetActive(state);
    }

    [Command(requiresAuthority = false)]
    void CmdSetDeadEscaperCount()
    {
        LobbyController.Instance.DeadEscaperCount++;
    }
}