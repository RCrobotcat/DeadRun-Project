using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    private bool isThirdPerson = false;
    [HideInInspector] public CinemachineCamera freeLookCam;

    public float shakeDuration = 1;
    public float shakeAmplitude = 1.2f;
    public float shakeFrequency = 2.0f;

    private float shakeTimer;
    private CinemachineBasicMultiChannelPerlin noise;

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
            noise =
                freeLookCam.GetCinemachineComponent(CinemachineCore.Stage.Noise) as CinemachineBasicMultiChannelPerlin;
        }
        else isThirdPerson = false;
    }

    void Update()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0f)
            {
                StopShake();
            }
        }
    }

    public void StartShake()
    {
        if (noise != null)
        {
            noise.AmplitudeGain = shakeAmplitude;
            noise.FrequencyGain = shakeFrequency;
            shakeTimer = shakeDuration;
        }
    }

    public void StopShake()
    {
        if (noise != null)
        {
            noise.AmplitudeGain = 0f;
        }
    }
}