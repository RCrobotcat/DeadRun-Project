using UnityEngine;

public class PlayerSplatonPainting : MonoBehaviour
{
    private PaintingColor paintingColor;

    public Texture2D normalMap;

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

        Material particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
        particleMaterial.SetFloat("_Surface", 1f); // 1 = Transparent
        particleMaterial.SetFloat("_Smoothness", 0.1f);
        particleMaterial.SetTexture("_NormalMap", normalMap);
        particleMaterial.SetFloat("_NormalScale", 0.2f);
        particleMaterial.color = color;

        mainParticles.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
        mainParticles.GetComponent<ParticleSystemRenderer>().trailMaterial = particleMaterial;

        splashParticles.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
        splashParticles.GetComponent<ParticleSystemRenderer>().trailMaterial = particleMaterial;

        subEmitter0.GetComponent<ParticleSystemRenderer>().material = particleMaterial;

        shootEffectParticles.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
        shootEffectParticles.GetComponent<ParticleSystemRenderer>().trailMaterial = particleMaterial;

        collisionParticles.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
    }
}