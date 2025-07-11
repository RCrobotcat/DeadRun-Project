using UnityEngine;
using UnityEngine.Rendering;

namespace RCrobotcat_Water_Plane
{
    public class PlaneDynamicWaves
    {
        private int _cascadeCount = 1; // 级联数量
        private RenderTexture _targets;
        private RenderTextureFormat _textureFormat = RenderTextureFormat.ARGBHalf;
        private int _resolution = -1;
        private bool _needToReadWriteTextureData = false;
        public const int MAX_LOD_COUNT = 15; // 最大LOD数量

        // 用于传递给Shader常量
        private Vector4[] _param_CascadePosScales = new Vector4[MAX_LOD_COUNT + 1]; // 每个级联的位置和缩放的数组
        private Vector4[] _param_CascadeSize = new Vector4[MAX_LOD_COUNT + 1];

        public PlaneDynamicWaves()
        {
        }

        /// <summary>
        /// 创建级联渲染纹理
        /// </summary>
        public static RenderTexture CreateCascadeDataTextures(int count, RenderTextureDescriptor desc, string name,
            bool needToReadWriteTextureData)
        {
            RenderTexture result = new RenderTexture(desc);
            result.wrapMode = TextureWrapMode.Clamp;
            result.antiAliasing = 1;
            result.filterMode = FilterMode.Bilinear;
            result.anisoLevel = 0;
            result.useMipMap = false;
            result.name = name;
            result.dimension = TextureDimension.Tex2DArray;
            result.volumeDepth = count;
            result.enableRandomWrite = needToReadWriteTextureData;
            result.Create();
            return result;
        }

        void InitData()
        {
            var resolution = WaterPlane.Instance.CascadeResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, _textureFormat, 0);
            _targets = CreateCascadeDataTextures(_cascadeCount, desc, "Water Dynamic Wave Data",
                _needToReadWriteTextureData);

            Shader.SetGlobalTexture("Water_DynamicDisplacement", _targets);
        }

        public void Init(int count)
        {
            _cascadeCount = count;

            InitData();
        }

        public void UpdateData()
        {
            int width = WaterPlane.Instance.CascadeResolution;
            if (_resolution == -1)
            {
                _resolution = width;
            }
            else if (width != _resolution)
            {
                _targets.Release();
                _targets.width = _targets.height = _resolution;
                _targets.Create();

                _resolution = width;
            }

            var lt = WaterPlane.Instance._cascadeTransform;
            for (int i = 0; i < _cascadeCount; i++)
            {
                _param_CascadePosScales[i] = new Vector4(
                    lt._renderData[i]._posSnapped.x,
                    lt._renderData[i]._posSnapped.z,
                    WaterPlane.Instance.CalcLodScale(i),
                    0f);

                _param_CascadeSize[i] = new Vector4(
                    lt._renderData[i]._texelWidth,
                    lt._renderData[i]._textureRes,
                    1f,
                    1f / lt._renderData[i]._textureRes);
            }

            _param_CascadePosScales[_cascadeCount] = _param_CascadePosScales[_cascadeCount - 1];
            _param_CascadeSize[_cascadeCount] = _param_CascadeSize[_cascadeCount - 1];
            _param_CascadeSize[_cascadeCount].z = 0f;
        }

        /// <summary>
        /// 渲染动态波浪纹理
        /// </summary>
        void SubmitDynamicDraws(int id, CommandBuffer cmd)
        {
            var lt = WaterPlane.Instance._cascadeTransform;

            lt.SetViewProjectionMatrices(id, cmd);

            foreach (var particle in WaterPlane.Instance.GetWaveParticles())
            {
                cmd.DrawRenderer(particle.GetComponent<ParticleSystemRenderer>(),
                    particle.GetComponent<ParticleSystemRenderer>().sharedMaterial, 0, 0);
            }
        }

        public void BuildCommandBuffer(CommandBuffer cmd)
        {
            for (int i = _cascadeCount - 1; i >= 0; i--)
            {
                cmd.SetRenderTarget(_targets, _targets, 0, CubemapFace.Unknown, i);
                cmd.ClearRenderTarget(false, true, new Color(0f, 0f, 0f, 0f));
                SubmitDynamicDraws(i, cmd);
            }
        }

        /// <summary>
        /// 传递Shader常量
        /// </summary>
        public void SetGlobalShaderVariables()
        {
            Shader.SetGlobalVectorArray("Water_CascadePosScale", _param_CascadePosScales);
            Shader.SetGlobalVectorArray("Water_CascadeSize", _param_CascadeSize);
        }
    }
}