Shader "Custom/StencilShadowND" {
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
      "Queue" = "Geometry+400"
      "LightMode" = "ForwardBase"
      "RenderType" = "Opaque"
    }

    Pass {
      Stencil {
        Ref 1
        Comp Always
        PassFront IncrWrap
        PassBack DecrWrap
      }
      Cull Off
      ZTest LEqual
      ZWrite Off
      ColorMask 0

      CGPROGRAM
      #include_with_pragmas "CoreND.cginc"
      ENDCG
    }
    Pass {
      Stencil {
        Ref 0
        Comp NotEqual
        Pass Zero
      }
      Cull Off
      ZTest LEqual
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha

      CGPROGRAM
      #define USE_ALPHA
      #include_with_pragmas "CoreND.cginc"
      ENDCG
    }
  }
}
