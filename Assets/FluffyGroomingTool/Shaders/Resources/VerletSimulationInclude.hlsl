#pragma warning( disable : 4714 ) 
int verletNodesCount; //
float _Decay; //
float3 _Gravity; //
#include "SDFColliderInclude.hlsl"

RWStructuredBuffer<VerletNode> verletNodes;
uniform float shapeConstraintRoot;
uniform float shapeConstraintTip;
uniform float stepSize;

uniform uint nearestPow;
uniform uint strandPointsCount;
uniform int solverIterations;
uniform uint layerVertexStartIndex;
uniform float3 worldSpaceCameraPos;
uniform uint numberOfFixedNodesInStrand;

float3 calculateTangent(float3 currentRestPos, float3 prevRestPosition, float3 nextRestPos, const bool isNextNodeFixed)
{
    float3 tangent;
    if (!isNextNodeFixed)
    {
        tangent = normalize(nextRestPos - currentRestPos);
    }
    else
    {
        tangent = normalize(currentRestPos - prevRestPosition);
    }
    return tangent;
}

bool calculateIndicesAndDiscard(uint3 id, out uint index, out uint localPointIndex, out uint hairStrandIndex)
{
    uint fakeIdx = id.x;
    hairStrandIndex = floor(fakeIdx / nearestPow);
    localPointIndex = fakeIdx - hairStrandIndex * nearestPow;
    index = layerVertexStartIndex + hairStrandIndex * strandPointsCount + localPointIndex;
    uint vnc = verletNodesCount;
    if (localPointIndex >= strandPointsCount || index >= vnc) return true;
    return false;
}

float3 slerp(float3 start, float3 end, float percent)
{
    float dotP = dot(start, end);
    dotP = clamp(dotP, -1.0, 1.0);
    const float theta = acos(dotP) * percent;
    const float3 relativeVec = normalize(end - start * dotP);
    return theta == 0 ? start : start * cos(theta) + relativeVec * sin(theta);
}

float3 bendConstraintInv(
    VerletNode n, 
    const VerletNode nNext,
    const float intensity
)
{
    float3 perfectMatch = nNext.position + (n.tangent - n.rotationDiffFromPrevNode2) * distance(n.position, nNext.position);
    return lerp(n.position, perfectMatch, stepSize * intensity*   deltaTime);
}

float3 bendConstraint(
    VerletNode n,
    const VerletNode nPrev,
    const float intensity
)
{
    float3 perfectMatch = nPrev.position + (nPrev.tangent - n.rotationDiffFromPrevNode) * distance(n.position, nPrev.position);
    return lerp(n.position, perfectMatch, stepSize * intensity*   deltaTime);
}

float3 compute_delta(float3 a, float3 b, float segmentLengt)
{
    float3 direction = normalize(a - b);
    const float mDist = length(a - b) - segmentLengt;
    return direction * mDist * 0.5;
}

void velocityGravityAndCollision(float3 currentRestPosition, bool isFixedNode, float currentVertMoveAbility, float pinnedAmount, int index)
{
    VerletNode currentNode = verletNodes[index];
    //Got teleported
    if (distance(currentNode.position, currentNode.prevPosition) > 1)
    {
        currentNode.position = currentRestPosition;
        currentNode.prevPosition = currentRestPosition;
    }

    //Gravity
    currentNode.position += _Gravity * deltaTime * !isFixedNode;

    //Velocity
    float3 v = currentNode.position - currentNode.prevPosition;
    float3 next = currentNode.position + v * _Decay;
    currentNode.prevPosition = currentNode.position;
    currentNode.position = next;
    currentNode.position = applyWind(currentNode.position, currentRestPosition);

    //Sphere and capsule colliders
    #if defined(HAS_COLLIDERS)
    currentNode.position  = applyColliders(currentNode.position); 
    #endif
    #if defined(COLLIDE_WITH_SOURCE_MESH)
    int didSDFCollide=0;
    #if defined(USE_FORWARD_COLLISION)
    CollideHairVerticesWithSdf_forward(didSDFCollide, currentNode.position, currentVertMoveAbility);
    //if(didSDFCollide == 1)  currentNode.prevPosition = currentNode.position;//Could we use this for friction? 
    #else
    collideWithSDF(didSDFCollide, currentNode.position, currentVertMoveAbility);
    #endif
    //if(didSDFCollide == 1)  currentNode.prevPosition = currentNode.position;
    #endif
    currentNode.position = lerp(currentNode.position, currentRestPosition, pinnedAmount * deltaTime);
    verletNodes[index] = currentNode;
    GroupMemoryBarrierWithGroupSync();
}

void constraints(int index, float3 restPos, float3 restPosNext, float3 restPosPrev, bool isNextNodeFixed, float shapeConstraintIntensity)
{
    const bool isNotLastNode = (int)index < verletNodesCount - 1;
    for (int i = 0; i < solverIterations; i++)
    {
        VerletNode currentNode = verletNodes[index];
        if (isNotLastNode)
        {
            if (!isNextNodeFixed)
            {
                VerletNode nNext = verletNodes[index + 1];
                float nodeLength = distance(restPos, restPosNext);
                currentNode.position = bendConstraintInv(currentNode, nNext,shapeConstraintIntensity);
                currentNode.position -= compute_delta(currentNode.position, nNext.position, nodeLength);
            }
        }

        GroupMemoryBarrierWithGroupSync();
        VerletNode nPrev = verletNodes[index - 1];
        const float nodeLengthPrev = distance(restPosPrev, restPos);
         currentNode.position = bendConstraint(currentNode, nPrev,shapeConstraintIntensity);
        GroupMemoryBarrierWithGroupSync();

        currentNode.position += compute_delta(nPrev.position, currentNode.position, nodeLengthPrev);

        verletNodes[index] = currentNode;
        GroupMemoryBarrierWithGroupSync();
        
    }
    GroupMemoryBarrierWithGroupSync();
 
}
