#ifndef GERSTNER_WAVES_INCLUDED
#define GERSTNER_WAVES_INCLUDED

uint _RCWaveCount;

// x: amplitude, y: direction, z: wavelength
half4 RCwaveData[20];

struct WaveStruct {
	float3 position;
	float3 normal;
};

/// <summary>
/// ����ʵ�ʵ�Gerstner��
/// �ĵ�: https://developer.nvidia.com/gpugems/gpugems/part-i-natural-effects/chapter-1-effective-water-simulation-physical-models
/// </summary>
WaveStruct GerstnerWave(half2 pos, float waveCountMulti, half amplitude, half direction, half wavelength) {
	WaveStruct waveOut;
	half3 wave = 0;

	// ��ȡ��ǰ��ʱ��
	float time = _Time.y;

	// �����Ƶ��: 2 * PI / ����
	float w = 6.28318 / wavelength;

	// ���㲨��
	float wSpeed = sqrt(9.8 * w);

	// ��������, Խ��Խ����(��ò�����1)
	half peak = 0.8;
	// ���㹫ʽ��Qi��
	half qi = peak / (amplitude * w * _RCWaveCount);

	// ת�ɻ���
	direction = radians(direction);
	// ���㷽������(xzƽ��)
	half2 dirWaveInput = half2(sin(direction), cos(direction));
	// ����������һ����Ϊ��ķ���
	half2 windDir = normalize(dirWaveInput);

	// ���㹫ʽ�з緽���λ�õĵ��
	half dir = dot(windDir, pos);

	// ���ݹ�ʽ
	half calc = dir * w + -time * wSpeed;
	half cosCalc = cos(calc);
	half sinCalc = sin(calc);

	// ˮƽλ��
	wave.xz = qi * amplitude * windDir.xy * cosCalc;
	// ��ֱλ��
	wave.y = ((sinCalc * amplitude)) * waveCountMulti;

	// ���㷨�߹�ʽwa��
	half wa = w * amplitude;
	// ��������
	half3 n = half3(-(windDir.xy * wa * cosCalc), 1 - (qi * wa * sinCalc));

	waveOut.position = wave;
	waveOut.normal = (n.xzy * waveCountMulti);

	return waveOut;
}

/// <summary>
/// ����ĳһ��λ�õ�Gerstner����λ�ƺͷ��ߵ��ܹ���
/// </summary>
void SampleWaves(float3 position, out WaveStruct waveOut) {
	half2 pos = position.xz;

	waveOut.position = 0;
	waveOut.normal = 0;

	half waveCountMulti = 1.0 / _RCWaveCount;

	UNITY_LOOP
		for (uint i = 0; i < _RCWaveCount; i++) {
			float amplitude = RCwaveData[i].x;
			float direction = RCwaveData[i].y;
			float wavelength = RCwaveData[i].z;

			WaveStruct wave = GerstnerWave(pos, waveCountMulti, amplitude, direction, wavelength);

			waveOut.position += wave.position;
			waveOut.normal += wave.normal;
		}
}

#endif // GERSTNER_WAVE_INCLUDED ͷ�ļ�
