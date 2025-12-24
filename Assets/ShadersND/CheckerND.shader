Shader "Custom/CheckerND" {
  Properties{
    _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
    _ShadowDist("ShadowDist", Float) = 40.0
    _Ambient("Ambient", Float) = 0.6
    _SpecularMul("SpecularMul", Float) = 0.0
    _SpecularPow("SpecularPow", Float) = 0.0
  }

  //Opaque shader
  SubShader{
    Tags{
      "Queue" = "Geometry"
      "LightMode" = "ForwardBase"
      "RenderType" = "Opaque"
    }

    Pass {
      Cull Back
      ZTest LEqual
    
      CGPROGRAM
      #pragma shader_feature LOCAL_UV
      #define PROC_TEXTURE
      #define USE_DITHER
      #define apply_proc_tex4D() \
        float4 checker = floor(i.uv * 3.0 + 0.01); \
        color.rgb *= 1.0 - 0.1 * frac((checker.x + checker.y + checker.z + checker.w) * 0.5);
      #define apply_proc_tex5D() \
        float4 checker = floor(i.uv * 3.0 + 0.01); \
        float checker_V = floor(i.v_nud.y * 3.0 + 0.01); \
        color.rgb *= 1.0 - 0.1 * frac((checker.x + checker.y + checker.z + checker.w + checker_V) * 0.5);
      #include_with_pragmas "CoreND.cginc"
      ENDCG
    }
  }
  CustomEditor "GeneralEditor"
}
