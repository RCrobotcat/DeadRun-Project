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

    public float respawnTime = 2.5f;
    float respawnTimer = -1f;

    public GameObject counterUIBase;
    public Image counterUIFillImage;

    public float CurrentHealth
    {
        get => currentHealth;
        set
        {
            // Prevention
            if (currentHealth <= 0)
            {
                currentHealth = Mathf.Clamp(value, 0, maxHealth);
                healthBarFillImage.DOFillAmount(currentHealth / maxHealth, 0.2f);

                if (NetworkServer.active)
                    RpcSyncPlayersHealthToClient(currentHealth);

                return;
            }

            currentHealth = Mathf.Clamp(value, 0, maxHealth);
            healthBarFillImage.DOFillAmount(currentHealth / maxHealth, 0.2f);

            if (NetworkServer.active)
                RpcSyncPlayersHealthToClient(currentHealth);

            if (currentHealth <= 0)
            {
                if (NetworkServer.active) // Server
                {
                    if (gameObject.scene.name == "Scene_3_1v1")
                    {
                        DieIn1V1();
                    }
                    else
                    {
                        animator.SetBool("Die", true);

                        if (TryGetComponent<PlayerMovement>(out PlayerMovement pm))
                            pm.isDead = true;
                        if (transform.GetChild(2).TryGetComponent<GunShooting>(out GunShooting gunShooting))
                            gunShooting.enabled = false;

                        DropCurrentItem();

                        if (respawnTimer <= -1)
                        {
                            if (playerID == LobbyController.Instance.LocalPlayerObjectController.playerID)
                            {
                                respawnTimer = respawnTime;
                                counterUIBase.SetActive(true);
                            }
                        }
                    }
                }
                else // Clients
                {
                    if (SceneManager.GetSceneByName("Scene_3_1v1").isLoaded)
                    {
                        if (playerID == LobbyController.Instance.LocalPlayerObjectController.playerID)
                            LobbyController.Instance.ShowMissionFailedText("Mission Failed: " + "\n" +
                                                                           "You died in 1v1!");
                        return;
                    }

                    animator.SetBool("Die", true);

                    if (TryGetComponent<PlayerMovement>(out PlayerMovement pm))
                        pm.isDead = true;
                    if (transform.GetChild(2).TryGetComponent<GunShooting>(out GunShooting gunShooting))
                        gunShooting.enabled = false;

                    CmdDropCurrentItem();

                    if (respawnTimer <= -1)
                    {
                        if (playerID == LobbyController.Instance.LocalPlayerObjectController.playerID)
                        {
                            respawnTimer = respawnTime;
                            counterUIBase.SetActive(true);
                        }
                    }
                }
            }
        }
    }

    [ClientRpc]
    void RpcSyncPlayersHealthToClient(float currentHealth)
    {
        if (!isClientOnly)
            return;

        if (!Mathf.Approximately(currentHealth, CurrentHealth))
        {
            CurrentHealth = currentHealth;
            healthBarFillImage.DOFillAmount(CurrentHealth / maxHealth, 0.2f);
        }
    }

    static int currentScore = 0;

    public int CurrentScore
    {
        get => currentScore;
        set { currentScore = value; }
    }

    [Header("Fell Count")] static int fellCount = 0;
    public Text fellCountText;

    public int FellCount
    {
        get => fellCount;
        set
        {
            fellCount = value;
            fellCountText.text = fellCount.ToString() + "/5";
            if (fellCount >= 5)
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
        if (NetworkServer.active)
        {
            if (gameObject.scene.name != "Scene_1")
                return;
        }
        else
        {
            if (!SceneManager.GetSceneByName("Scene_1").isLoaded)
                return;
        }

        Debug.Log("Mission Failed: Fell too many times.");
        LobbyController.Instance.ShowMissionFailedText("Mission Failed:" + "\n" + " Fell too many times!");
        fellCount = 0;
        fellCountText.gameObject.SetActive(false);
        CurrentHealth = maxHealth;

        LobbyController.Instance.ClearBullets();

        // TODO: Add Score to Trapper

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

        LobbyController.Instance.ClearBullets();

        if (playerID == LobbyController.Instance.LocalPlayerObjectController.playerID)
            LobbyController.Instance.ShowMissionFailedText("Mission Failed: " + "\n" + "You died in 1v1!");

        if (!NetworkServer.active)
        {
            CmdSet1v1DeadPlayerCount();
        }
        else
        {
            LobbyController.Instance.DeadEscaperCountIn1V1++;
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

    [Command(requiresAuthority = false)]
    void CmdSet1v1DeadPlayerCount()
    {
        LobbyController.Instance.DeadEscaperCountIn1V1++;
    }

    [Command(requiresAuthority = false)]
    void CmdResetHealth()
    {
        CurrentHealth = maxHealth;
    }

    [Command(requiresAuthority = false)]
    void CmdDropCurrentItem()
    {
        DropCurrentItem();
    }

    void DropCurrentItem()
    {
        PlayerMovement player = GetComponent<PlayerMovement>();
        if (!string.IsNullOrEmpty(player.currentEquippedItem))
        {
            string temp = player.currentEquippedItem;
            player.currentEquippedItem = "";
            DropItem(temp, transform.position, Quaternion.identity);
        }
    }

    public void DropItem(string itemName, Vector3 dropPosition, Quaternion dropRotation)
    {
        ItemsManager itemsManager = FindObjectOfType<ItemsManager>();
        GameObject itemPrefab = itemsManager.FindDropItemByTableItemName(itemName);

        GameObject go = Instantiate(itemPrefab, dropPosition, dropRotation);
        go.transform.localScale = Vector3.one;
        go.transform.position += Vector3.up;
        SceneManager.MoveGameObjectToScene(go, SceneManager.GetSceneByName(gameObject.scene.name));
        NetworkServer.Spawn(go);
    }
}