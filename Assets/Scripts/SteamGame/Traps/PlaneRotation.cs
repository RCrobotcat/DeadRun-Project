using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PlaneRotation : Singleton<PlaneRotation>
{
    public float rotationSpeed = 100f;

    public GameObject planeRotationCanvas;
    public Button leftRotationBtn;
    public Button rightRotationBtn;

    public GameObject trapperCamera;

    public GameObject planeToRotate;

    private bool isRotatingLeft = false;
    private bool isRotatingRight = false;

    private void Start()
    {
        var leftTrigger = leftRotationBtn.gameObject.AddComponent<EventTrigger>();
        var rightTrigger = rightRotationBtn.gameObject.AddComponent<EventTrigger>();

        var leftDown = new EventTrigger.Entry
            { eventID = EventTriggerType.PointerDown };
        leftDown.callback.AddListener((_) => isRotatingLeft = true);
        leftTrigger.triggers.Add(leftDown);

        var leftUp = new EventTrigger.Entry
            { eventID = EventTriggerType.PointerUp };
        leftUp.callback.AddListener((_) => isRotatingLeft = false);
        leftTrigger.triggers.Add(leftUp);

        var rightDown = new EventTrigger.Entry
            { eventID = EventTriggerType.PointerDown };
        rightDown.callback.AddListener((_) => isRotatingRight = true);
        rightTrigger.triggers.Add(rightDown);

        var rightUp = new EventTrigger.Entry
            { eventID = EventTriggerType.PointerUp };
        rightUp.callback.AddListener((_) => isRotatingRight = false);
        rightTrigger.triggers.Add(rightUp);
    }

    private void Update()
    {
        if (LobbyController.Instance != null)
        {
            if (LobbyController.Instance.LocalPlayerObjectController != null)
            {
                if (LobbyController.Instance.LocalPlayerObjectController.role == PlayerRole.Trapper)
                {
                    if (!planeRotationCanvas.activeSelf)
                        planeRotationCanvas.SetActive(true);
                    if (!trapperCamera.activeSelf)
                        trapperCamera.SetActive(true);

                    CameraController.Instance.gameObject.SetActive(false);
                    LobbyController.Instance.LocalPlayerObjectController.transform.position =
                        new Vector3(1000, 1000, 1000); // offscreen
                }
                else if (LobbyController.Instance.LocalPlayerObjectController.role == PlayerRole.Escaper)
                {
                    if (planeRotationCanvas.activeSelf)
                        planeRotationCanvas.SetActive(false);
                    if (trapperCamera.activeSelf)
                        trapperCamera.SetActive(false);

                    CameraController.Instance.gameObject.SetActive(true);
                }
            }
        }

        if (planeToRotate != null)
        {
            if (isRotatingLeft)
            {
                planeToRotate.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
            }

            if (isRotatingRight)
            {
                planeToRotate.transform.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);
            }
        }
    }
}