using UnityEngine;

public class UIFaceCamera : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (LobbyController.Instance.LocalPlayerObjectController.role == PlayerRole.Trapper)
        {
            if (mainCamera != null && mainCamera != FindObjectOfType<TrapperCameraMovement>().GetComponent<Camera>())
                mainCamera = FindObjectOfType<TrapperCameraMovement>().GetComponent<Camera>();
        }

        if (LobbyController.Instance.LocalPlayerObjectController.role == PlayerRole.Escaper)
        {
            if (mainCamera != null && mainCamera != Camera.main)
                mainCamera = Camera.main;
        }

        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
            mainCamera.transform.rotation * Vector3.up);
    }
}