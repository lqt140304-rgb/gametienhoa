#ifndef EVOLVE_SIGHT_INCLUDED
#define EVOLVE_SIGHT_INCLUDED

// Set every frame by MonochromeVision: _EvolveBlind 0 = full sight (also the value when nothing sets it), 1 = blind;
// _EvolveSightCenter = the slime's position (xyz) and the radius of the small area it can still feel (w; during a vibration scan the scan's).
float _EvolveBlind;
float4 _EvolveSightCenter;

// 1 where something may be seen, falling smoothly to 0 away from the slime while it is blind. Smooth on purpose: the light seems to
// spread softly out of the slime instead of cutting off at a ring.
float EvolveSightVisible(float3 positionWS)
{
    float near = _EvolveSightCenter.w > 0.0 ? saturate(1.0 - distance(positionWS, _EvolveSightCenter.xyz) / _EvolveSightCenter.w) : 0.0;
    near = near * near * (3.0 - 2.0 * near);
    near = near * near;
    return 1.0 - saturate(_EvolveBlind) * (1.0 - near);
}

#endif
