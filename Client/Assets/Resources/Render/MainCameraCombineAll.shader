Shader "SCM/MainCameraCombineAll"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "white" {}
		_VisionTex ("Vision Texture", 2D) = "white" {}
		_GroundTex("Ground Texture", 2D) = "white" {}
		_BuildingHistoryTex("Building History Text", 2D) = "white" {}
		_IndicatorTex("Indicator Texture", 2D) = "white" {}
		_TurnOnBattleFog("FogSwitch", int) = 1
		_PlayerIndex("PlayerIndex", int) = 1
		_FogOffset("FogOffset", float) = 0
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

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

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			sampler2D _VisionTex;
			sampler2D _GroundTex;
			sampler2D _BuildingHistoryTex;
			sampler2D _IndicatorTex;
			int _TurnOnBattleFog;
			int _PlayerIndex;
			fixed _FogOffset;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 main = tex2D(_MainTex, i.uv);
				fixed4 ground = tex2D(_GroundTex, i.uv);
				fixed4 bb = tex2D(_IndicatorTex, i.uv);
				fixed vy = i.uv.y * 140 / 200 - _FogOffset;
				fixed4 vc = tex2D(_VisionTex, float2(i.uv.x, vy));
				fixed4 history = tex2D(_BuildingHistoryTex, float2(i.uv.x, vy));
				fixed vision1 = (vc.r + vc.g + vc.b) / 3;
				fixed vision2 =  vy < 0.4f ? 1 : (vy > 0.6f ? 0 : vy * -2.5f + 2);
				fixed vision = vision1 > vision2 ? vision1 : vision2;
				if (_TurnOnBattleFog > 0)
				{
					fixed a = vision;
					fixed4 c = bb * bb.a + main * (1 - bb.a);
					if (a < 0.5)
					{
						fixed4 b = history * history.a + ground * (1 - history.a);
						c = b * (a < 0.5 ? 0.5 : a);
					}
					else
					{
						fixed div = (a - 0.5) * 2;
						if (div > 1)
							div = 1;
						c = (c * div + ground * (1 - div)) * a;
					}

					return c;
				}
				else
				{
					fixed4 c = bb * bb.a + main * (1 - bb.a);
					return c;
				}
			}
			ENDCG
		}
	}
}
