Shader "Custom/Distortion_Additive"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color",Color) = (1,1,1,1)
        [NoScaleOffset] _NoiseTex ("Texture", 2D) = "black" {}

        _Size("Noise Size", Range(0.001, 50)) = 1
        _Speed("Noise Speed", Range(0.001, 5)) = 1
        _Mag("Magnitude", Range(0.0001, 0.1)) = 1
    }
    SubShader
    {
        Tags {"Queue"="Transparent" }
        ZWrite Off
		Blend SrcAlpha One

        Stencil
	{
	    Ref 2
            Comp equal
            Pass keep
            ZFail decrWrap
	}

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Size;
            float _Speed;
            float _Mag;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture

                fixed2 uv = tex2D(_NoiseTex, i.uv*_Size+(_Time.y*_Speed)).rg*2-1;
                fixed4 col = tex2D(_MainTex, i.uv+uv*_Mag);

                return col*_Color;
            }
            ENDCG
        }
    }
}
