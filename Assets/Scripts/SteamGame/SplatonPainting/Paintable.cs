using Mirror;
using UnityEngine;

public class Paintable : NetworkBehaviour
{
    const int TEXTURE_SIZE = 1024;

    public float extendsIslandOffset = 1;

    RenderTexture extendIslandsRenderTexture;
    RenderTexture uvIslandsRenderTexture;
    RenderTexture maskRenderTexture;
    RenderTexture supportTexture;

    [Header("Owner Texture")] RenderTexture ownerTexture; // R8 格式，只存 ID
    public Material ownerMaterial;

    Renderer rend;

    int maskTextureID = Shader.PropertyToID("_MaskTexture");

    public RenderTexture getMask() => maskRenderTexture;
    public RenderTexture getUVIslands() => uvIslandsRenderTexture;
    public RenderTexture getExtend() => extendIslandsRenderTexture;
    public RenderTexture getSupport() => supportTexture;
    public Renderer getRenderer() => rend;

    public RenderTexture getOwnerTexture() => ownerTexture;

    void Start()
    {
        maskRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        maskRenderTexture.filterMode = FilterMode.Bilinear;

        extendIslandsRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        extendIslandsRenderTexture.filterMode = FilterMode.Bilinear;

        uvIslandsRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        uvIslandsRenderTexture.filterMode = FilterMode.Bilinear;

        supportTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        supportTexture.filterMode = FilterMode.Bilinear;

        ownerTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0,
            RenderTextureFormat.R8, RenderTextureReadWrite.Linear)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        ownerTexture.Create();
        ownerMaterial = new Material(Shader.Find("Painting/OwnerIdPainter"));

        rend = GetComponent<Renderer>();
        rend.material.SetTexture(maskTextureID, extendIslandsRenderTexture);

        PaintManager.Instance.initTextures(this);

        PaintablesManager.Instance.RegisterPaintable(this);
    }

    void OnDisable()
    {
        maskRenderTexture.Release();
        uvIslandsRenderTexture.Release();
        extendIslandsRenderTexture.Release();
        supportTexture.Release();
        ownerTexture.Release();
    }
}