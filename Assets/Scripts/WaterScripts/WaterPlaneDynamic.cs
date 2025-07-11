using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RCrobotcat_Water_Plane
{
    public partial class WaterPlane
    {
        public float _dynamicScale = 10;
        public int _cascadeCount = 1; // 级联数量

        private PlaneDynamicWaves _waveManager;

        private List<ParticleSystem> _waveParticles = new List<ParticleSystem>();

        // 级联对应的渲染纹理大小
        public enum CascadeResolutionLevel
        {
            RES_256x256,
            RES_512x512,
            RES_1024x1024,
        }

        [SerializeField] public CascadeResolutionLevel _cascadeResolution = CascadeResolutionLevel.RES_512x512;

        public int CascadeResolution
        {
            get
            {
                switch (_cascadeResolution)
                {
                    case CascadeResolutionLevel.RES_256x256:
                        return 256;
                    case CascadeResolutionLevel.RES_512x512:
                        return 512;
                    case CascadeResolutionLevel.RES_1024x1024:
                        return 1024;
                }

                return 512;
            }
        }

        public float Scale
        {
            get { return _dynamicScale; }
            set { _dynamicScale = value; }
        }

        /// <summary>
        /// 返回特定级联的LOD尺寸大小
        /// </summary>
        /// <param name="lodIndex">级联索引</param>
        /// <returns></returns>
        public float CalcLodScale(float lodIndex)
        {
            // 相邻级联之间的LOD尺寸大小相差2倍
            return Scale * Mathf.Pow(2f, lodIndex);
        }

        [HideInInspector] public PlaneCascadeTransform _cascadeTransform;

        public void InitDynamicWaves()
        {
            if (_cascadeTransform == null)
            {
                _cascadeTransform = new PlaneCascadeTransform();
                _cascadeTransform.InitCascadeData(_cascadeCount);
            }

            if (_waveManager == null)
            {
                _waveManager = new PlaneDynamicWaves();
                _waveManager.Init(_cascadeCount);
            }
        }

        public void UpdateDynamicWaves()
        {
            CommandBuffer cmd = CommandBufferPool.Get("Water Dynamic Simulation");

            _cascadeTransform?.UpdateTransforms();

            _waveManager?.UpdateData();
            _waveManager?.SetGlobalShaderVariables();
            _waveManager?.BuildCommandBuffer(cmd);

            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public List<ParticleSystem> GetWaveParticles()
        {
            return _waveParticles;
        }

        public void AddWaveParticle(ParticleSystem particle)
        {
            _waveParticles.Add(particle);
        }

        public void RemoveWaveParticle(ParticleSystem particle)
        {
            _waveParticles.Remove(particle);
        }
    }
}