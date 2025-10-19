Shader "Hidden/Griffin/SplineMask"
{
	Properties
	{
		_Falloff("Falloff", 2D) = "white" { }
		_FalloffNoise("Falloff Noise", 2D) = "white" { }
		_TerrainMask("TerrainMask", 2D) = "black" { }
	}

		CGINCLUDE
#include "UnityCG.cginc"

		struct appdata
	{
		float4 vertex: POSITION;
		float4 uv : TEXCOORD0;
	};

	struct v2f
	{
		float4 vertex: SV_POSITION;
		float2 uv: TEXCOORD0;
		float4 positionWS: TEXCOORD1;
	};

	sampler2D _SplineAlphaMap;
	sampler2D _FalloffCurve;
	sampler2D _FalloffNoise;
	float4 _FalloffNoise_ST;
	sampler2D _TerrainMask;
	float4 _WorldBounds;

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		o.positionWS = float4(_WorldBounds.x + _WorldBounds.z * v.uv.x, 0, _WorldBounds.y + _WorldBounds.w * v.uv.y, 0);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		float2 noiseUV = float2(i.positionWS.x * _FalloffNoise_ST.x, i.positionWS.z * _FalloffNoise_ST.y);
		float falloffNoise = tex2D(_FalloffNoise, noiseUV).r;
		float splineAlpha = tex2D(_SplineAlphaMap, i.uv).r;
		float falloffCurve = tex2D(_FalloffCurve, float2(splineAlpha, 0.5)).r;
		float falloff = (falloffCurve - falloffNoise) * (splineAlpha < 1) + splineAlpha * (splineAlpha == 1);
		falloff = saturate(falloff) * (splineAlpha > 0);

		float terrainMask = 1 - tex2D(_TerrainMask, i.uv).r;
		float4 overlayColor = float4(falloff, falloff, falloff, falloff) * terrainMask;

		return overlayColor;
	}

		ENDCG

		SubShader
	{
		Tags{ "RenderType" = "Transparent" }
			Cull Off

			Pass
		{
			Blend One Zero
			BlendOp Add
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			ENDCG

		}
	}
}
