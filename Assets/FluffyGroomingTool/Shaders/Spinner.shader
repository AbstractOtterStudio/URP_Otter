Shader "Hidden/Fur-spinner"
{
    Properties {
        _MainTex ("Texture", Any) = "white" {}
    }

    CGINCLUDE
    #pragma vertex vert
    #pragma fragment frag
    #pragma target 2.0

    #include "UnityCG.cginc"
    #define PI 3.1415926 

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
    sampler2D _GUIClipTexture;
    uniform bool _ManualTex2SRGB;
    uniform float expand;
    uniform float4 _MainTex_ST;
    uniform fixed4 _Color;
    uniform float4x4 unity_GUIClipTextureMatrix;
    uniform float _EditorTime;
    
    v2f vert (appdata_t v)
    {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        o.vertex = UnityObjectToClipPos(v.vertex);
        float3 eyePos = UnityObjectToViewPos(v.vertex);
        o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
        o.color = v.color;
        o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
        return o;
    }
 
    static float easeQuadInOut (float t) {
	    t /= 1.0/2.0;
	    if (t < 1.0) return 1.0/2.0*t*t*t*t ;
	    t -= 2.0;
	    return -1.0 / 2.0 * (t * t * t * t - 2.0) ;
    };

    
    float4 getTwirl(float4 color,float i,float2 uv)
    {
        float endCap = easeQuadInOut(saturate(distance(uv.x - 1.4, 0) / 1.5)) * easeQuadInOut(saturate( distance(uv.x + 1.4, 0) / 1.5));
        uv.y += sin((float) i * 200 +  _EditorTime * 2 + uv.x  * 1.5) * endCap * 0.9 * expand;
        color.a= saturate(abs(0.06 / uv.y));
        if(color.a < 0.5) {
            color.a = 0;
        }
        return color;
    }

   
    fixed4 frag (v2f i) : SV_Target
    { 
        const float2 uv = 2 * i.texcoord.xy - 1;
        const float4 pink = getTwirl(float4(207.0/255.0, 115.0/255.0, 229.0/255.0, 1.0),0, uv);
        const float4 orange = getTwirl(float4(1.0, 133.0/255.0, 45.0/255.0, 1.0),1.0, uv);
        const float4 green = getTwirl( float4(153.0/255.0, 220.0/255.0, 81.0/255.0,1.0),2, uv);
        const float4 cyan = getTwirl(float4(90.0/255.0, 211.0/255.0, 172.0/255.0, 1.0),3, uv);
        const float4 blue = getTwirl( float4(55.0/255.0, 210.0/255.0, 232.0/255.0, 1.0),4, uv);
        float4 col =  lerp(orange, pink, pink.a); 
        col = lerp(col, green, green.a); 
        col = lerp(col, cyan, cyan.a);
        col = lerp(col, blue, blue.a);
 
        return col;
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
