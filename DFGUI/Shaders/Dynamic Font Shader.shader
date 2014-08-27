Shader "Daikon Forge/Dynamic Font Shader"
{
	Properties
	{
		_MainTex("Font Texture", 2D) = "white" {}
		_Color("Text Color", Color) = (1, 1, 1, 1)
	}

	SubShader
	{

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

			CGPROGRAM

#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float2 clipPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform fixed4 _Color;

			v2f vert(appdata_t v)
			{

				v2f o;

				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.color = v.color * _Color;
				o.texcoord = v.texcoord;

				// Clip region: 
				//	_MainTex_ST.xy = size of clip region
				//	_MainTex_ST.zw = clip region offset from center
				o.clipPos = v.vertex.xy * _MainTex_ST.xy + _MainTex_ST.zw;

				return o;

			}

			fixed4 frag(v2f IN) : COLOR
			{

				fixed4 color = IN.color;
				color.a *= tex2D(_MainTex, IN.texcoord).a;

				// Determine whether the current pixel is within the clip region.
				// If it is not, set the pixel's alpha to zero.
				float2 clipFactor = abs(IN.clipPos);
				if( max(clipFactor.x, clipFactor.y) > 1.0 )
					color.a = 0.0;

				return color;

			}

			ENDCG

		}

	}

}
