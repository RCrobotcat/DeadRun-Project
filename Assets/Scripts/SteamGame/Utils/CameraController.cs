using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    private bool isThirdPerson = false;
    [HideInInspector] public CinemachineCamera freeLookCam;

    private void Start()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        if (GetComponent<CinemachineCamera>() != null)
        {
            isThirdPerson = true;
            freeLookCam = GetComponent<CinemachineCamera>();
        }
        else isThirdPerson = false;
    }
}