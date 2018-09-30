Shader "Unlit/AOBaker"
{
	Properties
	{
		_Noise("Noise", 2D) = "" {}
		_E("E", float) = 0
		_Samples("Samples", float) = 1
		_MaxRange ("MaxRange", float) = 1
		_Offset ("Offset", float) = 0.001
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
				float3 worldPos : TEXCOORD3;
				float4 vertex : SV_POSITION;
			};

			sampler2D _Noise;

			float _Samples;
			float _E;

			float4 _Vertices[9000];

			float _TriangleCount;

			float _MaxRange;
			float _Offset;

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

			float raycast(float3 dir, float3 origin, float3 v0, float v1, float v2) {
				float3 o = origin + dir*_Offset;

				float2 uv;
				float t;

				float3 E1 = v1 - v0;
				
				float3 E2 = v2 - v0;
				
				float3 P = cross(dir, E2);
				
				float det = dot(E1, P);
				float3 T;
				
				if (det >0)
				{
					T = origin - v0;
				}
				else
				{
					T = v0 - origin;
					det = -det;
				}
				if (det < 0.0001f) {
					return _MaxRange;
				}
				
				uv.x = dot(T, P);
				
				if (uv.x < 0.0f || uv.x > det)
					return _MaxRange;
				
				float3 Q = cross(T, E1);
				
				uv.y = dot(dir, Q);
				if (uv.y < 0.0f || uv.x + uv.y > det)
					return _MaxRange;
				
				t = dot(E2, Q);
				
				float fInvDet = 1.0f / det;
				
				t *= fInvDet;
				
				return t;
			}

			float raycast_scene(float3 dir, float3 origin) {
				float t = 0;

				for (int i = 0; i < _TriangleCount; i+=3) {
					float3 v0 = _Vertices[i * 3];
					float3 v1 = _Vertices[i * 3 + 1];
					float3 v2 = _Vertices[i * 3 + 2];

					t += min(_MaxRange, raycast(dir, origin, v0, v1, v2));
				}

				t /= _TriangleCount;
				return t;
			}
			
			v2f vert (appdata_full v)
			{
				v2f o;
				o.vertex = float4(v.texcoord.x * 2 - 1, (1 - v.texcoord.y) * 2 - 1, 0, 1);

				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldTan = UnityObjectToWorldDir(v.tangent.xyz);
				float tanSign = v.tangent.w * unity_WorldTransformParams.w;
				o.worldBinormal = cross(o.worldNormal, o.worldTan)*tanSign;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}
			
			fixed4 frag (v2f input) : SV_Target
			{
				float4 col = float4(1,1,1,1);

				float t = 0.0;
				for (float i = 0; i <= _Samples; i++)
				{
					for (float j = 0; j <= _Samples; j++)
					{
						float2 uv = float2(i / _Samples, j / _Samples);

						float2 sp = tex2D(_Noise, uv).rg;

						float3 hemiDir = hemisphere(_E, sp);

						float3 realDir = input.worldTan*hemiDir.x + input.worldNormal*hemiDir.z + input.worldBinormal*hemiDir.y;

						t += raycast_scene(realDir, input.worldPos);
					}
				}

				t /= ((_Samples + 1) * (_Samples + 1));

				col.rgb *= t;
				return col;
			}
			ENDCG
		}
	}
}
