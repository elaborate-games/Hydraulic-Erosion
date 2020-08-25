Shader "Custom/Terrain"
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


		// as in https://catlikecoding.com/unity/tutorials/rendering/part-6/
		float3 calculate_normal(float2 uv)
		{
			float texel = 1/1024.0;
			
			float2 du = float2(texel, 0);
			float u1 = tex2D(_heightMap, uv - du);
			float u2 = tex2D(_heightMap, uv + du);
			// float3 tu = float3(1, u2 - u1, 0);

			float2 dv = float2(0, texel);
			float v1 = tex2D(_heightMap, uv - dv);
			float v2 = tex2D(_heightMap, uv + dv);
			// float3 tv = float3(0, v2 - v1, 1);

			float3 normal = float3(u1 - u2, 1, v1 - v2);
			return normalize(normal);
		}


		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = 0;
			o.Metallic = 0;
			o.Smoothness = 0;
			o.Alpha = 1;

			float h = tex2D(_heightMap, IN.texcoord);
			// o.Emission = h;
			// return;

			o.Normal = calculate_normal(IN.texcoord);
			o.Emission = o.Normal;
			return;

			// float4 n = tex2D(_NormalMap, IN.texcoord);
			// o.Normal = UnpackNormal(n);// filterNormal(float4(IN.texcoord,0 ,0), _heightMap_TexelSize.xy);
			float slope = o.Normal.y; // slope = 0 when terrain is completely flat
			// o.Emission = slope;
			// return;

			float grassBlendHeight = _GrassSlopeThreshold * (1 - _GrassBlendAmount);
			float grassWeight = 1 - saturate((slope - grassBlendHeight) / (_GrassSlopeThreshold - grassBlendHeight));
			o.Emission = _GrassColour * grassWeight + _RockColour * (1 - grassWeight);
			// o.Albedo = o.Normal;
			// o.Normal = IN.worldNormal.xyz;
		}
		ENDCG
	}
}