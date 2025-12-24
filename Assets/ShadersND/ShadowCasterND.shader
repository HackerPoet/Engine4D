Shader "Custom/ShadowCasterND" {
  Properties{
    _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
    _ShadowDist("ShadowDist", Float) = 40.0
    _Ambient("Ambient", Float) = 0.6
    _SpecularMul("SpecularMul", Float) = 0.0
    _SpecularPow("SpecularPow", Float) = 0.0
    _ShadowMul("ShadowMul", Float) = 0.6
    _GroundLevel("GroundLevel", Float) = 0.0
  }

  //Opaque shader
  SubShader{
    Tags{
      "Queue" = "Geometry-1"
      "LightMode" = "ForwardBase"
      "RenderType" = "Opaque"
    }

    Pass {
      Cull Off
      ZTest Always
      ZWrite Off
      Blend Zero One, One One
      BlendOp Add, Min

      CGPROGRAM
      #define PROC_VERT
      #define PROC_TEXTURE
      #define PROC_POST_VERT
      uniform float _ShadowMul;
      uniform float _GroundLevel;
      #define apply_proc_vert4D_init() \
        float4 origYs = float4(simplex[0].y, simplex[1].y, simplex[2].y, simplex[3].y);
      #define apply_proc_vert4D(v) \
        v += _LightDirA * ((_GroundLevel - v.y) / _LightDirA.y);
      #define apply_proc_vert5D_init() \
        float origYs[5] = { simplex[0].y, simplex[1].y, simplex[2].y, simplex[3].y, simplex[4].y }; float pv;
      #define apply_proc_vert5D(v, V) \
        pv = (_GroundLevel - v.y) / _LightDirA.y; v += _LightDirA * pv; V += _LightDirV * pv;
      #define apply_proc_post_vert4D() \
        o.sHeight = (origYs[ix1] * v2.w + origYs[ix2] * v1.w) / (_LightDirA.y + _GroundLevel);
      #define apply_proc_post_vert5D() \
        o.sHeight = (origYs[ix1] + (origYs[ix2] - origYs[ix1]) * t + (origYs[ix3] - origYs[ix1]) * s) / (_LightDirA.y + _GroundLevel);
      #define apply_proc_tex4D() \
        float sH = exp(2.0 * min(i.sHeight, 0.0)); \
        color.a = _ShadowMul * sH + (0.75 + _ShadowMul * 0.25) * (1.0 - sH);
      #define apply_proc_tex5D() apply_proc_tex4D()
      #include_with_pragmas "CoreND.cginc"
      ENDCG
    }
  }
}
