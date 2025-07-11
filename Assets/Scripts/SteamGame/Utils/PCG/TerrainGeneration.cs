using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{
    public Texture2D noiseTexture;
    public float heightMultiplier = 100; // 高度系数
    private float lastHeightMultiplier;

    public TerrainLayer grassLayer;
    public TerrainLayer soilLayer;

    private Terrain terrain;

    private TerrainData terrainData;
    private TerrainCollider terrainCollider;

    void Start()
    {
        terrain = GetComponent<Terrain>();
        terrainCollider = GetComponent<TerrainCollider>();
        GenerateTerrainFromTexture();
    }

    private void Update()
    {
        if (lastHeightMultiplier != heightMultiplier)
        {
            terrainData.size = new Vector3(noiseTexture.width, heightMultiplier, noiseTexture.height);
            terrainCollider.terrainData = terrainData;
            lastHeightMultiplier = heightMultiplier;
        }
    }

    void GenerateTerrainFromTexture()
    {
        terrainData = new TerrainData();
        terrainData.heightmapResolution = noiseTexture.width + 1;
        terrainData.size = new Vector3(noiseTexture.width, heightMultiplier, noiseTexture.height);
        lastHeightMultiplier = heightMultiplier;

        // 创建一个高度图数组
        float[,] heights = new float[noiseTexture.width, noiseTexture.height];
        float[,,] splatmap = new float[noiseTexture.width, noiseTexture.height, 2];
        for (int x = 0; x < noiseTexture.width; x++)
        {
            for (int y = 0; y < noiseTexture.height; y++)
            {
                // 获取RGB通道的平均值
                Color pixelColor = noiseTexture.GetPixel(x, y);
                float height = (pixelColor.r + pixelColor.g + pixelColor.b) / 3f;

                heights[x, y] = height;

                if (heights[x, y] > 0.5f)
                {
                    splatmap[x, y, 1] = 1; // 使用草地
                }
                else
                {
                    splatmap[x, y, 0] = 1; // 使用土地
                }
            }
        }

        terrainData.SetHeights(0, 0, heights);

        terrainData.terrainLayers = new TerrainLayer[] { soilLayer, grassLayer };
        terrainData.SetAlphamaps(0, 0, splatmap);

        terrain.terrainData = terrainData;
        terrainCollider.terrainData = terrainData;
    }
}