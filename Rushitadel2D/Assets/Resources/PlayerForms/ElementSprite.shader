Shader "Rushitadel/ElementSprite"
{
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _ElementColor("Element",Color)=(.65,.25,1,1) _Pulse("Pulse",Float)=1 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { Tags {"LightMode"="Universal2D"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float4 _ElementColor;float _Pulse;
 struct A{float3 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p);o.uv=a.uv;o.c=a.c;return o;}
 half4 frag(V i):SV_Target{half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);float mask=saturate((min(c.r,c.b)-c.g-.05)*8);float luminance=max(c.r,c.b);c.rgb=lerp(c.rgb,_ElementColor.rgb*luminance*1.25*_Pulse,mask);return c*i.c;}
 ENDHLSL
 } }
}
