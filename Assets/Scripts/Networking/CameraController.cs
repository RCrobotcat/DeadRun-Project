using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    private bool isThirdPerson = false;
    private CinemachineCamera freeLookCam;

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        if (GetComponent<CinemachineCamera>() != null)
        {
            isThirdPerson = true;
            freeLookCam = GetComponent<CinemachineCamera>();
        }
        else isThirdPerson = false;
    }

    private void Update()
    {
        if (isThirdPerson)
        {
            if (SceneManager.GetSceneByName("Scene_1").isLoaded
                || SceneManager.GetSceneByName("Scene_2").isLoaded)
            {
                if (freeLookCam.Target.TrackingTarget == null)
                    freeLookCam.Target.TrackingTarget = FindObjectOfType<PlayerMovement>().transform;
            }
        }
    }
}