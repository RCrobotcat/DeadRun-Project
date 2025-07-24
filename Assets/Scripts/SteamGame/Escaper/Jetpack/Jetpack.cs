using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Jetpack : MonoBehaviour
{
    public float thrust = 10f;
    public float fuel = 100f;
    public float fuelConsumptionRate = 1f;
    public float maxHeight = 100f;
    public float cooldownTime = 5f;
    public float cooldownRecoveryRate = 10f;

    private Rigidbody rb;
    public float currentFuel;
    private bool isCoolingDown = false;
    private float cooldownTimer = 0f;

    public GameObject jetpackSmokeEffect;
    public Transform jetpackSmokeParent;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentFuel = fuel;
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
            }
        }

        // 燃料恢复
        if (isCoolingDown && currentFuel < fuel)
        {
            currentFuel += cooldownRecoveryRate * Time.deltaTime;
            currentFuel = Mathf.Min(currentFuel, fuel);
        }
    }

    void ApplyThrust()
    {
        rb.useGravity = false;
        transform.Translate(Vector3.up * thrust * Time.deltaTime, Space.World);
        if (transform.position.y >= maxHeight)
            transform.position = new Vector3(transform.position.x, maxHeight, transform.position.z);

        currentFuel -= fuelConsumptionRate * Time.deltaTime;
        jetpackSmokeEffect.GetComponent<ParticleSystem>()
            .Spawn(jetpackSmokeParent, jetpackSmokeParent.localPosition, Quaternion.identity);

        if (currentFuel <= 0)
        {
            currentFuel = 0;
            StartCooldown();
        }
    }

    void StopThrust()
    {
        rb.useGravity = true;
    }

    void StartCooldown()
    {
        if (!isCoolingDown)
        {
            isCoolingDown = true;
            cooldownTimer = 0f;
        }
    }
}