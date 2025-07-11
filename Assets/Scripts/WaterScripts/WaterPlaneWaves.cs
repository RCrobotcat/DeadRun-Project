using UnityEngine;

namespace RCrobotcat_Water_Plane
{
    public struct WaveStruct
    {
        public Vector3 position;
        public Vector3 normal;

        public WaveStruct(Vector3 position, Vector3 normal)
        {
            this.position = position;
            this.normal = normal;
        }

        public void Clear()
        {
            position = Vector3.zero;
            normal.Set(0, 1, 0);
        }
    }

    public partial class WaterPlane
    {
        [SerializeField] PlaneWavesSetting wavesSettings;

        int waveCount = 0;

        private WaveStruct waveOut;

        public void UpdateWaves()
        {
            wavesSettings.UpdateWavesData();
            waveCount = wavesSettings.GetWaveCount();

            if (waveCount > 0)
            {
                // 将波浪数据传递给shader(GPU) Pass wave data to shader(GPU)
                Shader.SetGlobalInt("_RCWaveCount", waveCount);
                Shader.SetGlobalVectorArray("RCwaveData", wavesSettings.wavesData);
            }
        }

        /// <summary>
        /// 计算格斯特纳波浪 
        /// Calculate Gerstner wave
        /// </summary>
        public WaveStruct GerstnerWave(Vector2 pos, float waveCountMulti, float amplitude, float direction,
            float wavelength)
        {
            WaveStruct waveOut = new WaveStruct(Vector3.zero, Vector3.zero);

            float time = Time.time;

            float w = 6.28318f / wavelength;
            float wSpeed = Mathf.Sqrt(9.8f * w);
            float peak = 0.8f;
            float qi = peak / (amplitude * w * waveCountMulti);

            direction = Mathf.Deg2Rad * direction; // convert degrees to radians

            Vector2 dirWaveInput = new Vector2(Mathf.Sin(direction), Mathf.Cos(direction));
            Vector2 windDir = dirWaveInput.normalized;
            float dir = Vector2.Dot(windDir, pos);

            float calc = dir * w + -time * wSpeed;
            float cosCalc = Mathf.Cos(calc);
            float sinCalc = Mathf.Sin(calc);

            Vector3 wave = new Vector3(
                qi * amplitude * windDir.x * cosCalc,
                (sinCalc * amplitude) * waveCountMulti,
                qi * amplitude * windDir.y * cosCalc
            );

            Vector3 n = new Vector3(
                -(windDir.x * w * amplitude * cosCalc),
                1 - (qi * w * amplitude * sinCalc),
                -(windDir.y * w * amplitude * cosCalc)
            ).normalized;

            waveOut.position = wave;
            waveOut.normal = n * waveCountMulti;

            return waveOut;
        }

        public void SampleWaves(Vector3 position, out WaveStruct waveOut)
        {
            Vector2 pos = new Vector2(position.x, position.z);
            waveOut = new WaveStruct(Vector3.zero, Vector3.zero);
            float waveCountMulti = 1.0f / waveCount;

            for (int i = 0; i < waveCount; i++)
            {
                Wave w = wavesSettings.waves[i];
                WaveStruct wave = GerstnerWave(pos, waveCountMulti, w.amplitude, w.direction, w.wavelength);

                waveOut.position += wave.position;
                waveOut.normal += wave.normal;
            }
        }

        /// <summary>
        /// 返回某个点的水面位移
        /// Return the water displacement of a point
        /// </summary>
        public Vector3 GetWaterDisplacement(Vector3 position)
        {
            waveOut.Clear();

            SampleWaves(position, out waveOut);

            return waveOut.position;
        }

        /// <summary>
        /// 返回某个点的水面高度
        /// Return the water height of a point
        /// </summary>
        public float GetWaterHeight(Vector3 position)
        {
            Vector3 displacement = GetWaterDisplacement(position);
            displacement = GetWaterDisplacement(position - displacement);
            displacement = GetWaterDisplacement(position - displacement);

            return GetWaterDisplacement(position - displacement).y;
        }
    }
}