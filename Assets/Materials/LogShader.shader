Shader "Unlit/LogShader"
{
	Properties
	{
		_Gradient ("Additive Heat Gradient", 2D) = "black" {}
		_Color ("Base Color", Color) = (0,0,0,0)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		ZWrite On
		LOD 100

		Pass
		{
            Name "FORWARD"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
            #define UNITY_PASS_FORWARDBASE

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _Gradient;
			float4 _Color;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = float2(v.color.r, 0.25); //vertex color should define this
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = _Color + tex2D(_Gradient, i.uv);
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
    FallBack "Diffuse"
}
