﻿Shader "Custom/Terrain"
{
	Properties
	{
		_HeightScale ("Height Scale", float) = 5
		_GrassColour ("Grass Colour", Color) = (0,1,0,1)
		_RockColour ("Rock Colour", Color) = (1,1,1,1)
		_GrassSlopeThreshold ("Grass Slope Threshold", Range(0,1)) = .5
		_GrassBlendAmount ("Grass Blend Amount", Range(0,1)) = .5
	}
	SubShader
	{
		Tags
		{
			"RenderType"="Opaque"
		}
		CGPROGRAM
#pragma surface surf Standard fullforwardshadows vertex:vert
		#pragma target 5.0

		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			float2 texcoord;
			INTERNAL_DATA
		};

		half _MaxHeight;
		half _GrassSlopeThreshold;
		half _GrassBlendAmount;
		fixed4 _GrassColour;
		fixed4 _RockColour;

		float _HeightScale;
		sampler2D _heightMap;
		float4 _heightMap_TexelSize;

		sampler2D _NormalMap;

		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);

			o.texcoord = v.texcoord;

			float height = tex2Dlod(_heightMap, v.texcoord);
			v.vertex.xyz += v.normal * height * _HeightScale;
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = 1;
			o.Metallic = 0;
			o.Smoothness = 0;
			o.Alpha = 1;

			float4 n = tex2D(_NormalMap, IN.texcoord);
			n.xyz = UnpackNormal(n);
			o.Normal = n.xyz;

			float3 worldNormal = mul(unity_ObjectToWorld, float4(n.xyz, 0));;
			float slope = abs(n.y); // slope = 0 when terrain is completely flat

			float grassBlendHeight = _GrassSlopeThreshold * (1 - _GrassBlendAmount);
			float grassWeight = 1 - saturate((slope - grassBlendHeight) / (_GrassSlopeThreshold - grassBlendHeight));
			o.Albedo = _GrassColour * grassWeight + _RockColour * (1 - grassWeight);
			// o.Albedo = o.Normal;
			// o.Normal = IN.worldNormal.xyz;
		}
		ENDCG
	}
}