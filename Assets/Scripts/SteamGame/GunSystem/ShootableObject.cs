using Mirror;
using UnityEngine;

public partial class PlayerMovement
{
    public float shotForce = 10f;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Bullet"))
        {
            Vector3 direction = (other.transform.position - transform.position).normalized;
            rb.AddForce(direction * shotForce, ForceMode.Impulse);

            if (NetworkServer.active)
                RpcAddForce(direction, shotForce);
            else
                CmdAddForce(direction, shotForce);

            Destroy(other.gameObject, 0.1f);
        }
    }

    [Command(requiresAuthority = false)]
    void CmdAddForce(Vector3 direction, float force)
    {
        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    [ClientRpc]
    void RpcAddForce(Vector3 direction, float force)
    {
        if (!isClientOnly)
            return;

        rb.AddForce(direction * force, ForceMode.Impulse);
    }
}