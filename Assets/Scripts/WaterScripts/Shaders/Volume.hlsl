#ifndef VOLUME_INCLUDED
#define VOLUME_INCLUDED

// Surface textures
TEXTURE2D(_AbsorptionScatteringRamp); 
SAMPLER(sampler_AbsorptionScatteringRamp);

// ���պ��� Absorption
// ����0��1����Ƚ��в�ѯ
half3 Absorption(half depth)
{
	return SAMPLE_TEXTURE2D(_AbsorptionScatteringRamp, sampler_AbsorptionScatteringRamp, half2(depth, 0.0)).rgb;
}

// ɢ�� Scattering
// ����0��1����Ƚ��в�ѯ
half3 Scattering(half depth)
{
	return SAMPLE_TEXTURE2D(_AbsorptionScatteringRamp, sampler_AbsorptionScatteringRamp, half2(depth, 0.8)).rgb;
}

#endif // VOLUME_INCLUDED