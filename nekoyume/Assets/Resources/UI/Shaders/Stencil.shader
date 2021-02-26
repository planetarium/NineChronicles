Shader "Custom/Stencil"
{
    Properties
    {
	// 스탠실 버퍼에 사용할 값을 미리 변수로 빼둡니다.
	// UI Mask에서 사용하는 값(1)과의 중복을 피해 2를 사용합니다.
	_RefNumber ("Stencil Masking Number", int) = 2
    }

    SubShader
    {
	Tags
	{
	    "Queue" = "Transparent"
	    "IgnoreProjector" = "True"
	    "RenderType" = "Transparent"
	    "PreviewType" = "Plane"
	    "CanUseSpriteAtlas" = "True"
	}

	Stencil
	{
	    // 스탠실 버퍼에 쓸 값
	    Ref [_RefNumber]
	    // 무조건 렌더링.
	    Comp always
	    // 버퍼의 값이 대체됩니다.
	    Pass replace
	}
	// 모든 색의 랜더링이 무효화됩니다.
	ColorMask 0
	// 깊이 버퍼 (depth buffer)를 사용하지 않습니다.
	ZWrite Off
	// 뒷면에선 보이지 않습니다.
	Cull back
	// z축 검사를 실행하지 않습니다.
	ZTest Less

	Pass
	{
	    CGPROGRAM
	    // 버텍스 쉐이더 정의
	    #pragma vertex vert
	    // 프래그먼트 쉐이더 (픽셀 쉐이더) 정의
	    #pragma fragment frag

	    // app data to vertex shader. 월드 포지션이 들어있음.
	    struct appdata
	    {
		float4 vertex : POSITION;
	    };

	    // vertex to fragment shader. 투영 공간 (뷰 공간)이 들어있음.
	    struct v2f
	    {
		float4 pos : SV_POSITION;
	    };

	    // 버텍스 쉐이더
	    v2f vert(appdata v)
	    {
		v2f o;
		// 투영 공간을 구합니다. (MVP 행렬 * 버텍스 위치)
		// MVP == model * view * projection
		o.pos = UnityObjectToClipPos(v.vertex);
		return o;
	    }

	    // 색을 정의한 프래그먼트 쉐이더
	    half4 frag(v2f i) : COLOR
	    {
		// 마스킹되는 영역이니 색은 아무 상관 없습니다.
		return half4(1,1,1,1);
	    }
	    ENDCG
	}
    }
    FallBack "Diffuse"
}
