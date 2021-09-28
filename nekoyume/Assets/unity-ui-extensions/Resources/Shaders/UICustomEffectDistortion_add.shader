// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UI Extensions/Particles/EffectDistortion_add"
{
	Properties 
	{
		_MainTex ("Base (RGB), Alpha (A)", 2D) = "white" {}
		_NoiseTex ("Noise (RGB)", 2D) = "black" {}

		[HDR]_Color ("Color", Color) = (1,1,1,1)
		_Strength ("Strength", Range(0.01, 0.1)) = 0.01
		_Speed ("Speed", Range(0.01, 1.0)) = 0.2
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
			Blend SrcAlpha One

			CGPROGRAM
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _NoiseTex;
			half4 _MainTex_ST;
			half4 _NoiseTex_ST;

			float4 _Color;
			fixed _Strength;
			float _Speed;

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

				float4 param1 : TEXCOORD1; // xy: uv - _Time.xz * _Speed, zw: uv + _Time.yx * _Speed
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				//o.texcoord = v.texcoord;
				
				o.texcoord = TRANSFORM_TEX( v.texcoord, _MainTex);

				o.color = v.color;
				o.color.rgba *= _Color;

				//- param1 
				half2 noiseUV = TRANSFORM_TEX( v.texcoord, _NoiseTex);
                //o.param1.xy = noiseUV - _Time.xz * _Speed;
                //o.param1.zw = noiseUV + _Time.yx * _Speed;

                o.param1.xy = noiseUV - float2(_MyTimeX, _MyTimeZ) * _Speed;
                o.param1.zw = noiseUV + float2(_MyTimeY, _MyTimeX) * _Speed;
                //---------------
                return o;
			}

			half4 frag (v2f IN) : COLOR
			{
				//---------------
				//half2 uv = IN.texcoord;
				//fixed4 offsetA = tex2D(_NoiseTex, uv - _Time.xz * _Speed);
				//fixed4 offsetB = tex2D(_NoiseTex, uv + _Time.yx * _Speed);
			
				//uv.x += ((offsetA.r + offsetB.g) - 1.0) * _Strength;
				//uv.y += ((offsetA.g + offsetB.b) - 1.0) * _Strength;
			
				//return tex2D(_MainTex, uv) * _Color * IN.color.rgba;
				//---------------

				float2 uv = IN.texcoord;
				fixed4 offsetA = tex2D(_NoiseTex, IN.param1.xy);
				fixed4 offsetB = tex2D(_NoiseTex, IN.param1.zw);

				uv.x += ((offsetA.r + offsetB.g) - 1.0) * _Strength;
				uv.y += ((offsetA.g + offsetB.b) - 1.0) * _Strength;

				return tex2D(_MainTex, uv) * IN.color.rgba;
			}
			ENDCG
		}
	}
}
