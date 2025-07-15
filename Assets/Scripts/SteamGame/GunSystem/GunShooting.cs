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
        if (_lastShootTime > 0)
            _lastShootTime -= Time.deltaTime;

        if (Input.GetMouseButton(1))
        {
            UpdateRotation();

            currentFOV = Mathf.SmoothDamp(currentFOV, zoomedFOV, ref fovSmoothVelocity, 0.2f);
            CameraController.Instance.freeLookCam.Lens.FieldOfView = currentFOV;

            crosshair.gameObject.SetActive(true);
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
            currentFOV = Mathf.SmoothDamp(currentFOV, normalFOV, ref fovSmoothVelocity, 0.2f);
            CameraController.Instance.freeLookCam.Lens.FieldOfView = currentFOV;
            crosshair.gameObject.SetActive(false);
        }
    }

    private void Shoot()
    {
        Bullet bullet = bulletPrefab.GetComponent<Bullet>()
            .Spawn(null, shootingPoint.position, Quaternion.identity);
        bullet.SetDirection(ray.direction);

        shootingFire.GetComponent<ParticleSystem>()
            .Spawn(shootingFirePoint, shootingFirePoint.localPosition, Quaternion.identity);

        _animator.SetTrigger("Shoot");
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