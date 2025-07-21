using System;
using UnityEngine;

public enum TowardType
{
    Forward,
    Right
}

public class Bullet : MonoBehaviour
{
    public float movementSpeed = 10f;

    private float lastTime = 0f;

    public TowardType towardType = TowardType.Forward;
    private Vector3 directionToCenter;
    private Vector3 scale;
    public float maxDistance = 100f;
    float curDistance = 0f;

    private void Start()
    {
        Physics.IgnoreCollision(GetComponent<Collider>(), GetComponent<Collider>());
    }

    private void Update()
    {
        if (curDistance > maxDistance)
        {
            curDistance = 0;
            Destroy(gameObject, 0.1f);
        }

        Quaternion targetRotation = Quaternion.LookRotation(directionToCenter);
        transform.rotation = targetRotation;
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

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerMovement player = other.transform.GetComponent<PlayerMovement>();
            if (player.OutlineShowTimer <= 0)
            {
                foreach (Transform child in player.astronautModel
                             .GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = LayerMask.NameToLayer("Outlined");
                player.OutlineShowTimer = player.outlineShowTime;
            }
        }
    }

    public void SetDirection(Vector3 directionToCrosshair)
    {
        directionToCenter = directionToCrosshair.normalized;
    }
}