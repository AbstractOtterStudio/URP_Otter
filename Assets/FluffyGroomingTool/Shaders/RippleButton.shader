Shader "Hidden/Ripple-Button"
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
    uniform float _circleAlpha;
    uniform float _hoverAlpha;
    uniform float _RippleExpand;
    uniform float2 _TextureSize;
    uniform float _GlobalAlpha;
        
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
    float2 getUV(float2 originalUV)
    {
        float2 textSize = _TextureSize; 
        float2 imageSize = _RectSize;
	    float2 uv = originalUV;
        float2 ratio = textSize.xy / imageSize;
        float2 border = textSize / 2.0 * (0.5 + 0.7 * 0.5); 
        float2 bDst = border / imageSize;

        if(uv.x < bDst.x)
            uv.x = uv.x / ratio.x;
        
        else if(uv.x > (1.0 - bDst.x))
            uv.x = 1.0 - (1.0 - uv.x) / ratio.x;
            
        else{
            float t = (border.x * (textSize.x - imageSize.x)) / (textSize.x * (2.0 * border.x - imageSize.x));
            uv.x = uv.x * (1.0 - t * 2.0) + t;
        }

        if(uv.y < bDst.y)
            uv.y = uv.y / ratio.y;
        
        else if(uv.y > (1.0 - bDst.y))
            uv.y = 1.0 - (1.0 - uv.y) / ratio.y;

        else {
            float t = (border.y * (textSize.y - imageSize.y)) / (textSize.y * (2.0 * border.y - imageSize.y));
            uv.y = uv.y * (1.0 - t * 2.0) + t;
        }
        
        return uv;
    }
    
    v2f vert (appdata_t v)
    {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        o.vertex = UnityObjectToClipPos(v.vertex); 
        o.color = v.color;
        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex); 
        return o;
    }
    
    float  circle(float2 uv, float2 pos, float rad) {
	    float d = length(pos - uv) - rad;
	    float t = clamp(d, 0.0, 1.0);
	    return 1.0 - t ;
    }

    fixed4 frag (v2f i) : SV_Target
    {
        float4 mask = colorCorrect(tex2D(_MainTex, getUV(i.texcoord)));
        float4 baseColor = mask.g;
        float4 finalColor = baseColor;
        finalColor += _hoverAlpha * mask.r;
        float circleMask = circle(i.texcoord * _RectSize, _MousePosition,_RectSize.x * _RippleExpand) * _circleAlpha;
        finalColor += circleMask * mask.r;
        finalColor.a = _GlobalAlpha;
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
