Shader "Hidden/Griffin/SplineExtract"
{
	CGINCLUDE
#pragma vertex vert
#pragma fragment frag
#pragma require compute

#include "UnityCG.cginc"

		struct v2f
	{
		float4 vertex: SV_POSITION;
		float alpha : TEXCOORD0;
		float4 localPos: TEXCOORD1;
		float height : TEXCOORD2;
	};

	StructuredBuffer<float3> _Vertices; //world(x,y,z)
	StructuredBuffer<float> _Alphas;
	float4 _WorldBounds; //(x, y, width, height)
	float2 _TextureSize;
	float _MaxHeight;

	RWStructuredBuffer<int> _DepthBuffer : register (u1);
#define MAX_INT 65535.0

	float inverseLerp(float value, float a, float b)
	{
		float v = (value - a) / (b - a);
		float aeb = (a == b);
		return 0 * aeb + v * (1 - aeb);
	}

	v2f vert(uint id: SV_VERTEXID)
	{
		v2f o;
		float3 v = _Vertices[id];
		float x = inverseLerp(v.x, _WorldBounds.x, _WorldBounds.x + _WorldBounds.z);
		float y = inverseLerp(v.z, _WorldBounds.y, _WorldBounds.y + _WorldBounds.w);
		o.vertex = UnityObjectToClipPos(float4(x, y, 0, 1));
		o.alpha = _Alphas[id];
		o.localPos = float4(x, y, 0, 1);
		o.height = saturate(v.y / _MaxHeight);
		return o;
	}
	ENDCG

		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
		Cull Off
		ZWrite Off
		ZTest Always
		Pass
		{
			Name "Render Depth"

			CGPROGRAM

			float frag(v2f input) : SV_Target
			{
				float pixelX = round(lerp(0, _TextureSize.x - 1, input.localPos.x));
				float pixelY = round(lerp(0, _TextureSize.y - 1, input.localPos.y));
				int index = pixelY * _TextureSize.y + pixelX;
				uint max;
				InterlockedMax(_DepthBuffer[index], int(input.alpha* MAX_INT), max);
				return 0;
			}
			ENDCG

		}
		Pass
		{
			Name "Render Mask"

			CGPROGRAM

			float frag(v2f input) : SV_Target
			{
				float pixelX = round(lerp(0, _TextureSize.x - 1, input.localPos.x));
				float pixelY = round(lerp(0, _TextureSize.y - 1, input.localPos.y));

				int index = pixelY * _TextureSize.y + pixelX;
				float maskValue = _DepthBuffer[index] / MAX_INT;

				return maskValue;
			}
			ENDCG
		}
		Pass
		{
			Name "Render Mask Bool"

			CGPROGRAM

			float frag(v2f input) : SV_Target
			{
				return 1;
			}
			ENDCG
		}
		Pass
		{
			Name "Render Height Mask"

			CGPROGRAM

			float frag(v2f input) : SV_Target
			{
				float pixelX = round(lerp(0, _TextureSize.x - 1, input.localPos.x));
				float pixelY = round(lerp(0, _TextureSize.y - 1, input.localPos.y));

				int index = pixelY * _TextureSize.y + pixelX;
				uint max;
				InterlockedMax(_DepthBuffer[index], int(input.height* MAX_INT), max);
				return 0;
			}
			ENDCG

		}
		Pass
		{
			Name "Render Height Map"

			CGPROGRAM

			float frag(v2f input) : SV_Target
			{
				float pixelX = round(lerp(0, _TextureSize.x - 1, input.localPos.x));
				float pixelY = round(lerp(0, _TextureSize.y - 1, input.localPos.y));

				int index = pixelY * _TextureSize.y + pixelX;
				float maskValue = _DepthBuffer[index] / MAX_INT;

				return input.height;///////
				//return maskValue; //-->won't work
			}
			ENDCG
		}

	}
}
