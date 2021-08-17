Shader "Custom/Distortion_Stencil"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry-1"
        }

        Stencil
        {
            Ref 2
            Comp always
            Pass replace
        }

        Pass
	{
	}
    }
}
