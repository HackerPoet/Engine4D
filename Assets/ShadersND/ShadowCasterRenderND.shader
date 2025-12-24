Shader "Custom/ShadowCasterRenderND" {
  Properties{
  }

  //Opaque shader
  SubShader{
    Tags{
      "Queue" = "Geometry+101"
      "LightMode" = "ForwardBase"
      "RenderType" = "Far"
    }

    Pass {
      Cull Off
      ZTest Always
      ZWrite Off
      Blend Zero DstAlpha

      CGPROGRAM
      #include "FarAway.cginc"
      #include_with_pragmas "CoreND.cginc"
      ENDCG
    }
  }
}
