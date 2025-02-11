struct StrandGroomStruct
{
    float2 flowDirectionRoot;
    float2 flowDirectionBend;
    float2 flowDirectionOrientation;
    float2 scale;
    float raise;
    float isErased;
    float windContribution;
    float clumpMask;
    float4 twist;
    float4 overrideColor;
};


uniform float worldScale = 1;

#define SIZEOF_FLOAT3 12
uniform RWByteAddressBuffer sourceMeshData;
uniform uint vertexBufferStride;

static int DIRECTION_ROOT = 0;
static int HEIGHT = 1;
static int RAISE = 2;
static int MASK = 3;
static int ADD_FUR = 4;
static int DELETE_FUR = 5;
static int WIND_MAX_DISTANCE = 6;
static int CLUMPING_MASK = 7;
static int DIRECTION_BEND = 8;
static int DIRECTION_ORIENTATION = 9;
static int TWIST = 10;
static int RESET = 11;
static int ATTRACT = 12;
static int SMOOTH = 13;
static int WIDTH = 14;
static int COLOR_OVERRIDE = 15;

uniform int brushMenuType;
uniform float3 mouseHitPoint;
uniform float3 previousMouseHitPoint;
uniform float3 mouseHitNormal;
uniform float brushSize;
uniform float brushFalloff;
uniform float brushOpacity;
uniform float maskErase;
uniform bool ignoreNormal;
uniform float brushIntensity;
uniform bool isClumpTwistSelected;
uniform float twistSpread;
uniform float twistAmount;
uniform float overrideIntensity;
uniform float4 overrideColor;
uniform float3 clickStartHitPoint;
uniform float4 resetValues;

static float OPACITY_SENSITIVITY = 0.1;
static float FALLOFF_MASK_TRESHOLD = 0.01;
static float CARD_MESH_HEIGHT = 0.12;

uniform float4x4 localToWorldMatrix;
uniform float4x4 localToWorldRotationMatrix;
uniform float4x4 worldToLocalMatrix;

float linearFalloff(float distance, float brushRadius)
{
    return saturate(1 - distance / brushRadius);
}

float normalFalloff(float3 normal, float3 hitNormal)
{
    float falloff = 1;
    if (!ignoreNormal)
    {
        falloff = saturate(dot(normalize(hitNormal), normalize(normal)) - 0.75f) * 4.0;
    }
    return falloff;
}

float calculateFalloff(float mag, float3 normal, float3 hitNormal)
{
    float falloff = linearFalloff(mag, brushSize);
    falloff = pow(falloff, saturate(1.0 - brushFalloff / brushSize)) * (brushOpacity * OPACITY_SENSITIVITY);
    falloff *= normalFalloff(normal, hitNormal);
    return falloff;
}

StrandGroomStruct lerpScale(StrandGroomStruct fpsp, float intensity, float value, bool scaleWidth, bool scaleHeight)
{
    if (value > 1)
    {
        if (scaleWidth) fpsp.scale = float2(intensity, fpsp.scale.y);
        if (scaleHeight) fpsp.scale = float2(fpsp.scale.x, intensity);
    }
    if (scaleWidth) fpsp.scale = float2(fpsp.scale.x + (intensity - fpsp.scale.x) * value, fpsp.scale.y);
    if (scaleHeight) fpsp.scale = float2(fpsp.scale.x, fpsp.scale.y + (intensity - fpsp.scale.y) * value);
    return fpsp;
}

StrandGroomStruct lerpRaise(StrandGroomStruct fpsp, float intensity, float value)
{
    if (value > 1)
    {
        fpsp.raise = intensity;
    }
    else
    {
        fpsp.raise = fpsp.raise + (intensity - fpsp.raise) * value;
    }

    return fpsp;
}

StrandGroomStruct lerpClumpingMask(StrandGroomStruct fpsp, float intensity, float value)
{
    if (value > 1)
    {
        fpsp.clumpMask = intensity;
    }
    else
    {
        fpsp.clumpMask = fpsp.clumpMask + (intensity - fpsp.clumpMask) * value;
    }
    return fpsp;
}

StrandGroomStruct lerpTwist(StrandGroomStruct fpsp, float twistAmount, float value, bool isClumpTwist,
                            float twistSpread)
{
    if (value > 1)
    {
        float4 twist = fpsp.twist;
        if (isClumpTwist)
        {
            twist.y = twistAmount;
            twist.w = twistSpread;
        }
        else
        {
            twist.x = twistAmount;
            twist.z = twistSpread;
        }
        fpsp.twist = twist;
    }
    else
    {
        float4 twist = fpsp.twist;
        if (isClumpTwist)
        {
            twist.y = twist.y + (twistAmount - twist.y) * value;
            twist.w = twist.w + (twistSpread - twist.w) * value;
        }
        else
        {
            twist.x = twist.x + (twistAmount - twist.x) * value;
            twist.z = twist.z + (twistSpread - twist.z) * value;
        }
        fpsp.twist = twist;
    }
    return fpsp;
}

StrandGroomStruct lerpColorOverride(StrandGroomStruct fpsp, float intensity, float value, float4 color)
{
    intensity = 1 - intensity;
    float4 overrideColor = fpsp.overrideColor;
    overrideColor.x = saturate(lerp(overrideColor.x, color.x, value));
    overrideColor.y = saturate(lerp(overrideColor.y, color.y, value));
    overrideColor.z = saturate(lerp(overrideColor.z, color.z, value));
    if (value > 1)
    {
        overrideColor.w = intensity;
        fpsp.overrideColor = overrideColor;
    }
    else
    {
        overrideColor.w = saturate(lerp(overrideColor.w, intensity, value));
        fpsp.overrideColor = overrideColor;
    }
    return fpsp;
}


float3 calculateFlowDirection(
    float3 normal,
    float4 tangent,
    float3 currentHitPont,
    float3 previousHitPoint
)
{
    float3 biNormal = cross(normal, tangent.xyz);
    float4x4 mat = float4x4(
        tangent.x, tangent.y, tangent.z, 0,
        biNormal.x, biNormal.y, biNormal.z, 0,
        normal.x, normal.y, normal.z, 0,
        0, 0, 0, 1);

    float3 currentPoint = mul(worldToLocalMatrix, float4(currentHitPont, 1.0)).xyz;
    float3 previousPoint = mul(worldToLocalMatrix, float4(previousHitPoint, 1.0)).xyz;
    float3 textureDirection = mul(mat, float4((currentPoint - previousPoint) / worldScale, 1.0)).xyz;
    textureDirection.z = -textureDirection.y / worldScale;
    textureDirection.y = 0;
    textureDirection = normalize(textureDirection);
    textureDirection = (textureDirection + float3(1, 1, 1)) / 2;
    return textureDirection;
}

StrandGroomStruct lerpDirectionBend(StrandGroomStruct fpsp, float dirX, float dirY, float value)
{
    fpsp.flowDirectionBend = float2(
        fpsp.flowDirectionBend.x + (dirX - fpsp.flowDirectionBend.x) * value,
        fpsp.flowDirectionBend.y + (dirY - fpsp.flowDirectionBend.y) * value
    );
    return fpsp;
}

StrandGroomStruct lerpDirectionOrientation(StrandGroomStruct fpsp, float dirX, float dirY, float value)
{
    fpsp.flowDirectionOrientation = float2(
        fpsp.flowDirectionOrientation.x + (dirX - fpsp.flowDirectionOrientation.x) * value,
        fpsp.flowDirectionOrientation.y + (dirY - fpsp.flowDirectionOrientation.y) * value
    );
    return fpsp;
}

StrandGroomStruct lerpDirectionRoot(StrandGroomStruct fpsp, float dirX, float dirY, float value)
{
    fpsp.flowDirectionRoot = float2(
        fpsp.flowDirectionRoot.x + (dirX - fpsp.flowDirectionRoot.x) * value,
        fpsp.flowDirectionRoot.y + (dirY - fpsp.flowDirectionRoot.y) * value
    );
    return fpsp;
}

float getCardHeight(StrandGroomStruct sgs)
{
    return CARD_MESH_HEIGHT * sgs.scale.y * 3.5;
    //Todo: WTF MAGIC NUMBER? The number should probably be a slider in the UI. The number says when the strand start standing up. 
}

StrandGroomStruct blendSmootie(StrandGroomStruct fpsp, float intensity, float falloff,
                               float resetLengthAmount, float resetWidthAmount, float resetOrientAmount,
                               float resetBendAmount, float2 bendAverage,
                               float2 orientationAverage, float2 scaleAverage)
{
    float brushIntensity = intensity * falloff;

    fpsp.flowDirectionBend = lerp(fpsp.flowDirectionBend, bendAverage, resetBendAmount * brushIntensity);
    fpsp.flowDirectionOrientation = lerp(fpsp.flowDirectionOrientation, orientationAverage,
                                         resetOrientAmount * brushIntensity);
    fpsp.scale.y = lerp(fpsp.scale.y, scaleAverage.y, resetLengthAmount * brushIntensity);
    fpsp.scale.x = lerp(fpsp.scale.x, scaleAverage.x, resetWidthAmount * brushIntensity);
    return fpsp;
}

StrandGroomStruct lerpReset(StrandGroomStruct fpsp, float intensity, float falloff,
                            float resetLengthAmount, float resetWidthAmount, float resetOrientAmount,
                            float resetBendAmount)
{
    float2 centered = float2(0.5, 0.5);
    return blendSmootie(fpsp, intensity, falloff, resetLengthAmount, resetWidthAmount, resetOrientAmount,
                        resetBendAmount, centered, centered, centered);
}

StrandGroomStruct paintAttract(
    StrandGroomStruct fpsp,
    float falloff,
    float3 normal,
    float4 tangent,
    float3 vertPos
)
{
    fpsp = lerpReset(
        fpsp,
        brushIntensity,
        falloff,
        0, 0, 0, 1);

    float3 texDir = calculateFlowDirection(normal, tangent, clickStartHitPoint, vertPos);
    float distanceToVertFomClickStart = distance(clickStartHitPoint, vertPos);
    float cardHeight = getCardHeight(fpsp);
    float standUpAtEndAmount = 1.0 - saturate(distanceToVertFomClickStart / cardHeight);
    texDir = lerp(texDir, float3(1, 1, 1) / 2.0, standUpAtEndAmount);
    return lerpDirectionOrientation(fpsp, texDir.x, texDir.z, falloff);
}

StrandGroomStruct lerpWindContribution(StrandGroomStruct fpsp, float intensity, float value)
{
    if (value > 1)
    {
        fpsp.windContribution = intensity;
    }
    else
    {
        fpsp.windContribution = fpsp.windContribution + (intensity - fpsp.windContribution) * value;
    }
    return fpsp;
}

AppendStructuredBuffer<float2> smoothIndicesAndFalloffs;
StructuredBuffer<float2> smoothIndicesAndFalloffsRead;
RWStructuredBuffer<int> smoothSumBuffer;


static float PI = 3.14159265358979;

float CalculateAngle(float2 arrival)
{
    float value = (float)((atan2(arrival.x, arrival.y) / PI) * 180.0);
    return value;
}

float4x4 getScaleMatrix(float3 scale) 
{
    if (scale.y == 0 || isnan(scale.y) || isinf(scale.y))scale.y = 0.0001;
    if (scale.x == 0 || isnan(scale.x) || isinf(scale.x))scale.x = 0.0001;
    return float4x4(
        scale.x, 0, 0, 0,
        0, scale.y, 0, 0,
        0, 0, scale.z, 0,
        0, 0, 0, 1
    );
}

float4x4 getQuaternionMatrix(float3 rotationEuler)
{
    float radX = radians(rotationEuler.x);
    float radY = radians(rotationEuler.y);
    float radZ = radians(rotationEuler.z);

    float sinX, cosX;
    sincos(radX, sinX, cosX);

    float sinY, cosY;
    sincos(radY, sinY, cosY);

    float sinZ, cosZ;
    sincos(radZ, sinZ, cosZ);

    return float4x4(
        cosY * cosZ, -cosY * sinZ, sinY, 0,
        cosX * sinZ + sinX * sinY * cosZ, cosX * cosZ - sinX * sinY * sinZ, -sinX * cosY, 0,
        sinX * sinZ - cosX * sinY * cosZ, sinX * cosZ + cosX * sinY * sinZ, cosX * cosY, 0,
        0, 0, 0, 1
    );
}

float getRaiseFactor(float raiseAmount)
{
    return lerp(90.0, 0.0, raiseAmount);
}

float maxScaleHeight;
float minScaleHeight;
float maxScaleWidth;
float minScaleWidth;

float2 getScaleFactor(float2 scale)
{
    scale.x = lerp(minScaleWidth, maxScaleWidth, scale.x);
    scale.y = lerp(minScaleHeight, maxScaleHeight, scale.y);
    return scale;
}

float4 getOrientationRotationValues(float2 flowDirection, float rootDirectionAngle)
{
    float thisDirectionAngle = CalculateAngle(float2(flowDirection.x - 0.5f, flowDirection.y - 0.5f));
    float bendAngle = thisDirectionAngle - rootDirectionAngle;
    float bendAngleInaRadians = bendAngle * PI / 180;
    float2 rootDir = normalize(float2(sin(rootDirectionAngle * PI / 180),
                                      cos(rootDirectionAngle * PI / 180)));
    float2 thisDir = normalize(float2(sin(thisDirectionAngle * PI / 180),
                                      cos(thisDirectionAngle * PI / 180)));
    float dotProd = abs(dot(rootDir, thisDir));
    float yRotation = 90 - dotProd * 90;
    float bendAmount = distance(flowDirection, float2(0.5, 0.5)) * 2;
    return float4(sin(bendAngleInaRadians), yRotation, cos(bendAngleInaRadians), bendAmount);
}

float4 Interpolate4(float4 v1, float4 v2, float4 v3, float3 barycentricCoord)
{
    return v1 * barycentricCoord.x + v2 * barycentricCoord.y + v3 * barycentricCoord.z;
}

float3 Interpolate3(float3 v1, float3 v2, float3 v3, float3 barycentricCoord)
{
    return v1 * barycentricCoord.x + v2 * barycentricCoord.y + v3 * barycentricCoord.z;
}

float2 Interpolate2(float2 v1, float2 v2, float2 v3, float3 barycentricCoord)
{
    return v1 * barycentricCoord.x + v2 * barycentricCoord.y + v3 * barycentricCoord.z;
}

float Interpolate(float v1, float v2, float v3, float3 barycentricCoord)
{
    return v1 * barycentricCoord.x + v2 * barycentricCoord.y + v3 * barycentricCoord.z;
}

float rand(float n)
{
    return frac(sin(dot(float2(n, n * 2), float2(12.9898, 4.1414))) * 43758.5453) - 0.5;
}
