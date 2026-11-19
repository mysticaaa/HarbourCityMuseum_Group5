Shader "WJY/Ocean Spectral Water"
{
    Properties
    {
        _Tint ("Water colour", Color) = (1,1,1,1)
        _Strength ("Light", Range(0,1)) = 1
        [Toggle] _Projection ("Silhouette", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" }
        Pass
        {
            Cull Back
            ZWrite Off
            Blend SrcAlpha One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct vertexInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct interpolator
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 world : TEXCOORD1;
                float3 normal : TEXCOORD2;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            fixed4 _Tint;
            half _Strength, _Projection;
            interpolator vert(vertexInput v)
            {
                interpolator o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(interpolator,o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                return o;
            }
            fixed4 frag(interpolator i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                if (_Projection > .5h)
                {
                    half3 n = normalize(i.normal);
                    half3 view = normalize(_WorldSpaceCameraPos - i.world);
                    half rim = pow(1 - saturate(abs(dot(n,view))), 1.8h);
                    half band = .5h + .5h * sin(i.world.y * 14.0h + i.world.x * 1.9h - _Time.y * 1.7h);
                    half wave = pow(band,7.0h);
                    half shade = .19h + .20h * saturate(dot(n,normalize(half3(-.6h,1,.4h)))) + .45h * rim + .16h * wave;
                    return fixed4(lerp(_Tint.rgb,1.0h,rim*.68h),_Strength*shade);
                }
                float2 uv = i.uv*2.0 - 1.0;
                half soft = saturate(1.0 - dot(uv,uv));
                return fixed4(i.color.rgb, soft * soft * i.color.a * _Strength);
            }
            ENDCG
        }
    }
}
