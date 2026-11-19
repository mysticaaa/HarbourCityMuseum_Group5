Shader "WJY/Open Water at Dusk"
{
    Properties { [HideInInspector] _Surface ("Surface", Float) = 0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            float _Surface;
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 local : TEXCOORD0;
                float3 world : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float kind : TEXCOORD3;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float hash(float2 p)
            {
                float3 q = frac(p.xyx * float3(.1031, .1030, .0973));
                q += dot(q, q.yzx + 33.33);
                return frac((q.x + q.y) * q.z);
            }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f*f*(3.-2.*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),
                            lerp(hash(i+float2(0,1)),hash(i+1.),f.x),f.y);
            }
            // Moon and sky colours are shared with the reflection on the moving sea.
            static const float3 moon = float3(.337,.267,.903);
            float3 skyGradient(float3 d)
            {
                float facing = saturate(dot(d, moon));
                float3 horizon = lerp(float3(.33,.40,.57),float3(.86,.58,.56),pow(facing,4.));
                float3 zenith = lerp(float3(.036,.074,.17),float3(.095,.14,.30),facing);
                return lerp(horizon,zenith,saturate(1.-exp(-5.2*max(0.,d.y))));
            }
            float stars(float3 d, float count)
            {
                // Octahedral coordinates keep the star spacing finite at the zenith.
                float2 p=d.xz/(abs(d.x)+max(0.,d.y)+abs(d.z)+.001)*count;
                float2 cell=floor(p);
                float n=hash(cell);
                float2 centre=.23+.54*float2(hash(cell+12.1),hash(cell-4.2));
                float distance=length(frac(p)-centre);
                float size=.027+.05*n;
                float aa=clamp(max(fwidth(p.x),fwidth(p.y)),.016,.13);
                return smoothstep(.90,.997,n)*(1.-smoothstep(size,size+aa,distance)) * saturate(size/aa);
            }
            float3 sky(float3 d)
            {
                float3 color=skyGradient(d);
                float moonDistance=length(d-moon);
                color+=float3(.27,.20,.14)*exp(-moonDistance*8.);
                color+=float3(.34,.26,.17)*exp(-moonDistance*32.);
                float disk=1.-smoothstep(.027,.03,moonDistance);
                float lunarDetail=noise(d.xy*145.)*.10;
                color=lerp(color,float3(1.,.92,.74)-lunarDetail,disk);
                float band=exp(-11.*abs(dot(d,normalize(float3(-.43,.78,.31)))-.22));
                float veil=noise(d.xz*12.)*.6+noise(d.xy*27.)*.4;
                float top=smoothstep(.025,.32,d.y);
                color+=float3(.16,.17,.25)*band*veil*top;
                color+=float3(.73,.88,1.)*(stars(d,134.)+.55*stars(d,213.))*top;
                // Thin layers of dusk cloud retain a clear moon and an open horizon.
                float2 uv = d.xz / (.13+max(.01,d.y));
                float cloud = noise(uv*float2(1.2,2.7))*.7 + noise(uv*5.7)*.3;
                float bank = smoothstep(.57,.77,cloud)*exp(-6.*abs(d.y-.12))*(1.-disk);
                color=lerp(color,float3(.49,.42,.54),bank*.38);
                return color;
            }
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f,o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                if(_Surface>.5 && _Surface<1.5)
                {
                    float t=_Time.y;
                    float height=.011*sin(dot(v.vertex.xz,float2(.71,.97))+t*.83)
                              +.007*sin(dot(v.vertex.xz,float2(-1.73,.58))-t*.62);
                    v.vertex.y+=height*lerp(.65,2.2,smoothstep(7.,30.,length(v.vertex.xz)));
                }
                o.pos=UnityObjectToClipPos(v.vertex);
                o.local=v.vertex.xyz;
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.kind=v.uv.x;
                o.color=v.color;
                return o;
            }
            float3 water(float2 p, float3 V)
            {
                float t=_Time.y;
                float bend=noise(p*.32+float2(t*.045,0.));
                float2 q=p+float2(bend,noise(p*.49-t*.036))*1.7;
                float a=sin(dot(q,float2(.78,1.38))+t*.83);
                float b=sin(dot(q,float2(-1.63,.73))-t*.62);
                float c=sin(dot(q,float2(4.3,3.2))+a*.78+t*1.15);
                float fine=sin(dot(q,float2(8.7,-4.2))+b+t*1.24);
                float farDetail=1.-smoothstep(9.,40.,length(p));
                float swell=1.-smoothstep(16.,55.,length(p))*.68;
                float3 n=normalize(float3((a*.042+b*.026)*swell+c*.026*farDetail,1.,
                                           (b*.042-a*.018)*swell+fine*.022*farDetail));
                float3 r=reflect(-V,n);
                float facing=saturate(dot(n,V));
                float fresnel=.25+.72*pow(1.-facing,3.);
                float3 under=lerp(float3(.021,.12,.18),float3(.043,.28,.32),.52+.23*a+.22*b);
                float3 color=lerp(under,skyGradient(r),fresnel);
                float moonDot=saturate(dot(r,moon));
                color+=float3(.83,.68,.47)*(pow(moonDot,240.)*.76+pow(moonDot,24.)*.14);
                // Luminous crests fragment into grains as opposing ripples cross.
                float crest=pow(saturate(.47+.29*c+.24*fine),8.);
                float lace=noise(p*float2(.83,.64)+float2(t*.065,0.));
                color+=float3(.07,.44,.41)*crest*smoothstep(.51,.84,lace)*farDetail;
                float horizon=smoothstep(38.,65.,length(p));
                return lerp(color,skyGradient(normalize(float3(-V.x,0.,-V.z))),horizon);
            }
            fixed4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 rgb;
                if(_Surface<.5) rgb=sky(normalize(i.world-_WorldSpaceCameraPos));
                else if(_Surface<1.5) rgb=water(i.local.xz,normalize(_WorldSpaceCameraPos-i.world));
                else
                {
                    float3 n=normalize(i.normal);
                    float lit=.64+.34*saturate(dot(n,normalize(float3(-.35,.86,.38))));
                    float lampLight=saturate(1.-length(i.local-float3(-1.28,.91,-1.32))/2.8);
                    rgb=i.color.rgb*lit*float3(.88,.90,1.03)+i.color.rgb*float3(.26,.13,.035)*lampLight;
                    if(i.kind<.5)
                    {
                        float grain=noise(float2(i.local.x*128.+i.local.y*23.,i.local.z*3.1));
                        float rings=sin(125.*i.local.x+grain*2.+sin(i.local.z*3.));
                        rgb*=.88+.20*grain+.06*rings;
                    }
                    if(i.kind>1.5) rgb=float3(1.,.80,.45);
                }
                #ifndef UNITY_COLORSPACE_GAMMA
                    rgb=GammaToLinearSpace(saturate(rgb));
                #endif
                return fixed4(rgb,1.);
            }
            ENDCG
        }
    }
}
