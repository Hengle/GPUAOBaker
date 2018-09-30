Shader "Unlit/NormalBaker"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			cull off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct v2f
			{
				float3 normal : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};
			
			v2f vert (appdata_full v)
			{
				v2f o;
				o.vertex = float4(v.texcoord.x*2-1, (1-v.texcoord.y)*2-1, 0, 1);
				
				//TANGENT_SPACE_ROTATION;

				//float3 normal = mul(rotation, v.normal);

				o.normal = v.normal * 0.5 + 0.5;

				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float4 col;
				col.rgb = i.normal;
				col.a = 1.0;
				return col;
			}
			ENDCG
		}
	}
}
