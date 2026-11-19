Shader "WJY/Ocean Liner Exhaust"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f,o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv*2.-1.;
                o.color=v.color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p=i.uv;
                float d=min(length(p-float2(-.24,.07))/.66, length(p-float2(.23,.17))/.56);
                d=min(d,length(p-float2(.04,-.26))/.6);
                float density=1.-smoothstep(.12,1.,d);
                float3 tint=i.color.rgb*(.89+.17*saturate(.5+p.y));
                #ifndef UNITY_COLORSPACE_GAMMA
                tint=GammaToLinearSpace(tint);
                #endif
                return fixed4(tint,i.color.a*density*.68);
            }
            ENDCG
        }
    }
}
