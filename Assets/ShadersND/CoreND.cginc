//Minimum shader version required
#pragma target 4.0

//Conditional compilation
#pragma multi_compile_instancing
#ifndef IS_5D
#pragma multi_compile IS_4D IS_5D
#endif
#pragma multi_compile __ IS_EDITOR IS_EDITOR_V
#if !defined(FOG) && !defined(NO_FOG)
#pragma multi_compile __ FOG
#endif
#if !defined(SHADOW) && !defined(USE_DITHER)
#pragma shader_feature __ USE_DITHER
#endif

//Define shader function names
#pragma vertex vert
#pragma fragment frag

//Instancing options
#pragma instancing_options assumeuniformscaling

//Optional definition overrides
#ifndef SPEC_POWER
#define SPEC_POWER 20
#endif
#if defined(VERTEX_AO) && !defined(VERTEX_AO_SKIP_MUL)
#define VERTEX_AO_MUL
#endif

//Unity includes
#include "UnityCG.cginc"

//Dither lookup table
static const float dither[16] = {
  0.0625, 0.5625, 0.1875,  0.6875,
  0.8125, 0.3125, 0.9375,  0.4375,
    0.25,   0.75,  0.125,   0.625,
     1.0,    0.5,  0.875,   0.375
};

//Useful for many shaders
static const float4x4 randRot = float4x4(
  0.36, 0.48, -0.8, 0.5,
  -0.8, 0.6, 0.0, -0.5,
  0.48, 0.64, 0.6, 0.0,
  0.0, 0.0, 0.0, 0.0);

//Common globals
uniform sampler2D _LUT;
uniform sampler3D _NOISE;
uniform float _DitherDist;
uniform float _DitherRadius;
uniform float _MinChecker;
uniform float _ShadowOpacity;
#ifdef FOG
uniform float _FogLevel;
#endif
#ifdef SHADOW
#ifndef KEEP_SHADOW_COLOR
uniform float4 _ShadowColor1;
uniform float4 _ShadowColor2;
#endif
#endif

//Common locals
#define DEFINE_COMMON_LOCALS \
  UNITY_DEFINE_INSTANCED_PROP(float4, _Color) \
  UNITY_DEFINE_INSTANCED_PROP(float, _ShadowDist) \
  UNITY_DEFINE_INSTANCED_PROP(float, _Ambient) \
  UNITY_DEFINE_INSTANCED_PROP(float, _SpecularMul) \
  UNITY_DEFINE_INSTANCED_PROP(float, _SpecularPow)

//Include sky for light and reflections
#include "SkyND.cginc"

//Include the correct core
#ifdef IS_5D
#include "Core5D.cginc"
#else
#include "Core4D.cginc"
#endif
