using UnityEngine;
using UnityEngine.Rendering;

namespace RCrobotcat_Water_Plane
{
    public class PlaneCascadeTransform
    {
        [System.Serializable]
        public struct RenderData // 单个级联渲染数据结构
        {
            public float _texelWidth; // 级联纹理每个像素对应的世界空间尺寸

            public float _textureRes; // 级联纹理分辨率

            public Vector3 _posSnapped; // 级联纹理在世界空间中的位置

            // public Rect RectXZ
            // {
            //     get
            //     {
            //         float w = _texelWidth * _textureRes;
            //         return new Rect(_posSnapped.x - w / 2f, _posSnapped.z - w / 2f, w, w);
            //     }
            // }
        }

        public RenderData[] _renderData = null;
        public int CascadeCount { get; private set; }

        Matrix4x4[] _worldToCameraMatrix; // view矩阵数组
        Matrix4x4[] _projectionMatrix; // projection矩阵数组

        /// <summary>
        /// 返回特定级联的View矩阵
        /// </summary>
        /// <param name="lodIdx"></param>
        /// <returns></returns>
        public Matrix4x4 GetWorldToCameraMatrix(int lodIdx)
        {
            return _worldToCameraMatrix[lodIdx];
        }

        /// <summary>
        /// 返回特定级联的Projection矩阵
        /// </summary>
        /// <param name="lodIdx"></param>
        /// <returns></returns>
        public Matrix4x4 GetProjectionMatrix(int lodIdx)
        {
            return _projectionMatrix[lodIdx];
        }

        public void InitCascadeData(int count)
        {
            CascadeCount = count;

            _renderData = new RenderData[count];
            _worldToCameraMatrix = new Matrix4x4[count];
            _projectionMatrix = new Matrix4x4[count];
        }

        /// <summary>
        /// 计算View矩阵
        /// </summary>
        public static Matrix4x4 CalculateWorldToCameraMatrixRHS(Vector3 position, Quaternion rotation)
        {
            // 逆矩阵将世界空间转换到局部空间
            // 由于Unity遵循OpenGL的约定(相机的前方是-Z轴)，而不是DirectX的约定(相机的前方是+Z轴)
            return Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(position, rotation, Vector3.one).inverse;
        }

        /// <summary>
        /// 更新级联的View和Projection矩阵
        /// </summary>
        /// <summary>
        /// 更新级联的View和Projection矩阵
        /// </summary>
        public void UpdateTransforms()
        {
            Vector3 planeCenter = WaterPlane.Instance.transform.position;

            for (int lodIdx = 0; lodIdx < CascadeCount; lodIdx++)
            {
                float lodScale = WaterPlane.Instance.CalcLodScale(lodIdx);
                float camOrthSize = lodScale; // 级联范围半径

                _renderData[lodIdx]._textureRes = WaterPlane.Instance.CascadeResolution;
                _renderData[lodIdx]._texelWidth = 2f * camOrthSize / _renderData[lodIdx]._textureRes;

                // 按级联范围对齐纹理
                float snappedX = planeCenter.x - Mathf.Repeat(planeCenter.x, _renderData[lodIdx]._texelWidth);
                float snappedZ = planeCenter.z - Mathf.Repeat(planeCenter.z, _renderData[lodIdx]._texelWidth);
                _renderData[lodIdx]._posSnapped = new Vector3(snappedX, planeCenter.y, snappedZ);

                // 相机放在水面上方，朝下看
                Vector3 camPos = _renderData[lodIdx]._posSnapped + Vector3.up * 100f;
                Quaternion camRot = Quaternion.AngleAxis(90f, Vector3.right);

                _worldToCameraMatrix[lodIdx] = CalculateWorldToCameraMatrixRHS(camPos, camRot);
                _projectionMatrix[lodIdx] = Matrix4x4.Ortho(
                    -camOrthSize, camOrthSize,
                    -camOrthSize, camOrthSize,
                    1f, 200f
                );
            }
        }


        public void SetViewProjectionMatrices(int lodIdx, CommandBuffer cmd)
        {
            cmd.SetViewProjectionMatrices(GetWorldToCameraMatrix(lodIdx), GetProjectionMatrix(lodIdx));
        }
    }
}