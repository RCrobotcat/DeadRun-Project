using UnityEngine;

public class ShootableObject : MonoBehaviour
{
    public float shotForce = 10f;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Bullet"))
        {
            Vector3 direction = -(other.transform.position - transform.position).normalized;
            rb.AddForce(direction * shotForce, ForceMode.Impulse);
            other.gameObject.Recycle();
        }
    }
}