#include "MarshnerLightingInclude.cginc" 
 

half4 additionalLights(half3 worldNormal, half3 viewDirection,
                          half specularIntensity, half reflectionArea, half smoothness,
                        half3 worldPosition,half smoothness2,half specularIntensity2)
{
    half3 worldNormalIzed = normalize(worldNormal);
    viewDirection = SafeNormalize(viewDirection);
    int pixelLightCount = GetAdditionalLightsCount();
    half4 color = 0;
    for (int i = 0; i < pixelLightCount; ++i)
    {
        Light light = GetAdditionalLight(i, worldPosition);
        half attenuation = light.distanceAttenuation * light.shadowAttenuation;
        half3 attenuatedLightColor = light.color * attenuation;
 
        half4 c = 0;
        URPLighting_float(worldNormal, viewDirection, light.direction, attenuation,
                          half4(attenuatedLightColor,1), specularIntensity, reflectionArea, smoothness, c);
        color += c;
        URPLighting_float(worldNormal, viewDirection, light.direction, attenuation,
                      half4(attenuatedLightColor,1), specularIntensity2, reflectionArea, smoothness2, c);
        color += c;
    }
    return color;
}
