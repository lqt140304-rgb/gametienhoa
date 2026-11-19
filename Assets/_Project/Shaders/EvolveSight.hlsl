#ifndef EVOLVE_SIGHT_INCLUDED
#define EVOLVE_SIGHT_INCLUDED

// Set every frame by MonochromeVision: _EvolveBlind 0 = full sight (also the value when nothing sets it), 1 = blind;
// _EvolveSightCenter = the slime's position (xyz) and the radius of the small area it can still feel (w; during a vibration scan the scan's).
float _EvolveBlind;
float4 _EvolveSightCenter;
// The vibration scan as a sound wave: xyz = where it started, w = the radius of its front now; _EvolveWaveFade 1..0 (0 = no wave).
float4 _EvolveWave;
float _EvolveWaveFade;

// 1 where something may be seen, falling smoothly to 0 away from the slime while it is blind. Smooth on purpose: the light seems to
// spread softly out of the slime instead of cutting off at a ring.
// 1 on the wave front, falling to 0 inside and outside it: only what the ring is passing through shows.
float EvolveWaveVisible(float3 positionWS)
{
    if (_EvolveWave.w <= 0.0 || _EvolveWaveFade <= 0.0) return 0.0;
    float width = max(1.2, 0.1 * _EvolveWave.w);
    float x = (distance(positionWS, _EvolveWave.xyz) - _EvolveWave.w) / width;
    return exp(-x * x) * _EvolveWaveFade;
}

float EvolveSightVisible(float3 positionWS)
{
    float near = _EvolveSightCenter.w > 0.0 ? saturate(1.0 - distance(positionWS, _EvolveSightCenter.xyz) / _EvolveSightCenter.w) : 0.0;
    near = near * near * (3.0 - 2.0 * near);
    near = near * near;
    return max(1.0 - saturate(_EvolveBlind) * (1.0 - near), EvolveWaveVisible(positionWS));
}

#endif
