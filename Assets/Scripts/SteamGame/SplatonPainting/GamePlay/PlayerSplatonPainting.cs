using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSplatonPainting : NetworkBehaviour
{
    private PaintingColor paintingColor;

    public Shader litShader;

    public float damageInterval = 0.5f;
    private float damageTimer = 0f;

    public PaintingColor SelfPaintingColor
    {
        get => paintingColor;
        set
        {
            paintingColor = value;
            Color color = Color.white;
            switch (paintingColor)
            {
                case PaintingColor.Red:
                    color = Color.red;
                    break;
                case PaintingColor.Green:
                    color = Color.green;
                    break;
                case PaintingColor.Blue:
                    color = Color.blue;
                    break;
                case PaintingColor.Yellow:
                    color = Color.yellow;
                    break;
                case PaintingColor.Purple:
                    color = new Color(0.5f, 0f, 0.5f); // Purple
                    break;
                case PaintingColor.Orange:
                    color = new Color(1f, 0.5f, 0f); // Orange
                    break;
                default:
                    color = Color.white;
                    break;
            }

            SetColors(color);
        }
    }

    public ParticlesController paintingParticles;

    public ParticleSystem mainParticles;
    public ParticleSystem splashParticles;
    public ParticleSystem subEmitter0;
    public ParticleSystem shootEffectParticles;
    public ParticleSystem collisionParticles;

    void SetColors(Color color)
    {
        paintingParticles.paintColor = color;

        Material particleMaterial = new Material(litShader);
        particleMaterial.SetFloat("_Smoothness", 0.5f);
        particleMaterial.SetColor("_BaseColor", color);

        mainParticles.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
        mainParticles.GetComponent<ParticleSystemRenderer>().trailMaterial = particleMaterial;

        splashParticles.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
        splashParticles.GetComponent<ParticleSystemRenderer>().trailMaterial = particleMaterial;

        subEmitter0.GetComponent<ParticleSystemRenderer>().material = particleMaterial;

        shootEffectParticles.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
        shootEffectParticles.GetComponent<ParticleSystemRenderer>().trailMaterial = particleMaterial;

        collisionParticles.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
    }

    private void Start()
    {
        _reader = new Texture2D(1, 1, TextureFormat.RGBA32, false);
    }

    private void Update()
    {
        if (NetworkServer.active)
        {
            if (gameObject.scene.name != "Scene_5_Painting")
                return;
        }
        else
        {
            if (!SceneManager.GetSceneByName("Scene_5_Painting").isLoaded)
                return;
        }

        if (damageTimer > 0)
            damageTimer -= Time.deltaTime;

        int ownerId = CheckPaintedGroundPlayerId();
        if (ownerId > 0)
            Debug.Log(ownerId);
        if (GetComponent<PlayerObjectController>().playerID == ownerId)
        {
            GetComponent<PlayerMovement>().moveSpeed = 8;
        }
        else
        {
            GetComponent<PlayerMovement>().moveSpeed = 5f;
            if (damageTimer <= 0)
            {
                if (ownerId > 0)
                {
                    if (NetworkServer.active)
                        GetComponent<PlayerMovement>().AttackPlayerRpc();
                    else
                        GetComponent<PlayerMovement>().AttackPlayerCmd();

                    damageTimer = damageInterval;
                }
            }
        }
    }

    const float rayHeight = 0.1f;
    const float rayDistance = 1;

    Texture2D _reader;

    int CheckPaintedGroundPlayerId()
    {
        Vector3 origin = transform.position + Vector3.up * rayHeight;
        Ray ray = new Ray(origin, Vector3.down);

        LayerMask paintableMask = LayerMask.GetMask("Ground") | LayerMask.GetMask("Default");

        if (isServer)
        {
            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
            if (physicsScene.Raycast(ray.origin, ray.direction, out RaycastHit hit, rayDistance, paintableMask))
            {
                var paintable = hit.collider.GetComponent<Paintable>();
                if (paintable != null)
                {
                    Vector2 uv = hit.textureCoord; // get uv

                    return SampleOwnerID(paintable.getOwnerTexture(), uv);
                }
            }
        }
        else
        {
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, paintableMask))
            {
                var paintable = hit.collider.GetComponent<Paintable>();
                if (paintable != null)
                {
                    Vector2 uv = hit.textureCoord; // get uv

                    return SampleOwnerID(paintable.getOwnerTexture(), uv);
                }
            }
        }

        return 0;
    }

    int SampleOwnerID(RenderTexture rt, Vector2 uv)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * rt.width), 0, rt.width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * rt.height), 0, rt.height - 1);

        // read 1x1 pixel
        _reader.ReadPixels(new Rect(x, y, 1, 1), 0, 0);
        _reader.Apply();

        RenderTexture.active = prev; // recover

        float idNorm = _reader.GetPixel(0, 0).r;
        return Mathf.RoundToInt(idNorm * 255f);
    }

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * rayHeight;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + Vector3.down * rayDistance);
    }
}