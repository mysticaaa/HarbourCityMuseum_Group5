Shader "WJY/Fishing Teleport Marker"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.07, 0.8, 1, 1)
        _GuideInvRadius ("Guide inverse radius", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 disc : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            fixed4 _Tint;
            float _GuideInvRadius;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Tint;
                o.disc = v.vertex.xz * _GuideInvRadius;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Sweep a soft wave across the translucent disc. The opaque rim and aiming beam stay steady.
                float waveRadius = frac(_Time.y * 0.7) * 1.25 - 0.12;
                float wave = 1.0 - smoothstep(0.02, 0.12, abs(length(i.disc) - waveRadius));
                i.color.a = lerp(i.color.a, 1.0, wave * 0.8 * step(0.001, _GuideInvRadius));
                return i.color;
            }
            ENDCG
        }
    }
}
