Shader "Custom/ShadowND" {
  Properties{
    _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
    _ShadowDist("ShadowDist", Float) = 40.0
    _Ambient("Ambient", Float) = 0.6
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
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha
      Offset 1, 1

      CGPROGRAM
      #define SHADOW 1
      #include_with_pragmas "CoreND.cginc"
      ENDCG
    }
  }

  //Far shader
  SubShader{
    Tags{
      "Queue" = "Geometry"
      "LightMode" = "ForwardBase"
      "RenderType" = "Far"
    }
    Pass {
      Cull Off
      ZTest LEqual
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha
      Offset 1, 1

      CGPROGRAM
      #include "FarAway.cginc"
      #define SHADOW 1
      #include_with_pragmas "CoreND.cginc"
      ENDCG
    }
  }
}
