Shader "Custom/Distortion_Stencil"
{
    Properties
    {
        // Unused _MainTex property, added to prevent runtime exceptions for elements whose parent is a ScrollRect
        [HideInInspector] _MainTex("Main Texture - unused", 2D) = "white" {}
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
            Ref 2
            Comp always
            Pass replace
        }

        Pass
	{
	}
    }
}
