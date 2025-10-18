Shader "Hidden/Polaris/VegetationDitheringMask"
{
	Properties
	{
	}
	SubShader
	{
		CGINCLUDE
		#pragma vertex vert
		#pragma fragment frag

		#include "UnityCG.cginc"

		struct appdata
		{
			float4 vertex : POSITION;
		};

		struct v2f
		{
			float4 vertex : SV_POSITION;
			float4 localPos : TEXCOORD0;
		};

		sampler2D _MainTex;

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.localPos = v.vertex;
			return o;
		}

		ENDCG

			Tags{ "RenderType" = "Opaque" }
			LOD 100

			Pass
		{
			Name "Fade"
			CGPROGRAM

			fixed4 frag(v2f i) : SV_Target
			{
				float current = tex2D(_MainTex, i.localPos.xy).r;
				float v = clamp(current - 0.02, 0, 1);
				return fixed4(v.xxx, 1);
			}
			ENDCG
		}
		Pass
		{
			Name "Add"
			CGPROGRAM

			fixed4 frag(v2f i) : SV_Target
			{
				float current = tex2D(_MainTex, i.localPos.xy).r;
				float v = clamp(current + 0.05, 0, 1);
				return fixed4(v.xxx, 1);
			}
			ENDCG
		}
	}
}
