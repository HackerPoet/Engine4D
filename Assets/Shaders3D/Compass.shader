Shader "Unlit/Compass" {
    Properties {
        _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
        _Ambient("Ambient", Float) = 0.5
        _MainTex("Texture", 2D) = "" {}
        _ZWrite("ZWrite", Int) = 0.0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcMode("SrcMode", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _DstMode("DstMode", Float) = 0
    }
    SubShader {
        Tags { "Queue"="Geometry" }
        LOD 100

        Pass {
            Cull Back
            ZTest LEqual
            ZWrite [_ZWrite]
            Blend [_SrcMode] [_DstMode]

            CGPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                //GPU instancing
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                //GPU instancing
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float, _Ambient)
            UNITY_INSTANCING_BUFFER_END(Props)

            sampler2D _MainTex;

            v2f vert (appdata v) {
                v2f o;

                //Instancing setup
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                //Instancing setup
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // sample the texture
                float4 col = UNITY_ACCESS_INSTANCED_PROP(Props, _Color) * tex2D(_MainTex, i.uv);
                float ambient = UNITY_ACCESS_INSTANCED_PROP(Props, _Ambient);
                float lightDot = dot(normalize(i.normal), _WorldSpaceLightPos0.xyz);
                col.rgb *= saturate(ambient + (1.0 - ambient) * lightDot);
                return col;
            }
            ENDCG
        }
    }
}
