using UnityEngine;

public class JetpackSmoke : MonoBehaviour
{
    public float recycleTime = 0.5f;
    private float lastTime = 0f;

    private void OnEnable()
    {
        lastTime = Time.time;
    }

    private void Update()
    {
        if (Time.time - lastTime > recycleTime)
        {
            lastTime = Time.time;
            gameObject.Recycle();
        }
    }
}