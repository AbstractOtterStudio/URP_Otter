Shader "Unlit/WizardOverlay"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		CGINCLUDE
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
	ENDCG
		SubShader
	{

		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Name "Water"
			CGPROGRAM


			fixed4 frag(v2f i) : SV_Target
			{
				return fixed4(0,0,0,0);
			}
		ENDCG
	}
	}
}
