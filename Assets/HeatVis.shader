Shader "Unlit/HeatVis"
{
	Properties
	{
		_HeatTex ("Texture", 2D) = "white" {}
		_LifeHeatColorGrad ("Life/Heat Color Gradient", 2D) = "white" {}
	}
	SubShader
	{
		Tags { 
			"RenderType"="Transparent" 
			"Queue"="Transparent"
		}
		LOD 100
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float4 worldPos : TEXCOORD0;
			};

			sampler2D _HeatTex, _LifeHeatColorGrad;
			StructuredBuffer<float4> points;
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			float radiusFromLife(float life) {
				return lerp(0, 0.25, sin(saturate(life) * 3.14159));
			}
			
			float4 colorFromLife(float life, float distToPoint) {
				return float4(3.0/255.0, 1.0/255.0, 0, 1);
				//return tex2D(_LifeHeatColorGrad, float2(life, distToPoint));
			}
					
			fixed4 frag (v2f i) : SV_Target {
				float4 color = float4(0,0,0,0);
				//get
				float4 camHitPoint = i.worldPos;
				float4 camRayDir = float4(normalize(camHitPoint - _WorldSpaceCameraPos), 0);
				
				
				//shader is 32x32 points to check against a raymarch
				for(uint px = 0; px < 32; px++){
					for(uint py = 0; py < 32; py++){
						float rayDistance = 0;
						float4 heatPoint = points[px + py * 32];//tex2D(_HeatTex, float2(px / 32, py / 32));
						for(int stepCount = 0; stepCount < 3; stepCount++) {
							float distToPoint = distance(heatPoint.xyz, camHitPoint + camRayDir * rayDistance);
							if(distToPoint < radiusFromLife(heatPoint.a)) {
								color += colorFromLife(heatPoint.a, distToPoint);
							}
							rayDistance += distToPoint;
						}
					}
				}
				
				/*
				float rayDistance = 0;
				color = float4(0,0,0,0);
				for(int stepCount = 0; stepCount < 5; stepCount++) {
					float distToPoint = distance(float3(0,0,0), camHitPoint + camRayDir * rayDistance);
					if(true) {
						color += float4(100,50,0,100);
					}
					rayDistance += distToPoint;
				}
				*/
			
				//color = float4(100,50,0,50);
				//UNITY_APPLY_FOG(i.fogCoord, color);
				return color;
			}
			ENDCG
		}
	}
}
