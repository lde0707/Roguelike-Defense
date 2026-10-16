Shader "Rushitadel/ElementSprite"
{
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _ElementColor("Element",Color)=(.65,.55,.8,1) _Pulse("Pulse",Float)=1 _Flash("Shot response",Range(0,1))=0 _RotationAA("Rotation edge coverage",Range(0,1))=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { Tags {"LightMode"="Universal2D"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float4 _ElementColor;float _Pulse,_Flash,_RotationAA,_CoreDisc,_CompactTurret;
 struct A{float3 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p);o.uv=a.uv;o.c=a.c;return o;}
 half4 tap(float2 uv){half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);return half4(c.rgb*c.a,c.a);}
 half4 frag(V i):SV_Target
 {
     if(_CompactTurret>.5)
     {
         // Uniform 18px output grid (previous art: 20px). Only the barrel above
         // the mount shoulder is shortened; the round mount is not squashed.
         i.uv=(floor(i.uv*18)+.5)/18;
         if(i.uv.y>.45)i.uv.y=.45+(i.uv.y-.45)/.75;
         if(i.uv.y>=1)return half4(0,0,0,0);
     }
     // Coverage sampling smooths rotating edges without blurring flat pixel interiors.
     float2 dx=ddx(i.uv)*.25*_RotationAA,dy=ddy(i.uv)*.25*_RotationAA;
     half4 c=(tap(i.uv-dx-dy)+tap(i.uv+dx-dy)+tap(i.uv-dx+dy)+tap(i.uv+dx+dy))*.25;
     if(_CompactTurret>.5 && i.uv.y>.45 && c.a>.5)
     {
         // Preserve narrow mint muzzle lamps when a shorter barrel skips a source row.
         half4 above=tap(i.uv+float2(0,.025)),below=tap(i.uv-float2(0,.025));
         if(above.a>.5 && above.g-above.r>.08 && above.b-above.r>.06)c=above;
         else if(below.a>.5 && below.g-below.r>.08 && below.b-below.r>.06)c=below;
     }
     c.rgb/=max(c.a,.0001);
     float light=dot(c.rgb,float3(.2126,.7152,.0722));
     // Restrained violet lamps, never the slate armor or ivory edge pixels.
     float mask=smoothstep(.04,.16,c.b-c.g)*smoothstep(.03,.12,c.r-c.g);
     c.rgb=lerp(c.rgb,_ElementColor.rgb*min(1.08,_Pulse),mask*.65);
     c.rgb=lerp(c.rgb,float3(.89,.91,.94),_Flash*.08);
     if(_CoreDisc>.5)
     {
         // 24px logical canvas: pixels are one third larger than the previous 32px core.
         // Keep the world diameter and central hit circle unchanged.
         float2 p=(floor(i.uv*24)+.5)/24-.5;
         float r=length(p);
         c=half4(.12,.14,.20,r<=.5?1:0);
         if(r<.45)c.rgb=half3(.69,.73,.78);
         if(r<.365)c.rgb=half3(.27,.31,.40);
         // Four symmetric armor seams remain readable while the unit rotates.
         if(r>.365 && r<.45 && min(abs(p.x),abs(p.y))<.025)c.rgb=half3(.27,.31,.40);
         if(r<.26)c.rgb=half3(.12,.14,.20);
         if(r<.205)c.rgb=half3(.62,.47,.82);
         if((abs(p.x)<.025 && abs(p.y)<.125) || (abs(p.y)<.025 && abs(p.x)<.125))c.rgb=half3(.69,.73,.78);
     }
     return c*i.c;
 }
 ENDHLSL
 } }
}
