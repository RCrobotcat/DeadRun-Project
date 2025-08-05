using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ParticlesController : MonoBehaviour
{
    public PlayerObjectController player;
    public PlayerSplatonPainting playerSplatonPainting;

    public Color paintColor;

    public float minRadius = 0.05f;
    public float maxRadius = 0.2f;
    public float strength = 1;
    public float hardness = 1;
    [Space] ParticleSystem part;
    List<ParticleCollisionEvent> collisionEvents;

    private float paintAreas = 0;

    public float PaintAreas
    {
        get => paintAreas;
        set
        {
            paintAreas = value;

            if (player.playerID == LobbyController.Instance.LocalPlayerObjectController.playerID)
            {
                playerSplatonPainting.currentPaintedAreasText.text = $"{paintAreas:F2} m²";
            }

            if (NetworkServer.active)
                playerSplatonPainting.RpcUpdatePaintAreas(paintAreas);
        }
    }

    void Start()
    {
        part = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
        //var pr = part.GetComponent<ParticleSystemRenderer>();
        //Color c = new Color(pr.material.color.r, pr.material.color.g, pr.material.color.b, .8f);
        //paintColor = c;
    }

    void OnParticleCollision(GameObject other)
    {
        if (player.playerID != LobbyController.Instance.LocalPlayerObjectController.playerID)
            return;

        int numCollisionEvents = part.GetCollisionEvents(other, collisionEvents);

        Paintable p = other.GetComponent<Paintable>();
        if (p != null)
        {
            for (int i = 0; i < numCollisionEvents; i++)
            {
                Vector3 pos = collisionEvents[i].intersection;
                float radius = Random.Range(minRadius, maxRadius);
                PaintManager.Instance.paint(p, pos, radius, hardness, strength, paintColor);

                if (NetworkServer.active)
                    PaintAreas += radius * 0.05f;
                else
                    playerSplatonPainting.CmdUpdatePaintAreas(radius * 0.05f);

                SyncPaint(p, pos, radius, hardness, strength, paintColor);
            }
        }
    }

    void SyncPaint(Paintable paintable, Vector3 pos, float radius, float hardness, float strength, Color color)
    {
        int paintableId = PaintablesManager.Instance.GetPaintableID(paintable);
        if (NetworkServer.active)
        {
            player.RpcSyncPaint(paintableId, pos, radius, hardness, strength, color);
        }
        else
        {
            player.CmdSyncPaint(paintableId, pos, radius, hardness, strength, color);
        }
    }
}