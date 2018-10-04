Shader "Hidden/AOMix"
{
	Properties
	{
		_MainTex ("MainTex", 2D) = "white" {}
		_SampleResult0 ("SampleResult0", 2D) = "white" {}
		_SampleResult1 ("SampleResult1", 2D) = "white" {}
		_SampleResult2 ("SampleResult2", 2D) = "white" {}
		//_SampleResult3 ("SampleResult3", 2D) = "white" {}
		_MixFactor ("MixFactor", float) = 0
		_ResultCount ("ResultCount", float) = 0
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass //该Pass对每次的光追结果进行混合计算AO（三次一组）
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

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			sampler2D _SampleResult0;
			sampler2D _SampleResult1;
			sampler2D _SampleResult2;
			//sampler2D _SampleResult3;

			float _MixFactor;
			float _ResultCount;

			float4 frag (v2f i) : SV_Target
			{
				float4 result = tex2D(_MainTex, i.uv);

				float ao = 0;
				float alpha = 1.0;

				float4 col;

				if (_ResultCount > 0) {
					col = tex2D(_SampleResult0, i.uv);
					ao += col.r *_MixFactor + col.g*_MixFactor + col.b*_MixFactor;
					alpha *= col.a;
				}
				if (_ResultCount > 1) {
					col = tex2D(_SampleResult1, i.uv);
					ao += col.r *_MixFactor + col.g*_MixFactor + col.b*_MixFactor;
					alpha *= col.a;
				}
				if (_ResultCount > 2) {
					col = tex2D(_SampleResult2, i.uv);
					ao += col.r *_MixFactor + col.g*_MixFactor + col.b*_MixFactor;
					alpha *= col.a;
				}
				/*if (_ResultCount > 3) {
					col = tex2D(_SampleResult3, i.uv);
					ao += col.r *_MixFactor + col.g*_MixFactor + col.b*_MixFactor;
					alpha *= col.a;
				}*/

				return float4(result.r + ao, result.g + ao, result.b + ao, alpha);
			}
			ENDCG
		}
		Pass //该Pass渲染边界扩展
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

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			float4 frag(v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);
				float4 colL = tex2D(_MainTex, i.uv + float2(-_MainTex_TexelSize.x, 0));
				float4 colR = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, 0));
				float4 colU = tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y));
				float4 colD = tex2D(_MainTex, i.uv + float2(0, -_MainTex_TexelSize.y));

				float4 result = float4(0,0,0,0);

				if (col.a > 0) {
					result.rgb = col.rgb;
					result.a = 1.0;
				}
				else {
					int c = 0;
					if (colL.a > 0)
					{
						result.rgb += colL.rgb;
						c += 1;
					}
					else if (colR.a > 0)
					{
						result.rgb += colR.rgb;
						c += 1;
					}
					else if (colU.a > 0)
					{
						result.rgb += colU.rgb;
						c += 1;
					}
					else if (colD.a > 0)
					{
						result.rgb += colD.rgb;
						c += 1;
					}
					if (c > 0) {
						result.rgb /= c;
						result.a = 1.0;
					}
				}

				return result;
			}
			ENDCG
		}
	}
}
