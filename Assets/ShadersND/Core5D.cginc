#include "Util5D.cginc"
#define CrossSign(a, b) \
  (simplex[a].w * simplex_V[b] > simplex_V[a] * simplex[b].w)

//Globals
Matrix5x5(_CamMatrix);
Vector5(_CamPosition);
#if defined(IS_EDITOR) || defined(IS_EDITOR_V)
uniform float _EditorSliceW;
uniform float _EditorSliceV;
#endif

//5D-specific additional shadow color
uniform float4 _ShadowColor3;

//Locals
UNITY_INSTANCING_BUFFER_START(Props)
DEFINE_COMMON_LOCALS
#ifdef DEFINE_CUSTOM_LOCALS
DEFINE_CUSTOM_LOCALS
#endif
InstancedMatrix5x5(_ModelMatrix)
InstancedVector5(_ModelPosition)
UNITY_INSTANCING_BUFFER_END(Props)

//Vertex input structure
struct vin {
  float4 va : POSITION;
#if defined(SHADOW)
  float va_V : TEXCOORD0;
#else
  uint4 normal : NORMAL;
  uint4 v_na : TEXCOORD0;
  float4 vb : TEXCOORD1;
  float4 vc : TEXCOORD2;
  float4 vd : TEXCOORD3;
  float4 ve : TEXCOORD4;
  float4 v_bcde : TEXCOORD5;
#if defined(CELL_AO) || defined(VERTEX_AO)
  uint ao : TEXCOORD6;
#endif
  uint vertexID : SV_VertexID;
#endif
  //GPU instancing
  UNITY_VERTEX_INPUT_INSTANCE_ID
};

//Fragment input structure
struct v2f {
  float4 vertex : SV_POSITION;
  float4 camPt : TEXCOORD3;
#ifdef SHADOW
  float v_camPt : TEXCOORD0;
#else
  float4 normal : NORMAL;
  float4 uv : TEXCOORD0;
  float4 viewDir : TEXCOORD1;
  float3 v_nud : TEXCOORD2;
#if defined(VERTEX_AO)
  float ao : TEXCOORD4;
#elif defined(CELL_AO)
  float4 ao : TEXCOORD4;
#else
  float sHeight : TEXCOORD4;
#endif
#endif
  //GPU instancing
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};

#if defined(IS_EDITOR)
#define ModelToCam(XYZW, V) XYZW = float4(mul(UNITY_MATRIX_V, float4(XYZW.xyz, 1.0)).xyz, XYZW.w - _EditorSliceW); V -= _EditorSliceV
#define CamToModel(XYZW, V) XYZW = float4(mul(XYZW.xyz - UNITY_MATRIX_V._m03_m13_m23, (float3x3)UNITY_MATRIX_V), XYZW.w + _EditorSliceW); V += _EditorSliceV
#elif defined(IS_EDITOR_V)
#define ModelToCam(XYZW, V) XYZW = float4(mul(UNITY_MATRIX_V, float4(XYZW.x, -XYZW.w, XYZW.z, 1.0)).xyz, XYZW.y - _EditorSliceW); V -= _EditorSliceV
#define CamToModel(XYZW, V) XYZW = float4(mul(XYZW.xyz - UNITY_MATRIX_V._m03_m13_m23, (float3x3)UNITY_MATRIX_V), XYZW.w + _EditorSliceW); XYZW.yw = float2(XYZW.w, -XYZW.y); V += _EditorSliceV
#else
#define ModelToCam(XYZW, V) \
  XYZW += _CamPosition; \
  V += _CamPosition_V; \
  temp = MulVec5V(_CamMatrix, XYZW, V); \
  XYZW = MulVec5(_CamMatrix, XYZW, V); \
  V = temp;
#define CamToModel(XYZW, V) \
  temp = MulVec5VT(_CamMatrix, XYZW, V) - _CamPosition_V; \
  XYZW = MulVec5T(_CamMatrix, XYZW, V) - _CamPosition; \
  V = temp;
#endif

v2f vert(vin v) {
  //Define output structure
  v2f o;
  float temp;

  //Instancing setup
  UNITY_SETUP_INSTANCE_ID(v);
  UNITY_INITIALIZE_OUTPUT(v2f, o);
  UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  UNITY_TRANSFER_INSTANCE_ID(v, o);

#if defined(SHADOW)
  o.vertex = MulVec5(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.va, v.va_V) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition);
  float vertex_V = MulVec5V(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.va, v.va_V) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition_V);
#if defined(PROC_VERT)
  apply_proc_vert5D(o.vertex, vertex_V);
#endif
  ModelToCam(o.vertex, vertex_V);
  o.camPt.w = o.vertex.w;
  o.v_camPt = vertex_V;
#else
  //Transform the simplex
  float4 simplex[5] = {
    MulVec5(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.va, asfloat(v.v_na.w)) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition),
    MulVec5(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.vb, v.v_bcde.x) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition),
    MulVec5(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.vc, v.v_bcde.y) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition),
    MulVec5(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.vd, v.v_bcde.z) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition),
    MulVec5(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.ve, v.v_bcde.w) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition),
  };
  float simplex_V[5] = {
    MulVec5V(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.va, asfloat(v.v_na.w)) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition_V),
    MulVec5V(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.vb, v.v_bcde.x) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition_V),
    MulVec5V(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.vc, v.v_bcde.y) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition_V),
    MulVec5V(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.vd, v.v_bcde.z) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition_V),
    MulVec5V(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.ve, v.v_bcde.w) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition_V),
  };
#if defined(PROC_VERT)
  apply_proc_vert5D_init()
  apply_proc_vert5D(simplex[0], simplex_V[0]);
  apply_proc_vert5D(simplex[1], simplex_V[1]);
  apply_proc_vert5D(simplex[2], simplex_V[2]);
  apply_proc_vert5D(simplex[3], simplex_V[3]);
  apply_proc_vert5D(simplex[4], simplex_V[4]);
#endif
  ModelToCam(simplex[0], simplex_V[0]);
  ModelToCam(simplex[1], simplex_V[1]);
  ModelToCam(simplex[2], simplex_V[2]);
  ModelToCam(simplex[3], simplex_V[3]);
  ModelToCam(simplex[4], simplex_V[4]);

  //Use a lookup-table to determine which edge should be sliced for this vertex
  float x = (v.vertexID % 5) + (CrossSign(0,1) ? 8 : 0) + (CrossSign(0,2) ? 16 : 0) + (CrossSign(0,3) ? 32 : 0) + (CrossSign(0,4) ? 64 : 0);
  float y = (CrossSign(1,2) ? 1 : 0) + (CrossSign(1,3) ? 2 : 0) + (CrossSign(1,4) ? 4 : 0) +
            (CrossSign(2,3) ? 8 : 0) + (CrossSign(2,4) ? 16 : 0) + (CrossSign(3,4) ? 32 : 0);
  float4 lookup = tex2Dlod(_LUT, float4((x + 0.5) / 128.0, (y + 0.5) / 64.0, 0.0, 0.0));
  uint ix1 = (uint)(lookup.r * 5.0);
  uint ix2 = (uint)(lookup.g * 5.0);
  uint ix3 = (uint)(lookup.b * 5.0);
  float4 a = simplex[ix1];
  float4 ab = simplex[ix2] - a;
  float4 ac = simplex[ix3] - a;
  float a_V = simplex_V[ix1];
  float ab_V = simplex_V[ix2] - a_V;
  float ac_V = simplex_V[ix3] - a_V;

  //Intersect the triangle for the final vertex position
  float denom = 1.0 / (ab_V*ac.w - ab.w*ac_V);
  float t = (ac_V*a.w - ac.w*a_V) * denom;
  float s = (ab.w*a_V - ab_V*a.w) * denom;
  o.vertex.xyz = a.xyz + ab.xyz * t + ac.xyz * s;
#if defined(PROC_POST_VERT)
  apply_proc_post_vert5D();
#endif

#if defined(VERTEX_AO)
  float ao1 = (float)((v.ao >> (ix1 * 6)) & 0x3F);
  float ao2 = (float)((v.ao >> (ix2 * 6)) & 0x3F);
  float ao3 = (float)((v.ao >> (ix3 * 6)) & 0x3F);
  o.ao = (ao1 + (ao2 - ao1) * t + (ao3 - ao1) * s) / 63.0;
#elif defined(CELL_AO)
  //Slice the AO coordinates
  uint ao1 = v.ao >> (ix1 * 4);
  uint ao2 = v.ao >> (ix2 * 4);
  uint ao3 = v.ao >> (ix3 * 4);
  float4 vao1 = float4((float)(ao1 & 1), (float)((ao1 >> 1) & 1), (float)((ao1 >> 2) & 1), (float)((ao1 >> 3) & 1));
  float4 vao2 = float4((float)(ao2 & 1), (float)((ao2 >> 1) & 1), (float)((ao2 >> 2) & 1), (float)((ao2 >> 3) & 1));
  float4 vao3 = float4((float)(ao3 & 1), (float)((ao3 >> 1) & 1), (float)((ao3 >> 2) & 1), (float)((ao3 >> 3) & 1));
  o.ao = vao1 + (vao2 - vao1) * t + (vao3 - vao1) * s;
#endif

  //Update textures
  o.uv = float4(o.vertex.xyz, 0.0);
  o.v_nud.y = 0.0;
  CamToModel(o.uv, o.v_nud.y);
  o.viewDir = -_CamPosition - o.uv;
  o.viewDir.xyz -= UNITY_MATRIX_V._m03_m13_m23;
  o.v_nud.z = -_CamPosition_V - o.v_nud.y;
#if defined(LOCAL_UV)
  float localV = MulVec5VT(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix),
                           o.uv - UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition),
                           o.v_nud.y - UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition_V));
  o.uv = MulVec5T(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix),
                  o.uv - UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition),
                  o.v_nud.y - UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition_V));
  o.v_nud.y = localV;
#endif

  //Calculate normal and normalize
  float4 n;
  uint xy1 = v.normal[ix1 / 2] >> ((ix1 & 1) * 16);
  uint xy2 = v.normal[ix2 / 2] >> ((ix2 & 1) * 16);
  uint xy3 = v.normal[ix3 / 2] >> ((ix3 & 1) * 16);
  float2 nxy1 = float2(uint2(xy1, xy1 >> 8) & 0xFF);
  float2 nxy2 = float2(uint2(xy2, xy2 >> 8) & 0xFF);
  float2 nxy3 = float2(uint2(xy3, xy3 >> 8) & 0xFF);
  n.xy = nxy1 + (nxy2 - nxy1) * t + (nxy3 - nxy1) * s - 128.0;

  uint zw1 = v.v_na[ix1 / 2] >> ((ix1 & 1) * 16);
  uint zw2 = v.v_na[ix2 / 2] >> ((ix2 & 1) * 16);
  uint zw3 = v.v_na[ix3 / 2] >> ((ix3 & 1) * 16);
  float2 nzw1 = float2(uint2(zw1, zw1 >> 8) & 0xFF);
  float2 nzw2 = float2(uint2(zw2, zw2 >> 8) & 0xFF);
  float2 nzw3 = float2(uint2(zw3, zw3 >> 8) & 0xFF);
  n.zw = nzw1 + (nzw2 - nzw1) * t + (nzw3 - nzw1) * s - 128.0;

  float nv1 = (float)((v.normal[(ix1 + 22) / 8] >> (((ix1 + 2) & 3) * 8)) & 0xFF);
  float nv2 = (float)((v.normal[(ix2 + 22) / 8] >> (((ix2 + 2) & 3) * 8)) & 0xFF);
  float nv3 = (float)((v.normal[(ix3 + 22) / 8] >> (((ix3 + 2) & 3) * 8)) & 0xFF);
  float nv = nv1 + (nv2 - nv1) * t + (nv3 - nv1) * s - 128.0;

  o.normal = MulVec5(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), n, nv);
  o.v_nud.x = MulVec5V(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), n, nv);
  float invMag = 1.0 / sqrt(MagSqr5(o.normal, o.v_nud.x));
  o.normal *= invMag;
  o.v_nud.x *= invMag;
#endif

  //Apply projection
#if !defined(IS_EDITOR) && !defined(IS_EDITOR_V)
  o.vertex = mul(UNITY_MATRIX_V, float4(o.vertex.x, o.vertex.y, -o.vertex.z, 1.0));
  o.camPt.xyz = o.vertex.xyz;
#endif
  o.vertex = mul(UNITY_MATRIX_P, float4(o.vertex.xyz, 1.0));
  return o;
}

#ifdef DOUBLE_SIDED_N
void frag(v2f i, half facing : VFACE, out float4 color : SV_Target) {
#else
void frag(v2f i, out float4 color : SV_Target) {
#endif
  //Instancing setup
  UNITY_SETUP_INSTANCE_ID(i);
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

#ifdef SHADOW
  color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
#ifndef KEEP_SHADOW_COLOR
  float3 shadowCol = lerp(_ShadowColor1.rgb, _ShadowColor2.rgb, clamp(i.camPt.w * 0.2, -0.5, 0.5) + 0.5);
  float3 shadowCol2 = lerp(_ShadowColor3.rgb, 1.0 - _ShadowColor3.rgb, clamp(i.v_camPt * 0.2, -0.5, 0.5) + 0.5);
  shadowCol = (shadowCol + shadowCol2) * 0.5;
  color.rgb = lerp(color.rgb, shadowCol, _ShadowColor1.a);
#endif
#ifndef SHADOW_ALPHA
  color.a = min(color.a * _ShadowOpacity, 0.25) * saturate(-0.2 * i.camPt.z);
  float d = length(float2(i.camPt.w, i.v_camPt)) / UNITY_ACCESS_INSTANCED_PROP(Props, _ShadowDist);
  color.a *= max(1.0 - d, 0.0);
#ifdef FOG
  color.a *= saturate(1.0 + i.camPt.z * _FogLevel);
#endif
#endif
#else

  //Dithering to look through walls
#ifdef USE_DITHER
#ifdef FORCE_DITHER
  float ditherZ = FORCE_DITHER;
#else
  float ditherZ = max(1.0 - _DitherDist - i.camPt.z, 0.375);
  float ditherR = max((length(i.camPt.xyz) - _DitherRadius) * 0.5 + 1.0, 0.125);
  ditherZ = min(ditherZ, ditherR);
#endif
  float limit = dither[uint(i.vertex.x) % 4 + (uint(i.vertex.y) % 4) * 4];
  if (limit > ditherZ) {
    discard;
  }
#endif

  //Normalize view direction if needed
#if !defined(SKIP_SPECULAR) || defined(FOG)
  Normalize5(i.viewDir, i.v_nud.z, vd);
#endif
  Normalize5(i.normal, i.v_nud.x, n);
#ifdef DOUBLE_SIDED_N
  float normalMul = (facing > 0 ? 1.0 : -1.0);
  n *= normalMul; n_v *= normalMul;
#endif

  //Apply base color
  float diffuse = 1.0;
  float3 lightColor = _LightCol.rgb;
  float ambient = UNITY_ACCESS_INSTANCED_PROP(Props, _Ambient);
  color = _LightCol * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
#ifndef USE_ALPHA
  color.a = 1.0;
#endif

  //Get specular parameters
#ifndef SKIP_SPECULAR
  float specularMul = UNITY_ACCESS_INSTANCED_PROP(Props, _SpecularMul);
  float specularPow = UNITY_ACCESS_INSTANCED_PROP(Props, _SpecularPow);
#endif

  //Apply procedural texturing
#if defined(PROC_TEXTURE)
  apply_proc_tex5D();
#endif

  //Apply diffuse lighting
  float lightDot = Dot5(n, n_v, _LightDirA, _LightDirV);
  diffuse *= saturate(ambient + (1.0 - ambient) * lightDot);
#ifdef VERTEX_AO_MUL
  diffuse *= saturate(i.ao);
#endif

  color.rgb *= lightColor;
#ifdef SKIP_SPECULAR
  //Apply only diffuse light
  color.rgb *= diffuse;
#else
  //Apply specular and diffuse
  float4 refN = _LightDirA - (2.0 * lightDot) * n;
  float refN_v = _LightDirV - (2.0 * lightDot) * n_v;
  float specular = pow(max(0.001, Dot5(refN, refN_v, vd, vd_v)), SPEC_POWER);
  Reflect5(vd, vd_v, n, n_v, reflectedDir);
  float3 skyColor = SkyColor(-reflectedDir, -reflectedDir_v);
  color.rgb = diffuse * lerp(color.rgb, skyColor, specularMul) + specularPow * specular;
#endif

  //Add fog
#ifdef FOG
  float a = saturate(vd_mag * max(_FogLevel, 0.05 * _MinChecker));
  a *= a;
  color.rgb = color.rgb * (1.0 - a) + a * SkyColorNoSun(-vd, -vd_v);
#endif
#endif
}
