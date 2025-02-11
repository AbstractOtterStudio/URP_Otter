half3 Interpolate3(half3 v1, half3 v2, half3 v3, half3 barycentricCoord)
{
    return v1 * barycentricCoord.x + v2 * barycentricCoord.y + v3 * barycentricCoord.z;
}

half4 Interpolate4(half4 v1, half4 v2, half4 v3, half3 barycentricCoord)
{
    return v1 * barycentricCoord.x + v2 * barycentricCoord.y + v3 * barycentricCoord.z;
}

float2 Interpolate2(float2 v1, float2 v2, float2 v3, float3 barycentricCoord)
{
    return v1 * barycentricCoord.x + v2 * barycentricCoord.y + v3 * barycentricCoord.z;
}

half3x3 createRotationMatrix(half3 Axis, half Rotation)
{
    Rotation = radians(Rotation);
    half s = sin(Rotation);
    half c = cos(Rotation);
    half one_minus_c = 1.0 - c;

    Axis = normalize(Axis);
    half3x3 rot_mat =
    {
        one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s,
        one_minus_c * Axis.z * Axis.x + Axis.y * s,
        one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c,
        one_minus_c * Axis.y * Axis.z - Axis.x * s,
        one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s,
        one_minus_c * Axis.z * Axis.z + c
    };
    return rot_mat;
}

half3 Unity_RotateAboutAxis_Degrees(half3 In, half3 Axis, half Rotation)
{
    return mul(createRotationMatrix(Axis, Rotation), In);
}

half4x4 quaternionToMatrix(half4 quat)
{
    half4x4 m = half4x4(half4(0, 0, 0, 0), half4(0, 0, 0, 0), half4(0, 0, 0, 0), half4(0, 0, 0, 0));

    half x = quat.x, y = quat.y, z = quat.z, w = quat.w;
    half x2 = x + x, y2 = y + y, z2 = z + z;
    half xx = x * x2, xy = x * y2, xz = x * z2;
    half yy = y * y2, yz = y * z2, zz = z * z2;
    half wx = w * x2, wy = w * y2, wz = w * z2;

    m[0][0] = 1.0 - (yy + zz);
    m[0][1] = xy - wz;
    m[0][2] = xz + wy;

    m[1][0] = xy + wz;
    m[1][1] = 1.0 - (xx + zz);
    m[1][2] = yz - wx;

    m[2][0] = xz - wy;
    m[2][1] = yz + wx;
    m[2][2] = 1.0 - (xx + yy);

    m[3][3] = 1.0;

    return m;
}

#define QUATERNION_IDENTITY half4(0, 0, 0, 1)

static half4 QuaternionLookRotation(half3 tangent, half3 normal)
{
    normal = normalize(normal);
    const half3 vector1 = normalize(tangent);
    const half3 vector2 = normalize(cross(normal, vector1));
    const half3 vector3 = cross(vector1, vector2);
    const half m00 = vector2.x;
    const half m01 = vector2.y;
    const half m02 = vector2.z;
    const half m10 = vector3.x;
    const half m11 = vector3.y;
    const half m12 = vector3.z;
    const half m20 = vector1.x;
    const half m21 = vector1.y;
    const half m22 = vector1.z;

    const half num8 = (m00 + m11) + m22;
    half4 quaternion = QUATERNION_IDENTITY;
    if (num8 > 0)
    {
        half num = sqrt(num8 + 1.0);
        quaternion.w = num * 0.5;
        num = 0.5 / num;
        quaternion.x = (m12 - m21) * num;
        quaternion.y = (m20 - m02) * num;
        quaternion.z = (m01 - m10) * num;
    }
    else if ((m00 >= m11) && (m00 >= m22))
    {
        const half num7 = sqrt(((1.0 + m00) - m11) - m22);
        const half num4 = 0.5 / num7;
        quaternion.x = 0.5 * num7;
        quaternion.y = (m01 + m10) * num4;
        quaternion.z = (m02 + m20) * num4;
        quaternion.w = (m12 - m21) * num4;
    }
    else if (m11 > m22)
    {
        const half num6 = sqrt(((1.0 + m11) - m00) - m22);
        const half num3 = 0.5 / num6;
        quaternion.x = (m10 + m01) * num3;
        quaternion.y = 0.5 * num6;
        quaternion.z = (m21 + m12) * num3;
        quaternion.w = (m20 - m02) * num3;
    }
    else
    {
        const half num5 = sqrt(((1.0 + m22) - m00) - m11);
        const half num2 = 0.5 / num5;
        quaternion.x = (m20 + m02) * num2;
        quaternion.y = (m21 + m12) * num2;
        quaternion.z = 0.5 * num5;
        quaternion.w = (m01 - m10) * num2;
    }
    return quaternion;
}

half4 qmul(half4 q1, half4 q2)
{
    return half4(
        q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz),
        q1.w * q2.w - dot(q1.xyz, q2.xyz)
    );
}

float3 Barycentric(float3 aV1, float3 aV2, float3 aV3, float3 aP)
{
    float3 a = aV2 - aV3, b = aV1 - aV3, c = aP - aV3;
    float aLen = a.x * a.x + a.y * a.y + a.z * a.z;
    float bLen = b.x * b.x + b.y * b.y + b.z * b.z;
    float ab = a.x * b.x + a.y * b.y + a.z * b.z;
    float ac = a.x * c.x + a.y * c.y + a.z * c.z;
    float bc = b.x * c.x + b.y * c.y + b.z * c.z;
    float d = aLen * bLen - ab * ab;

    float u = (aLen * bc - ab * ac) / d;
    float v = (bLen * ac - ab * bc) / d;
    float w = 1.0f - u - v;
    return float3(u, v, w);
}
