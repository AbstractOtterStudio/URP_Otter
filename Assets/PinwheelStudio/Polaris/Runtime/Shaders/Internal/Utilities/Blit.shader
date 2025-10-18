Shader "Hidden/Griffin/Blit"
{
	CGINCLUDE
	#pragma vertex vert
	#pragma fragment frag

	#include "UnityCG.cginc"

	struct appdata
	{
		float4 vertex: POSITION;
		float2 uv: TEXCOORD0;
	};

	struct v2f
	{
		float2 uv: TEXCOORD0;
		float4 vertex: SV_POSITION;
		float4 localPos: TEXCOORD1;
	};

	sampler2D _MainTex;

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		o.localPos = v.vertex;
		return o;
	}
	ENDCG

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Name "Blit Default"
			CGPROGRAM
			float4 frag(v2f input): SV_Target
			{
				return tex2D(_MainTex, input.uv);
			}
			ENDCG
		}

		Pass
		{
			Name "Blit Add"
			Blend One One
			BlendOp Add

			CGPROGRAM
			float4 frag(v2f input): SV_Target
			{
				return tex2D(_MainTex, input.uv);
			}
			ENDCG
		}
	}
}
