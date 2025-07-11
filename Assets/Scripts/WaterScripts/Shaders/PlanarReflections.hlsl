#ifndef PLANAR_REFLECTIONS_INCLUDED
#define PLANAR_REFLECTIONS_INCLUDED

TEXTURE2D(_PlanarReflectionsTex);
SAMPLER(sampler_PlanarReflectionsTex);

half4 SamplePlanarReflections(float2 screenUV)
{
    float2 uv = screenUV;
    uv.x = 1 - uv.x;
    return SAMPLE_TEXTURE2D(_PlanarReflectionsTex, sampler_PlanarReflectionsTex, uv);
}

#endif // PLANAR_REFLECTIONS_INCLUDED
