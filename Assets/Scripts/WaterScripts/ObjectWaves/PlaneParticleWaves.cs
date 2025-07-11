using UnityEngine;

namespace RCrobotcat_Water_Plane
{
    public class PlaneParticleWaves : MonoBehaviour
    {
        private void OnEnable()
        {
            ParticleSystem particleSystem = GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                WaterPlane.Instance.AddWaveParticle(particleSystem);
            }
        }

        private void OnDisable()
        {
            ParticleSystem particleSystem = GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                if (WaterPlane.Instance)
                    WaterPlane.Instance.RemoveWaveParticle(particleSystem);
            }
        }
    }
}