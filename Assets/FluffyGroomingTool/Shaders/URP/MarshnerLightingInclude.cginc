static const half MAX_TRANSLUNCENCY = 5;
static const half MAX_REFLECTION = 22;
#define PIc 3.1415926
#define SQRT2PI 2.50663

half3 BackLighting(half3 lightDirection, half3 viewDir, half3 worldNormal, half attenuation,
                    half distortion, half power, half scale, half4 subsurfaceColor)
{
    half3 H = normalize(lightDirection + worldNormal * distortion);
    half I = pow(saturate(dot(viewDir, -H)), power) * scale;
    return subsurfaceColor * 5 * I * attenuation;
}

inline half square(half x)
{
    return x * x;
}

inline half Hair_G(half B, half Theta)
{
    return exp(-0.5 * square(Theta) / (B * B)) / (SQRT2PI * B);
}

half HairIOF(half Eccentric)
{
    half n = 1.55;
    half a = 1 - Eccentric;
    half ior1 = 2 * (n - 1) * (a * a) - n + 2;
    half ior2 = 2 * (n - 1) / (a * a) - n + 2;
    return 0.5f * ((ior1 + ior2) + 0.5f * (ior1 - ior2));
}

inline half3 SpecularFresnel(half3 F0, half vDotH)
{
    return F0 + (1.0f - F0) * pow(1 - vDotH, 5);
}

half3 DiffuseLight(half3 Albedo, half3 L, half3 N)
{
    half DiffuseScatter = (1.0 / PIc) * saturate(dot(N, L));
    return Albedo * DiffuseScatter;
}

const half Alpha[] =
{
    -0.0998,
    0.0499f,
    0.1996
};

half3 HairSpecularMarschner(half3 Albedo, half3 L, half3 V, half3 N, half Area, half smoothness)
{
    half3 S = 0;

    const half VoL = dot(V, L);
    const half SinThetaL = dot(N, L);
    const half SinThetaV = dot(N, V);
    half cosThetaL = sqrt(max(0, 1 - SinThetaL * SinThetaL));
    half cosThetaV = sqrt(max(0, 1 - SinThetaV * SinThetaV));
    half CosThetaD = sqrt((1 + cosThetaL * cosThetaV + SinThetaV * SinThetaL) / 2.0);

    const half3 Lp = L - SinThetaL * N;
    const half3 Vp = V - SinThetaV * N;
    const half CosPhi = dot(Lp, Vp) * rsqrt(dot(Lp, Lp) * dot(Vp, Vp) + 1e-4);
    const half CosHalfPhi = sqrt(saturate(0.5 + 0.5 * CosPhi));


    half B[] =
    {
        Area + square(1 - smoothness),
        Area + square(1 - smoothness) / 2,
        Area + square(1 - smoothness * 2)
    };

    half hairIOF = HairIOF(0);
    half F0 = square((1 - hairIOF) / (1 + hairIOF));

    half3 Tp;
    half Mp, Np, Fp, f;
    half ThetaH = SinThetaL + SinThetaV;
    // R
    Mp = Hair_G(B[0], ThetaH - Alpha[0]);
    Np = 0.25 * CosHalfPhi;
    Fp = SpecularFresnel(F0, sqrt(saturate(0.5 + 0.5 * VoL)));
    S += (Mp * Np) * (Fp * lerp(1, 0, saturate(-VoL)));

    // TRT
    Mp = Hair_G(B[2], ThetaH - Alpha[2]);
    f = SpecularFresnel(F0, CosThetaD * 0.5f);
    Fp = square(1 - f) * f;
    Tp = pow(Albedo, 0.8 / CosThetaD);
    Np = exp(17 * CosPhi - 16.78);

    S += (Mp * Np) * (Fp * Tp);

    return S;
}

half3 HairBxDF(half3 Albedo, half3 N, half3 V, half3 L, half Shadow, half reflectionArea, half smoothness)
{
    return DiffuseLight(Albedo, L, N) + HairSpecularMarschner(Albedo, L, V, N, reflectionArea, clamp(smoothness, 0, 0.99)) * Shadow;
}

void URPLighting_float(half3 worldNormal, half3 viewDirection, half3 lightDirection, half attenuation,
                       half4 SpecularColor, half specularIntensity, half reflectionArea, half smoothness,
                       out half4 Color)
{
    #ifdef SHADERGRAPH_PREVIEW
    Color = 1;
    #else
    half3 marshnerLighting = SpecularColor * MAX_REFLECTION * specularIntensity *
        HairBxDF(
            half3(0, 0, 0),
            worldNormal,
            viewDirection,
            -lightDirection,
            attenuation,
            reflectionArea,
            smoothness
        );
    Color = half4(marshnerLighting.x, marshnerLighting.y, marshnerLighting.z, 1);

    #endif
}

void URPBackLighting_float(half3 lightDirection, half3 viewDir, half3 worldNormal, half attenuation,
                           half distortion, half power, half scale, half4 subsurfaceColor, out half4 Color)
{
    Color = 1;
    Color.xyz = BackLighting(lightDirection, viewDir, worldNormal, attenuation, distortion, power, scale, subsurfaceColor);
}
