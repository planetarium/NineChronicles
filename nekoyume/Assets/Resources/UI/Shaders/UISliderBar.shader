
Shader "Custom/UI"
{
Properties
{
[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
DistortionUV_WaveX_1("DistortionUV_WaveX_1", Range(0, 128)) = 10
DistortionUV_WaveY_1("DistortionUV_WaveY_1", Range(0, 128)) = 20.103
DistortionUV_DistanceX_1("DistortionUV_DistanceX_1", Range(0, 1)) = 0.139
DistortionUV_DistanceY_1("DistortionUV_DistanceY_1", Range(0, 1)) = 0.13
DistortionUV_Speed_1("DistortionUV_Speed_1", Range(-2, 2)) = 1
FishEyeUV_Size_1("FishEyeUV_Size_1", Range(0, 0.5)) = 0.227
ResizeUV_X_1("ResizeUV_X_1", Range(-1, 1)) = 0.107
ResizeUV_Y_1("ResizeUV_Y_1", Range(-1, 1)) = -0.232
ResizeUV_ZoomX_1("ResizeUV_ZoomX_1", Range(0.1, 3)) = 1.213
ResizeUV_ZoomY_1("ResizeUV_ZoomY_1", Range(0.1, 3)) = 1.11
_DisplacementPack_ValueX_1("_DisplacementPack_ValueX_1", Range(-1, 1)) = 0.214
_DisplacementPack_ValueY_1("_DisplacementPack_ValueY_1", Range(-1, 1)) = 0.214
_DisplacementPack_Size_1("_DisplacementPack_Size_1", Range(-3, 3)) = 0.377
DisplacementPack_1("DisplacementPack_1(RGB)", 2D) = "white" { }
_TintRGBA_Color_1("_TintRGBA_Color_1", COLOR) = (1,1,1,1)
_FadeToAlpha_Fade_1("_FadeToAlpha_Fade_1", Range(0, 1)) = 0
_Mul_Fade_1("_Mul_Fade_1", Range(0, 1)) = 1
_NewTex_3("NewTex_3(RGB)", 2D) = "white" { }
PositionUV_X_1("PositionUV_X_1", Range(-2, 2)) = 1.607
PositionUV_Y_1("PositionUV_Y_1", Range(-2, 2)) = 0.257
_NewTex_4("NewTex_4(RGB)", 2D) = "white" { }
_NewTex_1("NewTex_1(RGB)", 2D) = "white" { }
_TintRGBA_Color_2("_TintRGBA_Color_2", COLOR) = (0,0.07343698,1,1)
_TintRGBA_Color_3("_TintRGBA_Color_3", COLOR) = (1,1,1,1)
_NewTex_2("NewTex_2(RGB)", 2D) = "white" { }
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
float DistortionUV_WaveX_1;
float DistortionUV_WaveY_1;
float DistortionUV_DistanceX_1;
float DistortionUV_DistanceY_1;
float DistortionUV_Speed_1;
float FishEyeUV_Size_1;
float ResizeUV_X_1;
float ResizeUV_Y_1;
float ResizeUV_ZoomX_1;
float ResizeUV_ZoomY_1;
float _DisplacementPack_ValueX_1;
float _DisplacementPack_ValueY_1;
float _DisplacementPack_Size_1;
sampler2D DisplacementPack_1;
float4 _TintRGBA_Color_1;
float _FadeToAlpha_Fade_1;
float _Mul_Fade_1;
sampler2D _NewTex_3;
float PositionUV_X_1;
float PositionUV_Y_1;
sampler2D _NewTex_4;
sampler2D _NewTex_1;
float4 _TintRGBA_Color_2;
float4 _TintRGBA_Color_3;
sampler2D _NewTex_2;

v2f vert(appdata_t IN)
{
v2f OUT;
OUT.vertex = UnityObjectToClipPos(IN.vertex);
OUT.texcoord = IN.texcoord;
OUT.color = IN.color;
return OUT;
}


float2 DistortionUV(float2 p, float WaveX, float WaveY, float DistanceX, float DistanceY, float Speed)
{
Speed *=_Time*100;
p.x= p.x+sin(p.y*WaveX + Speed)*DistanceX*0.05;
p.y= p.y+cos(p.x*WaveY + Speed)*DistanceY*0.05;
return p;
}
float4 TintRGBA(float4 txt, float4 color)
{
float3 tint = dot(txt.rgb, float3(.222, .707, .071));
tint.rgb *= color.rgb;
txt.rgb = lerp(txt.rgb,tint.rgb,color.a);
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
float2 ResizeUV(float2 uv, float offsetx, float offsety, float zoomx, float zoomy)
{
uv += float2(offsetx, offsety);
uv = fmod(uv * float2(zoomx*zoomx, zoomy*zoomy), 1);
return uv;
}

float2 ResizeUVClamp(float2 uv, float offsetx, float offsety, float zoomx, float zoomy)
{
uv += float2(offsetx, offsety);
uv = fmod(clamp(uv * float2(zoomx*zoomx, zoomy*zoomy), 0.0001, 0.9999), 1);
return uv;
}
float2 FishEyeUV(float2 uv, float size)
{
float2 m = float2(0.5, 0.5);
float2 d = uv - m;
float r = sqrt(dot(d, d));
float power = (2.0 * 3.141592 / (2.0 * sqrt(dot(m, m)))) * (size+0.001);
float bind = sqrt(dot(m, m));
uv = m + normalize(d) * tan(r * power) * bind / tan(bind * power);
return uv;
}
float4 HdrCreate(float4 txt,float value)
{
if (txt.r>0.98) txt.r=2;
if (txt.g>0.98) txt.g=2;
if (txt.b>0.98) txt.b=2;
return lerp(saturate(txt),txt, value);
}
float2 PositionUV(float2 uv, float offsetx, float offsety)
{
uv += float2(offsetx, offsety);
return uv;
}

float4 FadeToAlpha(float4 txt,float fade)
{
return float4(txt.rgb, txt.a*fade);
}

float4 DisplacementPack(float2 uv,sampler2D source,float x, float y, float value, float motion, float motion2)
{
float t=_Time.y;
float2 mov =float2(x*t,y*t)*motion;
float2 mov2 =float2(x*t*2,y*t*2)*motion2;
float4 rgba=tex2D(source, uv + mov);
float4 rgba2=tex2D(source, uv + mov2);
float r=(rgba2.r+rgba2.g+rgba2.b)/3;
r*=rgba2.a;
uv+=mov2*0.25;
return tex2D(source,lerp(uv,uv+float2(rgba.r*x,rgba.g*y),value*r));
}
float4 frag (v2f i) : COLOR
{
float2 DistortionUV_1 = DistortionUV(i.texcoord,DistortionUV_WaveX_1,DistortionUV_WaveY_1,DistortionUV_DistanceX_1,DistortionUV_DistanceY_1,DistortionUV_Speed_1);
float2 ZoomUV_1 = ZoomUV(DistortionUV_1,2.347,0.5,0.5);
float2 FishEyeUV_1 = FishEyeUV(ZoomUV_1,FishEyeUV_Size_1);
float2 ResizeUV_1 = ResizeUV(FishEyeUV_1,ResizeUV_X_1,ResizeUV_Y_1,ResizeUV_ZoomX_1,ResizeUV_ZoomY_1);
float4 _DisplacementPack_1 = DisplacementPack(ResizeUV_1,DisplacementPack_1,_DisplacementPack_ValueX_1,_DisplacementPack_ValueY_1,_DisplacementPack_Size_1,1,1);
float4 TintRGBA_1 = TintRGBA(_DisplacementPack_1,_TintRGBA_Color_1);
float4 FadeToAlpha_1 = FadeToAlpha(_DisplacementPack_1,_FadeToAlpha_Fade_1);
TintRGBA_1 = lerp(TintRGBA_1,TintRGBA_1 * FadeToAlpha_1,_Mul_Fade_1);
float4 NewTex_3 = tex2D(_NewTex_3, i.texcoord);
float2 PositionUV_1 = PositionUV(FishEyeUV_1,PositionUV_X_1,PositionUV_Y_1);
float4 NewTex_4 = tex2D(_NewTex_4,PositionUV_1);
NewTex_3 = lerp(NewTex_3,NewTex_3 * NewTex_4,1);
float4 MaskRGBA_1=TintRGBA_1;
MaskRGBA_1.a = lerp(NewTex_3.r * TintRGBA_1.a, (1 - NewTex_3.r) * TintRGBA_1.a,0);
float4 NewTex_1 = tex2D(_NewTex_1,PositionUV_1);
float4 TintRGBA_2 = TintRGBA(NewTex_1,_TintRGBA_Color_2);
MaskRGBA_1 = lerp(MaskRGBA_1,MaskRGBA_1*MaskRGBA_1.a + TintRGBA_2*TintRGBA_2.a,1);
float4 TintRGBA_3 = TintRGBA(NewTex_4,_TintRGBA_Color_3);
MaskRGBA_1 = lerp(MaskRGBA_1,MaskRGBA_1*MaskRGBA_1.a + TintRGBA_3*TintRGBA_3.a,1);
NewTex_3 = lerp(NewTex_3,NewTex_3 * NewTex_4,1);
float4 MaskRGBA_2=MaskRGBA_1;
MaskRGBA_2.a = lerp(NewTex_3.r, 1 - NewTex_3.r ,0);
float4 NewTex_2 = tex2D(_NewTex_2, i.texcoord);
float4 OperationBlend_1 = OperationBlend(MaskRGBA_2, NewTex_2, 1); 
float4 HdrCreate_1 = HdrCreate(OperationBlend_1,0.256);
float4 FinalResult = HdrCreate_1;
FinalResult.rgb *= i.color.rgb;
FinalResult.a = FinalResult.a * _SpriteFade * i.color.a;
return FinalResult;
}

ENDCG
}
}
Fallback "Sprites/Default"
}
