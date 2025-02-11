//VHS license: https://www.shadertoy.com/view/XttfWH
Shader "Hidden/Start-Banner"
{
    Properties {
        _MainTex ("Texture", Any) = "white" {}
    }
 
    CGINCLUDE
    #pragma vertex vert
    #pragma fragment frag
    #pragma target 2.0

    #include "UnityCG.cginc" 

    struct appdata_t {
        float4 vertex : POSITION;
        fixed4 color : COLOR;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2f {
        float4 vertex : SV_POSITION;
        fixed4 color : COLOR;
        float2 texcoord : TEXCOORD0;
        float2 clipUV : TEXCOORD1;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    sampler2D _MainTex;
    sampler2D _MainTex2;
    sampler2D _GUIClipTexture;
    uniform bool _ManualTex2SRGB;
    uniform float expand;
    uniform float4 _MainTex_ST;
    uniform fixed4 _Color;
    uniform float4x4 unity_GUIClipTextureMatrix;

    uniform float _onAmount = 0;
    
    v2f vert (appdata_t v)
    {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        o.vertex = UnityObjectToClipPos(v.vertex);
        float3 eyePos = UnityObjectToViewPos(v.vertex);
        o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
        o.color = v.color;
        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
        return o;
    }


    uniform float2 _TextureSize;
    uniform float2 _RectSize;  
    uniform float iTime;
    static float PI5 = 3.14159265;

    static float wobble_intensity = 0.002;
    static  float grade_intensity = 1;
    static float line_intensity = 1.6;
    static float vignette_intensity = 0.1;

float rand(float2 co){
    return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
}
    
    float4 gammaToLinear(float4 c)
{
    return  pow(c, 0.454545); 
}

float4 colorCorrect(float4 input)
 {
     #ifndef UNITY_COLORSPACE_GAMMA
          return  gammaToLinear(input);
     #else
          return input;
     #endif
 }
    
float sample_noise(float2 fragCoord)
{
    float2 uv = fmod(fragCoord + float2(0, 100. * iTime), _TextureSize);
  
    float val = colorCorrect(tex2D(_MainTex2, uv / _RectSize)).r;
    return pow(val, 7.); //  sharper ramp
}
 
float4 vhs( float2 uv )
{
    float2 fragCoord = uv*_RectSize;
    uv = fragCoord / _RectSize;
    
    //  wobble
    float2 wobbl = float2(wobble_intensity * rand(float2(iTime, fragCoord.y)), 0.);
    
    //  band distortion
    float t_val = tan(0.25 * iTime + uv.y * PI5 * .67);
    float2 tan_off = float2(wobbl.x * min(0., t_val), 0.);

 
    //  chromab
    float4 color1 = colorCorrect(tex2D(_MainTex2, -0.003 * _onAmount + uv + wobbl + tan_off));
    float4 color2 = colorCorrect(tex2D(_MainTex2, (uv + (wobbl * 1.5) + (tan_off * 1.3))));
    //  combine + grade
    float4 color = float4(color2.r,color1.g, pow(color1.b, .67), 1.);
    color.rgb = lerp(colorCorrect(tex2D(_MainTex2, uv + tan_off)).rgb, color.rgb, grade_intensity);
    
    //  scanline sim
    float s_val = ((sin(2. * PI5 * uv.y + iTime * 20.) + sin(2. * PI5 * uv.y)) / 2.) * .015 * sin(iTime);
    color += s_val;
    
    //  noise lines
    float ival = _RectSize.y / 4.;
    float r = rand(float2(iTime, fragCoord.y));
    //  dirty hack to avoid conditional
    float on = floor(float(int(fragCoord.y + (iTime * r * 1000.)) % int(ival + line_intensity)) / ival);
    float wh = sample_noise(fragCoord) * on*0.2;
    color = float4(min(1., color.r + wh), 
                 min(1., color.g + wh),
                 min(1., color.b + wh), 1.);
    
    float vig = 1. - sin(PI5 * uv.x) * sin(PI5 * uv.y);
    
    return  color - (vig * vignette_intensity) ; 
}



    
    fixed4 frag (v2f i) : SV_Target
    {
        float4 offColor = colorCorrect(tex2D(_MainTex, i.texcoord) );  
        return lerp(offColor, vhs(i.texcoord), _onAmount);
    }
    ENDCG

    SubShader {
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha, One One
        Cull Off
        ZWrite Off
        ZTest Always

        Pass {
            CGPROGRAM
            ENDCG
        }
    }

    SubShader {
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass {
            CGPROGRAM
            ENDCG
        }
    }
}
