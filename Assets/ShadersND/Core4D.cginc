//Globals
uniform float4x4 _CamMatrix;
uniform float4 _CamPosition;
#if defined(IS_EDITOR) || defined(IS_EDITOR_V)
uniform float _EditorSliceW;
#endif

//Locals
UNITY_INSTANCING_BUFFER_START(Props)
DEFINE_COMMON_LOCALS
#ifdef DEFINE_CUSTOM_LOCALS
DEFINE_CUSTOM_LOCALS
#endif
//Unity does not allow overriding unity_ObjectToWorld, need entirely new matrix.
UNITY_DEFINE_INSTANCED_PROP(float4x4, _ModelMatrix)
UNITY_DEFINE_INSTANCED_PROP(float4x4, _ModelMatrixIT)
UNITY_DEFINE_INSTANCED_PROP(float4, _ModelPosition)
UNITY_INSTANCING_BUFFER_END(Props)

//Vertex input structure
struct vin {
  float4 va : POSITION;
#if !defined(SHADOW)
  uint4 normal : NORMAL;
  float4 vb : TEXCOORD0;
  float4 vc : TEXCOORD1;
  float4 vd : TEXCOORD2;
#if defined(CELL_AO) || defined(VERTEX_AO)
  uint ao : TEXCOORD3;
#endif
  uint vertexID : SV_VertexID;
#endif
  //GPU instancing
  UNITY_VERTEX_INPUT_INSTANCE_ID
};

//Fragment input structure
struct v2f {
  float4 vertex : SV_POSITION;
  float4 camPt : TEXCOORD2;
#ifdef SHADOW
#ifdef SHADOW_UV
  float4 uv : TEXCOORD0;
#endif
#else
  float4 normal : NORMAL;
  float4 uv : TEXCOORD0;
  float4 viewDir : TEXCOORD1;
#if defined(VERTEX_AO)
  float ao : TEXCOORD3;
#elif defined(CELL_AO)
  float3 ao : TEXCOORD3;
#else
  float sHeight : TEXCOORD3;
#endif
#endif
  //GPU instancing
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};

#if defined(IS_EDITOR)
#define ModelToCam(V) V = float4(mul(UNITY_MATRIX_V, float4(V.xyz, 1.0)).xyz, V.w - _EditorSliceW)
#define CamToModel(V) V = float4(mul(V.xyz - UNITY_MATRIX_V._m03_m13_m23, (float3x3)UNITY_MATRIX_V), V.w + _EditorSliceW)
#elif defined(IS_EDITOR_V)
#define ModelToCam(V) V = float4(mul(UNITY_MATRIX_V, float4(V.x, -V.w, V.z, 1.0)).xyz, V.y - _EditorSliceW)
#define CamToModel(V) V = float4(mul(V.xyz - UNITY_MATRIX_V._m03_m13_m23, (float3x3)UNITY_MATRIX_V), V.w + _EditorSliceW); V.yw = float2(V.w, -V.y)
#else
#define ModelToCam(V) V = mul(_CamMatrix, V + _CamPosition)
#define CamToModel(V) V = mul(V, _CamMatrix) - _CamPosition
#endif

v2f vert(vin v) {
  //Define output structure
  v2f o;

  //Instancing setup
  UNITY_SETUP_INSTANCE_ID(v);
  UNITY_INITIALIZE_OUTPUT(v2f, o);
  UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  UNITY_TRANSFER_INSTANCE_ID(v, o);

#if defined(SHADOW)
#ifdef SHADOW_UV
  o.uv = v.va;
#endif
  o.vertex = mul(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.va) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition);
#if defined(PROC_VERT)
  apply_proc_vert4D(o.vertex);
#endif
  ModelToCam(o.vertex);
  o.camPt.w = o.vertex.w;
#else
  //Transform the simplex
  float4 simplex[4] = {
    mul(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.va) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition),
    mul(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.vb) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition),
    mul(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.vc) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition),
    mul(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrix), v.vd) + UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition),
  };
#if defined(PROC_VERT)
  apply_proc_vert4D_init();
  apply_proc_vert4D(simplex[0]);
  apply_proc_vert4D(simplex[1]);
  apply_proc_vert4D(simplex[2]);
  apply_proc_vert4D(simplex[3]);
#endif
  ModelToCam(simplex[0]);
  ModelToCam(simplex[1]);
  ModelToCam(simplex[2]);
  ModelToCam(simplex[3]);

  //Use a lookup-table to determine which edge should be sliced for this vertex
  float x = (v.vertexID % 4) + (simplex[0].w > 0.0 ? 4 : 0);
  float y = (simplex[1].w > 0.0 ? 1 : 0) + (simplex[2].w > 0.0 ? 2 : 0) + (simplex[3].w > 0.0 ? 4 : 0);
  float4 lookup = tex2Dlod(_LUT, float4((x + 0.5)/8.0, (y + 0.5)/8.0, 0.0, 0.0));
  uint ix1 = (uint)(lookup.r * 4.0);
  uint ix2 = (uint)(lookup.g * 4.0);
  float4 v1 = simplex[ix1];
  float4 v2 = simplex[ix2];

  //Create interpolation factors and clamp during undefined behavior
  v1.w = saturate(v1.w / (v1.w - v2.w));
  v2.w = 1.0 - v1.w;

  //Slice the edge for the final vertex position
  o.vertex.xyz = v1.xyz * v2.w + v2.xyz * v1.w;
#if defined(PROC_POST_VERT)
  apply_proc_post_vert4D();
#endif

#if defined(VERTEX_AO)
  float ao1 = float((v.ao >> (ix1 * 8)) & 255);
  float ao2 = float((v.ao >> (ix2 * 8)) & 255);
  o.ao = (ao1 * v2.w + ao2 * v1.w) / 255.0;
#elif defined(CELL_AO)
  //Slice the AO coordinates
  uint ao1 = v.ao >> (ix1 * 4);
  uint ao2 = v.ao >> (ix2 * 4);
  float3 vao1 = float3(uint3(ao1, (ao1 >> 1), (ao1 >> 2)) & 1);
  float3 vao2 = float3(uint3(ao2, (ao2 >> 1), (ao2 >> 2)) & 1);
  o.ao = vao1 * v2.w + vao2 * v1.w;
#endif

  o.uv = float4(o.vertex.xyz, 0.0);
  CamToModel(o.uv);
  o.viewDir = -_CamPosition - o.uv;
  o.viewDir.xyz -= UNITY_MATRIX_V._m03_m13_m23;
#if defined(LOCAL_UV)
  o.uv = mul(o.uv - UNITY_ACCESS_INSTANCED_PROP(Props, _ModelPosition), UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrixIT));
#endif

  //Transfer normal for lighting
  uint n1 = v.normal[ix1];
  uint n2 = v.normal[ix2];
  float4 n1f = float4(uint4(n1, n1 >> 8, n1 >> 16, n1 >> 24) & 0xFF) - 128.0;
  float4 n2f = float4(uint4(n2, n2 >> 8, n2 >> 16, n2 >> 24) & 0xFF) - 128.0;
  float4 n = n1f * v2.w + n2f * v1.w;
  o.normal = normalize(mul(UNITY_ACCESS_INSTANCED_PROP(Props, _ModelMatrixIT), n));
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
  color = _LightCol * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
#if defined(PROC_TEXTURE)
  apply_proc_tex4D();
#endif
#ifndef KEEP_SHADOW_COLOR
  float3 shadowCol = lerp(_ShadowColor1.rgb, _ShadowColor2.rgb, clamp(i.camPt.w * 0.2, -0.5, 0.5) + 0.5);
  color.rgb = lerp(color.rgb, shadowCol, _ShadowColor1.a);
#endif
#ifndef SHADOW_ALPHA
  color.a = min(color.a * _ShadowOpacity, 0.25) * saturate(-0.2 * i.camPt.z);
  float d = abs(i.camPt.w) / UNITY_ACCESS_INSTANCED_PROP(Props, _ShadowDist);
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
  float ditherR = max((length(i.camPt.xyz) - _DitherRadius) * 0.5 + 1.0, clamp(2.0 - _DitherRadius, 0.0, 0.125));
  ditherZ = min(ditherZ, ditherR);
#endif
  float limit = dither[uint(i.vertex.x) % 4 + (uint(i.vertex.y) % 4) * 4];
  if (limit > ditherZ) {
    discard;
  }
#endif

  //Normalize view direction if needed
#if !defined(SKIP_SPECULAR) || defined(FOG)
  float4 vd = normalize(i.viewDir);
#endif
  float4 n = normalize(i.normal);
#ifdef DOUBLE_SIDED_N
  n *= (facing > 0 ? 1.0 : -1.0);
#endif

  //Apply base color
  float diffuse = 1.0;
  float3 lightColor = _LightCol.rgb;
  float ambient = UNITY_ACCESS_INSTANCED_PROP(Props, _Ambient);
  color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
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
  apply_proc_tex4D();
#endif

  //Apply diffuse lighting
  float lightDot = dot(n, _LightDirA);
  diffuse *= clamp(ambient + (1.0 - ambient) * lightDot * 1.5, 0.0, 1.5);
#ifdef VERTEX_AO_MUL
  diffuse *= saturate(i.ao);
#endif

  color.rgb *= lightColor;
#ifdef SKIP_SPECULAR
  //Apply only diffuse light
  color.rgb *= diffuse;
#else
  //Apply specular and diffuse
  float specular = pow(max(0.001, dot(_LightDirA - (2.0 * lightDot) * n, vd)), SPEC_POWER);
  float4 reflectedDir = reflect(vd, n);
  float3 skyColor = SkyColor(-reflectedDir);
#ifdef DIFFUSE_COLOR
  color.rgb = lerp(DIFFUSE_COLOR, lerp(color.rgb, skyColor, specularMul), diffuse) + specularPow * specular;
#else
  color.rgb = diffuse * lerp(color.rgb, skyColor, specularMul) + specularPow * specular;
#endif
#endif

  //Add fog
#ifdef FOG
  float a = saturate(length(i.viewDir) * max(_FogLevel, 0.05 * _MinChecker));
  a *= a;
  color.rgb = color.rgb * (1.0 - a) + a * SkyColorNoSun(-vd);
#endif
#endif
}
