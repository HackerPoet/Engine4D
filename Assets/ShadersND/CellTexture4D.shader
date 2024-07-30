Shader "Custom/CellTextureND" {
  Properties{
    _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
    _CellTex("Texture", 3D) = "" {}
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
      Cull Off
      ZTest LEqual
    
      CGPROGRAM
      #define PROC_TEXTURE
      #define CELL_AO
      #define apply_proc_tex4D() \
        color.rgb *= tex3D(_CellTex, i.ao);
      #define apply_proc_tex5D()
      sampler3D _CellTex;
      #include_with_pragmas "CoreND.cginc"
      ENDCG
    }
  }
  CustomEditor "GeneralEditor"
}
