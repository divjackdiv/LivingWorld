Shader "Sprites/VerticalGradient"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_GradientSize("Gradient Size", float) = 1.0
		_Center("Center", float) = 0.5
		_Color("Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

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
			
			float4 _Color;
			float _GradientSize;
			float _Center;
			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{				
				fixed4 col = tex2D(_MainTex, i.uv) * _Color;
				float alpha = 1.0 - min(max((abs(i.uv[1] - _Center)/_GradientSize),0.0),1.0);
				col = col  * alpha;
				col.rgb *= col.a;
				return col;
			}
			ENDCG
		}
	}
}
