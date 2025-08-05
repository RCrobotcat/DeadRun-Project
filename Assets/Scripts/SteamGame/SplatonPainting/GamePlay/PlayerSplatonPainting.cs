using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerSplatonPainting : NetworkBehaviour
{
    public GameObject currentPaintedAreasPanel;
    public Text currentPaintedAreasText;

    public Shader litShader;

    public float damageInterval = 1f;
    private float damageTimer = 0f;
    
    private PaintingColor paintingColor;
    public PaintingColor SelfPaintingColor
    {
        get => paintingColor;
        set
        {
            paintingColor = value;
            Color color = GetColorFromPaintingColor(paintingColor);

            SetColors(color);

            if (NetworkServer.active)
            {
                RpcSetPaintingColor(paintingColor);
            }
            else
            {
                CmdSetPaintingColor(paintingColor);
            }
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

    private MyNetworkManager _myNetworkManager;

    private MyNetworkManager MyNetworkManager
    {
        get
        {
            if (_myNetworkManager != null)
            {
                return _myNetworkManager;
            }

            return _myNetworkManager = MyNetworkManager.singleton as MyNetworkManager;
        }
    }

    private void Start()
    {
        _reader = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        damageTimer = damageInterval;
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

        int num = CheckIfPaintedGroundIsPlayer();
        if (num == 1)
        {
            GetComponent<PlayerMovement>().moveSpeed = 10;
        }
        else
        {
            GetComponent<PlayerMovement>().moveSpeed = 5f;
            if (num == 0)
            {
                GetComponent<PlayerMovement>().moveSpeed = 3f;
                if (damageTimer <= 0)
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
    const float rayDistance = 0.7f;

    Texture2D _reader;

    int CheckIfPaintedGroundIsPlayer()
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
                    return SampleOwnerID(paintable.getSupport(), uv);
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
                    return SampleOwnerID(paintable.getSupport(), uv);
                }
            }
        }

        return -1;
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

        Color pixelColor = _reader.GetPixel(0, 0);

        if (pixelColor == Color.clear)
            return -1;

        if (IsColorSimilar(pixelColor, GetColorFromPaintingColor(SelfPaintingColor)))
            return 1; // self color

        if (MyNetworkManager.GamePlayers.Count > 1)
        {
            foreach (var p in MyNetworkManager.GamePlayers)
            {
                Color color = GetColorFromPaintingColor(p.GetComponent<PlayerSplatonPainting>().SelfPaintingColor);
                if (p.playerID != LobbyController.Instance.LocalPlayerObjectController.playerID &&
                    IsColorSimilar(pixelColor, color))
                {
                    return 0; // other player color
                }
            }
        }

        return -1;
    }

    bool IsColorSimilar(Color a, Color b, float threshold = 0.8f)
    {
        float diff = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        return diff < threshold;
    }

    Color GetColorFromPaintingColor(PaintingColor color)
    {
        switch (color)
        {
            case PaintingColor.Red: return Color.red;
            case PaintingColor.Green: return Color.green;
            case PaintingColor.Blue: return Color.blue;
            default: return Color.white;
        }
    }

    [ClientRpc]
    void RpcSetPaintingColor(PaintingColor paintColor)
    {
        if (!isClientOnly) return;

        paintingColor = paintColor;

        SetColors(GetColorFromPaintingColor(paintingColor));

        Debug.Log("RpcSetPaintingColor called with color: " + paintColor);
    }

    [Command(requiresAuthority = false)]
    void CmdSetPaintingColor(PaintingColor paintColor)
    {
        paintingColor = paintColor;

        SetColors(GetColorFromPaintingColor(paintingColor));

        Debug.Log("CmdSetPaintingColor called with color: " + paintColor);
    }

    [ClientRpc]
    public void RpcUpdatePaintAreas(float paintAreas)
    {
        if (!isClientOnly) return;

        if (paintAreas < 0)
            return;

        GetComponent<PlayerSplatonPainting>().paintingParticles.PaintAreas = paintAreas;
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdatePaintAreas(float paintAreas)
    {
        if (paintAreas < 0)
            return;

        GetComponent<PlayerSplatonPainting>().paintingParticles.PaintAreas = paintAreas;
    }

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * rayHeight;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + Vector3.down * rayDistance);
    }
}