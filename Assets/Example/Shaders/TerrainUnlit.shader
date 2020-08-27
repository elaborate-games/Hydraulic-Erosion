Shader "Custom/TerrainUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HeightScale("Height Scale", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
		float _HeightScale;
		sampler2D _heightMap;
		float4 _heightMap_TexelSize;

		sampler2D _NormalMap;

            
            v2f vert (appdata_full v)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			    float height = tex2Dlod(_heightMap, float4(o.uv, 1,1));
			    v.vertex.xyz += v.normal * height * _HeightScale;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
			    float4 n = tex2D(_NormalMap, i.uv).xyzw;
                // return n;
                n.xyz = UnpackNormal(n);
                return 1-abs(n.y);
                return float4(n);
                n = n * 2 - 1;
                return float4(n);

                float3 up = float3(0,1,0);
                float slope = dot(n, up);
                // return slope;
                // return float4(n,1);
                // n = UnpackNormal(float4(n,1));
                return pow(1 - slope, 10);
                // return float4(n,1);
                // return pow(1-n.y,1);
                return float4(n);
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}