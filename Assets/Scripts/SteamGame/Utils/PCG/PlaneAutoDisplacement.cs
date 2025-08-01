using System;
using System.Collections;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlaneAutoDisplacement : MonoBehaviour
{
    public float verticalAutoDisplacement = 0.5f;
    public float horizontalAutoDisplacement = 0.5f;
    public float autoDisplacementSpeed = 1f;
    public float pauseTime = 1.5f;

    Vector3 originalPos;

    private void Start()
    {
        originalPos = transform.position;
        StartCoroutine(AutoDisplaceLoop());
    }

    IEnumerator AutoDisplaceLoop()
    {
        while (true)
        {
            bool vertical = Random.value > 0.5f;
            Vector3 offset = vertical
                ? new Vector3(0, 0, Random.Range(-verticalAutoDisplacement, verticalAutoDisplacement))
                : new Vector3(Random.Range(-horizontalAutoDisplacement, horizontalAutoDisplacement), 0, 0);
            Vector3 targetPos = originalPos + offset;
            
            yield return MoveToPosition(targetPos);
            yield return new WaitForSeconds(pauseTime);
            
            yield return MoveToPosition(originalPos);
            yield return new WaitForSeconds(pauseTime);
        }
    }

    IEnumerator MoveToPosition(Vector3 dest)
    {
        Vector3 start = transform.position;
        float distance = Vector3.Distance(start, dest);
        if (distance < 0.001f)
            yield break;

        float duration = distance / autoDisplacementSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, dest, t);
            yield return null;
        }

        transform.position = dest;
    }
}