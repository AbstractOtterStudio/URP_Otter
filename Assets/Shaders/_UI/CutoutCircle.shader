Shader "UI/CutoutCircle" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _InnerRadius ("Inner Radius", Range(0,1)) = 0.2
        _OuterRadius ("Outer Radius", Range(0,1)) = 0.5
        _ArcAngle ("Arc Angle", Range(0, 180)) = 45 // the angle subtended by the arc
    }
    SubShader {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100

        Pass {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _InnerRadius;
            float _OuterRadius;
            float _ArcAngle;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 localPos : TEXCOORD1;
            };

            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.localPos = v.vertex.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // polar coordinates
                float2 pos = i.vertex.xy;
                float angle = atan2(pos.y, pos.x); // [-pi, pi]
                float dist = length(pos);
                if (dist < _InnerRadius || dist > _OuterRadius) discard;
                float arcAngleRad = _ArcAngle * UNITY_PI / 180.0;
                if (abs(angle) > arcAngleRad / 2.0) discard;

                float u = (angle + UNITY_PI) / (2.0 * UNITY_PI); // [0,1]
                return _Color;
            }
            ENDCG
        }
    }
}
