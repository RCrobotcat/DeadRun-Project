using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Jetpack : NetworkBehaviour
{
    public float thrust = 10f;
    public float fuel = 100f;
    public float fuelConsumptionRate = 1f;
    public float maxHeight = 100f;
    public float cooldownTime = 5f;
    public float cooldownRecoveryRate = 10f;
    public float restoreTime = 1.2f;

    private Rigidbody rb;
    public float currentFuel;
    private bool isCoolingDown = false;
    private float cooldownTimer = 0f;
    private float restoreTimer = 0f;

    public GameObject jetpackSmokeEffect;
    public Transform jetpackSmokeParent;

    public GameObject jetpackUIPanel;
    public Image fuelBarImage;

    private MyNetworkManager _myNetworkManager;

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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentFuel = fuel;
        restoreTimer = restoreTime;
    }

    void Update()
    {
        if (NetworkServer.active)
        {
            if (gameObject.scene.name != "Scene_4")
                return;
        }
        else
        {
            if (!SceneManager.GetSceneByName("Scene_4").isLoaded)
                return;
        }

        if (!GetComponent<PlayerMovement>().isLocalPlayer)
            return;

        if (isCoolingDown)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= cooldownTime)
            {
                isCoolingDown = false;
                cooldownTimer = 0f;
            }
        }

        if (!isCoolingDown)
        {
            if (Input.GetKey(KeyCode.F) && currentFuel > 0)
            {
                ApplyThrust();
                if (currentFuel <= 0)
                {
                    StopThrust();
                    StartCooldown();
                }
            }
            else
            {
                StopThrust();

                if (restoreTimer > 0)
                    restoreTimer -= Time.deltaTime;

                if (restoreTimer <= 0)
                {
                    RestoreFuel();
                    if (Mathf.Approximately(currentFuel, fuel))
                        restoreTimer = restoreTime;
                }
            }
        }

        if (isCoolingDown && currentFuel < fuel)
        {
            RestoreFuel();
        }
    }

    void ApplyThrust()
    {
        rb.useGravity = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        jetpackUIPanel.gameObject.SetActive(true);
        if (SoundController.Instance != null)
        {
            if (!SoundController.Instance.sfxSource_jetpack.isPlaying)
            {
                SoundController.Instance.PlayJetpackThrust(3f, true);
            }
        }

        transform.Translate(Vector3.up * thrust * Time.deltaTime, Space.World);
        if (transform.position.y >= maxHeight)
            transform.position = new Vector3(transform.position.x, maxHeight, transform.position.z);

        currentFuel -= fuelConsumptionRate * Time.deltaTime;
        fuelBarImage.fillAmount = currentFuel / fuel;
        jetpackSmokeEffect.GetComponent<ParticleSystem>()
            .Spawn(jetpackSmokeParent, jetpackSmokeParent.localPosition, Quaternion.identity);

        if (NetworkServer.active)
            RpcJetpackSmoke(GetComponent<PlayerObjectController>().playerID);
        else
            CmdJetpackSmoke(GetComponent<PlayerObjectController>().playerID);

        if (currentFuel <= 0)
        {
            currentFuel = 0;
            StartCooldown();
        }
    }

    void StopThrust()
    {
        rb.useGravity = true;
        if (SoundController.Instance != null)
        {
            if (SoundController.Instance.sfxSource_jetpack.isPlaying)
            {
                SoundController.Instance.sfxSource_jetpack.Stop();
            }
        }
    }

    void StartCooldown()
    {
        if (!isCoolingDown)
        {
            isCoolingDown = true;
            cooldownTimer = 0f;
        }
    }

    void RestoreFuel()
    {
        currentFuel += cooldownRecoveryRate * Time.deltaTime;
        currentFuel = Mathf.Min(currentFuel, fuel);
        fuelBarImage.fillAmount = currentFuel / fuel;
        if (currentFuel >= fuel)
            jetpackUIPanel.gameObject.SetActive(false);
    }

    [ClientRpc]
    void RpcJetpackSmoke(int playerId)
    {
        if (!isClientOnly)
            return;

        var player = MyNetworkManager.GamePlayers.FirstOrDefault(p => p.playerID == playerId);
        if (player != null)
        {
            Jetpack jetpack = player.GetComponent<Jetpack>();
            jetpack.jetpackSmokeEffect.GetComponent<ParticleSystem>()
                .Spawn(jetpack.jetpackSmokeParent, jetpack.jetpackSmokeParent.localPosition, Quaternion.identity);
        }
    }

    [Command(requiresAuthority = false)]
    void CmdJetpackSmoke(int playerId)
    {
        var player = MyNetworkManager.GamePlayers.FirstOrDefault(p => p.playerID == playerId);
        if (player != null)
        {
            Jetpack jetpack = player.GetComponent<Jetpack>();
            jetpack.jetpackSmokeEffect.GetComponent<ParticleSystem>()
                .Spawn(jetpack.jetpackSmokeParent, jetpack.jetpackSmokeParent.localPosition, Quaternion.identity);
        }
    }
}