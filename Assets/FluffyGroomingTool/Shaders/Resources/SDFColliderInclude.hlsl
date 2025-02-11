 
#define INITIAL_DISTANCE 1e10f
#define MARGIN g_CellSize
#define GRID_MARGIN int3(1, 1, 1)
#include "FurMotionInclude.cginc"
#include "Thread.hlsl"


uniform float4 g_Origin;
uniform float g_CellSize;
uniform int g_NumCellsX;
uniform int g_NumCellsY;
uniform int g_NumCellsZ;


// bindsets: 0 -> GenerateSDFLayout
//           1 -> ApplySDFLayout
//           2 -> 

//Actually contains floats; make sure to use asfloat() when accessing. uint is used to allow atomics.
 
RWStructuredBuffer<uint> g_SignedDistanceField;
StructuredBuffer<uint> g_SignedDistanceField_read_only;

RWByteAddressBuffer g_TrimeshVertexIndices;

uniform RWStructuredBuffer<MeshProperties> collMeshVertexPositions;
//When building the SDF we want to find the lowest distance at each SDF cell. In order to allow multiple threads to write to the same
//cells, it is necessary to use atomics. However, there is no support for atomics with 32-bit floats so we convert the float into unsigned int
//and use atomic_min() / InterlockedMin() as a workaround.
//
//When used with atomic_min, both FloatFlip2() and FloatFlip3() will store the float with the lowest magnitude.
//The difference is that FloatFlip2() will preper negative values ( InterlockedMin( FloatFlip2(1.0), FloatFlip2(-1.0) ) == -1.0 ),
//while FloatFlip3() prefers positive values (  InterlockedMin( FloatFlip3(1.0), FloatFlip3(-1.0) ) == 1.0 ).
//Using FloatFlip3() seems to result in a SDF with higher quality compared to FloatFlip2().

uint FloatFlip3(float fl)
{
    uint f = asuint(fl);
    return (f << 1) | (f >> 31); //Rotate sign bit to least significant
}

uint IFloatFlip3(uint f2)
{
    return (f2 >> 1) | (f2 << 31);
}

// Get SDF cell index coordinates (x, y and z) from a point position in world space
int3 GetSdfCoordinates(float3 positionInWorld)
{
    float3 sdfPosition = (positionInWorld - g_Origin.xyz) / g_CellSize;

    int3 result;
    result.x = (int)sdfPosition.x;
    result.y = (int)sdfPosition.y;
    result.z = (int)sdfPosition.z;

    return result;
}

float3 GetSdfCellPosition(int3 gridPosition)
{
    float3 cellCenter = float3(gridPosition.x, gridPosition.y, gridPosition.z) * g_CellSize;
    cellCenter += g_Origin.xyz;

    return cellCenter;
}

int GetSdfCellIndex(int3 gridPosition)
{
    int cellsPerLine = g_NumCellsX;
    int cellsPerPlane = g_NumCellsX * g_NumCellsY;

    return cellsPerPlane * gridPosition.z + cellsPerLine * gridPosition.y + gridPosition.x;
}

float DistancePointToEdge(float3 p, float3 x0, float3 x1, out float3 n)
{
    float3 x10 = x1 - x0;

    float t = dot(x1 - p, x10) / dot(x10, x10);
    t = max(0.0f, min(t, 1.0f));

    float3 a = p - (t * x0 + (1.0f - t) * x1);
    float d = length(a);
    n = a / (d + 1e-30f);

    return d;
}

// Check if p is in the positive or negative side of triangle (x0, x1, x2)
// Positive side is where the normal vector of triangle ( (x1-x0) x (x2-x0) ) is pointing to.
float SignedDistancePointToTriangle(float3 p, float3 x0, float3 x1, float3 x2)
{
    float d = 0;
    float3 x02 = x0 - x2;
    float l0 = length(x02) + 1e-30f;
    x02 = x02 / l0;
    float3 x12 = x1 - x2;
    float l1 = dot(x12, x02);
    x12 = x12 - l1 * x02;
    float l2 = length(x12) + 1e-30f;
    x12 = x12 / l2;
    float3 px2 = p - x2;

    float b = dot(x12, px2) / l2;
    float a = (dot(x02, px2) - l1 * b) / l0;
    float c = 1 - a - b;

    // normal vector of triangle. Don't need to normalize this yet.
    float3 nTri = cross((x1 - x0), (x2 - x0));
    float3 n;

    float tol = 1e-8f;

    if (a >= -tol && b >= -tol && c >= -tol)
    {
        n = p - (a * x0 + b * x1 + c * x2);
        d = length(n);

        float3 n1 = n / d;
        float3 n2 = nTri / (length(nTri) + 1e-30f); // if d == 0

        n = (d > 0) ? n1 : n2;
    }
    else
    {
        float3 n_12;
        float3 n_02;
        d = DistancePointToEdge(p, x0, x1, n);

        float d12 = DistancePointToEdge(p, x1, x2, n_12);
        float d02 = DistancePointToEdge(p, x0, x2, n_02);

        d = min(d, d12);
        d = min(d, d02);

        n = (d == d12) ? n_12 : n;
        n = (d == d02) ? n_02 : n;
    }

    d = (dot(p - x0, nTri) < 0.f) ? -d : d;

    return d;
}


float LinearInterpolate(float a, float b, float t)
{
    return a * (1.0f - t) + b * t;
}

//    bilinear interpolation
//
//         p    :  1-p
//     c------------+----d
//     |            |    |
//     |            |    |
//     |       1-q  |    |
//     |            |    |
//     |            x    |
//     |            |    |
//     |         q  |    |
//     a------------+----b
//         p    :  1-p
//
//    x = BilinearInterpolate(a, b, c, d, p, q)
//      = LinearInterpolate(LinearInterpolate(a, b, p), LinearInterpolate(c, d, p), q)
float BilinearInterpolate(float a, float b, float c, float d, float p, float q)
{
    return LinearInterpolate(LinearInterpolate(a, b, p), LinearInterpolate(c, d, p), q);
}

float TrilinearInterpolate(float a, float b, float c, float d, float e, float f, float g, float h, float p, float q, float r)
{
    return LinearInterpolate(BilinearInterpolate(a, b, c, d, p, q), BilinearInterpolate(e, f, g, h, p, q), r);
}

// Get signed distance at the position in world space

float GetSignedDistance(float3 positionInWorld)
{
    int3 gridCoords = GetSdfCoordinates(positionInWorld);

    if (!(0 <= gridCoords.x && gridCoords.x < g_NumCellsX - 2)
        || !(0 <= gridCoords.y && gridCoords.y < g_NumCellsY - 2)
        || !(0 <= gridCoords.z && gridCoords.z < g_NumCellsZ - 2))
        return INITIAL_DISTANCE;

    int sdfIndices[8];
    {
        int index = GetSdfCellIndex(gridCoords);
        for (int i = 0; i < 8; ++i) sdfIndices[i] = index;

        int x = 1;
        int y = g_NumCellsX;
        int z = g_NumCellsY * g_NumCellsX;

        sdfIndices[1] += x;
        sdfIndices[2] += y;
        sdfIndices[3] += y + x;

        sdfIndices[4] += z;
        sdfIndices[5] += z + x;
        sdfIndices[6] += z + y;
        sdfIndices[7] += z + y + x;
    }

    float distances[8];

    for (int j = 0; j < 8; ++j)
    {
        int sdfCellIndex = sdfIndices[j];
        float dist = asfloat(g_SignedDistanceField_read_only[sdfCellIndex]);

        if (dist == INITIAL_DISTANCE)
            return INITIAL_DISTANCE;

        distances[j] = dist;
    }

    float distance_000 = distances[0]; // X,  Y,  Z
    float distance_100 = distances[1]; //+X,  Y,  Z
    float distance_010 = distances[2]; // X, +Y,  Z
    float distance_110 = distances[3]; //+X, +Y,  Z
    float distance_001 = distances[4]; // X,  Y, +Z
    float distance_101 = distances[5]; //+X,  Y, +Z
    float distance_011 = distances[6]; // X, +Y, +Z
    float distance_111 = distances[7]; //+X, +Y, +Z

    float3 cellPosition = GetSdfCellPosition(gridCoords);
    float3 interp = (positionInWorld - cellPosition) / g_CellSize;
    return TrilinearInterpolate(distance_000, distance_100, distance_010, distance_110,
                                distance_001, distance_101, distance_011, distance_111,
                                interp.x, interp.y, interp.z);
}

float collisionMargin;

void collideWithSDF(inout int didCollide, inout float3 hairVertex, float vertexMoveAbility)
{
    //When using forward differences to compute the SDF gradient,
    //we need to use trilinear interpolation to look up 4 points in the SDF.
    //In the worst case, this involves reading the distances in 4 different cubes (reading 32 floats from the SDF).
    //In the ideal case, only 1 cube needs to be read (reading 8 floats from the SDF).
    //The number of distance lookups varies depending on whether the 4 points cross cell boundaries.
    //
    //If we assume that (h < g_CellSize), where h is the distance between the points used for finite difference,
    //we can mix forwards and backwards differences to ensure that the points never cross cell boundaries (always read 8 floats).
    //By default we use forward differences, and switch to backwards differences for each dimension that crosses a cell boundary.
    //
    //This is much faster than using forward differences only, but it could also be less stable.
    //In particular, it has the effect of making the gradient less accurate. The amount of inaccuracy is 
    //proportional to the size of h, so it is recommended keep h as low as possible. 
    float3 vertexInSdfLocalSpace = hairVertex;
    didCollide = 0;
    int3 gridCoords = GetSdfCoordinates(vertexInSdfLocalSpace);

    if (!(0 <= gridCoords.x && gridCoords.x < g_NumCellsX - 2)
        || !(0 <= gridCoords.y && gridCoords.y < g_NumCellsY - 2)
        || !(0 <= gridCoords.z && gridCoords.z < g_NumCellsZ - 2))
        return;

    float distances[8];
    {
        int sdfIndices[8];
        {
            int index = GetSdfCellIndex(gridCoords);
            for (int i = 0; i < 8; ++i) sdfIndices[i] = index;

            int x = 1;
            int y = g_NumCellsX;
            int z = g_NumCellsY * g_NumCellsX;

            sdfIndices[1] += x;
            sdfIndices[2] += y;
            sdfIndices[3] += y + x;

            sdfIndices[4] += z;
            sdfIndices[5] += z + x;
            sdfIndices[6] += z + y;
            sdfIndices[7] += z + y + x;
        }

        for (int j = 0; j < 8; ++j)
        {
            int sdfCellIndex = sdfIndices[j];
            float dist = asfloat(g_SignedDistanceField[sdfCellIndex]);
            if (dist == INITIAL_DISTANCE) return;

            distances[j] = dist;
        }
    }

    float distance_000 = distances[0]; // X,  Y,  Z
    float distance_100 = distances[1]; //+X,  Y,  Z
    float distance_010 = distances[2]; // X, +Y,  Z
    float distance_110 = distances[3]; //+X, +Y,  Z
    float distance_001 = distances[4]; // X,  Y, +Z
    float distance_101 = distances[5]; //+X,  Y, +Z
    float distance_011 = distances[6]; // X, +Y, +Z
    float distance_111 = distances[7]; //+X, +Y, +Z

    float3 cellPosition = GetSdfCellPosition(gridCoords);
    float3 interp = (vertexInSdfLocalSpace - cellPosition) / g_CellSize;
    float distanceAtVertex = TrilinearInterpolate(distance_000, distance_100, distance_010, distance_110,
                                                  distance_001, distance_101, distance_011, distance_111,
                                                  interp.x, interp.y, interp.z);

    //Compute gradient with finite differences
    float3 sdfGradient;
    {
        float h = 0.1f * g_CellSize;
        float3 direction;
        direction.x = (interp.x + h < 1.0f) ? 1.0f : -1.0f;
        direction.y = (interp.y + h < 1.0f) ? 1.0f : -1.0f;
        direction.z = (interp.z + h < 1.0f) ? 1.0f : -1.0f;

        float3 neighborDistances;
        neighborDistances.x = TrilinearInterpolate(distance_000, distance_100, distance_010, distance_110,
                                                   distance_001, distance_101, distance_011, distance_111,
                                                   interp.x + h * direction.x, interp.y, interp.z);
        neighborDistances.y = TrilinearInterpolate(distance_000, distance_100, distance_010, distance_110,
                                                   distance_001, distance_101, distance_011, distance_111,
                                                   interp.x, interp.y + h * direction.y, interp.z);
        neighborDistances.z = TrilinearInterpolate(distance_000, distance_100, distance_010, distance_110,
                                                   distance_001, distance_101, distance_011, distance_111,
                                                   interp.x, interp.y, interp.z + h * direction.z);

        sdfGradient = (direction * (neighborDistances - float3(distanceAtVertex, distanceAtVertex, distanceAtVertex))) / h;
    }

    //Project hair vertex out of SDF 
    float3 normal = normalize(sdfGradient);
    float collMargin = collisionMargin;
    if (!isnan(normal.x) && !isinf(normal.x) && distanceAtVertex < collMargin)
    {
#if defined(SHADER_API_METAL)
        distanceAtVertex = clamp(distanceAtVertex, 0.0001, 0.03);
#endif
        hairVertex = hairVertex.xyz + normal * (collMargin - distanceAtVertex);
        didCollide = 1;
    }
}

//SDF-Hair collision using forward differences only
// One thread per one hair vertex

void CollideHairVerticesWithSdf_forward(inout int didCollide, inout float3 hairVertex, float vertexMoveAbility)
{
    didCollide = 0;
    const float3 vertexInSdfLocalSpace = hairVertex;

    float distance = GetSignedDistance(vertexInSdfLocalSpace);

    // early exit if the distance is larger than collision margin
    if (distance > collisionMargin)
        return;

    // small displacement. 
    float h = 0.1f * g_CellSize;

    float3 sdfGradient;
    {
        //Compute gradient using forward difference
        float3 offset[3];
        offset[0] = float3(1, 0, 0);
        offset[1] = float3(0, 1, 0);
        offset[2] = float3(0, 0, 1);

        float3 neighborCellPositions[3];

        for (int i = 0; i < 3; ++i)
            neighborCellPositions[i] = vertexInSdfLocalSpace + offset[i] * h;

        //Use trilinear interpolation to get distances
        float neighborCellDistances[3];

        for (int j = 0; j < 3; ++j)
            neighborCellDistances[j] = GetSignedDistance(neighborCellPositions[j]);

        float3 forwardDistances;
        forwardDistances.x = neighborCellDistances[0];
        forwardDistances.y = neighborCellDistances[1];
        forwardDistances.z = neighborCellDistances[2];

        sdfGradient = (forwardDistances - float3(distance, distance, distance)) / h;
    }

    //Project hair vertex out of SDF
    const float3 normal = normalize(sdfGradient);

    float collMargin = collisionMargin;
    if (!isnan(normal.x) && !isinf(normal.x) && distance < collMargin)
    {
#if defined(SHADER_API_METAL)
        distance = clamp(distance, 0.0001, 0.03);
#endif
         
        hairVertex = hairVertex + normal * (collMargin - distance);
        didCollide = 1;
    }

}
