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
/// 计算实际的Gerstner波
/// 文档: https://developer.nvidia.com/gpugems/gpugems/part-i-natural-effects/chapter-1-effective-water-simulation-physical-models
/// </summary>
WaveStruct GerstnerWave(half2 pos, float waveCountMulti, half amplitude, half direction, half wavelength) {
	WaveStruct waveOut;
	half3 wave = 0;

	// 获取当前的时间
	float time = _Time.y;

	// 计算角频率: 2 * PI / 波长
	float w = 6.28318 / wavelength;

	// 计算波速
	float wSpeed = sqrt(9.8 * w);

	// 计算尖锐度, 越大越尖锐(最好不超过1)
	half peak = 0.8;
	// 计算公式的Qi项
	half qi = peak / (amplitude * w * _RCWaveCount);

	// 转成弧度
	direction = radians(direction);
	// 计算方向向量(xz平面)
	half2 dirWaveInput = half2(sin(direction), cos(direction));
	// 方向向量归一化作为风的方向
	half2 windDir = normalize(dirWaveInput);

	// 计算公式中风方向和位置的点乘
	half dir = dot(windDir, pos);

	// 根据公式
	half calc = dir * w + -time * wSpeed;
	half cosCalc = cos(calc);
	half sinCalc = sin(calc);

	// 水平位移
	wave.xz = qi * amplitude * windDir.xy * cosCalc;
	// 垂直位移
	wave.y = ((sinCalc * amplitude)) * waveCountMulti;

	// 计算法线公式wa项
	half wa = w * amplitude;
	// 法线向量
	half3 n = half3(-(windDir.xy * wa * cosCalc), 1 - (qi * wa * sinCalc));

	waveOut.position = wave;
	waveOut.normal = (n.xzy * waveCountMulti);

	return waveOut;
}

/// <summary>
/// 计算某一个位置的Gerstner波对位移和法线的总贡献
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

#endif // GERSTNER_WAVE_INCLUDED 头文件
