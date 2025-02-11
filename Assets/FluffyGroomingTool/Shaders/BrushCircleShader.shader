Shader "Hidden/BrushCircleShader"
{

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True"
        }
        ZTest Always
        
        Blend SrcAlpha OneMinusSrcAlpha
 
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 circle(float2 uv, float2 pos, float rad, float3 color)
            {
                float d = length(pos - uv) - rad;
                float t = clamp(d, 0.0, 1.0);
                return float4(color, 1.0 - saturate(t * 1000));
            }

            uniform float circleObjectScale;
            uniform float falloffScale;
            uniform float fillOpacity;
            static float3 diskColor = float3(1.0 / 255.0, 1.0 / 255.0, 5.0 / 255.0);
            
            fixed4 frag(v2f i) : SV_Target
            {
                float circleWidth = 0.005 / circleObjectScale;
                float2 center = float2(0.5, 0.5);
                float4 finalColor = circle(i.uv, center, 0.5, diskColor);
                finalColor.a -= circle(i.uv, center, 0.5 - circleWidth, float3(1, 1, 1)).a;

                float4 circle2 = circle(i.uv, center, falloffScale, diskColor);
                circle2.a -= circle(i.uv, center, falloffScale - circleWidth, float3(1, 1, 1)).a;
                finalColor.a += circle2.a;

                finalColor.rgb = finalColor + float3(finalColor.a * 2, finalColor.a * 2, finalColor.a * 2);

                float gradient1 = length(center - i.uv) - falloffScale;
                float t = clamp(gradient1, 0.0, 1.0);
                finalColor.a += (1.0 - saturate(t * 4)) * fillOpacity;
                return finalColor;
            }
            ENDCG
        }
    }
}