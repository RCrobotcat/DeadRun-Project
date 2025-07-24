using Mirror;
using UnityEngine;

public partial class PlayerMovement
{
    public GameObject astronautModel;

    public float outlineShowTime = 0.5f;
    private float outlineShowTimer = 0;
    private float outlineShowTimerLocal = 0;

    public float shotForce = 10f;

    public PlayerObjectController playerObjectController;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Bullet"))
        {
            //playerObjectController.CurrentHealth -= 10f;
            if (NetworkServer.active)
            {
                AttackPlayerRpc();
            }
            else
            {
                AttackPlayerCmd();
            }

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

        if (outlineShowTimer <= 0)
        {
            foreach (Transform child in astronautModel.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = LayerMask.NameToLayer("Outlined");
            outlineShowTimer = outlineShowTime;
        }
    }

    [Command(requiresAuthority = false)]
    void CmdDamagePlayer(float damage)
    {
        playerObjectController.CurrentHealth -= damage;

        if (outlineShowTimer <= 0)
        {
            foreach (Transform child in astronautModel.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = LayerMask.NameToLayer("Outlined");
            outlineShowTimer = outlineShowTime;
        }
    }

    public void AttackPlayerRpc()
    {
        RpcDamagePlayer(5f);
        if (outlineShowTimerLocal <= 0)
        {
            foreach (Transform child in astronautModel.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = LayerMask.NameToLayer("Outlined");
            outlineShowTimerLocal = outlineShowTime;
        }
    }

    public void AttackPlayerCmd()
    {
        CmdDamagePlayer(5f);
        if (outlineShowTimerLocal <= 0)
        {
            foreach (Transform child in astronautModel.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = LayerMask.NameToLayer("Outlined");
            outlineShowTimerLocal = outlineShowTime;
        }
    }

    public void MonsterAttackPlayer(float damage)
    {
        playerObjectController.CurrentHealth -= damage;
        if (outlineShowTimer <= 0)
        {
            foreach (Transform child in astronautModel.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = LayerMask.NameToLayer("Outlined");
            outlineShowTimer = outlineShowTime;
        }
    }
}