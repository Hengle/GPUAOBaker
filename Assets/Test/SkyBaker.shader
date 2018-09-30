Shader "Unlit/SkyBaker"
{
	Properties
	{
		_Sky ("Sky", cube) = "" {}
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
				float3 normal : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			samplerCUBE _Sky;
			
			v2f vert (appdata_base v)
			{
				v2f o;
				o.vertex = float4(v.texcoord.x * 2 - 1, (1 - v.texcoord.y) * 2 - 1, 0, 1);
				o.normal = v.normal;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = texCUBE(_Sky, i.normal);
			col.a = 1.0;
				return col;
			}
			ENDCG
		}
	}
}
