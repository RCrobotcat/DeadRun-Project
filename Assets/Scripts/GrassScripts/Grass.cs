using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Grass_RC_14
{
    public partial class Grass : MonoBehaviour
    {
        [SerializeField] private ComputeShader computeShader;
        [SerializeField] private Material material;

        public Camera cam;

        //public float grassSpacing = 0.1f;
        //public int resolution = 100;

        // Grass Tile
        public int tileResolution = 32;
        public int tileCount = 10; // 边长上Tile的数量

        [SerializeField, Range(0, 2)] public float jitterStrength;

        // LOD0: 中心区域; LOD1: 外围区域
        [Header("Culling")] public float distanceCullStartDisLOD0; // 中心区域开始裁剪距离
        public float distanceCullEndDisLOD0; // 中心区域最远裁剪距离
        public float distanceCullStartDisLOD1; // 边缘区域开始裁剪距离
        public float distanceCullEndDisLOD1; // 边缘区域最远裁剪距离

        //[Range(0f, 1f)] public float distanceCullMinimumGrassAmount;
        public float frustumCullNearOffset;
        public float frustumCullEdgeOffset;

        [Header("Clumping")] public int clumpTextureHeight;
        public int clumpTextureWidth;
        public Material clumpingVoronoiMaterial;
        public float clumpScale;
        public List<ClumpParameters> clumpParameters;

        [Header("Wind")] [SerializeField] private Texture2D localWindTex;
        [Range(0.0f, 1.0f)] [SerializeField] private float localWindStrength = 0.5f;
        [SerializeField] private float localWindScale = 0.01f;
        [SerializeField] private float localWindSpeed = 0.1f;
        [Range(0.0f, 1.0f)] [SerializeField] private float localWindRotateAmount = 0.3f;
        [SerializeField] private Vector2 localWindDirection = new Vector2(1, 0.7f);

        // Unique IDs for shader properties
        private static readonly int
            grassBladesBufferID = Shader.PropertyToID("_GrassBlades"),
            resolutionXID = Shader.PropertyToID("_ResolutionX"),
            resolutionYID = Shader.PropertyToID("_ResolutionY"),
            grassSpacingID = Shader.PropertyToID("_GrassSpacing"),
            jitterStrengthID = Shader.PropertyToID("_JitterStrength"),
            heightMapID = Shader.PropertyToID("_HeightMap"),
            detailMapID = Shader.PropertyToID("_DetailMap"),
            terrainPositionID = Shader.PropertyToID("_TerrainPosition"),
            tilePositionID = Shader.PropertyToID("_TilePosition"),
            heightMapScaleID = Shader.PropertyToID("_HeightMapScale"),
            heightMapMultiplierID = Shader.PropertyToID("_HeightMapMultiplier"), // 高度图平面缩放
            distanceCullStartDistID = Shader.PropertyToID("_DistanceCullStartDist"),
            distanceCullEndDistID = Shader.PropertyToID("_DistanceCullEndDist"),
            distanceCullMinimumGrassAmountlID = Shader.PropertyToID("_DistanceCullMinimumGrassAmount"),
            worldSpaceCameraPositionID = Shader.PropertyToID("_WSpaceCameraPos"),
            vpMatrixID = Shader.PropertyToID("_VP_MATRIX"),
            frustumCullNearOffsetID = Shader.PropertyToID("_FrustumCullNearOffset"),
            frustumCullEdgeOffsetID = Shader.PropertyToID("_FrustumCullEdgeOffset"),
            clumpParametersID = Shader.PropertyToID("_ClumpParameters"),
            numClumpParametersID = Shader.PropertyToID("_NumClumpParameters"),
            clumpTexID = Shader.PropertyToID("ClumpTex"),
            clumpScaleID = Shader.PropertyToID("_ClumpScale"),
            LocalWindTexID = Shader.PropertyToID("_LocalWindTex"),
            LocalWindScaleID = Shader.PropertyToID("_LocalWindScale"),
            LocalWindSpeedID = Shader.PropertyToID("_LocalWindSpeed"),
            LocalWindStrengthID = Shader.PropertyToID("_LocalWindStrength"),
            LocalWindRotateAmountID = Shader.PropertyToID("_LocalWindRotateAmount"),
            TimeID = Shader.PropertyToID("_Time"),
            LocalWindDirectionID = Shader.PropertyToID("_LocalWindDirection");

        private ComputeBuffer grassBladesBuffer; // 对应 computeShader 中的 AppendStructuredBuffer

        private ComputeBuffer meshTrianglesBuffer; // 三角形索引Buffer

        // private ComputeBuffer meshPositionsBuffer; // 顶点位置Buffer
        private ComputeBuffer meshColorsBuffer; // 顶点颜色Buffer
        private ComputeBuffer meshUvsBuffer; // 顶点UV Buffer
        private ComputeBuffer argsBuffer; // 用于存储绘制参数的Buffer
        private const int ARGS_STRIDE = sizeof(int) * 4; // 参数的Buffer大小
        private Mesh clonedMesh; // 克隆的网格，用于渲染草叶
        private Bounds bounds; // 用于渲染的包围盒

        private ComputeBuffer clumpParametersBuffer;
        private ClumpParameters[] clumpParametersArray;
        private Texture2D clumpTexture;

        private float grassSpacing = 0.1f;

        // Grass Tile
        private List<Tile> visibleTiles = new List<Tile>(); // 摄像机周围的草区块(Tile)列表
        private List<Tile> tilesToRender = new List<Tile>(); // 需要渲染的草区块列表
        private float tileSizeX = 0, tileSizeZ = 0; // 每个草区块Tile的大小
        private List<Terrain> terrains = new List<Terrain>();

        private void Awake()
        {
            Initialize();
            bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
        }

        private void Update()
        {
            if (NetworkServer.active)
            {
                if (LobbyController.Instance.localPlayerObject.scene.name != "Scene_3_1v1")
                    return;
            }
            else
            {
                if (!SceneManager.GetSceneByName("Scene_3_1v1").isLoaded)
                    return;
            }

            // Fine in Editor, but in build, terrains may not be initialized yet
            // Don't know whu, lol
            if (terrains.Count == 0)
                Initialize();

            if (cam == null)
            {
                cam = Camera.main;
            }

            UpdateGrassTiles();
            UpdateGpuParameters(); // 每帧都往Compute Shader中传递参数
        }

        private void LateUpdate()
        {
            RenderGrass();
        }

        private void OnDestroy()
        {
            DisposeBuffers();
            DestroyClumpTexture();
        }

        void Initialize()
        {
            CollectTerrains();
            InitializeComputeBuffers();
            SetupMeshBuffers();
            CreateClumpTexture();
            CalculateGrassSpacing();
        }

        private void CalculateGrassSpacing()
        {
            // 假设所有地形都一样大
            if (terrains.Count > 0)
            {
                grassSpacing = terrains[0].terrainData.size.x / (tileCount * tileResolution);
            }
        }

        private void InitializeComputeBuffers()
        {
            int tileMax = 16;
            //14 floats: position, rotAngle, hash, height, width, tilt, bend, surfaceNorm, windForce, sideBend
            grassBladesBuffer =
                new ComputeBuffer(tileResolution * tileResolution * tileMax, sizeof(float) * 14,
                    ComputeBufferType.Append);
            grassBladesBuffer.SetCounterValue(0);

            argsBuffer = new ComputeBuffer(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);

            clumpParametersBuffer = new ComputeBuffer(clumpParameters.Count, sizeof(float) * 10);
            UpdateClumpParametersBuffer();
        }

        private void SetupMeshBuffers()
        {
            clonedMesh = GrassMesh.CreateHighLODMesh();
            clonedMesh.name = "Grass Instance Mesh";

            CreateComputeBuffersForMesh();

            argsBuffer.SetData(new int[] { meshTrianglesBuffer.count, 0, 0, 0 });
        }

        private void CreateComputeBuffersForMesh()
        {
            int[] triangles = clonedMesh.triangles;
            Vector3[] positions = clonedMesh.vertices;
            Color[] colors = clonedMesh.colors;
            Vector2[] uvs = clonedMesh.uv;

            meshTrianglesBuffer = CreateBuffer<int>(triangles, sizeof(int));
            // meshPositionsBuffer = CreateBuffer<Vector3>(positions, sizeof(float) * 3); // x, y, z
            meshColorsBuffer = CreateBuffer<Color>(colors, sizeof(float) * 4); // r, g, b, a
            meshUvsBuffer = CreateBuffer<Vector2>(uvs, sizeof(float) * 2); // u, v

            material.SetBuffer("Triangles", meshTrianglesBuffer);
            material.SetBuffer("Colors", meshColorsBuffer);
            material.SetBuffer("Uvs", meshUvsBuffer);
            material.SetBuffer(grassBladesBufferID, grassBladesBuffer);
        }

        private ComputeBuffer CreateBuffer<T>(T[] data, int stride) where T : struct
        {
            ComputeBuffer buffer = new ComputeBuffer(data.Length, stride);
            buffer.SetData(data);
            return buffer;
        }

        private void UpdateGpuParameters()
        {
            grassBladesBuffer.SetCounterValue(0);

            computeShader.SetVector(worldSpaceCameraPositionID, cam.transform.position);

            Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
            Matrix4x4 viewProjectionMatrix = projectionMatrix * cam.worldToCameraMatrix;
            computeShader.SetMatrix(vpMatrixID, viewProjectionMatrix);
            computeShader.SetFloat(TimeID, Time.time);

            foreach (Tile tile in visibleTiles)
            {
                SetupComputeShaderForTile(tile);

                int threadGroupsX = Mathf.CeilToInt(tileResolution / (8f * tile.xResolutionDivisor));
                int threadGroupsZ = Mathf.CeilToInt(tileResolution / (8f * tile.zResolutionDivisor));

                computeShader.Dispatch(0, threadGroupsX, threadGroupsZ, 1);
            }
        }

        private void CollectTerrains()
        {
            terrains.Clear();

            Terrain[] allTerrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            foreach (Terrain t in allTerrains)
            {
                if (t.enabled)
                {
                    terrains.Add(t);
                }
            }

            if (terrains.Count == 0)
            {
                Debug.LogWarning("Terrain count = 0");
            }
        }

        private void UpdateGrassTiles()
        {
            tilesToRender.Clear();

            foreach (Terrain terrain in terrains)
            {
                if (terrain != null)
                {
                    UpdateSurroundingTilesForTerrain(terrain);
                }
            }

            UpdateVisibleTiles();
        }

        /// <summary>
        /// 更新摄像机周围的16个草区块(Tile)列表
        /// </summary>
        /// <param name="terrain"></param>
        private void UpdateSurroundingTilesForTerrain(Terrain terrain)
        {
            Vector3 terrainSize = terrain.terrainData.size;
            tileSizeZ = tileSizeX = terrainSize.x / tileCount;

            // 计算摄像机在地形空间中的位置
            Vector3 cameraPositionInTerrainSpace = cam.transform.position - terrain.transform.position;
            int cameraTileXIndex = Mathf.FloorToInt(cameraPositionInTerrainSpace.x / tileSizeX);
            int cameraTileZIndex = Mathf.FloorToInt(cameraPositionInTerrainSpace.z / tileSizeZ);

            if (cameraTileXIndex >= -6 && cameraTileXIndex < tileCount + 5 &&
                cameraTileZIndex >= -6 && cameraTileZIndex < tileCount + 5)
            {
                HashSet<Vector2Int> mergedTileGridPositions = new HashSet<Vector2Int>();

                // 遍历摄像机周围 10x10 的Tile
                for (int xIndex = cameraTileXIndex - 5; xIndex <= cameraTileXIndex + 6; xIndex++)
                {
                    for (int zIndex = cameraTileZIndex - 5; zIndex <= cameraTileZIndex + 6; zIndex++)
                    {
                        Vector2Int currentGridPosition = new Vector2Int(xIndex, zIndex);

                        // 把中心区域的Tile找出并生成出来
                        if (IsStandardTile(xIndex, cameraTileXIndex) && IsStandardTile(zIndex, cameraTileZIndex) &&
                            IsTileWithinTerrainBounds(xIndex, zIndex))
                        {
                            AddStandardTile(terrain, currentGridPosition);
                        }
                        else
                        {
                            (Vector2Int mergedTileStartPosition, bool isMerged) =
                                CalculateMergedTileStartPosition(xIndex, zIndex, cameraTileXIndex, cameraTileZIndex);
                            if (isMerged)
                            {
                                mergedTileGridPositions.Add(mergedTileStartPosition);
                            }
                        }
                    }
                }

                AddMergedTiles(terrain, mergedTileGridPositions);
            }
        }

        private bool IsStandardTile(int tileIndex, int cameraTileIndex)
        {
            // 是否是在摄像机周围 4x4 的范围
            return tileIndex >= cameraTileIndex - 1 && tileIndex <= cameraTileIndex + 2;
        }

        private bool IsTileWithinTerrainBounds(int xIndex, int zIndex)
        {
            return xIndex >= 0 && xIndex < tileCount && zIndex >= 0 && zIndex < tileCount;
        }

        private void AddStandardTile(Terrain terrain, Vector2Int gridPosition)
        {
            Bounds tileBounds = CalculateTileBounds(terrain, gridPosition.x, gridPosition.y);
            tilesToRender.Add(new Tile(terrain, tileBounds, gridPosition, 1f, 1, 1));
        }

        private void AddMergedTiles(Terrain terrain, HashSet<Vector2Int> mergedTileGridPositions)
        {
            foreach (Vector2Int gridPosition in mergedTileGridPositions)
            {
                if (gridPosition.x <= -2 || gridPosition.x >= tileCount || gridPosition.y <= -2 ||
                    gridPosition.y >= tileCount) continue;

                int xResolutionDivisor = 1;
                int zResolutionDivisor = 1;
                int posX = gridPosition.x;
                int posY = gridPosition.y;

                if (gridPosition.x == -1)
                {
                    xResolutionDivisor = 2;
                    posX = 0;
                }

                if (gridPosition.x == tileCount - 1)
                {
                    xResolutionDivisor = 2;
                }

                if (gridPosition.y == -1)
                {
                    zResolutionDivisor = 2;
                    posY = 0;
                }

                if (gridPosition.y == tileCount - 1)
                {
                    zResolutionDivisor = 2;
                }

                Bounds mergedBounds = CalculateTileBounds(terrain, posX, posY, 2f / xResolutionDivisor,
                    2f / zResolutionDivisor);
                tilesToRender.Add(new Tile(terrain, mergedBounds, new Vector2Int(posX, posY), 2f, xResolutionDivisor,
                    zResolutionDivisor));
            }
        }

        private (Vector2Int, bool) CalculateMergedTileStartPosition(int xIndex, int zIndex, int cameraTileXIndex,
            int cameraTileZIndex)
        {
            Vector2Int mergedStartPos = Vector2Int.zero;
            bool isMerged = false;

            // 左侧两列 (cx-3, cx-2)
            if (xIndex <= cameraTileXIndex - 2)
            {
                int startZIndex = cameraTileZIndex - 3;
                int groupZIndex = (zIndex - startZIndex) / 2;
                mergedStartPos = new Vector2Int(cameraTileXIndex - 3, startZIndex + groupZIndex * 2);
                isMerged = true;
            }
            // 右侧两列 (cx+3, cx+4)
            else if (xIndex >= cameraTileXIndex + 3)
            {
                int startZIndex = cameraTileZIndex - 3;
                int groupZIndex = (zIndex - startZIndex) / 2;
                mergedStartPos = new Vector2Int(cameraTileXIndex + 3, startZIndex + groupZIndex * 2);
                isMerged = true;
            }
            // 上方两行且在中间列 (cy-3, cy-2)
            else if (zIndex <= cameraTileZIndex - 2 && IsStandardTile(xIndex, cameraTileXIndex))
            {
                int startXIndex = cameraTileXIndex - 1;
                int groupXIndex = (xIndex - startXIndex) / 2;
                mergedStartPos = new Vector2Int(startXIndex + groupXIndex * 2, cameraTileZIndex - 3);
                isMerged = true;
            }
            // 下方两行且在中间列 (cy+3, cy+4)
            else if (zIndex >= cameraTileZIndex + 3 && IsStandardTile(xIndex, cameraTileXIndex))
            {
                int startXIndex = cameraTileXIndex - 1;
                int groupXIndex = (xIndex - startXIndex) / 2;
                mergedStartPos = new Vector2Int(startXIndex + groupXIndex * 2, cameraTileZIndex + 3);
                isMerged = true;
            }

            return (mergedStartPos, isMerged);
        }

        /// <summary>
        /// 计算每个草区块(Tile)的包围盒
        /// </summary>
        private Bounds CalculateTileBounds(Terrain terrain, int tileXIndex, int tileZIndex, float tileScaleX = 1.0f,
            float tileScaleZ = 1.0f)
        {
            Vector3 min = terrain.transform.position +
                          new Vector3(tileXIndex * tileSizeX, -10f, tileZIndex * tileSizeZ);
            Vector3 max = min + new Vector3(tileSizeX * tileScaleX, 20f, tileSizeZ * tileScaleZ);

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        /// <summary>
        /// 更新可见的草区块(Tile)列表
        /// </summary>
        private void UpdateVisibleTiles()
        {
            visibleTiles.Clear();
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);

            foreach (Tile tile in tilesToRender)
            {
                if (IsVisibleInFrustum(frustumPlanes, tile.bounds))
                {
                    visibleTiles.Add(tile);
                }
            }
        }

        private bool IsVisibleInFrustum(Plane[] planes, Bounds bounds)
        {
            // 注意控制生成的包围盒不要太大
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }

        private void SetupComputeShaderForTile(Tile tile)
        {
            Terrain terrain = tile.terrain;

            // 内圈（中心区域）
            if (tile.spaceMultiplier == 1)
            {
                computeShader.SetFloat(distanceCullStartDistID, distanceCullStartDisLOD0);
                computeShader.SetFloat(distanceCullEndDistID, distanceCullEndDisLOD0);
                computeShader.SetFloat(distanceCullMinimumGrassAmountlID, 0.25f); // 末端衰减密度 => 0.25f => Tile的边缘平滑过渡
            }
            else // 外圈（外围区域）
            {
                computeShader.SetFloat(distanceCullStartDistID, distanceCullStartDisLOD1);
                computeShader.SetFloat(distanceCullEndDistID, distanceCullEndDisLOD1);
                computeShader.SetFloat(distanceCullMinimumGrassAmountlID, 0);
            }

            computeShader.SetInt(resolutionXID, tileResolution / tile.xResolutionDivisor);
            computeShader.SetInt(resolutionYID, tileResolution / tile.zResolutionDivisor);
            computeShader.SetBuffer(0, grassBladesBufferID, grassBladesBuffer);

            float adjustedGrassSpacing = grassSpacing * tile.spaceMultiplier;
            computeShader.SetFloat(grassSpacingID, adjustedGrassSpacing);
            computeShader.SetFloat(jitterStrengthID, jitterStrength);
            computeShader.SetVector(tilePositionID, tile.bounds.min);

            computeShader.SetVector(terrainPositionID, terrain.transform.position);
            computeShader.SetTexture(0, heightMapID, terrain.terrainData.heightmapTexture);
            if (terrain.terrainData.alphamapTextures.Length > 0) // 地形的alphamap可能不止一个
            {
                computeShader.SetTexture(0, detailMapID, terrain.terrainData.alphamapTextures[0]);
            }

            computeShader.SetFloat(heightMapScaleID, terrain.terrainData.size.x);
            computeShader.SetFloat(heightMapMultiplierID, terrain.terrainData.size.y);

            computeShader.SetFloat(frustumCullNearOffsetID, frustumCullNearOffset);
            computeShader.SetFloat(frustumCullEdgeOffsetID, frustumCullEdgeOffset);

            UpdateClumpParametersBuffer();
            computeShader.SetBuffer(0, clumpParametersID, clumpParametersBuffer);
            computeShader.SetFloat(numClumpParametersID, clumpParameters.Count);
            computeShader.SetTexture(0, clumpTexID, clumpTexture);
            computeShader.SetFloat(clumpScaleID, clumpScale);

            computeShader.SetTexture(0, LocalWindTexID, localWindTex);
            computeShader.SetFloat(LocalWindScaleID, localWindScale);
            computeShader.SetFloat(LocalWindSpeedID, localWindSpeed);
            computeShader.SetFloat(LocalWindStrengthID, localWindStrength);
            computeShader.SetFloat(LocalWindRotateAmountID, localWindRotateAmount);
            computeShader.SetVector(LocalWindDirectionID, localWindDirection);
        }

        private void RenderGrass()
        {
            ComputeBuffer.CopyCount(grassBladesBuffer, argsBuffer, sizeof(int));

            // 只会在cam摄像机渲染草叶
            Graphics.DrawProceduralIndirect(material, bounds, MeshTopology.Triangles, argsBuffer,
                0, null, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
        }

        /// <summary>
        /// Create Voronoi texture for clumping.
        /// </summary>
        private void CreateClumpTexture()
        {
            clumpingVoronoiMaterial.SetFloat("_NumClumpTypes", clumpParameters.Count);
            RenderTexture clumpVoronoiRenderTexture = RenderTexture.GetTemporary(clumpTextureWidth, clumpTextureHeight,
                0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            Graphics.Blit(null, clumpVoronoiRenderTexture, clumpingVoronoiMaterial, 0); // 使用材质渲染Voronoi纹理

            // 将渲染纹理拷贝到一般的Texture2D纹理上面 以供使用
            RenderTexture.active = clumpVoronoiRenderTexture;
            clumpTexture = new Texture2D(clumpTextureWidth, clumpTextureHeight, TextureFormat.RGBAHalf, false, true);
            clumpTexture.filterMode = FilterMode.Point;
            clumpTexture.ReadPixels(new Rect(0, 0, clumpTextureWidth, clumpTextureHeight), 0, 0, true);
            clumpTexture.Apply();
            RenderTexture.active = null;

            RenderTexture.ReleaseTemporary(clumpVoronoiRenderTexture);
        }

        private void UpdateClumpParametersBuffer()
        {
            if (clumpParameters.Count > 0)
            {
                if (clumpParametersArray == null || clumpParametersArray.Length != clumpParameters.Count)
                {
                    clumpParametersArray = new ClumpParameters[clumpParameters.Count];
                }

                clumpParameters.CopyTo(clumpParametersArray);
                clumpParametersBuffer.SetData(clumpParametersArray);
            }
        }

        private void DisposeBuffers()
        {
            DisposeBuffer(grassBladesBuffer);
            DisposeBuffer(meshTrianglesBuffer);
            // DisposeBuffer(meshPositionsBuffer);
            DisposeBuffer(meshColorsBuffer);
            DisposeBuffer(meshUvsBuffer);
            DisposeBuffer(argsBuffer);
            DisposeBuffer(clumpParametersBuffer);
        }

        private void DisposeBuffer(ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Dispose();
                buffer = null;
            }
        }

        private void DestroyClumpTexture()
        {
            if (clumpTexture != null)
            {
                Destroy(clumpTexture);
                clumpTexture = null;
            }
        }

        /// <summary>
        /// 显示需要渲染的Tile的包围盒
        /// </summary>
        private void OnDrawGizmos()
        {
            if (visibleTiles == null) return;

            Color gizmoColor = Color.cyan;
            gizmoColor.a = 0.5f;
            Gizmos.color = gizmoColor;

            foreach (Tile tile in visibleTiles)
            {
                Gizmos.DrawWireCube(tile.bounds.center, tile.bounds.size);
            }
        }
    }
}