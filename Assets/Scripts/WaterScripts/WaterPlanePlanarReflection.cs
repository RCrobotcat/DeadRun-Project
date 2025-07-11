using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RCrobotcat_Water_Plane
{
    public class WaterPlanePlanarReflection : MonoBehaviour
    {
        [Header("Reflection Settings")]
        // 反射质量 Reflection quality
        // 影响反射纹理的分辨率，值越大分辨率越高
        // The resolution of the reflection texture is affected, the higher the value, the higher the resolution
        [Range(0.1f, 1.0f)]
        public float reflectionQuality = 0.5f;

        public float farClipPlane = 100f; // 反射相机的远裁剪面距离 The distance of the far clipping plane of the reflection camera
        public LayerMask reflectionLayers = -1; // all layers

        private Camera reflectionCamera; // 反射相机 The reflection camera
        private GameObject reflectionCameraGo;
        private readonly Dictionary<Camera, RenderTexture> cameraTextures = new Dictionary<Camera, RenderTexture>();

        void OnEnable()
        {
            // RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void Update()
        {
            InitializeReflectionCamera();

            var normal = GetReflectionNormal();
            ConfigureReflectionCamera(Camera.main);
            UpdateRenderTexture(Camera.main);
            UpdateReflectionCameraTransform(Camera.main, normal);
            SetupObliqueProjectionMatrix(normal);

            RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();

            if (RenderPipeline.SupportsRenderRequest(reflectionCamera, request))
            {
                // 2D Texture
                request.destination = cameraTextures[Camera.main];
                // Render camera and fill texture2D with its view
                RenderPipeline.SubmitRenderRequest(reflectionCamera, request);
            }

            var textureName = "_PlanarReflectionsTex";
            reflectionCamera.targetTexture.SetGlobalShaderProperty(textureName);
        }

        void OnDisable()
        {
            CleanUp();
            // RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        void OnDestroy()
        {
            CleanUp();
        }

        /// <summary>
        /// 反射相机渲染逻辑
        /// Reflection camera rendering logic
        /// </summary>
        /* void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
         {
             if (ShouldSkipCamera(camera)) return;

             InitializeReflectionCamera();

             var normal = GetReflectionNormal();
             ConfigureReflectionCamera(camera);
             UpdateRenderTexture(camera);
             UpdateReflectionCameraTransform(camera, normal);
             SetupObliqueProjectionMatrix(normal);

             UniversalRenderPipeline.RenderSingleCamera(context, reflectionCamera);

             var textureName = "_PlanarReflectionsTex";
             reflectionCamera.targetTexture.SetGlobalShaderProperty(textureName);
         }*/

        /// <summary>
        /// 初始化反射相机
        /// Initialize reflection camera
        /// </summary>
        void InitializeReflectionCamera()
        {
            if (reflectionCamera != null) return;

            reflectionCameraGo = new GameObject("ReflectionCamera", typeof(Camera));
            reflectionCameraGo.hideFlags = HideFlags.HideAndDontSave;
            reflectionCamera = reflectionCameraGo.GetComponent<Camera>();
            reflectionCamera.enabled = false;
        }

        /// <summary>
        /// 设置反射相机的参数
        /// Configure the parameters of the reflection camera
        /// </summary>
        void ConfigureReflectionCamera(Camera sourceCam)
        {
            reflectionCamera.CopyFrom(sourceCam);
            reflectionCamera.cameraType = CameraType.Reflection;
            reflectionCamera.usePhysicalProperties = false;
            reflectionCamera.farClipPlane = farClipPlane;
            reflectionCamera.cullingMask = reflectionLayers;
            reflectionCamera.clearFlags = sourceCam.clearFlags;
        }

        /// <summary>
        /// 如果是反射相机或者预览相机则不渲染平面反射
        /// If it is a reflection camera or a preview camera, the planar reflection is not rendered
        /// </summary>
        bool ShouldSkipCamera(Camera camera)
        {
            return camera.cameraType == CameraType.Reflection
                   || camera.cameraType == CameraType.Preview;
        }

        /// <summary>
        /// 更新反射相机的渲染目标纹理
        /// Set the render target texture of the reflection camera
        /// </summary>
        private void UpdateRenderTexture(Camera sourceCamera)
        {
            int width = Mathf.RoundToInt(sourceCamera.pixelWidth * reflectionQuality);
            int height = Mathf.RoundToInt(sourceCamera.pixelHeight * reflectionQuality);

            if (!cameraTextures.TryGetValue(sourceCamera, out RenderTexture texture) ||
                texture == null ||
                texture.width != width ||
                texture.height != height)
            {
                if (texture != null)
                {
                    texture.Release();
                    cameraTextures.Remove(sourceCamera);
                }

                texture = new RenderTexture(width, height, 24)
                {
                    name = $"PlanarReflection_{sourceCamera.name}",
                    autoGenerateMips = true
                };
                texture.Create();

                cameraTextures[sourceCamera] = texture;
            }

            reflectionCamera.targetTexture = texture;
        }

        /// <summary>
        /// 更新反射相机的Transform
        /// Update the Transform of the reflection camera
        /// </summary>
        private void UpdateReflectionCameraTransform(Camera sourceCamera, Vector3 normal)
        {
            Vector3 proj = normal * Vector3.Dot(
                normal, sourceCamera.transform.position - transform.position);

            reflectionCamera.transform.position = sourceCamera.transform.position - 2 * proj;

            Vector3 forward = Vector3.Reflect(sourceCamera.transform.forward, normal);
            Vector3 up = Vector3.Reflect(sourceCamera.transform.up, normal);
            reflectionCamera.transform.LookAt(
                reflectionCamera.transform.position + forward, up);
        }

        /// <summary>
        /// 获取反射平面的法线
        /// Get the normal of the reflection plane
        /// </summary>
        /// <returns></returns>
        Vector3 GetReflectionNormal()
        {
            return transform.up;
        }

        /// <summary>
        /// 设置斜投影矩阵
        /// Set the oblique projection matrix
        /// </summary>
        void SetupObliqueProjectionMatrix(Vector3 normal)
        {
            Matrix4x4 viewMatrix = reflectionCamera.worldToCameraMatrix;
            Vector3 viewPosition = viewMatrix.MultiplyPoint(transform.position);
            Vector3 viewNormal = viewMatrix.MultiplyVector(normal).normalized;

            Vector4 plane = new Vector4(viewNormal.x, viewNormal.y, viewNormal.z,
                -Vector3.Dot(viewPosition, viewNormal));

            // 传入视锥空间下的投影平面得到斜投影矩阵
            // Passing the projection plane in view space to get the oblique projection matrix
            reflectionCamera.projectionMatrix = reflectionCamera.CalculateObliqueMatrix(plane);
        }

        /// <summary>
        /// 清除所有的渲染纹理和反射摄像机
        /// Clear all rendering textures and reflection cameras
        /// </summary>
        void CleanUp()
        {
            foreach (var texture in cameraTextures.Values)
            {
                texture.Release();
            }

            cameraTextures.Clear();

            if (reflectionCamera == null) return;

            if (Application.isEditor)
                DestroyImmediate(reflectionCameraGo);
            else Destroy(reflectionCameraGo);
        }
    }
}