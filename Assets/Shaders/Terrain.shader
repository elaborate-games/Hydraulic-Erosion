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

			float height = tex2Dlod(_heightMap, v.texcoord);
			v.vertex.xyz += v.normal * height * _HeightScale;
			o.texcoord = v.texcoord;
		}


		  float3 HeightToNormal(float height, float3 normal, float3 pos)
        {
            float3 worldDirivativeX = ddx(pos);
            float3 worldDirivativeY = ddy(pos);
            float3 crossX = cross(normal, worldDirivativeX);
            float3 crossY = cross(normal, worldDirivativeY);
            float3 d = abs(dot(crossY, worldDirivativeX));
            float3 inToNormal = ((((height + ddx(height)) - height) * crossY) + (((height + ddy(height)) - height) * crossX)) * sign(d);
            inToNormal.y *= -1.0;
            return normalize((d * normal) - inToNormal);
        }
 
        float3 WorldToTangentNormalVector(Input IN, float3 normal) {
            float3 t2w0 = WorldNormalVector(IN, float3(1,0,0));
            float3 t2w1 = WorldNormalVector(IN, float3(0,1,0));
            float3 t2w2 = WorldNormalVector(IN, float3(0,0,1));
            float3x3 t2w = float3x3(t2w0, t2w1, t2w2);
            return normalize(mul(t2w, normal));
        }
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// IN.worldNormal = WorldNormalVector(IN, float3(0,0,1));
			     // half h = tex2D(_heightMap, IN.texcoord).r * _HeightScale;
        //     IN.worldNormal = WorldNormalVector(IN, float3(0,0,1));
        //     float3 worldNormal = HeightToNormal(h, IN.worldNormal, IN.worldPos);
        //
        //     o.Normal = worldNormal;// WorldToTangentNormalVector(IN, worldNormal);

			o.Normal = UnpackNormal(tex2D(_NormalMap, IN.texcoord));// filterNormal(float4(IN.texcoord,0 ,0), _heightMap_TexelSize.xy);
			float slope = 1 - o.Normal.y; // slope = 0 when terrain is completely flat
			float grassBlendHeight = _GrassSlopeThreshold * (1 - _GrassBlendAmount);
			float grassWeight = 1 - saturate((slope - grassBlendHeight) / (_GrassSlopeThreshold - grassBlendHeight));
			o.Albedo = _GrassColour * grassWeight + _RockColour * (1 - grassWeight);
			// o.Albedo = o.Normal;
			// o.Normal = IN.worldNormal.xyz;


			
            o.Metallic = 0;
            o.Smoothness = 0;
            o.Alpha = 1;
		}
		ENDCG
	}
}