using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
            fellCountText.text = fellCount.ToString() + "/3";
            if (fellCount > 3)
            {
                MissionFailed();
            }
        }
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        healthBarFillImage.fillAmount = 1f;
        fellCountText.text = "0/3";
    }

    private void MissionFailed()
    {
        Debug.Log("Mission Failed: Fell too many times.");
        fellCount = 0;
        fellCountText.text = "0/3";
        // TODO: Go to 1v1
    }

    void DieIn1V1()
    {
        Debug.Log($"Player {playerID} died in 1v1.");
        // TODO
    }

    public void SetPlayerUIState(bool state)
    {
        healthBarBase.SetActive(state);
        fellCountText.gameObject.SetActive(state);
    }
}