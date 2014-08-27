Shader "Daikon Forge/Default UI Shader"
{
	Properties
	{
		_MainTex("Base (RGB), Alpha (A)", 2D) = "white" {}
	}

	SubShader
	{
		LOD 200

		Tags
		{
		"IgnoreProjector" = "True"
		"RenderType" = "Transparent"
		"Queue" = "Overlay"
		"PreviewType" = "Plane"
	}

		Pass
		{

			Cull Off
			Lighting Off
			ZWrite Off
			Fog{ Mode Off }
			Offset -1, -1
			ColorMask RGBA
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMaterial AmbientAndDiffuse

			CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				float2 tex : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				float2 tex : TEXCOORD0;
				float2 clipPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata_t v)
			{

				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.color = v.color;
				o.tex.xy = v.tex.xy;

				// Clip region: 
				//	_MainTex_ST.xy = size of clip region
				//	_MainTex_ST.zw = clip region offset from center
				o.clipPos = v.vertex.xy * _MainTex_ST.xy + _MainTex_ST.zw;

				return o;

			}

			half4 frag(v2f IN) : COLOR
			{

				half4 color = IN.color;

				// Determine whether the current pixel is within the clip region.
				// If it is not, set the pixel's alpha to zero.
				float2 clipFactor = abs(IN.clipPos);
				if( max( clipFactor.x, clipFactor.y ) > 1.0 ) 
					color.a = 0.0;

				return tex2D(_MainTex, IN.tex.xy) * color;

			}

			ENDCG

		}

	}

}