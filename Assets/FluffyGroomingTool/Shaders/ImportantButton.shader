Shader "Hidden/Important-Button"
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
        float2 texcoord2 : TEXCOORD1;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    sampler2D _MainTex;
    uniform sampler2D _Gradient;
    sampler2D _GUIClipTexture;
    uniform bool _ManualTex2SRGB;
    uniform float expand;
    uniform float4 _MainTex_ST;
 
    uniform fixed4 _Color;
    uniform float4x4 unity_GUIClipTextureMatrix;

    uniform float2 _MousePosition;
    uniform float2 _RectSize;
    uniform float _Offset; 
    uniform float _SpeedDiff;
    uniform float _circleAlpha;
    uniform fixed4 _FillColor;
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
    
    v2f vert (appdata_t v)
    {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        o.vertex = UnityObjectToClipPos(v.vertex); 
        o.color = v.color;
        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
        o.texcoord2 = float2(o.texcoord.x * 0.5 + _Offset,  o.texcoord.y); 
        return o;
    }
    float  circle(float2 uv, float2 pos, float rad) {
	    float d = length(pos - uv) - rad;
	    float t = clamp(d, 0.0, 1.0);
	    return 1.0 - t ;
    }
    fixed4 frag (v2f i) : SV_Target
    {
        float4 mask = colorCorrect(tex2D(_MainTex, i.texcoord));
        float4 color = colorCorrect(tex2D(_Gradient, i.texcoord2));
        float4 textColor = 1;
        
        float4 finalColor = _FillColor;
   
        finalColor = lerp(finalColor,color , mask.g); 
        finalColor = lerp(finalColor, textColor, mask.r);

        float radius = _RectSize.y * 0.7;
        float circleMask = circle(i.texcoord * _RectSize, _MousePosition,radius - radius*0.9 * _SpeedDiff)*_circleAlpha;
        finalColor = lerp(finalColor, color, circleMask * (mask.b - mask.g - mask.r));
        float3 invertedColor = float3(1,1,1) - color.rgb;
        finalColor.rgb = lerp(finalColor.rgb, invertedColor, circleMask  * mask.r);
        finalColor.rgb = lerp(_FillColor, finalColor, mask.b);
        finalColor.a = saturate(mask.b * _FillColor.a + mask.r + mask.g);
    
        return finalColor;
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
