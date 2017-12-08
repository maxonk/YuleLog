Shader "Unlit/HeatVis"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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
					
			float3 sampleVolume(float3 pos, float3 smokeVisHeat){
				float2 heatSmoke = tex3D(_FuelSmokeVolume, pos).xy;

				//ret = change in vis, change in visible heat
				return float3(												
					heatSmoke.y,													//change in smoke density
					-heatSmoke.y * smokeVisHeat.y, //* saturate(heat * 100.0),			//change in visibility
					max(0.0000000, heatSmoke.x )//- smokeVisHeat.z) //increase if it's more					//change in visible heat
				) ;//* step(pos, float3(1,1,1)) * step(float3(0,0,0), pos); //returns 0 if pos is outside of 01 range
			}
			
			fixed4 frag (v2f i) : SV_Target {
				float4 color = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
				
				float visibleDepth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv));
				
				float3 nearXY = 
					lerp(
						lerp(_FrustumNearBottomLeft, _FrustumNearBottomRight, i.uv.x), //near bottom x 
						lerp(_FrustumNearTopLeft, _FrustumNearTopRight, i.uv.x), //near top x
					i.uv.y);
				float3 farXY = 
					lerp(
						lerp(_FrustumFarBottomLeft, _FrustumFarBottomRight, i.uv.x),  //far bottom x
						lerp(_FrustumFarTopLeft, _FrustumFarTopRight, i.uv.x),  //far top x
					i.uv.y);
				
				float3 smokeVisHeat = float3(0.0, 1.0, 0.0);

				for (float z = 0.0; z < 1.0; z += 0.00390625) { //0.00390625 = 1 / 256
					smokeVisHeat += sampleVolume(lerp(nearXY, farXY, z), smokeVisHeat);
						//* step(z, visibleDepth); //1 if visibleDepth > z, 0 otherwise
				}
				float pctThru = fmod(visibleDepth, 0.00390625); //add remainder
				smokeVisHeat += sampleVolume(lerp(nearXY, farXY, visibleDepth), smokeVisHeat) * pctThru; 

				smokeVisHeat = saturate(smokeVisHeat);

				float4 smokeColor = tex2D(_SmokeHeatColorGrad, float2(saturate(smokeVisHeat.x * 10), 0.75));
				
				float4 fireColor = tex2D(_SmokeHeatColorGrad, float2(saturate(pow(smokeVisHeat.z * 500, 5) * 20), 0.25));
							 
				return (color * smokeVisHeat.y)
					+ (smokeColor * (1 - smokeVisHeat.y))
					+ smokeVisHeat.xyzx * 0.1; /* //heat is 0?????
			//		+ (fireColor * 1.0);// * pow(saturate(1 - visibility * 3.0), 0.1);// * (1 - visibility));
					*/
			}
			
			ENDCG
		}
	}
}
