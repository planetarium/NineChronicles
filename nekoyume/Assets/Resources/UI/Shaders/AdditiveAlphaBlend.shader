Shader "Custom/AdditiveAlphaBlend" {
Properties {
	_MainTex ("MainTex", 2D) = "white" {}
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"  }
	Blend One OneMinusSrcAlpha
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
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t IN)
			{
				v2f v;
				v.color = IN.color;
				v.texcoord = TRANSFORM_TEX(IN.texcoord,_MainTex);

				return v;
			}
			
			fixed4 frag (v2f IN) : SV_Target
			{
				fixed4 color = tex2D(_MainTex, IN.texcoord);
				color *= IN.color;
				return color;
			}
			ENDCG 
		}
	}
}
}
