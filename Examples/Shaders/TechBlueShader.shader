Shader "Daikon Forge/Tech UI Shader"
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
			
				float t = _Time.y * 1.33;
				float3 pos = IN.pos; 
				
				float d = 0.25 * abs( 0.33 * sin( pos.y * 3.14159 * 0.33 + t * 1.1 ) );
				float d2 = 0.175 * abs( 0.33 * sin( pos.y * 3.14159 * 33 + t * 3 ) );
				
				half4 texColor = tex2D( _MainTex, IN.tex.xy );
				half4 result = texColor * IN.color - d;
				result.a -= d2;

				return result;
				
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