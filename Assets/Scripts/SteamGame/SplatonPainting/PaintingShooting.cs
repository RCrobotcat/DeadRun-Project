using Mirror;
using UnityEngine;

public class PaintingShooting : MonoBehaviour
{
    [SerializeField] ParticleSystem inkParticle;

    public Transform player;

    Ray ray;
    public Camera playerCamera;
    public RectTransform crosshair;

    private float turnSmoothVelocity;

    // FOV settings
    public float normalFOV = 80f;
    public float zoomedFOV = 50f;
    private float currentFOV;
    private float fovSmoothVelocity;

    void Update()
    {
        if (!playerCamera) playerCamera = Camera.main;

        if (Input.GetMouseButton(1))
        {
            //player.GetComponent<PlayerMovementOffline>().isAiming = true;
            player.GetComponent<PlayerMovement>().isAiming = true;

            UpdateRotation();

            currentFOV = Mathf.SmoothDamp(currentFOV, zoomedFOV, ref fovSmoothVelocity, 0.2f);
            CameraController.Instance.freeLookCam.Lens.FieldOfView = currentFOV;

            SetParticleDirection();
            if (Input.GetMouseButtonDown(0))
            {
                inkParticle.Play();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                inkParticle.Stop();
            }
        }
        else
        {
            inkParticle.Stop();

            //player.GetComponent<PlayerMovementOffline>().isAiming = false;
            player.GetComponent<PlayerMovement>().isAiming = false;

            currentFOV = Mathf.SmoothDamp(currentFOV, normalFOV, ref fovSmoothVelocity, 0.2f);
            CameraController.Instance.freeLookCam.Lens.FieldOfView = currentFOV;
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

    void SetParticleDirection()
    {
        inkParticle.transform.rotation = Quaternion.LookRotation(ray.direction + Vector3.up * 0.05f);
    }
}