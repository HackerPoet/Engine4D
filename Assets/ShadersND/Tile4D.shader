Shader "Custom/Tile4D" {
  Properties{
    _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
    _ShadowDist("ShadowDist", Float) = 40.0
    _Ambient("Ambient", Float) = 0.6
    _SpecularMul("SpecularMul", Float) = 0.0
    _SpecularPow("SpecularPow", Float) = 0.0
    _TileTex("Texture", 3D) = "" {}
    _TileTexST("TextureST", Vector) = (1.0,1.0,1.0,1.0)
  }

  //Opaque shader
  SubShader{
    Tags{
      "Queue" = "Geometry"
      "LightMode" = "ForwardBase"
      "RenderType" = "Opaque"
    }

    Pass {
      Cull Off
      ZTest LEqual
    
      CGPROGRAM
      #pragma shader_feature VERTEX_AO
      #define PROC_TEXTURE
      sampler3D _TileTex;
      #define apply_proc_tex4D() \
        float4 tileTex = UNITY_ACCESS_INSTANCED_PROP(Props, _TileTexST); \
        color.rgb *= lerp(1.0, tex3D(_TileTex, i.uv.xzw * tileTex.xyz).r, tileTex.w);
      #define DEFINE_CUSTOM_LOCALS \
        UNITY_DEFINE_INSTANCED_PROP(float4, _TileTexST)
      #include_with_pragmas "CoreND.cginc"
      ENDCG
    }
  }
  CustomEditor "GeneralEditor"
}
