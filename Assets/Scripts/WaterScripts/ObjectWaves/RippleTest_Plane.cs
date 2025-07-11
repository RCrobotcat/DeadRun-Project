using UnityEngine;

namespace RCrobotcat_Water_Plane
{
    public class RippleTest_Plane : MonoBehaviour
    {
        public float speedEmitRate = 10;

        [Range(0.1f, 10)] public float maxFadeDepth = 2;

        ParticleSystem ps;
        Vector3 oldPosition = Vector3.zero;

        void Start()
        {
            ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var mainModule = ps.main;
                mainModule.loop = true;
            }

            oldPosition = transform.position;
        }

        void Update()
        {
            // 根据粒子的移动速度来生成粒子
            if (ps != null)
            {
                Vector3 horizonMovement = transform.position - oldPosition;
                horizonMovement.y = 0;

                float submergeFade = Mathf.Clamp01(1 - Mathf.Abs(transform.position.y / maxFadeDepth));

                float speed = horizonMovement.magnitude / Time.deltaTime;
                float emitRate = speed * speedEmitRate * submergeFade;

                var emissionModule = ps.emission;
                emissionModule.rateOverTime = emitRate;

                oldPosition = transform.position;
            }
        }
    }
}