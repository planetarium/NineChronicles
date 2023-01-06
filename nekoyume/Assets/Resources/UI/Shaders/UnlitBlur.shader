// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Blur"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _BlurIntensity("BlurIntensity", Range(0, 30)) = 1
    }

        SubShader
        {
            Tags {"Queue" = "Transparent"}
            LOD 100

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
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                float4 _MainTex_TexelSize;
                float4 _MainTex_ST;
                float _BlurIntensity;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float horizontal = _MainTex_TexelSize.x;
                    float vertical = _MainTex_TexelSize.y;

                    float radius = _BlurIntensity;
                    float count = (2 * radius + 1) * (2 * radius + 1);
                    fixed x = i.uv.x;
                    fixed y = i.uv.y;

                    fixed4 col = fixed4(0,0,0,0);

                    for (float m = -radius; m <= radius; ++m) {
                        fixed u = clamp(x + m * horizontal, 0, 1);
                        for (float n = -radius; n <= radius; ++n) {
                            fixed v = clamp(y + n * vertical, 0, 1);
                            col += tex2D(_MainTex, fixed2(u, v));
                        }
                    }

                    return col / count;
                }

                ENDCG
            }
        }
}
