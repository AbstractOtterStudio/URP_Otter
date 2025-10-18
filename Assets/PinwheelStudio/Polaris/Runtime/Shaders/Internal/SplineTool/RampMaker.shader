Shader "Hidden/Griffin/RampMaker"
{
	Properties
	{
		_HeightMap("Height Map", 2D) = "black"{}
	}

		CGINCLUDE
#include "UnityCG.cginc"
#include "SplineToolCommon.cginc"

		struct appdata
	{
		float4 vertex: POSITION;
		float4 uv : TEXCOORD0;
	};

	struct v2f
	{
		float4 vertex: SV_POSITION;
		float4 uv : TEXCOORD0;
		float4 positionWS: TEXCOORD2;
	};

	sampler2D _HeightMap;
	sampler2D _SplineHeightMap;
	sampler2D _SplineAlphaMask;
	sampler2D _FalloffCurve;
	sampler2D _FalloffNoise;
	float4 _FalloffNoise_ST;
	float _HeightOffset01;
	int _LowerHeight;
	int _RaiseHeight;
	float _AdditionalMeshResolution;
	sampler2D _TerrainMask;
	int _StepCount;
	float4 _WorldBounds;
	float _TerrainHeight;

	float stepValue(float v, int stepCount)
	{
		float step = 1.0 / stepCount;
		return v - v % step;
	}

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		o.positionWS = float4(_WorldBounds.x + _WorldBounds.z * v.uv.x, 0, _WorldBounds.y + _WorldBounds.w * v.uv.y, 0);
		return o;
	}

	fixed4 fragRamp(v2f i) : SV_Target
	{
		float2 uv = i.uv.xy;
		float4 heightMapColor = tex2D(_HeightMap, uv);
		float currentHeight = GriffinDecodeFloatRG(heightMapColor.rg);
		float splineHeight = tex2D(_SplineHeightMap, uv).r + _HeightOffset01;
		float delta = splineHeight - currentHeight;
		float targetHeight = currentHeight + (delta < 0) * _LowerHeight * delta + (delta >= 0) * _RaiseHeight * saturate(delta);

		float2 noiseUV = float2(i.positionWS.x * _FalloffNoise_ST.x, i.positionWS.z * _FalloffNoise_ST.y);
		float falloffNoise = tex2D(_FalloffNoise, noiseUV).r;
		float splineFalloff = tex2D(_SplineAlphaMask, uv).r;;
		float falloffCurve = tex2D(_FalloffCurve, float2(splineFalloff, 0.5)).r;
		float falloff = (falloffCurve - falloffNoise) * (splineFalloff < 1) + splineFalloff * (splineFalloff == 1);
		falloff = saturate(falloff);

		float terrainMask = 1 - tex2D(_TerrainMask, uv).r;
		float h = lerp(currentHeight, targetHeight, falloff);
		h = lerp(currentHeight, h, terrainMask);
		h = stepValue(h, _StepCount);
		h = lerp(currentHeight, h, splineFalloff);
		h = max(0, min(0.999999, h));

		float2 encodedHeight = GriffinEncodeFloatRG(h);
		float addRes = lerp(0, _AdditionalMeshResolution * splineFalloff, terrainMask);
		return saturate(float4(encodedHeight.rg, heightMapColor.b + addRes, heightMapColor.a));
	}

		ENDCG

		SubShader
	{
		Tags{ "RenderType" = "Transparent" }
			Cull Off

			Pass
		{
			Name "Ramp"
			Blend One Zero
			BlendOp Add
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment fragRamp
			ENDCG

		}
	}
}
