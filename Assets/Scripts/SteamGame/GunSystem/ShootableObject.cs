using Mirror;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public partial class PlayerMovement
{
    public UniversalRendererData urpRendererData;

    public float outlineShowTime = 0.5f;
    private float outlineShowTimer = 0;

    public float shotForce = 10f;

    public PlayerObjectController playerObjectController;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Bullet"))
        {
            //playerObjectController.CurrentHealth -= 10f;
            if (NetworkServer.active)
                RpcDamagePlayer(5f);
            else
                CmdDamagePlayer(5f);

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

    [ClientRpc]
    void RpcDamagePlayer(float damage)
    {
        if (!isClientOnly)
            return;

        playerObjectController.CurrentHealth -= damage;

        if (outlineShowTime <= 0)
        {
            urpRendererData.rendererFeatures[1].SetActive(true);
            outlineShowTimer = outlineShowTime;
        }
    }

    [Command(requiresAuthority = false)]
    void CmdDamagePlayer(float damage)
    {
        playerObjectController.CurrentHealth -= damage;

        if (outlineShowTime <= 0)
        {
            urpRendererData.rendererFeatures[1].SetActive(true);
            outlineShowTimer = outlineShowTime;
        }
    }
}