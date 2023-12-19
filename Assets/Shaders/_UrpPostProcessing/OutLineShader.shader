// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Outline"
{
    Properties
	{
		_OutColor("Color",Color) = (0,0,1,1)
		_MainTex("Texture", 2D) = "white" {}
		_Width("width",Range(0,0.1)) = 0.1
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 100
			Pass
            {
                Cull Front
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_fog
                #include "UnityCG.cginc"

                struct a2v
                {
                    float4 vertex : POSITION;
                    float3 normal: NORMAL;
                };

                struct v2f
                {
                    float4 pos : SV_POSITION;
                };

                fixed4 _OutColor;
                float _Width;

                v2f vert(a2v v)
                {
                    v2f o;
                    v.vertex.xyz += v.normal * _Width;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    return _OutColor;
                }
                ENDCG
            }
        }
}
