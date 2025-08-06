using System.Collections;
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

    [Header("Score")] public GameObject scoreUIBase;
    public Text scoreText;
    public Text plusOneScoreText;

    [Header("Collecions")] public GameObject collectionUIBase;
    public Text collectionText;
    [HideInInspector] public int currentCollectionCount = 0;

    public bool isEnd = false;

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

            float formalValue = currentHealth;
            currentHealth = Mathf.Clamp(value, 0, maxHealth);
            healthBarFillImage.DOFillAmount(currentHealth / maxHealth, 0.2f);

            if (formalValue > value)
            {
                if (playerID == LobbyController.Instance.LocalPlayerObjectController.playerID)
                {
                    StartCoroutine(DamageImageFadeInOut());
                    if (!SoundController.Instance.sfxSource_hit_2.isPlaying)
                        SoundController.Instance.PlaySFX(SoundController.Instance.sfxSource_hit_1,
                            SoundController.Instance.sfxClip_hit, 0.7f);
                }
                else
                {
                    if (!SoundController.Instance.sfxSource_hit_2.isPlaying)
                        SoundController.Instance.PlaySFX(SoundController.Instance.sfxSource_hit_2,
                            SoundController.Instance.sfxClip_hit, 0.7f);
                }
            }

            if (NetworkServer.active)
                RpcSyncPlayersHealthToClient(currentHealth);

            if (currentHealth <= 0)
            {
                if (NetworkServer.active) // Server
                {
                    if (gameObject.scene.name == "Scene_3_1v1")
                    {
                        damageImage.gameObject.SetActive(false);
                        DieIn1V1();
                    }
                    else
                    {
                        animator.SetBool("Die", true);

                        if (TryGetComponent<PlayerMovement>(out PlayerMovement pm))
                            pm.isDead = true;
                        if (transform.GetChild(2).TryGetComponent<GunShooting>(out GunShooting gunShooting))
                            gunShooting.enabled = false;
                        if (transform.GetChild(2)
                            .TryGetComponent<PaintingShooting>(out PaintingShooting paintingShooting))
                        {
                            paintingShooting.inkParticle.Stop();
                            paintingShooting.enabled = false;
                            if (SoundController.Instance.sfxSource_splash.isPlaying)
                                SoundController.Instance.sfxSource_splash.Stop();
                        }

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
                        damageImage.gameObject.SetActive(false);
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
                    if (transform.GetChild(2)
                        .TryGetComponent<PaintingShooting>(out PaintingShooting paintingShooting))
                    {
                        paintingShooting.inkParticle.Stop();
                        paintingShooting.enabled = false;
                        if (SoundController.Instance.sfxSource_splash.isPlaying)
                            SoundController.Instance.sfxSource_splash.Stop();
                    }

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

    [SerializeField] int currentScore = 0;

    public int CurrentScore
    {
        get => currentScore;
        set
        {
            int formalValue = currentScore;
            currentScore = value;

            if (currentScore > formalValue && playerID == LobbyController.Instance.LocalPlayerObjectController.playerID)
            {
                plusOneScoreText.transform.localScale.To(Vector3.one, 0.2f,
                    (scale) => { plusOneScoreText.transform.localScale = scale; },
                    () => { plusOneScoreText.transform.localScale = Vector3.zero; });
            }

            scoreText.text = currentScore.ToString();

            if (NetworkServer.active)
                RpcSyncPlayerScoreToClient(currentScore);
        }
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

        // Add Score to Trapper
        foreach (var player in MyNetworkManager.GamePlayers)
        {
            if (player.role == PlayerRole.Trapper)
            {
                if (NetworkServer.active)
                    player.CurrentScore++;
                else
                    player.CmdAddScore();
                break;
            }
        }

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

        // On Server
        if (playerID == LobbyController.Instance.LocalPlayerObjectController.playerID)
            LobbyController.Instance.ShowMissionFailedText("Mission Failed: " + "\n" + "You died in 1v1!");

        // Add Score to the other player
        foreach (var player in MyNetworkManager.GamePlayers)
        {
            if (player.playerID != this.playerID)
            {
                if (NetworkServer.active)
                    player.CurrentScore++;
                else
                    player.CmdAddScore();
            }
        }

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
        scoreUIBase.SetActive(state);
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

    [ClientRpc]
    public void RpcSyncPlayerScoreToClient(int score)
    {
        if (!isClientOnly)
            return;

        CurrentScore = score;
    }

    [Command(requiresAuthority = false)]
    public void CmdAddScore()
    {
        CurrentScore++;
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

    [ClientRpc]
    public void RpcUpdateCountdown(float currentTime)
    {
        if (!isClientOnly)
            return;

        LobbyController.Instance.countDownPanel.SetActive(true);
        LobbyController.Instance.countDownTimer = currentTime;

        if (currentTime > 0f)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            LobbyController.Instance.countDownText.text = $"{minutes:D2}:{seconds:D2}";
        }
        else if (currentTime == 0)
            LobbyController.Instance.countDownText.text = "00:00";
    }

    [ClientRpc]
    public void RpcSetCollectionUIState(bool state)
    {
        if (!isClientOnly)
            return;

        collectionUIBase.SetActive(state);
        if (state)
            GetComponent<PlayerMovement>().moveSpeed = 10f;
        else
            GetComponent<PlayerMovement>().moveSpeed = 5f;
    }

    [ClientRpc]
    public void RpcUpdateCollectionsText(int current, int required)
    {
        if (!isClientOnly)
            return;

        currentCollectionCount = current;
        collectionText.text = current + "/" + required;
    }

    [ClientRpc]
    public void RpcHideOtherPlayersResultsPanel()
    {
        if (!isClientOnly)
            return;

        FindObjectOfType<MatchResultsList>().HideMatchResults();
    }

    public Image damageImage;

    private IEnumerator DamageImageFadeInOut()
    {
        float fadeInDuration = 0.2f;
        float fadeOutDuration = 0.3f;
        float maxAlpha = 0.35f;

        if (damageImage == null)
            yield break;

        if (!damageImage.gameObject.activeSelf)
        {
            damageImage.gameObject.SetActive(true);
        }

        Color color = damageImage.color;

        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(0, maxAlpha, timer / fadeInDuration);
            damageImage.color = color;
            yield return null;
        }

        color.a = maxAlpha;
        damageImage.color = color;

        yield return new WaitForSeconds(0.1f);

        timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(maxAlpha, 0, timer / fadeOutDuration);
            damageImage.color = color;
            yield return null;
        }

        color.a = 0;
        damageImage.color = color;

        damageImage.gameObject.SetActive(false);
    }

    [ClientRpc]
    public void RpcShowResultCanvas(string winnerName, int winnerScore, int yourScore)
    {
        if (!isClientOnly)
            return;

        CameraController.Instance.gameObject.SetActive(false);
        LobbyController.Instance.countDownPanel.gameObject.SetActive(false);

        foreach (var p in MyNetworkManager.GamePlayers)
        {
            p.isEnd = true;
            p.GetComponent<PlayerMovement>().isEnd = true;
        }

        ResultCanvas resultCanvas = FindObjectOfType<ResultCanvas>();
        if (resultCanvas != null)
        {
            resultCanvas.ShowResult(winnerName, winnerScore, yourScore);
        }

        gameObject.SetActive(false);
    }
}