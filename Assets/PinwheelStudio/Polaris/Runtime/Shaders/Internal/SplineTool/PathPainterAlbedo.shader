// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Hidden/Polaris/PathPainterAlbedo"
{
	Properties{ }
		SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		ZTest Always
		Cull Off

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

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

			float4 _Color;
			sampler2D _MainTex;
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
				float4 currentColor = tex2D(_MainTex, i.uv);

				float2 noiseUV = float2(i.positionWS.x * _FalloffNoise_ST.x, i.positionWS.z * _FalloffNoise_ST.y);
				float falloffNoise = tex2D(_FalloffNoise, noiseUV).r;
				float splineAlpha = tex2D(_SplineAlphaMap, i.uv).r;
				float falloffCurve = tex2D(_FalloffCurve, float2(splineAlpha, 0.5)).r;
				float falloff = (falloffCurve - falloffNoise) * (splineAlpha < 1) + splineAlpha * (splineAlpha == 1);
				falloff = saturate(falloff) * (splineAlpha > 0);

				float terrainMask = 1 - tex2D(_TerrainMask, i.uv).r;
				float4 overlayColor = float4(_Color.rgb, _Color.a * falloff * terrainMask);
				overlayColor = lerp(currentColor, overlayColor, terrainMask * falloff);
				return overlayColor;
			}
			ENDCG

		}
	}
}
