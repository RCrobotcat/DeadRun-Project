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

            // Reset
            if (!isRotatingRight && !isRotatingLeft)
            {
                Quaternion currentRotation = planeToRotate.transform.rotation;
                Quaternion targetRotation = Quaternion.identity;

                planeToRotate.transform.rotation =
                    Quaternion.Lerp(currentRotation, targetRotation, 0.1f);
            }
        }
    }
}