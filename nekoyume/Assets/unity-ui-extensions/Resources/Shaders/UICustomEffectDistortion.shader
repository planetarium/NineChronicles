// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UI Extensions/Particles/EffectDistortion"
{
	Properties 
	{
		_MainTex ("Base (RGB), Alpha (A)", 2D) = "white" {}
		_NoiseTex ("Noise (RGB)", 2D) = "black" {}
		[HDR]_Color ("Color", Color) = (1,1,1,1)
		_Strength ("Strength", Range(0.01, 0.1)) = 0.01
		_Speed ("Speed", Range(0.01, 1.0)) = 0.2
		_PixelMultiply("Pixel Multiply", Range(1, 10)) = 1.0
	}
	SubShader 
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		Pass
		{	
			LOD 100
			Cull Off
			Lighting Off
			ZWrite Off
			AlphaTest Greater .01
			Offset -1, -1
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _NoiseTex;
			float4 _Color;
			fixed _Strength;
			float _Speed;
			half _PixelMultiply;

			half4 _MainTex_ST;
			half4 _NoiseTex_ST;

            float _MyTimeX;
            float _MyTimeY;
            float _MyTimeZ;

            struct appdata_t
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float2 noise_texcoord : TEXCOORD1;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.noise_texcoord = TRANSFORM_TEX(v.texcoord, _NoiseTex);
				o.color = v.color;
				return o;
			}

			half4 frag (v2f IN) : COLOR
			{
				float2 uv = IN.texcoord;
                //half4 offsetA = tex2D(_NoiseTex, IN.noise_texcoord - _Time.xz * _Speed);
                //half4 offsetB = tex2D(_NoiseTex, IN.noise_texcoord + _Time.yx * _Speed);
                
                half4 offsetA = tex2D(_NoiseTex, IN.noise_texcoord - float2(_MyTimeX, _MyTimeZ) * _Speed);
                half4 offsetB = tex2D(_NoiseTex, IN.noise_texcoord + float2(_MyTimeY, _MyTimeX) * _Speed);

                uv.x += ((offsetA.r + offsetB.g) - 1.0) * _Strength;
				                uv.y += ((offsetA.g + offsetB.b) - 1.0) * _Strength;
			
				return tex2D(_MainTex, uv) * _PixelMultiply * _Color * IN.color.rgba;
			}
			ENDCG
		}
	}
}
