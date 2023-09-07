Shader "Custom/AdditiveAlphaBlend" {
Properties {

	_MainTex ("MainTex", 2D) = "white" {}
	_Adjust ("Adjust Factor", Float) = 1.0
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"  }
	Blend One OneMinusSrcColor
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off
	ZTest [unity_GUIZTestMode]

	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			sampler2D _MainTex;
			
			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t IN)
			{
				v2f v;
				v.vertex = UnityObjectToClipPos(IN.vertex);
				v.color = IN.color;
				v.texcoord = TRANSFORM_TEX(IN.texcoord,_MainTex);

				return v;
			}
			float _Adjust;
			
			fixed4 frag (v2f IN) : SV_Target
			{
				//return (IN.color * (1-IN.color.a* _Adjust))* tex2D(_MainTex, IN.texcoord) * IN.color.a;
				//return IN.color * tex2D(_MainTex, IN.texcoord)* _Adjust;
				return tex2D(_MainTex, IN.texcoord) + (tex2D(_MainTex, IN.texcoord) * tex2D(_MainTex, IN.texcoord).a * _Adjust);
				//return tex2D(_MainTex, IN.texcoord).a;
			}
			ENDCG 
		}
	}
}
}
