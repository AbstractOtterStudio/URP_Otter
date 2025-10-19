// Made with Amplify Shader Editor v1.9.3.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Polaris/BuiltinRP/Foliage/GrassLOD1"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("MainTex", 2D) = "white" {}
		_FadeMinDistance("Fade Min Distance", Float) = 50
		_FadeMaxDistance("Fade Max Distance", Float) = 100
		_Occlusion("Occlusion", Range( 0 , 1)) = 0.2
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" "DisableBatching" = "True" }
		Cull Off
		CGPROGRAM
		#pragma target 2.0
		#pragma multi_compile_instancing
		#pragma instancing_options nolodfade nolightmap
		#pragma surface surf Lambert keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform half _FadeMaxDistance;
		uniform half _FadeMinDistance;
		uniform float4 _Color;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float _Occlusion;
		uniform float _Cutoff = 0;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_vertex3Pos = v.vertex.xyz;
			half3 objToView2_g31 = mul( UNITY_MATRIX_MV, float4( ase_vertex3Pos, 1 ) ).xyz;
			half _FadeMaxDistance67_g30 = _FadeMaxDistance;
			half temp_output_1_0_g32 = _FadeMaxDistance67_g30;
			half _FadeMinDistance65_g30 = _FadeMinDistance;
			half clampResult7_g31 = clamp( ( ( -objToView2_g31.z - temp_output_1_0_g32 ) / ( _FadeMinDistance65_g30 - temp_output_1_0_g32 ) ) , 0.0 , 1.0 );
			float3 vertexPosition48_g30 = ( clampResult7_g31 * ase_vertex3Pos );
			v.vertex.xyz = vertexPosition48_g30;
			v.vertex.w = 1;
			float3 vertexNormal49_g30 = float3(0,1,0);
			v.normal = vertexNormal49_g30;
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float4 _Color22_g30 = _Color;
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			half4 temp_output_37_0_g30 = ( _Color22_g30 * tex2D( _MainTex, uv_MainTex ) );
			float _Occlusion70_g30 = _Occlusion;
			half lerpResult75_g30 = lerp( 0.0 , _Occlusion70_g30 , ( ( 1.0 - i.uv_texcoord.y ) * ( 1.0 - i.uv_texcoord.y ) ));
			float4 albedoColor50_g30 = ( temp_output_37_0_g30 - half4( ( 0.5 * float3(1,1,1) * lerpResult75_g30 ) , 0.0 ) );
			o.Albedo = albedoColor50_g30.rgb;
			o.Alpha = 1;
			float alpha47_g30 = temp_output_37_0_g30.a;
			clip( alpha47_g30 - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
}
/*ASEBEGIN
Version=19303
Node;AmplifyShaderEditor.FunctionNode;106;-416,112;Inherit;False;GrassSimpleBaseGraph;1;;30;6d97dccf2f6586c4bac184f19bc52c70;0;0;4;COLOR;0;FLOAT;54;FLOAT3;56;FLOAT3;58
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;76;0,0;Half;False;True;-1;0;;0;0;Lambert;Polaris/BuiltinRP/Foliage/GrassLOD1;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;False;True;False;False;False;True;Off;0;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0;True;True;0;True;Opaque;;AlphaTest;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;True;True;Absolute;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;76;0;106;0
WireConnection;76;10;106;54
WireConnection;76;11;106;56
WireConnection;76;12;106;58
ASEEND*/
//CHKSM=E8718D6180E673BCE044C39C7147D78B6578E029