using UnityEngine;

public class TerrainController : Singleton<TerrainController>
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
                terrainGeneration.GenerateTerrainFromTexture();
        }
    }

    private void Start()
    {
        CanGenerateTerrain = false;
    }
}