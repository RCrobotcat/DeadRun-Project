using UnityEngine;
using Unity.Cinemachine;

public class PaintingShooting : MonoBehaviour
{
    [SerializeField] ParticleSystem inkParticle;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            inkParticle.Play();
        else if (Input.GetMouseButtonUp(0))
            inkParticle.Stop();
    }
}