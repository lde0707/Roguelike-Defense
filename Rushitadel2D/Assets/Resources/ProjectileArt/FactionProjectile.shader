Shader "Rushitadel/FactionProjectile"
{
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Hostile("Hostile",Float)=0 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   Tags {"LightMode"="Universal2D"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float _Hostile;
   struct A{float3 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
   V vert(A a){V o;o.p=TransformObjectToHClip(a.p);o.uv=a.uv;o.c=a.c;return o;}
   half4 frag(V i):SV_Target
   {
    half4 source=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
    float value=dot(source.rgb,float3(.2126,.7152,.0722));
    // A fixed faction palette preserves the source silhouette and stepped pixel shading.
    // Bright friendly cores versus warm hostile cores stay distinct without relying on hue alone.
    float3 dark=lerp(float3(.08,.13,.24),float3(.20,.10,.15),_Hostile);
    float3 mid=lerp(float3(.38,.69,.79),float3(.78,.30,.25),_Hostile);
    float3 bright=lerp(float3(.86,.94,.91),float3(.98,.79,.52),_Hostile);
    float3 color=value<.22?dark:value<.65?mid:bright;
    return half4(color*i.c.rgb,source.a*i.c.a);
   }
   ENDHLSL
  }
 }
}
