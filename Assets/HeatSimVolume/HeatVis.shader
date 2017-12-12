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

			float4 _FrustumNearBottomLeft, _FrustumFarBottomLeft, //these are stored in volume space
				_FrustumNearBottomRight, _FrustumFarBottomRight, //dont need the nears rn, maybe ditch
				_FrustumNearTopLeft, _FrustumFarTopLeft,
				_FrustumNearTopRight, _FrustumFarTopRight,
				_VolSpaceCamPos;
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			#define numStepsToFullHeat 8.0
			#define minVisibleHeat 0.525
			#define maxHeat 1.075
					
			float3 smokeVisHeatNext(float3 pos, float3 smokeVisHeatLast){
				float2 heatSmoke = tex3D(_FuelSmokeVolume, pos).xy
					 * (max(max(pos.x, pos.y), pos.z) < 1.0) //clamp positive range
					 * (min(min(pos.x, pos.y), pos.z) > 0.0); //clamp negative range

				//ret = change in vis, change in visible heat
				return float3(												
					heatSmoke.y,													//change in smoke density
					-heatSmoke.y * smokeVisHeatLast.y,// * saturate(heatSmoke.x * 5.0),			//change in visibility
					pow(max(0, (heatSmoke.x - minVisibleHeat) / (maxHeat - minVisibleHeat)), 1) * (1.0 / numStepsToFullHeat)//- smokeVisHeat.z) //increase if it's more					//change in visible heat
				); 
			}
			
			fixed4 frag (v2f i) : SV_Target {
				float4 color = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));

				float visibleDepth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv)); //i dont think this is working right
				//return visibleDepth.xxxx;
				/*
				float3 nearXY = 
					lerp(
						lerp(_FrustumNearBottomLeft, _FrustumNearBottomRight, i.uv.x), //near bottom x 
						lerp(_FrustumNearTopLeft, _FrustumNearTopRight, i.uv.x), //near top x
					i.uv.y);*/
				float3 farXY = 
					lerp(
						lerp(_FrustumFarBottomLeft, _FrustumFarBottomRight, i.uv.x),  //far bottom x
						lerp(_FrustumFarTopLeft, _FrustumFarTopRight, i.uv.x),  //far top x
					i.uv.y);
				
				float3 smokeVisHeat = float3(0.0, 1.0, 0.0);

				for (float z = 0.0; z < 1.0; z += 0.00390625) { //0.00390625 = 1 / 256
					float pastOurDepth01 = step(z, visibleDepth);
					smokeVisHeat += smokeVisHeatNext(
							lerp(/*nearXY*/ _VolSpaceCamPos, farXY, z),//position
							smokeVisHeat //last time's smokeVisHeat'
						) * pastOurDepth01.xxx; //1 if visibleDepth > z, 0 otherwise
				}
				float pctThru = fmod(visibleDepth, 0.00390625) / 0.00390625; //add remainder
				smokeVisHeat += smokeVisHeatNext(lerp(_VolSpaceCamPos, farXY, visibleDepth), smokeVisHeat) * pctThru; 
				
				float4 smokeColor = tex2D(_SmokeHeatColorGrad, float2(saturate(smokeVisHeat.x * 1.0), 0.75));
				
				float4 fireColor = tex2D(_SmokeHeatColorGrad, float2(pow(saturate(smokeVisHeat.z - 0.075), 1.5), 0.25));
							 
						//	 return smokeVisHeat.zzzz;
					//	smokeVisHeat.y = 1;//REMOVE

				return (color * smokeVisHeat.y)
					+ (smokeColor * (1 - smokeVisHeat.y))
					+ (fireColor);
				
			}
			
			ENDCG
		}
	}
}
