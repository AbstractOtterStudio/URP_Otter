// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)


Shader "Hidden/Color-Fade"
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
    sampler2D _GUIClipTexture;
    uniform bool _ManualTex2SRGB; 
    uniform float4 _MainTex_ST;
    uniform fixed4 _Color;
    uniform float4x4 unity_GUIClipTextureMatrix;

    uniform float _EditorTime;
    uniform float _Alpha;
    
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
 
    static float4 color1      = float4(0.494, 0.737, 0.752, 1.0);
    static float4 color2      = float4(0.686, 0.301, 0.517, 1.0);
    static float4 color3      = float4(0.133, 0.447, 0.788, 1.0);
    static float4 color4      = float4(0, 0.694, 0.596, 1.0); 
     
    
    
    float4 frag (v2f i) : SV_Target
    {  
        float time = _EditorTime % 4;
        float4 color = 1;
        //Really dumb and quick code. Apologies. Don't do this at home kids.
        if(time<1)
        {
            color = lerp(color1, color2, _EditorTime - floor(_EditorTime));
        }else if(time <2)
        {
            color = lerp(color2, color3, _EditorTime - floor(_EditorTime));
        }else if(time <3)
        {
             color = lerp(color3, color4, _EditorTime - floor(_EditorTime));
        }else if(time <4)
        {
             color = lerp(color4, color1, _EditorTime - floor(_EditorTime));
        }
        color.a = _Alpha;
        return color;
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
