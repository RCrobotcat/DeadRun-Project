#ifndef VOLUME_INCLUDED
#define VOLUME_INCLUDED

// Surface textures
TEXTURE2D(_AbsorptionScatteringRamp); 
SAMPLER(sampler_AbsorptionScatteringRamp);

// 吸收函数 Absorption
// 基于0到1的深度进行查询
half3 Absorption(half depth)
{
	return SAMPLE_TEXTURE2D(_AbsorptionScatteringRamp, sampler_AbsorptionScatteringRamp, half2(depth, 0.0)).rgb;
}

// 散射 Scattering
// 基于0到1的深度进行查询
half3 Scattering(half depth)
{
	return SAMPLE_TEXTURE2D(_AbsorptionScatteringRamp, sampler_AbsorptionScatteringRamp, half2(depth, 0.8)).rgb;
}

#endif // VOLUME_INCLUDED