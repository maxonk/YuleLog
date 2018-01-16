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
				float2 uv : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			sampler2D _CameraDepthTexture;

			sampler2D _HeatTex, _SmokeHeatColorGrad;

			sampler3D _VelocityHeatVolume;

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

			#define STEPS_TO_FULL_HEAT 6.5
			#define MIN_VISIBLE_HEAT 0.65
			#define MAX_HEAT 1.075
					
			float getHeat(float3 pos){
				float heat = tex3D(_VelocityHeatVolume, pos).a
					 * (max(max(pos.x, pos.y), pos.z) < 1.0) //clamp positive range
					 * (min(min(pos.x, pos.y), pos.z) > 0.0); //clamp negative range

				//ret = change in vis, change in visible heat
				return max(0, (heat - MIN_VISIBLE_HEAT) / (MAX_HEAT - MIN_VISIBLE_HEAT)) / STEPS_TO_FULL_HEAT;
			}
			
			fixed4 frag (v2f i) : SV_Target {

				float visibleDepth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv));

		//		return visibleDepth.xxxx;

				float3 farXY = 
					lerp(
						lerp(_FrustumFarBottomLeft, _FrustumFarBottomRight, i.uv.x),  //far bottom x
						lerp(_FrustumFarTopLeft, _FrustumFarTopRight, i.uv.x),  //far top x
					i.uv.y);
				
				float heat = 0.0;

				for (float z = 0.0; z < 1.0; z += 0.00390625) { //0.00390625 = 1 / 256
					float pastOurDepth01 = step(z, visibleDepth);
					heat += getHeat(lerp(_VolSpaceCamPos, farXY, z)) //lerp to find position (consider getting a normalized dir and incrementing it instead..)
								* pastOurDepth01.xxx; //1 if visibleDepth > z, 0 otherwise
				}
				float pctThru = fmod(visibleDepth, 0.00390625) / 0.00390625; //add remainder
				heat += getHeat(lerp(_VolSpaceCamPos, farXY, visibleDepth)) * pctThru; 
								
				float4 fireColor = tex2D(_SmokeHeatColorGrad, float2(pow(saturate(heat - 0.075), 1.5), 0.25));
							 
				float4 color = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));

				return color + fireColor;
				
			}
			
			ENDCG
		}
	}
}
