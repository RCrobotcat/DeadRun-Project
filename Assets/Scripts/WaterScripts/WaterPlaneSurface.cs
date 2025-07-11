using UnityEngine;

namespace RCrobotcat_Water_Plane
{
    public partial class WaterPlane
    {
        [Header("Material")] [Tooltip("Assign this material to the plane's MeshRenderer to display the water shader")]
        public Material waterMaterial;

        MeshFilter _meshFilter;

        [SerializeField] Transform viewer;

        void InitGeometry()
        {
            if (viewer == null)
                viewer = Camera.main.transform;

            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
            {
                Debug.LogError("[WaterSurface] 缺少 MeshFilter，请确保此脚本挂在一个带 MeshFilter 的平面对象上。");
                enabled = false;
                return;
            }
            
            if (waterMaterial != null)
            {
                var mr = GetComponent<MeshRenderer>();
                mr.material = waterMaterial;
            }
        }
        
        public void SetView(Transform transform)
        {
            if (transform)
                viewer = transform;
        }
        
        public Transform GetViewer()
        {
            return viewer;
        }
    }
}