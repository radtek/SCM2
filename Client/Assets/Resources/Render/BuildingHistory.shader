Shader "SCM/BuildingHistory"
{
	Properties
	{
		_VisionTex("Texture", 2D) = "white" {}
		_BuildingTex("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _VisionTex;
			sampler2D _BuildingTex;

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 v = tex2D(_VisionTex, i.uv);
				fixed4 b =  tex2D(_BuildingTex, i.uv);

				fixed a = (v.r + v.g + v.b) / 3;
				if (a <= 0.5)
				{
					discard;
					return fixed4(0, 0, 0, 0);
				}
				else
					return b;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return fixed4(0, 0, 0, 0);
			}
			ENDCG
		}
	}
}
