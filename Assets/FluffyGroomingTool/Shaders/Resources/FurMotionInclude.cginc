#define SKIN_WIDTH 1.01
#define MAX_EASED_MOVE_DISTANCE 1
#define WIND_SENSITIVITY_DIVIDER 40
#include "CommonConstants.cginc"

struct MeshProperties
{
    float3 sourceVertex;
    float3 sourceNormal;
    float4 sourceTangent;
};

struct MeshFullProperties
{
    float3 sourceVertex;
    float3 sourceNormal;
    float4 sourceTangent;
    float4 sourceColor;
    float2 sourceUV;
};

uniform RWByteAddressBuffer furMeshBuffer;
uniform uint furMeshBufferStride;
float sourceMeshNormalToStrandNormalPercent;

void writeVector3(uint index, uint offset, float3 value)
{
    furMeshBuffer.Store3(index + offset, asuint(value));
}

void writeVector4(uint index, uint offset, float4 value)
{
    furMeshBuffer.Store4(index + offset, asuint(value));
}

void writeVector2(uint index, uint offset, float2 value)
{
    furMeshBuffer.Store2(index + offset, asuint(value));
}

float3 loadSourceVertex(uint index)
{
    return asfloat(furMeshBuffer.Load3(index));
}

float rand(float n)
{
    return frac(sin(dot(float2(n, n * 2), float2(12.9898, 4.1414))) * 43758.5453) - 0.5;
}

struct HairStrandStruct
{
    float4 bend;
    float4x4 scaleMatrix;
    float4x4 rootAndOrientationMatrix;
    float2 uv;
    float2 uvOffset;
    float3 triangles;
    float3 barycentricCoordinates;
    float windContribution;
    int clumpIndices;
    float clumpMask;
    float4 twist;
    float4 overrideColor;
};

struct ColliderStruct
{
    float3 position;
    float3 position2;
    float radius;
};

struct VerletNode
{
    float3 position;
    float3 prevPosition;
    bool isFixed;
    float3 tangent;
    float3 rotationDiffFromPrevNode;
    float3 rotationDiffFromPrevNode2;
};

struct VerletRestNode
{
    float3 position;
    float3 worldSourceMeshNormal;
    float vertMoveAbility;
    float pinnedAmount;
};

bool isFixed(VerletNode node)
{
    return node.isFixed;
}

uniform float objectGlobalScale;

uniform StructuredBuffer<HairStrandStruct> hairStrandsBuffer;
uniform float4x4 objectRotationMatrix;
float4x4 localToWorldMatrix;


float deltaTime;

//WIND 
float time;
Texture2D<float4> WindDistortionMap;
SamplerState sampler_WindDistortionMap;
float WindGust;
float WindStrength;
float3 WindDirection;
//END WIND

//COLLISION
int CollidersCount;
RWStructuredBuffer<ColliderStruct> ColliderBuffer;
//END COLLISION

float3 Step(float3 current, float omega)
{
    float ex = saturate(exp(-omega * deltaTime));
    return lerp(float3(0, 0, 0), current, ex);
}


float3 applyWind(float3 desiredPosition, float3 currentPosition)
{
    const float3 worldPos = currentPosition;
    float2 uv = worldPos.xz + float2(WindGust, WindGust) * time;
    float2 windSample = WindDistortionMap.SampleLevel(sampler_WindDistortionMap, uv, 0).xy;
    float ws = WindStrength + (sin(time + worldPos.y * 4) + cos(time * 5 + worldPos.y * 4)) / 4 * WindStrength;
    desiredPosition += float3((windSample.x - 0.5) * 2, 0, (windSample.y - 0.5) * 2) / WIND_SENSITIVITY_DIVIDER * ws *float3( WindDirection.x,1,WindDirection.z) ;
    return desiredPosition;
}

float sqrMag(float3 input)
{
    return input.x * input.x + input.y * input.y + input.z * input.z;
}

float3 sphereCollide(ColliderStruct collider, float3 desiredPosition)
{
    //Sphere collider
    float dist = distance(desiredPosition, collider.position);
    dist = collider.radius * SKIN_WIDTH - clamp(0.0, collider.radius * SKIN_WIDTH, dist);
    desiredPosition += normalize(desiredPosition - collider.position) * dist;
    return desiredPosition;
}

float3 capsuleMiddleCollide(ColliderStruct collider, float3 desiredPosition)
{
    const float3 top = collider.position;
    const float3 bottom = collider.position2;
    const float3 position = desiredPosition;
    const float3 bottomMinusTop = bottom - top;
    const float3 topMinusBottom = -bottomMinusTop;
    const float3 posMinusTop = position - top;
    const float3 posMinusBottom = position - bottom;
    const float dot_Top = dot(bottomMinusTop, posMinusTop);
    const float dot_Bottom = dot(topMinusBottom, posMinusBottom);
    if (dot_Top >= 0.0f && dot_Bottom >= 0.0f) // we are between the top and bottom.
    {
        // project posMinusTop onto bottomMinusTop and move to world cords.
        const float3 positionProjection = dot_Top * bottomMinusTop / sqrMag(bottomMinusTop) + top;
        float dist = distance(positionProjection, position);
        dist = collider.radius * SKIN_WIDTH - clamp(0.0, collider.radius * SKIN_WIDTH, dist);
        desiredPosition += normalize(position - positionProjection) * dist;
    }
    return desiredPosition;
}

float3 applyColliders(float3 desiredPosition)
{
    for (int i = 0; i < CollidersCount; i++)
    {
        const ColliderStruct collider = ColliderBuffer[i];
        if (ColliderBuffer[i].position2.x == -1000) //For now -1000 means, that it's a single sphere collider. 
        {
            desiredPosition = desiredPosition = sphereCollide(collider, desiredPosition);
        }
        else
        {
            desiredPosition = sphereCollide(collider, desiredPosition);
            desiredPosition = sphereCollide(collider, desiredPosition);
            desiredPosition = capsuleMiddleCollide(collider, desiredPosition);
        }
    }
    return desiredPosition;
}

void writeNormal(const uint furMeshLeftIndex, const uint furMeshRightIndex, const float3 sourceMeshNormal, float3 tangent)
{
    float3 left = normalize(cross(tangent, float3(0, 1, 0)));

    float3 normal = cross(left, tangent);
    normal = lerp(sourceMeshNormal, normal, sourceMeshNormalToStrandNormalPercent);
    writeVector3(furMeshLeftIndex, SIZEOF_FLOAT3, normal);
    writeVector3(furMeshRightIndex, SIZEOF_FLOAT3, normal);
}
