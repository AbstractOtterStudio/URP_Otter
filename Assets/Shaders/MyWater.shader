Shader "Water/MyWater"
{
    Properties
    {
        [Header(Colors)][Space]
        [KeywordEnum(Linear, Gradient Texture)] _ColorMode ("     Source{Colors}", Float) = 0.0
        [KeywordEnum(Luma, Multiplicative)] _WaterBlendMode ("     Water Blend", Int) = 0
        _ColorShallow ("[_COLORMODE_LINEAR]     Shallow", Color) = (0.35, 0.6, 0.75, 0.8) // Color alpha controls opacity
        _ColorDeep ("[_COLORMODE_LINEAR]     Deep", Color) = (0.65, 0.9, 1.0, 1.0)
        [NoScaleOffset] _ColorGradient("[_COLORMODE_GRADIENT_TEXTURE]     Gradient", 2D) = "white" {}
        _StartFade("     Start fade", Float) = 0.5
        _FadeDistance("     Shallow depth", Float) = 0.5
        _WaterDepth("     Gradient size", Float) = 5.0
        _LightContribution("     Light Color Contribution", Range(0, 1)) = 0
        _Alpha("     Alpha", Range(0, 1)) = 1


        [Space]
        //_WaterClearness("     Transparency", Range(0, 1)) = 0.3
        _ShadowColor("     Shadow Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _ShadowStrength("     Surface Shadow", Range(0, 1)) = 0.35

        [Header(Crest)][Space]
        _CrestColor("     Color{Crest}", Color) = (1.0, 1.0, 1.0, 0.9)
        _CrestSize("     Size{Crest}", Range(0, 1)) = 0.1
        _CrestSharpness("     Sharp transition{Crest}", Range(0, 1)) = 0.1

        [Space][Header(Wave geometry)][Space]
        [KeywordEnum(None, Round, Grid, Pointy)] _WaveMode ("     Shape{Wave}", Float) = 1.0
        _WaveSpeed("[!_WAVEMODE_NONE]     Speed{Wave}", Float) = 0.5
        _WaveAmplitude("[!_WAVEMODE_NONE]     Amplitude{Wave}", Float) = 0.25
        _WaveFrequency("[!_WAVEMODE_NONE]     Frequency{Wave}", Float) = 1.0
        _WaveDirection("[!_WAVEMODE_NONE]     Direction{Wave}", Range(-1.0, 1.0)) = 0
        _WaveNoise("[!_WAVEMODE_NONE]     Noise{Wave}", Range(0, 1)) = 0.25

        [Space][Header(Foam)][Space]
        [KeywordEnum(None, Gradient Noise, Texture)] _FoamMode ("     Source{Foam}", Float) = 1.0
        [KeywordEnum(Move, Stack)] _FoamSampleMode("[_FOAMMODE_TEXTURE]           Sample Mode{Foam}", Float) = 1.0
        [NoScaleOffset] _NoiseMap("[_FOAMMODE_TEXTURE]           Texture{Foam}", 2D) = "white" {}
        _FoamColor("[!_FOAMMODE_NONE]     Color{Foam}", Color) = (1, 1, 1, 1)
        [Space]
        _ShoreFoamDepth("[!_FOAMMODE_NONE]     Shore Foam Depth{Foam}", Float) = 0.5
        _FoamNoiseAmount("[!_FOAMMODE_NONE]     Shore Blending{Foam}", Range(0.0, 1.0)) = 1.0
        [Space]
        _FoamAmount("[!_FOAMMODE_NONE]     Amount{Foam}", Range(0, 3)) = 0.25
        [Space]
        _FoamScale("[!_FOAMMODE_NONE]     Scale{Foam}", Float) = 1
        _FoamStretchX("[!_FOAMMODE_NONE]     Stretch X{Foam}", Range(0, 10)) = 1
        _FoamStretchY("[!_FOAMMODE_NONE]     Stretch Y{Foam}", Range(0, 10)) = 1
        [Space]
        _FoamSharpness("[!_FOAMMODE_NONE]     Sharpness{Foam}", Range(0, 1)) = 0.5
        [Space]
        _FoamSpeed("[!_FOAMMODE_NONE]     Speed{Foam}", Float) = 0.1
        _FoamDirection("[!_FOAMMODE_NONE]     Direction{Foam}", Range(-1.0, 1.0)) = 0
        _FoamFadeSpeed("     Foam fade speed", Float) = 0.1

        [Space][Header(Refraction)][Space]
        _RefractionDirection("     Direction{Refraction}", Range(0.0, 2.0)) = 0
        _RefractionFrequency("     Frequency", Float) = 35
        _RefractionAmplitude("     Amplitude", Range(0, 0.1)) = 0.01
        _RefractionSpeed("     Speed", Float) = 0.1
        _RefractionScale("     Scale", Float) = 1

        /*
        [Space][Header(Specular (Experimental))][Space]
        _FresnelAmount("     Amount", Range(0, 1)) = 0.0
        _FresnelSharpness("     Sharpness", Range(0, 1)) = 0.5
        _SpecularColor("     Color", Color) = (1, 1, 1, 0)
        _SunReflection("     Sun Reflection", Range(0, 1)) = 0.0
        */

        [Space][Header(Rendering options)][Space]
        [ToggleOff] _Opaque("     Opaque", Float) = 0.0
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"
        }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        Lighting Off

        Pass
        {
            ZWrite Off
            ZTest LEqual
            HLSLPROGRAM
            //#pragma prefer_hlslcc gles
            //#pragma exclude_renderers d3d11_9x
            //#pragma target 2.0
            #pragma target 4.0
            #pragma enable_d3d11_debug_symbols

            #pragma shader_feature_local _COLORMODE_LINEAR _COLORMODE_GRADIENT_TEXTURE
            #pragma shader_feature_local _WATERBLENDMODE_LUMA _WATERBLENDMODE_MULTIPLICATIVE
            #pragma shader_feature_local _FOAMMODE_NONE _FOAMMODE_GRADIENT_NOISE _FOAMMODE_TEXTURE
            #pragma shader_fetaure_local _FOAMSAMPLEMODE_MOVE _FOAMSAMPLEMODE_STACK
            #pragma shader_feature_local _WAVEMODE_NONE _WAVEMODE_ROUND _WAVEMODE_GRID _WAVEMODE_POINTY

            #pragma multi_compile_fog

            // -------------------------------------
            // Universal Render Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
            #include "Assets/Shaders/include/EncodingHelper.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            #if defined(_COLORMODE_GRADIENT_TEXTURE)
            TEXTURE2D(_ColorGradient);
            SAMPLER(sampler_ColorGradient);
            float4 _ColorGradient_ST;
            #endif

            #if defined(_COLORMODE_LINEAR)
            half4 _ColorShallow, _ColorDeep;
            #endif

            half4 _ShadowColor;

            float _FadeDistance, _WaterDepth, _StartFade;
            float _Alpha;

            half _LightContribution;

            half _WaveFrequency, _WaveAmplitude, _WaveSpeed, _WaveDirection, _WaveNoise;
            half /*_WaterClearness, */_CrestSize, _CrestSharpness, _ShadowStrength;

            half4 _CrestColor;
            half4 _FoamColor;
            half _ShoreFoamDepth, _FoamAmount, _FoamScale, _FoamSharpness, _FoamStretchX, _FoamStretchY, _FoamSpeed,
                _FoamDirection, _FoamNoiseAmount, _RefractionFrequency, _RefractionAmplitude, _RefractionSpeed,
                _RefractionScale, _FresnelAmount, _FresnelSharpness, _SunReflection, _FoamFadeSpeed, _RefractionDirection,
                _SurfaceFoamStartDepth, _SurfaceFoamEndDepth, _FoamFilterStride, _FoamFilterSize;

            half4 _SpecularColor;
            half _SpecularStrength;

            sampler2D _ShorelineBuffer;

            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);
            float4 _NoiseMap_ST;

            sampler2D _WaterDepthBuffer;

            struct VertexInput
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                float4 positionCS : POSITION0;
                float3 positionWS : POSITION1;
                float2 uv : TEXCOORD0;
                float4 screenPosition : TEXCOORD1;
                float waveHeight : TEXCOORD2;

                float3 normal : TEXCOORD3; // World space.
                float3 viewDir : TEXCOORD4; // World space.

                half fogFactor : TEXCOORD5;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 GradientNoise_Dir(float2 p)
            {
                // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
                // 3d0a9085-1fec-441a-bba6-f1121cdbe3ba
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }

            float GradientNoise(float2 UV, float Scale)
            {
                const float2 p = UV * Scale;
                const float2 ip = floor(p);
                float2 fp = frac(p);
                const float d00 = dot(GradientNoise_Dir(ip), fp);
                const float d01 = dot(GradientNoise_Dir(ip + float2(0, 1)), fp - float2(0, 1));
                const float d10 = dot(GradientNoise_Dir(ip + float2(1, 0)), fp - float2(1, 0));
                const float d11 = dot(GradientNoise_Dir(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
            }

            inline float GetWaterDepth(float2 uv, VertexOutput i)
            {
                const float is_ortho = unity_OrthoParams.w;
                const float is_persp = 1.0 - unity_OrthoParams.w;

                const float depth_packed = SampleSceneDepth(uv);

                // Separately handles orthographic and perspective cameras.
                const float scene_depth = lerp(_ProjectionParams.z, _ProjectionParams.y, depth_packed) * is_ortho +
                    LinearEyeDepth(depth_packed, _ZBufferParams) * is_persp;
                const float surface_depth = lerp(_ProjectionParams.z, _ProjectionParams.y, i.screenPosition.z) *
                    is_ortho + i.
                               screenPosition.w * is_persp;

                return scene_depth - surface_depth;
            }

            inline float SineWave(float3 pos, float offset)
            {
                return sin(
                    offset + _Time.z * _WaveSpeed + (pos.x * sin(offset + _WaveDirection * PI) + pos.z *
                        cos(offset + _WaveDirection * PI)) * _WaveFrequency);
            }

            inline float WaveHeight(float2 texcoord, float3 positionOS)
            {
                float s = 0;

                #if !defined(_WAVEMODE_NONE)
                    float2 noise_uv = texcoord * _WaveFrequency;
                    float noise01 = GradientNoise(noise_uv, 1.0);
                    float noise = (noise01 * 2.0 - 1.0) * _WaveNoise;

                    s = SineWave(positionOS, noise);

                #if defined(_WAVEMODE_GRID)
                        s *= SineWave(positionOS, HALF_PI + noise);
                #endif

                #if defined(_WAVEMODE_POINTY)
                        s = 1.0 - abs(s);
                #endif
                #endif

                return s;
            }

            /*
            void Unity_NormalFromHeight_World(float In, out float3 Out)
            {
                float3 worldDerivativeX = ddx(Position * 100);
                float3 worldDerivativeY = ddy(Position * 100);
                float3 crossX = cross(TangentMatrix[2].xyz, worldDerivativeX);
                float3 crossY = cross(TangentMatrix[2].xyz, worldDerivativeY);
                float3 d = abs(dot(crossY, worldDerivativeX));
                float3 inToNormal = ((((In + ddx(In)) - In) * crossY) + (((In + ddy(In)) - In) * crossX)) * sign(d);
                inToNormal.y *= -1.0;
                Out = normalize((d * TangentMatrix[2].xyz) - inToNormal);
            }
            */

            VertexOutput vert(VertexInput i)
            {
                VertexOutput o = (VertexOutput)0;

                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Vertex animation.
                const float3 original_pos_ws = TransformObjectToWorld(i.positionOS.xyz);
                const float s = WaveHeight(i.texcoord, original_pos_ws);
                o.positionWS = original_pos_ws;
                o.positionWS.y += s * _WaveAmplitude;
                o.positionCS = TransformWorldToHClip(o.positionWS);
                const float4 screenPosition = ComputeScreenPos(o.positionCS);
                o.screenPosition = screenPosition;
                o.waveHeight = s;

                o.uv = i.texcoord;

                {
                    // Normals.
                    const float3 viewDirWS = GetCameraPositionWS() - o.positionWS;
                    o.viewDir = viewDirWS;

                    const VertexNormalInputs normalInput = GetVertexNormalInputs(i.normalOS, i.tangentOS);

                    const float sample_distance = 0.01;

                    float3 pos_tangent = original_pos_ws + normalInput.tangentWS * sample_distance;
                    pos_tangent.y += WaveHeight(i.texcoord, pos_tangent) * _WaveAmplitude;

                    float3 pos_bitangent = original_pos_ws + normalInput.bitangentWS * sample_distance;
                    pos_bitangent.y += WaveHeight(i.texcoord, pos_bitangent) * _WaveAmplitude;

                    const float3 modified_tangent = pos_tangent - o.positionWS;
                    const float3 modified_bitangent = pos_bitangent - o.positionWS;
                    const float3 modified_normal = cross(modified_tangent, modified_bitangent);

                    o.normal = normalize(modified_normal);
                }

                half fogFactor = ComputeFogFactor(o.positionCS.z);
                o.fogFactor = fogFactor;

                return o;
            }

            half3 hint(half3 color1, half3 color2)
            {
                float Y = 0.299 * color1.r + 0.587 * color1.g + 0.114 * color1.b;
                float U = -0.169 * color2.r - 0.331 * color2.g + 0.5 * color2.b + 0.5;
                float V = 0.5 * color2.r - 0.419 * color2.g - 0.081 * color2.b + 0.5;
                half3 ret;
                ret.r = Y + 1.13983 * (V - 0.5);
                ret.g = Y - 0.39465 * (U - 0.5) - 0.58060 * (V - 0.5);
                ret.b = Y + 2.03211 * (U - 0.5);
                return ret;
            }

            half4 frag(VertexOutput i) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);


                // Refraction.
                const float2 noise_uv_refraction = i.uv * _RefractionFrequency + _Time.zz * _RefractionSpeed;
                const float noise01_refraction = GradientNoise(noise_uv_refraction, _RefractionScale);
                const float2 screen_uv = i.screenPosition.xy / i.screenPosition.w;
                //const float depth_fade_original = DepthFade(screen_uv, i);
                const float water_depth_original = GetWaterDepth(screen_uv, i);
                const float depth_fade_original = saturate((water_depth_original - _FadeDistance) / _WaterDepth);
                float2 displaced_uv = screen_uv + noise01_refraction * _RefractionAmplitude /* * depth_fade_original*/ * 
                    float2(sin(_RefractionDirection * PI), cos(_RefractionDirection * PI));
                //float depth_fade = DepthFade(displaced_uv, i);
                float water_depth = GetWaterDepth(displaced_uv, i);
                float depth_fade = saturate((water_depth - _FadeDistance) / _WaterDepth);


                if (water_depth <= 0.0f) // If above water surface.
                {
                    displaced_uv = screen_uv;
                    //depth_fade = DepthFade(displaced_uv, i);
                    depth_fade = depth_fade_original;
                    water_depth = water_depth_original;
                }
                //depth_fade = lerp(depth_fade_original, depth_fade, _Alpha);
                const half3 scene_color = SampleSceneColor(displaced_uv);
                half3 c = scene_color;

                // Water depth.
                half4 depth_color;
                half4 color_shallow;
                #if defined(_COLORMODE_LINEAR)
                depth_color = lerp(_ColorShallow, _ColorDeep, depth_fade);
                color_shallow = _ColorShallow;
                #endif

                #if defined(_COLORMODE_GRADIENT_TEXTURE)
                float2 gradient_uv = float2(depth_fade, 0.5f);
                depth_color = SAMPLE_TEXTURE2D(_ColorGradient, sampler_ColorGradient, gradient_uv);
                color_shallow = SAMPLE_TEXTURE2D(_ColorGradient, sampler_ColorGradient, float2(0.0f, 0.5f));
                #endif

                #if defined(_WATERBLENDMODE_LUMA)
                depth_color.rgb = hint(c, depth_color);
                #endif
                #if defined(_WATERBLENDMODE_MULTIPLICATIVE)
                depth_color.rgb *= c;
                #endif
                c = lerp(c, depth_color.rgb, depth_color.a * saturate(water_depth / _StartFade));

                #if !defined(_FOAMMODE_NONE)
                // Foam.
                float foam_shore = saturate(abs(_ShoreFoamDepth / (water_depth_original - 0.5 * _ShoreFoamDepth)));

                // test foam tex
                //#if defined(_FOAMMODE_TEXTURE)
                //    float2 noise_uv_foam;
                //    float2 tmp_uv = screen_uv * 2 - 1;
                //    noise_uv_foam.x = atan2(tmp_uv.x, tmp_uv.y) / TWO_PI + 0.5;
                //    noise_uv_foam.y = length(tmp_uv) * _FoamScale + _Time.z * _FoamSpeed;
                //    float noise_foam_base = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noise_uv_foam).r;
                //    return half4(noise_foam_base, 0, 0, 1.0);
                //#endif

                // Foam around shore
                const int _FoamFilterHalfSize = 1;
                const int _FoamFilterSquareSize = 9;
                const float _FoamFilterStride = 4.0;
                const uint shorelineData = asuint(tex2D(_ShorelineBuffer, screen_uv).r);
                float shoreline = float(shorelineData >> 24) / 255.0;

                // Debug uv rotation
                //float uv_angle = atan2(i.uv.y, i.uv.x);
                //float cs = cos(uv_angle + _FoamDirection * PI);
                //float sn = sin(uv_angle + _FoamDirection * PI);
                //float2 rotated_uv = float2(i.uv.x * cs - i.uv.y * sn, i.uv.x * sn + i.uv.y * cs);
                //float2 noise_uv_foam = rotated_uv * 100.0f + _Time.zz * _FoamSpeed;
                //    float noise_foam_base;
                //    #if defined(_FOAMMODE_TEXTURE)
                //        float2 stretch_factor = float2(_FoamStretchX, _FoamStretchY);
                //        noise_foam_base = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap,
                //            noise_uv_foam * stretch_factor / (_FoamScale * 100.0)).r;
                //    #endif

                //    #if defined(_FOAMMODE_GRADIENT_NOISE)
                //        float2 stretch_factor = float2(_FoamStretchX, _FoamStretchY);
                //        noise_foam_base = GradientNoise(noise_uv_foam * stretch_factor, _FoamScale);
                //    #endif
                //        return half4(noise_foam_base, noise_foam_base, noise_foam_base, 1);

                if (/*shoreline != 0 && */ water_depth_original > 0)
                {
                    //float filtered = 0.0, step_size_x = _FoamFilterStride / _ScreenParams.x, step_size_y = _FoamFilterStride / _ScreenParams.y;
                    //for (int dx = -_FoamFilterHalfSize; dx <= _FoamFilterHalfSize; ++dx)
                    //{
                    //    for (int dy = -_FoamFilterHalfSize; dy <= _FoamFilterHalfSize; ++dy)
                    //    {
                    //        if (tex2D(_ShorelineBuffer, screen_uv + float2(dx * step_size_x, dy * step_size_y)).r != 0)
                    //            filtered += 1.0;
                    //    }
                    //}
                    //filtered /= _FoamFilterSquareSize;
                    //filtered *= filtered;

                    //float shorelineAngle, shorelineDist;
                    //unpack_two_floats(shoreline, shorelineAngle, shorelineDist);

                    //float uv_angle = atan2(i.uv.y, i.uv.x);
                    //float cs = cos(uv_angle + shorelineAngle);
                    //float sn = sin(uv_angle + shorelineAngle);
                    //float2 rotated_uv = float2(i.uv.x * cs - i.uv.y * sn, i.uv.x * sn + i.uv.y * cs);
                    //float2 noise_uv_foam = rotated_uv * 100.0f + _Time.zz * _FoamSpeed;


                    //float shorelineAngle, shorelineDist;
                    //shorelineAngle = float((shorelineData >> 16) & 0x000000ffu) / 255.0 * TWO_PI - PI;
                    //shorelineDist = f16tof32(shorelineData & 0x0000ffffu);
                    //float2 noise_uv_foam;
                    //noise_uv_foam.x = shorelineAngle / TWO_PI + 0.5;
                    //float3 dir = float3(cos(shorelineAngle), 0, sin(shorelineAngle));
                    //float3 WO = i.positionWS.xyz - dir * shorelineDist; // retrieve object's origin
                    //float4 CO = TransformWorldToHClip(WO);
                    //float4 UVO = ComputeScreenPos(CO);
                    //UVO.xy /= UVO.w;
                    //return half4(length(UVO.xy) * shoreline, length(UVO.xy) * shoreline, length(UVO.xy) * shoreline, 1);
                    //noise_uv_foam.y = length(UVO.xy); noise_uv_foam.y += _Time.z * _FoamSpeed;

                    float noise_foam_base = 0.0;
                    #if defined(_FOAMMODE_TEXTURE)
                        float2 stretch_factor = float2(_FoamStretchX, _FoamStretchY);
                        //#if defined(_FOAMSAMPLEMODE_MOVE)
                        //    float2 noise_uv_foam = i.uv * 100.0f + _Time.zz * _FoamSpeed; 
                        //    noise_uv_foam *= stretch_factor / (_FoamScale * 100.0);
                        //    noise_foam_base = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noise_uv_foam).r;
                        //#elif defined(_FOAMSAMPLEMODE_STACK)
                            float t = smoothstep(0.0, 1, abs(frac(_Time.z * _FoamSpeed)));
                            float2 noise_uv_foam = i.uv * 100.0f;
                            noise_uv_foam *= stretch_factor / (_FoamScale * 100.0);
                            float base1 = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noise_uv_foam + int(_Time.z * _FoamSpeed) * 0.1).r;
                            float base2 = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noise_uv_foam + int(_Time.z * _FoamSpeed) * 0.1 + 0.1).r;
                            noise_foam_base = lerp(base1, base2, t);
                            //noise_foam_base = lerp(base1, base2, abs(sin(_Time.z * _FoamSpeed)));
                        //#endif
                    #endif

                    #if defined(_FOAMMODE_GRADIENT_NOISE)
                        float2 noise_uv_foam = i.uv * 100.0f + _Time.zz * _FoamSpeed;
                        float2 stretch_factor = float2(_FoamStretchX, _FoamStretchY);
                        noise_foam_base = GradientNoise(noise_uv_foam * stretch_factor, _FoamScale);
                    #endif

                    float foam_blur = 1.0 - _FoamSharpness;
                    float hard_foam_end = 0.1;
                    float soft_foam_end = hard_foam_end + foam_blur * 0.3;
                    foam_shore += smoothstep(0.5 - foam_blur * 0.5, 0.5 + foam_blur * 0.5, noise_foam_base) * shoreline;

                }

                c = lerp(c, _FoamColor.rgb, saturate(foam_shore) * _FoamColor.a);
                #endif

                // Shadow.
                #if defined(_MAIN_LIGHT_SHADOWS)
                    VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                    vertexInput.positionWS = i.positionWS.xyz;
                    float4 shadowCoord = GetShadowCoord(vertexInput);
                    half shadowAttenutation = MainLightRealtimeShadow(shadowCoord);
                    c = lerp(c, c * _ShadowColor, _ShadowStrength * (1.0h - shadowAttenutation));
                #endif

                c *= lerp(half3(1, 1, 1), _MainLightColor.rgb, _LightContribution);

                c = MixFog(c, i.fogFactor);
                
                return half4(c, _Alpha);
            }
            ENDHLSL
        }



        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "MyWaterEditor"
}