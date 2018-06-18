Shader "Sprites/SpriteMix"
{
	Properties
	{
		_PrimaryTex ("Primary Texture", 2D) = "white" {}
		_SecondaryTex ("Secondary Texture", 2D) = "white" {}
		_Mix ("Mix", Range(0.0,1.0)) = 0.5
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
			
			sampler2D _PrimaryTex;
			sampler2D _SecondaryTex;
			float _Mix;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col1 = tex2D(_PrimaryTex, i.uv);
				fixed4 col2 = tex2D(_SecondaryTex, i.uv);
				return lerp(col1, col2, _Mix);
			}
			ENDCG
		}
	}
}
