using UnityEngine;

namespace RCrobotcat_Water_Plane
{
    [System.Serializable]
    public struct Wave
    {
        public float amplitude;
        public float direction;
        public float wavelength;

        public Wave(float amp, float dir, float length)
        {
            amplitude = amp;
            direction = dir;
            wavelength = length;
        }
    }

    [CreateAssetMenu(fileName = "RC Plane Waves Settings", menuName = "RCrobotcat/Plane Waves Settings")]
    public class PlaneWavesSetting : ScriptableObject
    {
        public Wave[] waves;

        [HideInInspector] public Vector4[] wavesData = new Vector4[6];

        public int GetWaveCount()
        {
            if (waves.Length < 6)
                return waves.Length;

            return 6;
        }

        public void UpdateWavesData()
        {
            for (int i = 0; i < waves.Length; i++)
            {
                if (i >= 6) break;

                wavesData[i].Set(waves[i].amplitude, waves[i].direction, waves[i].wavelength, 0);
            }
        }
    }
}