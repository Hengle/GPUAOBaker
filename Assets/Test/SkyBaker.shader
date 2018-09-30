Shader "Unlit/SkyBaker"
{
	Properties
	{
		_Noise ("Noise", 2D) = "" {}
		_Sky ("Sky", cube) = "" {}
		_E ("E", float) = 0
		_Samples ("Samples", float) = 1
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

			struct v2f
			{
				float3 worldNormal : TEXCOORD0;
				float3 worldTan : TEXCOORD1;
				float3 worldBinormal : TEXCOORD2;
				//float2 uv : TEXCOORD3;
				float4 vertex : SV_POSITION;
			};

			samplerCUBE _Sky;
			sampler2D _Noise;

			float _Samples;
			float _E;

			float3 hemisphere(float e, float2 sp) {
				float cos_phi = cos(2.0 * UNITY_PI * sp.x);
				float sin_phi = sin(2.0 * UNITY_PI * sp.x);
				float cos_theta = pow(1.0 - sp.y, 1.0 / (e + 1.0));
				float sin_theta = sqrt(1.0 - cos_theta*cos_theta);
				float pu = sin_theta*cos_phi;
				float pv = sin_theta*sin_phi;
				float pw = cos_theta;

				return float3(pu, pv, pw);
			}
			
			v2f vert (appdata_full v)
			{
				v2f o;
				o.vertex = float4(v.texcoord.x * 2 - 1, (1 - v.texcoord.y) * 2 - 1, 0, 1);
				
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldTan = UnityObjectToWorldDir(v.tangent.xyz);
				float tanSign = v.tangent.w * unity_WorldTransformParams.w;
				o.worldBinormal = cross(o.worldNormal, o.worldTan)*tanSign;

				//o.uv = v.texcoord.xy;

				return o;
			}
			
			float4 frag (v2f input) : SV_Target
			{
				float4 col = float4(0,0,0,1);

				for (float i = 0; i <= _Samples; i++)
				{
					for (float j = 0; j <= _Samples; j++)
					{
						float2 uv = float2(i / _Samples, j / _Samples);

						float2 sp = tex2D(_Noise, uv).rg;

						float3 hemiDir = hemisphere(_E, sp);

						float3 realDir = input.worldTan*hemiDir.x + input.worldNormal*hemiDir.z + input.worldBinormal*hemiDir.y;

						col.rgb += texCUBE(_Sky, realDir).rgb;
					}
				}

				col.rgb /= ((_Samples+1) * (_Samples+1));

				/*float4 col = texCUBE(_Sky, input.worldNormal);
				col.a = 1.0;*/

				return col;
			}
			ENDCG
		}
	}
}
