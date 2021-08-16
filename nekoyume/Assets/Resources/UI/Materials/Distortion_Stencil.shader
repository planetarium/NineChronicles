Shader "Custom/Distortion_Stencil"
{
    Properties
    {
        _RefNumber ("Stencil Masking Number", int) = 3
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry-1"
        }

        Stencil
        {
            Ref [_RefNumber]
            Comp Never
            Fail Replace
        }

        Pass
	{
	}
    }
}
