Shader "Rushitadel/PressureCliffBlend"
{
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Left("Left",Float)=-5.4 _Right("Right",Float)=5.4 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { Tags {"LightMode"="Universal2D"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float _Left,_Right;
 struct A {float3 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
 struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;float2 world:TEXCOORD1;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p);o.world=TransformObjectToWorld(a.p).xy;o.uv=a.uv;o.c=a.c;return o;}
 half4 frag(V i):SV_Target
 {
   half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.c;
   float edge=min(i.world.x-_Left,_Right-i.world.x);
   // Only outer ends taper; internal tile joins remain solid. Pixel steps match the map.
   float2 pixel=floor(i.world*48);
   float stepNoise=frac(sin(dot(pixel,float2(12.9898,78.233)))*43758.5453);
   float coverage=saturate(edge/.28);
   c.a*=stepNoise<=coverage && edge>0?1:0;
   c.rgb*=lerp(.65,1,saturate(edge/.4));
   return c;
 }
 ENDHLSL
 } }
}
