Shader "Custom/Distortion_Additive_Masked"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _NoiseTex ("Texture", 2D) = "black" {}
        _Size("Noise Size", Range(0.001, 50)) = 1
        _Speed("Noise Speed", Range(0.001, 5)) = 1
        _Mag("Magnitude", Range(0.0001, 0.1)) = 1
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
        }
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityUI.cginc"
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
            	fixed4 color	: COLOR;
            	half2 texcoord  : TEXCOORD0;
            	float4 worldPosition : TEXCOORD1;
                float4 vertex   : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Size;
            float _Speed;
            float _Mag;
            float4 _ClipRect;

            v2f vert(appdata v)
            {
            	v2f OUT;
            	OUT.worldPosition = v.vertex;
            	OUT.vertex = UnityObjectToClipPos(v.vertex);
            	OUT.texcoord = TRANSFORM_TEX(v.uv, _MainTex);
            	return OUT;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture

                fixed2 uv = tex2D(_NoiseTex, i.texcoord * _Size + (_Time.y * _Speed)).rg * 2 - 1;
                fixed4 col = tex2D(_MainTex, i.texcoord + uv * _Mag);

            	col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                return col * _Color;
            }
            ENDCG
        }
    }
}
