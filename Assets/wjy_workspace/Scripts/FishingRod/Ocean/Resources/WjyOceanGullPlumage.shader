Shader "WJY/Ocean Gull Plumage"
{
    Properties { [HideInInspector] _WingPose ("Shoulder and wrist angle", Vector) = (0,0,0,0) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            float4 _WingPose;
            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 position : SV_POSITION;
                fixed3 plumage : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(Varyings, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 p = v.vertex.xyz;
                float3 normal = v.normal;
                if (v.color.a > .5 && p.z > -.205)
                {
                    float side = sign(p.x);
                    float span = max(0., abs(p.x) - .065);
                    float inner = min(span, .325);
                    float outer = span - inner;
                    float2 angle = _WingPose.xx + float2(0., _WingPose.y);
                    float2 sine, cosine;
                    sincos(angle, sine, cosine);
                    p.x = side * (.065 + dot(float2(inner, outer), cosine));
                    p.y += dot(float2(inner, outer), sine);
                    float s = side * (outer > 0. ? sine.y : sine.x);
                    float c = outer > 0. ? cosine.y : cosine.x;
                    normal.xy = float2(c * normal.x - s * normal.y, s * normal.x + c * normal.y);
                }
                o.position = UnityObjectToClipPos(float4(p, 1.));
                float light = .72 + .24 * abs(dot(UnityObjectToWorldNormal(normal), normalize(float3(.34,.79,.51))));
                fixed3 color = v.color.rgb * light * fixed3(.88,.94,1.);
                #ifndef UNITY_COLORSPACE_GAMMA
                    color = GammaToLinearSpace(color);
                #endif
                o.plumage = color;
                return o;
            }
            fixed4 frag(Varyings i) : SV_Target { return fixed4(i.plumage, 1.); }
            ENDCG
        }
    }
}
