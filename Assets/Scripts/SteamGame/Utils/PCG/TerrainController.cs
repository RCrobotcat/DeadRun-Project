using UnityEngine;

public class TerrainController : MonoBehaviour
{
    public TerrainGeneration terrainGeneration;

    private bool canGenerateTerrain = false;

    public bool CanGenerateTerrain
    {
        get { return canGenerateTerrain; }
        set
        {
            canGenerateTerrain = value;
            if (canGenerateTerrain)
            {
                terrainGeneration.gameObject.SetActive(true);
                terrainGeneration.GenerateTerrainFromTexture();
            }
            else
            {
                terrainGeneration.gameObject.SetActive(false);
            }
        }
    }

    private void Start()
    {
        CanGenerateTerrain = false;
    }
}