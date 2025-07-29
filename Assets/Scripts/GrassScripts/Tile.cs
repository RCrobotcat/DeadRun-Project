using UnityEngine;

namespace Grass_RC_14
{
    public partial class Grass : MonoBehaviour
    {
        struct Tile
        {
            public Terrain terrain;
            public Bounds bounds; // 包围盒
            public Vector2Int gridPosition; // 草地区块在地形中的网格位置
            public float spaceMultiplier;
            public int xResolutionDivisor;
            public int zResolutionDivisor;

            public Tile(Terrain t, Bounds b, Vector2Int pos, float mul = 1, int xd = 1, int zd = 1)
            {
                terrain = t;
                bounds = b;
                gridPosition = pos;
                spaceMultiplier = mul;
                xResolutionDivisor = xd;
                zResolutionDivisor = zd;
            }
        }
    }
}