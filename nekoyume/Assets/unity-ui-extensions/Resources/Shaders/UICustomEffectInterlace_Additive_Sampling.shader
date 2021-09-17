// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UI Extensions/Particles/CustomEffectInterlace_Additive_Sampling" 
{
	Properties {
        _MainTex ("Base", 2D) = "white" {}
		[HDR]_Color ("Color", Color) = (1,1,1,1)
        _InterlacePattern_A ("InterlacePattern_A", 2D) = "white" {}
        _InterlacePattern_B ("InterlacePattern_B", 2D) = "white" {}
        

		//_PixelRate ("Pixel Rate", Range(0, 1)) = 0.3
		//_PixelSampleCount ("Pixel Sample count", Range(0,3)) = 1        

		[Gamma] _PixelMultiply("Pixel Multiply", Range(1, 10)) = 1.5
	}
	
	CGINCLUDE

		#include "UnityCG.cginc"

		sampler2D _MainTex;
		sampler2D _InterlacePattern_A;
		sampler2D _InterlacePattern_B;
		
		half4 _MainTex_ST;
		half4 _InterlacePattern_A_ST;
		half4 _InterlacePattern_B_ST;
		float4 _Color;	
		
		uniform half4 _MainTex_TexelSize;

		half _PixelMultiply;

        float _MyTimeX;

        struct appdata_t
		{
			float4 vertex : POSITION;
			half4 color : COLOR;
			half2 texcoord : TEXCOORD0;
		};
				
		struct v2f {
			float4 pos : SV_POSITION;
			half2 uv0 : TEXCOORD0;

			half4 uv1_2 : TEXCOORD1; // xy: uv1, zw: uv2
			
			half4 color : COLOR;
		};

		//v2f vert(appdata_full v)
		v2f vert(appdata_t v)
		{
			v2f o;
			
			o.pos = UnityObjectToClipPos (v.vertex);	
			o.uv0.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex);

			o.uv1_2.xy = TRANSFORM_TEX(v.texcoord.xy, _InterlacePattern_A);
			o.uv1_2.zw = TRANSFORM_TEX(v.texcoord.xy, _InterlacePattern_B);

			o.color = v.color * _PixelMultiply * _Color;
			return o; 
		}
		
		half4 frag( v2f i ) : COLOR
		{	
			fixed4 colorTex = tex2D (_MainTex, i.uv0);

            //fixed4 interlace = tex2D(_InterlacePattern_A, i.uv1_2.xy + _Time.xx * _InterlacePattern_A_ST.zw)
            //	* tex2D(_InterlacePattern_B, i.uv1_2.zw + _Time.xx * _InterlacePattern_B_ST.zw);

            fixed4 interlace = tex2D(_InterlacePattern_A, i.uv1_2.xy + float2(_MyTimeX, _MyTimeX) * _InterlacePattern_A_ST.zw)
                * tex2D(_InterlacePattern_B, i.uv1_2.zw + float2(_MyTimeX, _MyTimeX) * _InterlacePattern_B_ST.zw);


            half4 col = colorTex * interlace * i.color.rgba;
			
			clip( col.a < 0.01 ? -1.0 : col.a );
			
			return col;
		}

	ENDCG
	
	SubShader {
    	Tags {"RenderType" = "Transparent" "Queue" = "Transparent" "Reflection" = "RenderReflectionTransparentAdd" }
		Cull Off
		ZWrite Off
       	Blend SrcAlpha One
		
	Pass {
	
		CGPROGRAM
		
		#pragma vertex vert
		#pragma fragment frag
		#pragma fragmentoption ARB_precision_hint_fastest 
		
		ENDCG
		 
		}
				
	} 
	FallBack Off
}
