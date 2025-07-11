using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace RCrobotcat_Water_Plane
{
    public partial class WaterPlane
    {
        [SerializeField] private ColorPreset_SO colorsPreset;

        private Texture2D rampTexture; // 生成LUT的2D纹理

        /// <summary>
        /// 初始化LUT纹理
        /// </summary>
        void InitLUT()
        {
            if (!rampTexture)
                GenerateColorRamp();

            // 将纹理设置给 shader
            Shader.SetGlobalTexture("_AbsorptionScatteringRamp", rampTexture);
        }

        /// <summary>
        /// 销毁LUT纹理
        /// </summary>
        private void DestroyLUT()
        {
            if (rampTexture)
            {
                if (Application.isEditor)
                    DestroyImmediate(rampTexture);
                else
                    Destroy(rampTexture);
            }
        }


        /// <summary>
        /// 生成查询纹理
        /// Generate lookup texture(LUT)
        /// </summary>
        void GenerateColorRamp()
        {
            if (rampTexture == null)
                rampTexture = new Texture2D(128, 2, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);

            rampTexture.wrapMode = TextureWrapMode.Clamp;

            // 将颜色填充进256个像素中
            var cols = new Color[256];
            for (var i = 0; i < 128; i++)
            {
                cols[i] = colorsPreset.absorptionRamp.Evaluate(i / 128f);
            }

            for (var i = 0; i < 128; i++)
            {
                cols[i + 128] = colorsPreset.scatterRamp.Evaluate(i / 128f);
            }

            rampTexture.SetPixels(cols);
            rampTexture.Apply();
        }
    }
}