// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UI Extensions/Particles/CustomEffectInterlace_Blend" 
{
	Properties {
        _MainTex ("Base", 2D) = "white" {}
		[HDR]_Color("Color", Color) = (1,1,1,1)
        _InterlacePattern_A ("InterlacePattern_A", 2D) = "white" {}
        _InterlacePattern_B ("InterlacePattern_B", 2D) = "white" {}
	}
	
	CGINCLUDE

		#include "UnityCG.cginc"

		sampler2D _MainTex;
		sampler2D _InterlacePattern_A;
		sampler2D _InterlacePattern_B;
						
		half4 _MainTex_ST;
		half4 _InterlacePattern_A_ST;
		half4 _InterlacePattern_B_ST;
		half4 _Color;
        float _MyTimeX;

        struct vertexInput {
			float4 vertex : POSITION;
			half4 texcoord : TEXCOORD0;
			half4 color : COLOR;
		};
				
		struct v2f {
			float4 pos : SV_POSITION;
			half2 uv0 : TEXCOORD0;

			half4 uv1_2 : TEXCOORD1; // xy: uv1, zw:uv2
			
			half4 color : COLOR;
		};

		//v2f vert(appdata_full v)
		v2f vert(vertexInput v)
		{
			v2f o;
			
			o.pos = UnityObjectToClipPos (v.vertex);	
			o.uv0.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex);

            //o.uv1_2.xy = TRANSFORM_TEX(v.texcoord.xy, _InterlacePattern_A) + _Time.xx * _InterlacePattern_A_ST.zw;
            //o.uv1_2.zw = TRANSFORM_TEX(v.texcoord.xy, _InterlacePattern_B) + _Time.xx * _InterlacePattern_B_ST.zw;

            o.uv1_2.xy = TRANSFORM_TEX(v.texcoord.xy, _InterlacePattern_A) + float2(_MyTimeX, _MyTimeX) * _InterlacePattern_A_ST.zw;
            o.uv1_2.zw = TRANSFORM_TEX(v.texcoord.xy, _InterlacePattern_B) + float2(_MyTimeX, _MyTimeX) * _InterlacePattern_B_ST.zw;


            //o.color = v.color * 1.5 * _Color;
            o.color = v.color * _Color;
			return o; 
		}
		
		half4 frag( v2f i ) : COLOR
		{	
			fixed4 colorTex = tex2D (_MainTex, i.uv0);
			//fixed4 interlace = tex2D (_InterlacePattern_A, i.uv1) * tex2D (_InterlacePattern_B, i.uv2);

			fixed4 interlace = tex2D(_InterlacePattern_A, i.uv1_2.xy) * tex2D(_InterlacePattern_B, i.uv1_2.zw);
			
			colorTex *= interlace;
			half4 col = colorTex * i.color.rgba;
			col.rgb *= 1.5;
			
			clip( col.a < 0.01 ? -1.0 : col.a );
			 
			return col;
		}

	ENDCG
	
	SubShader {
    	Tags {"RenderType" = "Transparent" "Queue" = "Transparent" "Reflection" = "RenderReflectionTransparentAdd" }
		Cull Off
		ZWrite Off
       	Blend SrcAlpha OneMinusSrcAlpha
		
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
