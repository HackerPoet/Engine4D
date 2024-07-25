Shader "CustomSky/SkyBackgroundND" {
  Properties {
  }

  SubShader {
    Tags {
      "Queue" = "Background"
      "RenderType" = "Far"
    }
    Pass {
      ZWrite Off
      Cull Off

      CGPROGRAM
      #pragma multi_compile_instancing
      #pragma multi_compile __ IS_5D
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"
      #include "Util5D.cginc"
      #include "SkyND.cginc"

#ifdef IS_5D
      Matrix5x5(_CamMatrix);
#else
      uniform float4x4 _CamMatrix;
#endif

      struct vin {
        float4 vertex : POSITION;

        //GPU instancing
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2f {
        float4 pos : SV_POSITION;
        float4 n : NORMAL;
#ifdef IS_5D
        float n_v : TEXCOORD0;
#endif

        //GPU instancing
        UNITY_VERTEX_OUTPUT_STEREO
      };

      v2f vert(vin v) {
        //Define output structure
        v2f o;

        //Instancing setup
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_OUTPUT(v2f, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        float4 n = float4(normalize(v.vertex.xyz), 0.0);
        n.z = -n.z;
#ifdef IS_5D
        o.n = MulVec5T(_CamMatrix, n, 0.0);
        o.n_v = MulVec5VT(_CamMatrix, n, 0.0);
#else
        o.n = mul(n, _CamMatrix);
#endif
        o.pos = UnityObjectToClipPos(v.vertex.xyz);
        return o;
      }

      fixed4 frag(v2f i) : SV_Target {
#ifdef IS_5D
        float invMag = 1.0 / sqrt(dot(i.n, i.n) + i.n_v * i.n_v);
        return SkyColor(i.n * invMag, i.n_v * invMag);
#else
        return SkyColor(normalize(i.n));
#endif
      }
      ENDCG
    }
  }
}
