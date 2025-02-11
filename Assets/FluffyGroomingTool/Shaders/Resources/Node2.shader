Shader "Node2"
{

	Properties
	{
	    _Size ("Size", Range(0.0, 1.0)) = 0.25
	    _Color ("Color", Color) = (1, 0, 0, 1)
	}

  CGINCLUDE

  #include "UnityCG.cginc"
  #include "FurMotionInclude.cginc"

  struct appdata
  {
    float4 vertex : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
  };

  struct v2f
  {
    float4 position : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
  };

StructuredBuffer<VerletNode> verletNodes;
StructuredBuffer<VerletRestNode> _RestPositions;

  float _Size;
  float4 _Color;

 

  v2f vert(appdata IN, uint iid : SV_InstanceID)
  {
    v2f OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

    VerletNode n = verletNodes[iid];
    float3 position = n.position.xyz + IN.vertex.xyz * 0.02 * _Size;
    float4 vertex = float4(position, 1);
    OUT.position = UnityObjectToClipPos(vertex);
    return OUT;
  }

  fixed4 frag(v2f IN) : SV_Target
  {
    return _Color;
  }

  ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM

      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing
 

			ENDCG
		}
	}
}