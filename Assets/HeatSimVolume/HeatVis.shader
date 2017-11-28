Shader "Unlit/HeatVis"
{
	Properties
	{
		_MainTex ("Source", 2D) = "white" {}
		_HeatTex ("Texture", 2D) = "white" {}
		_LifeHeatColorGrad ("Life/Heat Color Gradient", 2D) = "white" {}
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

			sampler2D _HeatTex, _LifeHeatColorGrad;
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
					
			float3 sampleVolume(float3 worldPos){
				float3 volumePos = worldPos; // -1 = 0, 1 = 1
				volumePos.x = (worldPos.x + 8) * 0.0675;
				volumePos.y = (worldPos.y + 1) * 0.125;
				volumePos.z = (worldPos.z + 4) * 0.125;
				
				if(abs(volumePos.x) > 1) return 0;
				if(abs(volumePos.y) > 1) return 0;
				if(abs(volumePos.z) > 1) return 0;
				
				float density = tex3D(_FuelSmokeVolume, volumePos).y;
				float3 colorPerDensity = float3(0.1, 0.055, 0);
				return density * colorPerDensity;
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

				float3 fireAddColor = 0;
				for (float z = 0; z < 300; z++) {
					float3 normalizedFrustumIndex = float3(i.uv, z / 300.0);
					if(normalizedFrustumIndex.z < visibleDepth) { 
						float3 worldPos = lerp(nearXY, farXY, normalizedFrustumIndex.z);
						fireAddColor += sampleVolume(worldPos); //transform world pos into volume space
					} //else { 
						//z = 256; //force finish loop breaks everything??
					//}
				}

				// uv is working return i.uv.xxxx;
				//this works too -- return nearBottomX.xxxx; //why does this not differ across the image ??
				//return tex3D(_VelocityHeatVolume, float3(i.uv, 0)).xxxx;
				
				//return visibleDepth.rrrr;
				
				return color + float4(fireAddColor, 0);
			}
			
			ENDCG
		}
	}
}
