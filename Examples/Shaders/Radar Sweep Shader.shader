Shader "Daikon Forge/Radar Sweep Shader"
{
	Properties
	{
		_MainTex ( "Base (RGB), Alpha (A)", 2D ) = "white" {}
	}

	SubShader
	{
		LOD 200

		Tags
		{
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"Queue" = "Overlay"
		}
		
		Pass
		{
			Cull Off
			Lighting Off
			ZWrite Off
			Fog { Mode Off }
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
				float4 vertex		: POSITION;
				half4 color			: COLOR;
				float2 tex			: TEXCOORD0;
			}; 

			struct v2f
			{
				float4 vertex		: POSITION;
				float4 pos			: TEXCOORD1;
				half4 color			: COLOR;
				float2 tex			: TEXCOORD0;
			};

			sampler2D _MainTex; 

			#define GRID_SIZE 32
			#define PI 3.1416

			float3 color(float d) 
			{
				return d * float3(0.4, 0.75, 1);	
			}

			int mod(int a, int b) 
			{
				return a - ((a / b) * b);
			}

			v2f vert (appdata_t v)
			{

				v2f o;
				o.vertex = o.pos = mul( UNITY_MATRIX_MVP, v.vertex );
				o.color = v.color;
				o.tex.xy  = v.tex.xy;  

				return o;

			}

			half4 frag (v2f IN) : COLOR
			{

				float2 p = -1.0 + (2.0 * IN.tex.xy);
				float2 uv;

				float t = _Time.y * 2;
				float a = atan(p.yx) + t;
				float r = sqrt(dot(p,p));

				uv.x = 0.1/r;
				uv.y = a/(PI);
	
				float len = dot(p,p);
	
				float3 col = color( pow( frac( uv.y / -2.0 ), 15.0 ) );
				if (len > 0.73) col = float3(0,0,0);
	
				return float4( col, col.b * 0.25 );

			}

			ENDCG
		}
	}
	
	SubShader
	{
		Tags
		{
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"Queue" = "Overlay"
		}
		
		LOD 100
		Cull Off
		Lighting Off
		ZWrite Off
		Fog { Mode Off }
		ColorMask RGBA
		AlphaTest Greater .01
		Blend SrcAlpha OneMinusSrcAlpha
		
		Pass
		{
			ColorMaterial AmbientAndDiffuse
			
			SetTexture [_MainTex]
			{
				Combine Texture * Primary
			}
		}
	}
}