#ifdef IS_5D
#include "Util5D.cginc"
#endif

//Constants
#define T1 -0.1
#define T2 0.05
#define T3 0.3
#define T4 0.6

//Globals
uniform float4 _LightDirA;
uniform float _LightDirV;
uniform float4 _LightCol;
uniform float4 _SkyColor1;
uniform float4 _SkyColor2;
uniform float4 _SkyColor3;
uniform float4 _SkyColor4;
uniform float4 _SunColor;

//Just the sky color without the sun
#ifdef IS_5D
float4 SkyColorNoSun(float4 n, float n_v) {
#else
float4 SkyColorNoSun(float4 n) {
#endif
  float h = clamp(n.y, T1, T4);

  if (h < T2) {
    float a = (h - T1) / (T2 - T1);
    return (1.0 - a)*_SkyColor1 + a * _SkyColor2;
  } else if (h < T3) {
    float a = (h - T2) / (T3 - T2);
    return (1.0 - a)*_SkyColor2 + a * _SkyColor3;
  } else {
    float a = (h - T3) / (T4 - T3);
    return (1.0 - a)*_SkyColor3 + a * _SkyColor4;
  }
}
#ifdef IS_5D
float4 SkyCustomNoSun(float4 n, float n_v, float4 c1, float4 c2, float4 c3, float4 c4) {
#else
float4 SkyCustomNoSun(float4 n, float4 c1, float4 c2, float4 c3, float4 c4) {
#endif
  float lowT = -0.4;
  float h = clamp(n.y, lowT, T4);
  if (h < T2) {
    float a = (h - lowT) / (T2 - lowT);
    return (1.0 - a) * c1 + a * c2;
  } else if (h < T3) {
    float a = (h - T2) / (T3 - T2);
    return (1.0 - a) * c2 + a * c3;
  } else {
    float a = (h - T3) / (T4 - T3);
    return (1.0 - a) * c3 + a * c4;
  }
}

//Sky color with sun added in
#ifdef IS_5D
float4 SkyColor(float4 n, float n_v) {
  float sun = pow(max(Dot5(-_LightDirA, -_LightDirV, n, n_v), 0.0), 160);
  return lerp(SkyColorNoSun(n, n_v), _SunColor, saturate(sun * 10.0));
}
float4 SkyCustom(float4 n, float n_v, float4 c1, float4 c2, float4 c3, float4 c4) {
  float sun = pow(max(Dot5(-_LightDirA, -_LightDirV, n, n_v), 0.0), 160);
  return lerp(SkyCustomNoSun(n, n_v, c1, c2, c3, c4), _SunColor, saturate(sun * 10.0));
}
#else
float4 SkyColor(float4 n) {
  float sun = pow(max(dot(-_LightDirA, n), 0.0), 160);
  return lerp(SkyColorNoSun(n), _SunColor, saturate(sun * 10.0));
}
float4 SkyCustom(float4 n, float4 c1, float4 c2, float4 c3, float4 c4) {
  float sun = pow(max(dot(-_LightDirA, n), 0.0), 160);
  return lerp(SkyCustomNoSun(n, c1, c2, c3, c4), _SunColor, saturate(sun * 10.0));
}
#endif
