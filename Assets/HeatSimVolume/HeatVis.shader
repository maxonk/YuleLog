﻿Shader "Unlit/HeatVis"
{
	Properties
	{
		_MainTex ("Source", 2D) = "white" {}
		_HeatTex ("Texture", 2D) = "white" {}
		_SmokeHeatColorGrad ("Life/Heat Color Gradient", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float4 worldPos : TEXCOORD0;
				float2 uv : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			sampler2D _CameraDepthTexture;

			sampler2D _HeatTex, _SmokeHeatColorGrad;
			StructuredBuffer<float4> points;
			StructuredBuffer<float> heat;

			sampler3D _VelocityHeatVolume, _FuelSmokeVolume;

			float4 _FrustumNearBottomLeft, _FrustumFarBottomLeft,
				_FrustumNearBottomRight, _FrustumFarBottomRight,
				_FrustumNearTopLeft, _FrustumFarTopLeft,
				_FrustumNearTopRight, _FrustumFarTopRight;
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
					
			float2 sampleVolume(float3 worldPos, float visibility, float visibleHeat){
				float3 volumePos = worldPos; // -1 = 0, 1 = 1
				volumePos.x = saturate((worldPos.x + 8) * 0.0675);
				volumePos.y = saturate((worldPos.y + 1) * 0.125);
				volumePos.z = saturate((worldPos.z + 4) * 0.125);

				float smoke = tex3D(_FuelSmokeVolume, volumePos).y;
				float heat = tex3D(_VelocityHeatVolume, volumePos).a;

				return float2(-visibility * smoke, heat * smoke * visibility);
			}
			
			fixed4 frag (v2f i) : SV_Target {
				float4 color = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
				
				float visibleDepth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv));
				
				float3 nearBottomX = lerp(_FrustumNearBottomLeft, _FrustumNearBottomRight, i.uv.x);
				float3 nearTopX = lerp(_FrustumNearTopLeft, _FrustumNearTopRight, i.uv.x);
				float3 farBottomX = lerp(_FrustumFarBottomLeft, _FrustumFarBottomRight, i.uv.x);
				float3 farTopX = lerp(_FrustumFarTopLeft, _FrustumFarTopRight, i.uv.x);
				float3 nearXY = lerp(nearBottomX, nearTopX, i.uv.y);
				float3 farXY = lerp(farBottomX, farTopX, i.uv.y);

				float visibility = 1.0;
				float visibleHeat = 0.0;

				float2 samp;
				for (float z = 0; z < 300; z++) {
					float3 normalizedFrustumIndex = float3(i.uv, z / 300.0);
					if(normalizedFrustumIndex.z < visibleDepth) { 
						float3 worldPos = lerp(nearXY, farXY, normalizedFrustumIndex.z);
						samp = sampleVolume(worldPos, visibility, visibleHeat); //transform world pos into volume space
						//test against next frustrum index and only do by pct?
						visibility += samp.x;
						visibleHeat += samp.y;
					} else if((z - 1) / 300.0 < visibleDepth){
						float3 worldPos = lerp(nearXY, farXY, visibleDepth);
						float pctThru = (visibleDepth - (z-1) / 300.0) / (1 / 300.0);
						samp = sampleVolume(worldPos, visibility, visibleHeat); //transform world pos into volume space
						//test against next frustrum index and only do by pct?
						visibility += samp.x * pctThru;
						visibleHeat += samp.y * pctThru;
					}
				}

				float4 smokeColor = tex2D(_SmokeHeatColorGrad, float2(pow(saturate(pow(saturate(visibleHeat), 4) * 1000.0), 1), 0.5));
				
				float4 fireColor = tex2D(_SmokeHeatColorGrad, float2(pow(saturate(pow(saturate(visibleHeat),2.15) * 10000000), 3), 0.5));

				return (color * visibility)
					+ (smokeColor * (1 - visibility))
					+ (fireColor * visibleHeat * 3000);// * (1 - visibility));
			}
			
			ENDCG
		}
	}
}
