Shader "Rushitadel/Title UI Effects"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
        _Scroll ("Vertical scroll", Float) = 0
        _Scrolling ("Scrolling background", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1; float2 canvas:TEXCOORD2; };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd;
            float4 _ClipRect, _CursorLens;
            float4x4 _WorldToCanvas;
            float _Scroll, _Scrolling;
            v2f vert(appdata v)
            {
                v2f o;
                o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex);
                o.canvas=mul(_WorldToCanvas,mul(unity_ObjectToWorld,v.vertex)).xy;
                o.uv=v.uv; o.color=v.color*_Color;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;
                // Mirrored repeats join the same edge: no blank strip or abrupt tile seam.
                if (_Scrolling > .5) uv.y=1-abs(frac((uv.y+_Scroll)*.5)*2-1);
                fixed4 c=(tex2D(_MainTex,uv)+_TextureSampleAdd)*i.color;
                float2 pixel=floor(i.canvas/3)*3+1.5;
                if (_CursorLens.w > .5 && distance(pixel,_CursorLens.xy) < _CursorLens.z)
                    c.rgb=1-saturate(c.rgb);
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
