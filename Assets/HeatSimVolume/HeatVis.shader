Shader "Unlit/HeatVis"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_HeatTex ("Texture", 2D) = "white" {}
		_SmokeHeatColorGrad ("Life/Heat Color Gradient", 2D) = "white" {}
		_CloudNoise ("Texture", 2D) = "white" {}
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

			sampler2D _HeatTex, _SmokeHeatColorGrad, _CloudNoise;

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

			#define STEPS_TO_FULL_HEAT 1.8
			#define MIN_VISIBLE_HEAT 0.675
			#define MAX_HEAT 1.075
			#define TAU 1.57079632679

			//curve = 0-->1 sin wave segment
			float lightAttenCurve(float t){
				return (sin(3.1415 * t - 1.5707) + 1) / 2;
			}
					
			float getHeat(float3 pos){
				float heat = tex3D(_VelocityHeatVolume, pos).a
					 * (max(max(pos.x, pos.y), pos.z) < 1.0) //clamp positive range
					 * (min(min(pos.x, pos.y), pos.z) > 0.0); //clamp negative range

				//ret = change in vis, change in visible heat
				return max(0, (heat - MIN_VISIBLE_HEAT) / (MAX_HEAT - MIN_VISIBLE_HEAT)) / STEPS_TO_FULL_HEAT;
			}


			float getHeat_noiseModulated(float3 pos, float3 noise){
				return	getHeat(pos - noise);
			}
			
			fixed4 frag (v2f i) : SV_Target {

				float visibleDepth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv));
				
				float3 farXY = 
					lerp(
						lerp(_FrustumFarBottomLeft, _FrustumFarBottomRight, i.uv.x),  //far bottom x
						lerp(_FrustumFarTopLeft, _FrustumFarTopRight, i.uv.x),  //far top x
					i.uv.y);
				
				float heat = 0.0;

				//-------------------------------------------------------------------------------------------------------------
				//NOISE											tightness		    speed						magnitude
				float2 ncoords = i.uv.xy;// + float2(visibleDepth * 0.1, 0);
				float3 noise =	tex2D(_CloudNoise,	ncoords   * float2(3, 0.51) +	float2(_Time.g / 5, 0))	 *  0.01 - 0.005; // large grain, 
					   noise += tex2D(_CloudNoise,	ncoords  *	float2(5, 0.88) +	float2(_Time.g / 2, 0))	 *  0.008 - 0.004;

				//---------------------------------------------------------------------------------------------------

				//------------------------------------------------------------------------------SAMPLE VOLUME
				for (float z = 0.0; z < 1.0; z += 0.00390625) { //0.00390625 = 1 / 256
					float pastOurDepth01 = step(z, visibleDepth);
					heat += getHeat_noiseModulated(lerp(_VolSpaceCamPos, farXY, z), noise) //lerp to find position (consider getting a normalized dir and incrementing it instead..)
								* pastOurDepth01.xxx //1 if visibleDepth > z, 0 otherwise
								* (1 - heat); //reduce influence to blur
				}
				float pctThru = fmod(visibleDepth, 0.00390625) / 0.00390625; //add remainder
				heat += getHeat_noiseModulated(lerp(_VolSpaceCamPos, farXY, visibleDepth), noise) * pctThru * (1 - heat); 
				//----------------------------------------------------------------------------------------------		

				heat = lightAttenCurve(heat);
						
				float4 fireColor = tex2D(_SmokeHeatColorGrad, float2(saturate(heat), 0.25));
							 
				float4 color = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));

				return color + fireColor;
				
			}
			
			ENDCG
		}
	}
}
