Shader "Water/ShorelineBlur"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white"
	}

		SubShader
	{
		ZTest Always
		ZWrite Off

		HLSLINCLUDE
		#pragma vertex vert
		#pragma fragment frag

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#pragma multi_compile __BLUR_SIZE_3 __BLUR_SIZE_5 __BLUR_SIZE_7 __BLUR_SIZE_9

		#if defined(__BLUR_SIZE_3)
		static const int _BlurHalfSize = 1;
		static const float _BlurWeight[3] = {0.27406862, 0.45186276, 0.27406862};
		#elif defined(__BLUR_SIZE_5)
		static const int _BlurHalfSize = 2;
		static const float _BlurWeight[5] = { 0.13357471, 0.22921512, 0.27442033, 0.22921512, 0.13357471 };
		#elif defined(__BLUR_SIZE_7)
		static const int _BlurHalfSize = 3;
		static const float _BlurWeight[7] = { 0.08605388, 0.13620448, 0.17940889, 0.19666549, 0.17940889, 0.13620448, 0.08605388 };
		#else
		static const int _BlurHalfSize = 4;
		static const float _BlurWeight[9] = { 0.0629702, 0.0929025, 0.12264921, 0.14489292, 0.15317033, 0.14489292, 0.12264921, 0.0929025, 0.0629702 };
		#endif
		struct Attributes
		{
			float4 positionOS : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct Varyings
		{
			float4 positionHCS : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		TEXTURE2D(_MainTex);

		SAMPLER(sampler_MainTex);
		float4 _MainTex_TexelSize;
		float4 _MainTex_ST;

		Varyings vert(Attributes IN)
		{
			Varyings OUT;
			OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
			OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
			return OUT;
		}
		ENDHLSL

		Pass
		{
			Name "VERTICAL BOX BLUR"

			HLSLPROGRAM
			float frag(Varyings IN) : SV_TARGET
			{
				float2 res = _MainTex_TexelSize.xy;
				float sum = 0;
				uint data_out = 0;
				for (float y = -_BlurHalfSize; y <= _BlurHalfSize; y++)
				{
					float2 offset = float2(0, y * res.y);
					uint data = asuint(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + offset).r);
					if (y == 0) data_out = data;
					float weight = float(data >> 24) / 255.0;
					sum += weight * _BlurWeight[y + _BlurHalfSize];
				}
				data_out &= 0x00ffffffu;
				data_out |= uint(saturate(sum) * 255) << 24;
				return asfloat(data_out);
			}
			ENDHLSL
		}

		Pass
		{
			Name "HORIZONTAL BOX BLUR"

			HLSLPROGRAM
			float frag(Varyings IN) : SV_TARGET
			{
				float2 res = _MainTex_TexelSize.xy;
				float sum = 0;
				uint data_out = 0;

				for (float x = -_BlurHalfSize; x <= _BlurHalfSize; x++)
				{
					float2 offset = float2(x * res.x, 0);
					uint data = asuint(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + offset).r);
					if (x == 0) data_out = data;
					float weight = float(data >> 24) / 255.0;
					sum += weight * _BlurWeight[x + _BlurHalfSize];
				}
				data_out &= 0x00ffffffu;
				data_out |= uint(saturate(sum) * 255) << 24;
				//return sum;
				return asfloat(data_out);
			}
			ENDHLSL
		}
	}
}