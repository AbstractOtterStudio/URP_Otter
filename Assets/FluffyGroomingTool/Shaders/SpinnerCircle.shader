// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)


Shader "Hidden/Fur-spinner-circle"
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

    const float Pi = 3.1415; 
    const float Width = 0.001;
    static  float4  Color_BackGround =float4(0, 0, 0, 0);
    static float4 Color_Ring1      = float4(207.0/255.0, 115.0/255.0, 229.0/255.0, 1.0);
    static float4 Color_Ring2      = float4(1.0, 133.0/255.0, 45.0/255.0, 1.0);
    static float4 Color_Ring3      = float4(153.0/255.0, 220.0/255.0, 81.0/255.0,1.0);
    static float4 Color_Ring4      = float4(90.0/255.0, 211.0/255.0, 172.0/255.0, 1.0);
    static float4 Color_Ring5      = float4(55.0/255.0, 210.0/255.0, 232.0/255.0, 1.0);
     
     
    float4 addRing(float4 Color, float4 ringColor,float2 Scaled, float t, float ringSize,float time){
     
        const float Tau = 2.0*Pi;
        float Length = length(Scaled);
        float Angle  = atan2(Scaled.y,Scaled.x)+Pi; 
        float Wave = fmod(time,Tau); 
        
        float AngleDifference = abs(Wave-Angle);
        float DistanceToWave  = min(AngleDifference,Tau-AngleDifference);
        float FinalMultiplier = pow(max(1.,DistanceToWave),-1.);
        float Ring1 = ringSize  + 0.03*cos(Angle*2.0 +time)*FinalMultiplier;
        Color = lerp(Color,ringColor,smoothstep(Width+t,Width,abs(Ring1-Length)));
        return Color;
    }

    
    float4 frag (v2f i) : SV_Target
    {  
        float2 Scaled =  i.texcoord.xy + float2(-0.5, -0.5);
        float t = 0.007;
 
	    float4 Color = Color_BackGround;   
    
        Color = addRing(Color, Color_Ring1, Scaled, t, .40,_EditorTime * 2.25); 
        Color = addRing(Color, Color_Ring2, Scaled, t, .395,_EditorTime * 2.5);
        Color = addRing(Color, Color_Ring3, Scaled, t, .39,_EditorTime * 2.75);
        Color = addRing(Color, Color_Ring4, Scaled, t, .385,_EditorTime * 3.0);
        Color = addRing(Color, Color_Ring5, Scaled, t, .38,_EditorTime * 3.25);
  
        return Color;
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
