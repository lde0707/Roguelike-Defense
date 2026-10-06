Shader "Rushitadel/PixelGlow"
{
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Intensity("Intensity",Float)=.2 }
 SubShader { Tags {"Queue"="Transparent+1" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off Blend SrcAlpha One
 Pass { Tags {"LightMode"="Universal2D"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float _Intensity;struct A{float3 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p);o.uv=a.uv;o.c=a.c;return o;}
 half4 frag(V i):SV_Target{float2 p=(floor(i.uv*16)+.5)/16-.5;float a=saturate(1-length(p)*2);return half4(i.c.rgb,a*a*_Intensity*i.c.a);}
 ENDHLSL
 } }
}
