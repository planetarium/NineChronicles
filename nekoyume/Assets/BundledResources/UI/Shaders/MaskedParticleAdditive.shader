Shader "Custom/Masked Particle Additive"
{
    Properties
    {
	_MainTex ("Particle Texture", 2D) = "white" {}
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
	// Additive
	Blend SrcAlpha One
	Cull Off
	Lighting Off
	ZWrite Off
	Fog { Color (0,0,0,0) }
	
	Stencil
	{
	    // 참조할 값
	    Ref [_RefNumber]
	    // 참조할 값과 같다면 렌더링.
	    Comp Equal
	    // 스탠실 버퍼의 값은 내비둡니다.
	    Pass keep
	}
    
        Pass
        {
    	    SetTexture[_MainTex]
    	    {
    	        combine texture * primary
    	    }
        }
    }
}
