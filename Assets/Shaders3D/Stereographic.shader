Shader "Unlit/Stereographic" {
    Properties {
        _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
        _Ambient("Ambient", Float) = 0.5
    }
    SubShader {
        Tags { "Queue"="Transparent" }
        LOD 100

        Pass {
            Cull Back
            ZTest LEqual
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #define H 1.01
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                //GPU instancing
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float alpha : TEXCOORD0;
                //GPU instancing
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float, _Ambient)
            UNITY_DEFINE_INSTANCED_PROP(float4x4, _CMatrix)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v) {
                v2f o;

                //Instancing setup
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float4x4 cmat = UNITY_ACCESS_INSTANCED_PROP(Props, _CMatrix);
                float4 d = mul(cmat, float4(v.vertex.xyz, 0.0));

                float a = (H + 1.0f) / (H + d.y);
                float3 flatDirection = 0.4f * a * d.xzw;

                o.alpha = min(1.0 - d.w * d.w, smoothstep(0.0, 0.2, 0.38 + d.y));
                o.alpha = min(o.alpha, 1.0 - 2.0 * v.vertex.z * v.vertex.z);
                o.vertex = UnityObjectToClipPos(flatDirection);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                //Instancing setup
                UNITY_SETUP_INSTANCE_ID(i);

                if (i.alpha <= 0.0) { discard; }

                // sample the texture
                float4 col = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                float ambient = UNITY_ACCESS_INSTANCED_PROP(Props, _Ambient);
                float lightDot = dot(normalize(i.normal), _WorldSpaceLightPos0.xyz);
                col.rgb *= saturate(ambient + (1.0 - ambient) * lightDot);
                col.a *= i.alpha;
                return col;
            }
            ENDCG
        }
    }
}
