#ifndef TREE_BILLBOARD_COMMON_URP
#define TREE_BILLBOARD_COMMON_URP

float4 _ImageTexcoords[256];
int _ImageCount;

void GetImageTexcoord_float(float2 UV0, float3 LocalNormal, out float2 BillboardUV)
{
	float3 normal = normalize(mul(unity_ObjectToWorld,-LocalNormal));
	float dotZ = dot(normal, float3(0, 0, 1));
	float dotX = dot(normal, float3(1, 0, 0));
	float rad = atan2(dotZ, dotX);
	rad = (rad + TWO_PI) % TWO_PI;
	float f = rad / TWO_PI - 0.5 / _ImageCount;
	int imageIndex = f * _ImageCount;

	float4 rect = _ImageTexcoords[imageIndex];
	float2 min = rect.xy;
	float2 max = rect.xy + rect.zw;

	float2 result = float2(
		lerp(min.x, max.x, UV0.x),
		lerp(min.y, max.y, UV0.y));
	BillboardUV = result;
}

void BillboardVertex_float(float3 InLocalPosition, float2 InUV0, out float3 LocalPosition, out float3 LocalNormal, out float3 LocalTangent, out float2 ImageTexcoord)
{
	//Calculate new billboard vertex position and normal;
	float3 upCamVec = float3(0, 1, 0);
	float3 forwardCamVec = UNITY_MATRIX_V._m20_m21_m22;
	forwardCamVec.y = 0;
	forwardCamVec = -normalize(forwardCamVec);

	float3 rightCamVec = UNITY_MATRIX_V._m00_m01_m02;
	rightCamVec.y = 0;
	rightCamVec = normalize(rightCamVec);

	float4x4 rotationCamMatrix = float4x4(rightCamVec, 0, upCamVec, 0, forwardCamVec, 0, 0, 0, 0, 1);
	LocalNormal = mul(unity_WorldToObject, -forwardCamVec);
	LocalTangent = mul(unity_WorldToObject, float4(rightCamVec, 0));
	LocalPosition = InLocalPosition;
	LocalPosition.x *= length(unity_ObjectToWorld._m00_m10_m20);
	LocalPosition.y *= length(unity_ObjectToWorld._m01_m11_m21);
	LocalPosition.z *= length(unity_ObjectToWorld._m02_m12_m22);
	LocalPosition = mul(float4(LocalPosition,1), rotationCamMatrix);
	LocalPosition.xyz += unity_ObjectToWorld._m03_m13_m23;
	LocalPosition = mul(unity_WorldToObject, float4(LocalPosition,1));

	GetImageTexcoord_float(InUV0, LocalNormal, ImageTexcoord);
}

#endif