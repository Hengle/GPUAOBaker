Shader "Hidden/Filter"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

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

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			float4 frag (v2f i) : SV_Target
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
