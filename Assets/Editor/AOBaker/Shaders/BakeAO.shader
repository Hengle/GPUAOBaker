Shader "Hidden/BakeAO"
{
	Properties
	{
		_PreTex ("PreTex", 2D) = "white" {}      //保留上一次采样的结果
		_E("E", float) = 0
		_Sample0 ("Sample0", vector) = (0,0,0,0) //采样坐标0
		_Sample1 ("Sample1", vector) = (0,0,0,0) //采样坐标1
		_Sample2 ("Sample2", vector) = (0,0,0,0) //采样坐标2
		_Sample3 ("Sample3", vector) = (0,0,0,0) //采样坐标3
		_TraceRadius ("TraceRadius", float) = 1  //光线追踪有效半径
		_Bias ("Bias", float) = 0.0001           
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "ForceSupported" = "True" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct v2f
			{
				float3 worldNormal   : TEXCOORD0;
				float3 worldTan      : TEXCOORD1;
				float3 worldBinormal : TEXCOORD2;
				float3 worldPos      : TEXCOORD3;
				float2 texcoord      : TEXCOORD4;
				float4 vertex        : SV_POSITION;
			};

			sampler2D _PreTex;

			float _E;

			float2 _Sample0;
			float2 _Sample1;
			float2 _Sample2;
			float2 _Sample3;

			float _TraceRadius;
			float _Bias;

			float4 _Vertices[1000];
			float _VertexCount;

			/*
				半球体映射
			*/
			float3 hemisphere(float e, float2 sp) {
				float cos_phi = cos(2.0 * UNITY_PI * sp.x);
				float sin_phi = sin(2.0 * UNITY_PI * sp.x);
				float cos_theta = pow(1.0 - sp.y, 1.0 / (e + 1.0));
				float sin_theta = sqrt(1.0 - cos_theta * cos_theta);
				float pu = sin_theta * cos_phi;
				float pv = sin_theta * sin_phi;
				float pw = cos_theta;

				return float3(pu, pv, pw);
			}

			/*
				三角面射线检测
			*/
			bool raycast_triangle(float3 dir, float3 orig, float3 p0, float3 p1, float3 p2, inout float rt) {
				rt = _TraceRadius;

				float3 o = orig + normalize(dir) * _Bias;

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
				float3 t = float3(0, 0, 0);
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

			/*
				对场景进行光线追踪
			*/
			float raytracing_scene(float3 dir, float3 origin) {
				float t = _TraceRadius;

				for (int i = 0; i < (int)_VertexCount; i += 3) {
					float3 v0 = _Vertices[i].xyz;
					float3 v1 = _Vertices[i + 1].xyz;
					float3 v2 = _Vertices[i + 2].xyz;

					float tmpt;
					bool result = raycast_triangle(dir, origin, v0, v1, v2, tmpt);

					if (result) {
						if (tmpt < t)
							t = tmpt;
					}
				}

				return t;
			}

			float raytracing(v2f input, float2 sampleUV) {
				float3 hemiDir = hemisphere(_E, sampleUV);

				float3 dir = input.worldTan*hemiDir.x + input.worldNormal*hemiDir.z + input.worldBinormal*hemiDir.y;

				return clamp(raytracing_scene(dir, input.worldPos), 0, _TraceRadius) / _TraceRadius;  //将该条光线的光线追踪结果压缩到0-1内
			}
			
			v2f vert (appdata_full v)
			{
				v2f o;
				o.vertex = float4(v.texcoord.x * 2 - 1, (1 - v.texcoord.y) * 2 - 1, 0, 1); //直接使用uv作为投影坐标，以渲染uv空间到rendertexture

				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldTan = UnityObjectToWorldDir(v.tangent.xyz);
				float tanSign = v.tangent.w * unity_WorldTransformParams.w;
				o.worldBinormal = cross(o.worldNormal, o.worldTan)*tanSign;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.texcoord = v.texcoord;
				return o;
			}
			
			float4 frag (v2f input) : SV_Target
			{
				float4 pre = tex2D(_PreTex, input.texcoord);

				float4 col = float4(1,1,1,1);

				//对四个采样坐标做光线追踪
				col.r = min(pre.r, raytracing(input, _Sample0));
				col.g = min(pre.g, raytracing(input, _Sample1));
				col.b = min(pre.b, raytracing(input, _Sample2));
				col.a = min(pre.a, raytracing(input, _Sample3));

				return col;
			}
			ENDCG
		}
	}
}
