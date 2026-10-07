Shader "Rushitadel/FlightVfx"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        _Progress("Progress",Range(0,1))=0
        _Mode("0 halo, 1 ring, 2 muzzle, 3 plume",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent+1" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Cull Off ZWrite Off Blend SrcAlpha One
        Pass
        {
            Tags {"LightMode"="Universal2D"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _Progress,_Mode;
            struct A {float3 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
            struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
            V vert(A a) {V o;o.p=TransformObjectToHClip(a.p);o.uv=a.uv;o.c=a.c;return o;}
            half4 frag(V i):SV_Target
            {
                float2 p=(floor(i.uv*24)+.5)/24-.5;
                float r=length(p), a=0;
                if(_Mode<.5) a=pow(saturate(1-r*2),2);
                else if(_Mode<1.5)
                {
                    float ring=lerp(.14,.46,_Progress);
                    a=saturate(1-abs(r-ring)*36)*(1-_Progress);
                }
                else if(_Mode<2.5)
                {
                    float width=lerp(.3,.02,saturate(p.y+.5));
                    a=step(abs(p.x),width)*step(abs(p.y),.46)*(1-_Progress);
                    a*=.65+.35*step(abs(p.x),width*.4);
                }
                else if(_Mode<3.5)
                {
                    float width=(.5-p.y)*.3;
                    a=step(abs(p.x),width)*saturate(1-abs(p.y)*2)*(1-_Progress);
                }
                else
                {
                    float angle=atan2(p.y,p.x);
                    float sector=frac(angle/6.2831853*5+.5);
                    a=saturate(1-abs(r-.43)*42)*saturate(1-sector*2.5);
                }
                return half4(i.c.rgb,a*i.c.a);
            }
            ENDHLSL
        }
    }
}
