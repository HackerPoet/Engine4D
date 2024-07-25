#ifndef __5D_UTIL_INCLUDED__
#define __5D_UTIL_INCLUDED__

#define Matrix5x5(name) \
  uniform float4x4 name; \
  uniform float4 name ## _C4; \
  uniform float4 name ## _R4; \
  uniform float name ## _VV;
#define Vector5(name) \
  uniform float4 name; \
  uniform float name ## _V;
#define InstancedMatrix5x5(name) \
  UNITY_DEFINE_INSTANCED_PROP(float4x4, name) \
  UNITY_DEFINE_INSTANCED_PROP(float4, name ## _C4) \
  UNITY_DEFINE_INSTANCED_PROP(float4, name ## _R4) \
  UNITY_DEFINE_INSTANCED_PROP(float, name ## _VV)
#define InstancedVector5(name) \
  UNITY_DEFINE_INSTANCED_PROP(float4, name) \
  UNITY_DEFINE_INSTANCED_PROP(float, name ## _V)
#define MulVec5(M, V, v) \
  mul(M,V) + M ## _C4 * (v)
#define MulVec5T(M, V, v) \
  mul(V,M) + M ## _R4 * (v)
#define MulVec5V(M, V, v) \
  dot(M ## _R4,V) + M ## _VV * (v)
#define MulVec5VT(M, V, v) \
  dot(V,M ## _C4) + M ## _VV * (v)
#define MagSqr5(V, v) \
  (dot(V, V) + (v) * (v))
#define Length5(V, v) \
  sqrt(dot(V, V) + (v) * (v))
#define Dot5(aV, av, bV, bv) \
  (dot(aV, bV) + (av) * (bv))
#define Normalize5(V, v, name) \
  float name ## _mag = Length5(V, v); \
  float4 name = (V) / name ## _mag; \
  float name ## _v = (v) / name ## _mag;
#define Reflect5(D, d, N, n, name) \
  float name ## _dp = Dot5(D, d, N, n); \
  float4 name = (D) - (2.0 * name ## _dp) * (N); \
  float name ## _v = (d) - (2.0 * name ## _dp) * (n);

#endif //__5D_UTIL_INCLUDED__
