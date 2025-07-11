#ifndef LIGHTING_INCLUDED
#define LIGHTING_INCLUDED

half3 SampleReflections(half3 normalWS, half3 viewDirectionWS, half2 screenUV)
{
    half3 reflection = 0;

    half2 reflectionUV = screenUV + normalWS.zx * half2(0.02, 0.15);
    half3 planarReflections = SamplePlanarReflections(reflectionUV).rgb;

    reflection = planarReflections;
    return reflection;
}

#endif // LIGHTING_INCLUDED
