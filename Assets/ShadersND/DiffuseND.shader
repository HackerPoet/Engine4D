Shader "Custom/DiffuseND" {
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
      #pragma shader_feature VERTEX_AO
      #include_with_pragmas "CoreND.cginc"
      ENDCG
    }
  }
  CustomEditor "GeneralEditor"
}
