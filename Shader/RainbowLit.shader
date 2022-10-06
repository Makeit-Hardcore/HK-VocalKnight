Shader "Custom/RainbowLit"
{
	// Based on unity's Sprites-Diffuse.shader
    Properties
    {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Vector) = (1,1,1,1)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		[HideInInspector] _RendererColor ("RendererColor", Vector) = (1,1,1,1)
		[HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
		[PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
		[PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        
		_Phase ("Phase", Float) = 0
        _Frequency ("Frequency", Vector) = (0,0,0,0) 
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		Lighting Off
        Cull Off
		ZWrite Off

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert nofog nolightmap nodynlightmap keepalpha noinstancing
        #pragma target 2.0
        #pragma multi_compile_local _ PIXELSNAP_ON
        #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
        
        #include "Rainbow.cginc"
        #include "UnitySprites.cginc"

        struct Input
        {
            float2 uv_MainTex;
            fixed4 color;
        	fixed3 worldPos;
        };

        void vert (inout appdata_full v, out Input o)
        {
            v.vertex = UnityFlipSprite(v.vertex, _Flip);
        	// o.worldPos = mul(unity_ObjectToWorld, v.vertex);

            #if defined(PIXELSNAP_ON)
            v.vertex = UnityPixelSnap (v.vertex);
            #endif

            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color * _Color * _RendererColor;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = SampleSpriteTexture (IN.uv_MainTex) * IN.color;
			// Unity's built in shader seems to multiply rgb by alpha here.
			// HK's shaders don't I think?
            o.Albedo = hueshift(IN.worldPos, c.rgb);
            o.Alpha = c.a;
        }

        ENDCG
    }
}
