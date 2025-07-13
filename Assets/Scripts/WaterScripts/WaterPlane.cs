using UnityEngine;

namespace RCrobotcat_Water_Plane
{
    public partial class WaterPlane : MonoBehaviour
    {
        private static WaterPlane instance = null;

        public static WaterPlane Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<WaterPlane>();

                    if (instance == null)
                    {
                        Debug.Log("Water is not exist!");
                    }
                }

                return instance;
            }
        }

        float waterTime = 0;

        void Awake()
        {
            instance = (WaterPlane)this;

            InitGeometry(); // 初始化网格对象 Init the mesh object
            InitDynamicWaves(); // 初始化动态波浪 Init the dynamic waves
            InitLUT(); // 初始化LUT纹理 Init the LUT texture
        }

        void Update()
        {
            UpdateWaves(); // 更新波浪数据 Update the wave data
            //UpdateDynamicWaves(); // 更新动态波浪 Update the dynamic waves => 高度不为0时会出现bug 暂时不加
        }

        private void OnDestroy()
        {
            DestroyLUT(); // 销毁LUT纹理 Destroy the LUT texture
        }
    }
}