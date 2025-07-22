using Mirror;
using UnityEngine;

public class GunShooting : MonoBehaviour
{
    public Transform shootingPoint;
    public Transform shootingFirePoint;
    public GameObject bulletPrefab;
    public GameObject shootingFire;

    private Animator _animator;

    public float shootingDelay = 0.3f;
    private float _lastShootTime = 0f;

    Ray ray;
    public Camera playerCamera;
    public RectTransform crosshair;

    public Transform player;
    private float turnSmoothVelocity;

    // FOV settings
    public float normalFOV = 80f;
    public float zoomedFOV = 50f;
    private float currentFOV;
    private float fovSmoothVelocity;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        if (!playerCamera) playerCamera = Camera.main;
    }

    private void Update()
    {
        if (!player.GetComponent<PlayerMovement>().isLocalPlayer)
            return;

        HandleShooting();
    }

    void HandleShooting()
    {
        if (_lastShootTime > 0)
            _lastShootTime -= Time.deltaTime;

        if (Input.GetMouseButton(1))
        {
            //player.GetComponent<PlayerMovementOffline>().isAiming = true;
            player.GetComponent<PlayerMovement>().isAiming = true;

            UpdateRotation();

            currentFOV = Mathf.SmoothDamp(currentFOV, zoomedFOV, ref fovSmoothVelocity, 0.2f);
            CameraController.Instance.freeLookCam.Lens.FieldOfView = currentFOV;

            if (Input.GetMouseButton(0))
            {
                if (_lastShootTime > 0)
                    return;

                _lastShootTime = shootingDelay;
                Shoot();
            }
        }
        else
        {
            //player.GetComponent<PlayerMovementOffline>().isAiming = false;
            player.GetComponent<PlayerMovement>().isAiming = false;

            currentFOV = Mathf.SmoothDamp(currentFOV, normalFOV, ref fovSmoothVelocity, 0.2f);
            CameraController.Instance.freeLookCam.Lens.FieldOfView = currentFOV;
        }
    }

    private void Shoot()
    {
        Bullet bullet = Instantiate(bulletPrefab, shootingPoint.position, Quaternion.identity,
                gameObject.scene.GetRootGameObjects()[0].transform)
            .GetComponent<Bullet>();

        // if (NetworkServer.active) // Host
        // {
        //     NetworkServer.Spawn(bullet.gameObject);
        //     player.GetComponent<PlayerMovement>()
        //         .CmdSpawnFireEffect(shootingFirePoint, shootingFirePoint.position);
        // }

        bullet.SetDirection(ray.direction + Vector3.up * 0.05f);

        // Server
        if (NetworkServer.active)
            player.GetComponent<PlayerMovement>()
                .RPCSpawnBulletFromHost(shootingPoint.position, ray.direction + Vector3.up * 0.05f);
        else // Client
            player.GetComponent<PlayerMovement>()
                .CmdSpawnBullet(shootingPoint.position, ray.direction + Vector3.up * 0.05f);

        shootingFire.GetComponent<ParticleSystem>()
            .Spawn(shootingFirePoint, shootingFirePoint.localPosition, Quaternion.identity);

        _animator.SetTrigger("Shoot");
        if (SoundController.Instance != null)
        {
            SoundController.Instance.PlayShooting(0.3f, 2f);
        }
    }

    void UpdateRotation()
    {
        ray = Camera.main.ScreenPointToRay(crosshair.position);
        //Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 1f);
        float targetAngle = Mathf.Atan2(ray.direction.x, ray.direction.z) * Mathf.Rad2Deg;
        float smoothAngle = Mathf.SmoothDampAngle(
            player.transform.eulerAngles.y,
            targetAngle,
            ref turnSmoothVelocity,
            0.1f
        );
        player.transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
    }
}