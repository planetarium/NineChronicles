//////////////////////////////////////////////////////////////
/// Shadero Sprite: Sprite Shader Editor - by VETASOFT 2020 //
/// Shader generate with Shadero 1.9.9                      //
/// http://u3d.as/V7t #AssetStore                           //
/// http://www.shadero.com #Docs                            //
//////////////////////////////////////////////////////////////

Shader "UI/UIOutGlow"
{
Properties
{
[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
_Blur_Intensity_1("_Blur_Intensity_1", Range(1, 16)) = 0
_FillColor_Color_1("_FillColor_Color_1", COLOR) = (1,0.7068092,0.09019607,1)
_InnerGlowHQ_Intensity_1("_InnerGlowHQ_Intensity_1", Range(1, 16)) = 5.6
_InnerGlowHQ_Size_1("_InnerGlowHQ_Size_1", Range(1, 16)) = 7.026
_InnerGlowHQ_Color_1("_InnerGlowHQ_Color_1", COLOR) = (1,0.380123,0,1)
_OperationBlend_Fade_2("_OperationBlend_Fade_2", Range(0, 1)) = 0.429
_SpriteFade("SpriteFade", Range(0, 1)) = 1.0

// required for UI.Mask
[HideInInspector]_StencilComp("Stencil Comparison", Float) = 8
[HideInInspector]_Stencil("Stencil ID", Float) = 0
[HideInInspector]_StencilOp("Stencil Operation", Float) = 0
[HideInInspector]_StencilWriteMask("Stencil Write Mask", Float) = 255
[HideInInspector]_StencilReadMask("Stencil Read Mask", Float) = 255
[HideInInspector]_ColorMask("Color Mask", Float) = 15

}

SubShader
{

Tags {"Queue" = "Transparent" "IgnoreProjector" = "true" "RenderType" = "Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
ZWrite Off Blend SrcAlpha OneMinusSrcAlpha Cull Off 

// required for UI.Mask
Stencil
{
Ref [_Stencil]
Comp [_StencilComp]
Pass [_StencilOp]
ReadMask [_StencilReadMask]
WriteMask [_StencilWriteMask]
}

Pass
{

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest
#include "UnityCG.cginc"

struct appdata_t{
float4 vertex   : POSITION;
float4 color    : COLOR;
float2 texcoord : TEXCOORD0;
};

struct v2f
{
float2 texcoord  : TEXCOORD0;
float4 vertex   : SV_POSITION;
float4 color    : COLOR;
};

sampler2D _MainTex;
float _SpriteFade;
float _Blur_Intensity_1;
float4 _FillColor_Color_1;
float _InnerGlowHQ_Intensity_1;
float _InnerGlowHQ_Size_1;
float4 _InnerGlowHQ_Color_1;
float _OperationBlend_Fade_2;

v2f vert(appdata_t IN)
{
v2f OUT;
OUT.vertex = UnityObjectToClipPos(IN.vertex);
OUT.texcoord = IN.texcoord;
OUT.color = IN.color;
return OUT;
}


float4 Blur(float2 uv, sampler2D source, float Intensity)
{
float stepU = 0.00390625f * Intensity;
float stepV = stepU;
float4 result = float4 (0, 0, 0, 0);
float2 texCoord = float2(0, 0);
texCoord = uv + float2(-stepU, -stepV); result += tex2D(source, texCoord);
texCoord = uv + float2(-stepU, 0); result += 2.0 * tex2D(source, texCoord);
texCoord = uv + float2(-stepU, stepV); result += tex2D(source, texCoord);
texCoord = uv + float2(0, -stepV); result += 2.0 * tex2D(source, texCoord);
texCoord = uv; result += 4.0 * tex2D(source, texCoord);
texCoord = uv + float2(0, stepV); result += 2.0 * tex2D(source, texCoord);
texCoord = uv + float2(stepU, -stepV); result += tex2D(source, texCoord);
texCoord = uv + float2(stepU, 0); result += 2.0* tex2D(source, texCoord);
texCoord = uv + float2(stepU, -stepV); result += tex2D(source, texCoord);
result = result * 0.0625;
return result;
}
float4 UniColor(float4 txt, float4 color)
{
txt.rgb = lerp(txt.rgb,color.rgb,color.a);
return txt;
}
float2 ZoomUV(float2 uv, float zoom, float posx, float posy)
{
float2 center = float2(posx, posy);
uv -= center;
uv = uv * zoom;
uv += center;
return uv;
}
float4 OperationBlend(float4 origin, float4 overlay, float blend)
{
float4 o = origin; 
o.a = overlay.a + origin.a * (1 - overlay.a);
o.rgb = (overlay.rgb * overlay.a + origin.rgb * origin.a * (1 - overlay.a)) * (o.a+0.0000001);
o.a = saturate(o.a);
o = lerp(origin, o, blend);
return o;
}
float4 HdrCreate(float4 txt,float value)
{
if (txt.r>0.98) txt.r=2;
if (txt.g>0.98) txt.g=2;
if (txt.b>0.98) txt.b=2;
return lerp(saturate(txt),txt, value);
}
float InnerGlowAlpha(sampler2D source, float2 uv)
{
return (1 - tex2D(source, uv).a);
}
float4 InnerGlow(float2 uv, sampler2D source, float Intensity, float size, float4 color)
{
float step1 = 0.00390625f * size*2;
float step2 = step1 * 2;
float4 result = float4 (0, 0, 0, 0);
float2 texCoord = float2(0, 0);
texCoord = uv + float2(-step2, -step2); result += InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(-step1, -step2); result += 4.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(0, -step2); result += 6.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(step1, -step2); result += 4.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(step2, -step2); result += InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(-step2, -step1); result += 4.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(-step1, -step1); result += 16.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(0, -step1); result += 24.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(step1, -step1); result += 16.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(step2, -step1); result += 4.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(-step2, 0); result += 6.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(-step1, 0); result += 24.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv; result += 36.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(step1, 0); result += 24.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(step2, 0); result += 6.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(-step2, step1); result += 4.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(-step1, step1); result += 16.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(0, step1); result += 24.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(step1, step1); result += 16.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(step2, step1); result += 4.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(-step2, step2); result += InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(-step1, step2); result += 4.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(0, step2); result += 6.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(step1, step2); result += 4.0 * InnerGlowAlpha(source, texCoord);
texCoord = uv + float2(step2, step2); result += InnerGlowAlpha(source, texCoord);
result = result*0.00390625;
result = lerp(tex2D(source,uv),color*Intensity,result*color.a);
result.a = tex2D(source, uv).a;
return saturate(result);
}
float4 frag (v2f i) : COLOR
{
float2 ZoomUV_1 = ZoomUV(i.texcoord,1.012,1,1);
float4 _Blur_1 = Blur(ZoomUV_1,_MainTex,_Blur_Intensity_1);
float4 FillColor_1 = UniColor(_Blur_1,_FillColor_Color_1);
float4 HdrCreate_1 = HdrCreate(FillColor_1,1);
float2 ZoomUV_2 = ZoomUV(i.texcoord,1.069,0.5,0.5);
float4 _MainTex_1 = tex2D(_MainTex,ZoomUV_2);
float4 OperationBlend_1 = OperationBlend(HdrCreate_1, _MainTex_1, 1); 
float2 ZoomUV_3 = ZoomUV(i.texcoord,1,0.5,0.5);
float4 _InnerGlowHQ_1 = InnerGlow(ZoomUV_3,_MainTex,_InnerGlowHQ_Intensity_1,_InnerGlowHQ_Size_1,_InnerGlowHQ_Color_1);
float4 OperationBlend_2 = OperationBlend(OperationBlend_1, _InnerGlowHQ_1, _OperationBlend_Fade_2); 
float4 FinalResult = OperationBlend_2;
FinalResult.rgb *= i.color.rgb;
FinalResult.a = FinalResult.a * _SpriteFade * i.color.a;
return FinalResult;
}

ENDCG
}
}
Fallback "Sprites/Default"
}
