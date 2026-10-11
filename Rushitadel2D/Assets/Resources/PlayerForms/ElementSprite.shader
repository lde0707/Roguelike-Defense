Shader "Rushitadel/ElementSprite"
{
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _ElementColor("Element",Color)=(.65,.55,.8,1) _Pulse("Pulse",Float)=1 _Flash("Shot response",Range(0,1))=0 _RotationAA("Rotation edge coverage",Range(0,1))=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { Tags {"LightMode"="Universal2D"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float4 _ElementColor;float _Pulse,_Flash,_RotationAA;
 struct A{float3 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p);o.uv=a.uv;o.c=a.c;return o;}
 half4 tap(float2 uv){half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);return half4(c.rgb*c.a,c.a);}
 half4 frag(V i):SV_Target
 {
     // Coverage sampling smooths rotating edges without blurring flat pixel interiors.
     float2 dx=ddx(i.uv)*.25*_RotationAA,dy=ddy(i.uv)*.25*_RotationAA;
     half4 c=(tap(i.uv-dx-dy)+tap(i.uv+dx-dy)+tap(i.uv-dx+dy)+tap(i.uv+dx+dy))*.25;
     c.rgb/=max(c.a,.0001);
     float light=dot(c.rgb,float3(.2126,.7152,.0722));
     // Restrained violet lamps, never the slate armor or ivory edge pixels.
     float mask=smoothstep(.04,.16,c.b-c.g)*smoothstep(.03,.12,c.r-c.g);
     c.rgb=lerp(c.rgb,_ElementColor.rgb*min(1.08,_Pulse),mask*.65);
     c.rgb=lerp(c.rgb,float3(.89,.91,.94),_Flash*.08);
     return c*i.c;
 }
 ENDHLSL
 } }
}
