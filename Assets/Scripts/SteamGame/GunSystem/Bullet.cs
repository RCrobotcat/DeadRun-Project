//using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.VFX;

public enum TowardType
{
    Forward,
    Right
}

public class Bullet : MonoBehaviour
{
    public float rotationSpeed = 100f;

    public float movementSpeed = 10f;

    //public float delayTime = 0f;
    private bool isPlay = false;

    //public float time = 1f;
    private float lastTime = 0f;

    //private Vector3 startPos;
    public TowardType towardType = TowardType.Forward;
    private Vector3 directionToCenter;
    private Vector3 scale;
    private VisualEffect[] vfxs;
    private TrailRenderer[] trails;
    public float maxDistance = 100f;
    float curDistance = 0f;

    private void Start()
    {
        vfxs = GetComponentsInChildren<VisualEffect>(false);
        trails = GetComponentsInChildren<TrailRenderer>(false);
        //startPos = transform.position;
        SetPlay(true);

        Physics.IgnoreCollision(GetComponent<Collider>(), GetComponent<Collider>());
    }

    public async void SetPlay(bool play)
    {
        //await Task.Delay((int)(delayTime * 1000));
        isPlay = play;
    }

    private void Update()
    {
        if (!isPlay)
            return;

        if (curDistance > maxDistance)
        {
            curDistance = 0;
            gameObject.Recycle();
            // scale = transform.localScale;
            // for (int i = 0; i < vfxs.Length; i++)
            // {
            //     vfxs[i].enabled = false;
            // }
            //
            // for (int i = 0; i < trails.Length; i++)
            // {
            //     trails[i].enabled = false;
            // }
            //
            // transform.localScale = Vector3.zero;
            // transform.position = startPos;
            // curDistance = 0f;
            // transform.localScale = scale;
            // isPlay = false;
            // DelayEnable();
        }

        //directionToCenter = transform.forward;
        Quaternion targetRotation = Quaternion.LookRotation(directionToCenter);
        transform.rotation = targetRotation;
        //Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        if (towardType == TowardType.Forward)
        {
            transform.Translate(Vector3.forward * movementSpeed * Time.deltaTime);
        }
        else if (towardType == TowardType.Right)
        {
            transform.Translate(Vector3.right * movementSpeed * Time.deltaTime);
        }

        curDistance += movementSpeed * Time.deltaTime;
    }

    // public async void DelayEnable()
    // {
    //     await Task.Delay(100);
    //     for (int i = 0; i < vfxs.Length; i++)
    //     {
    //         vfxs[i].enabled = true;
    //     }
    //
    //     for (int i = 0; i < trails.Length; i++)
    //     {
    //         trails[i].enabled = true;
    //     }
    //
    //     isPlay = true;
    // }

    public void SetDirection(Vector3 directionToCrosshair)
    {
        directionToCenter = directionToCrosshair.normalized;
    }
}