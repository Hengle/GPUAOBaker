Shader "Unlit/AOBaker"
{
	Properties
	{
		_Noise("Noise", 2D) = "" {}
		_E("E", float) = 0
		_Samples("Samples", float) = 1
		_MaxRange ("MaxRange", float) = 1
		_Offset ("Offset", float) = 0.01
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

			float4 _Vertices[1000];
			//float _Indices[1000]; 

			float _VertexCount;

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

			bool raycast(float3 dir, float3 orig, float3 p0, float3 p1, float3 p2, inout float rt) {
				rt = _MaxRange;

				float3 o = orig + dir * _Offset;

				float3 e1 = p1 - p0;
				float3 e2 = p2 - p0;

				float v = 0;
				float u = 0;

				float3 n = cross(e1, e2);
				float ndv = dot(dir, n);
				if (ndv > 0.0)
				{
					return false;
				}

				float3 p = cross(dir, e2);

				float det = dot(e1, p);
				float3 t = float3(0,0,0);
				if (det > 0.0)
				{
					t = o - p0;
				}
				else
				{
					t = p0 - o;
					det = -det;
				}
				if (det < 0.0000001f)
				{
					return false;
				}

				u = dot(t, p);
				if (u < 0.0f || u > det)
					return false;

				float3 q = cross(t, e1);

				v = dot(dir, q);
				if (v < 0.0f || u + v > det)
					return false;

				rt = dot(e2, q);

				float finvdet = 1.0f / det;
				rt *= finvdet;
				if (rt < 0.001f)
					return false;
				u *= finvdet;
				v *= finvdet;

				return true;
			}

			float raycast_scene(float3 dir, float3 origin) {
				float t = _MaxRange;

				for (int i = 0; i < (int)_VertexCount; i+=3) {
					//float i0 = _Indices[i * 3];
					//float i1 = _Indices[i * 3 + 1];
					//float i2 = _Indices[i * 3 + 2];
					float3 v0 = _Vertices[i].xyz;
					float3 v1 = _Vertices[i+1].xyz;
					float3 v2 = _Vertices[i+2].xyz;

					float tmpt;
					bool result = raycast(dir, origin, v0, v1, v2, tmpt);

					if (result) {
						if (tmpt < t)
							t = tmpt;
					}
				}

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

				//t = raycast_scene(input.worldNormal, input.worldPos);

				t /= ((_Samples + 1) * (_Samples + 1));

				t = saturate(t / _MaxRange);

				col.rgb *= t;

				return col;
			}
			ENDCG
		}
	}
}
