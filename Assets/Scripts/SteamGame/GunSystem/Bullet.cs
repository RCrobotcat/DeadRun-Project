using System;
using Mirror;
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

    public ParticleSystem explosionParticle;

    private void Start()
    {
        Physics.IgnoreCollision(LobbyController.Instance.localPlayerObject.GetComponent<Collider>(),
            GetComponent<Collider>());
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
            // Player Scripts will handle the collision
            return;

        if (NetworkServer.active)
            NetworkServer.Destroy(gameObject);
    }

    public void SetDirection(Vector3 directionToCrosshair)
    {
        directionToCenter = directionToCrosshair.normalized;
    }
}