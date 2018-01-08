Shader "Unlit/Fire"
{
	Properties
	{
		_CloudNoise ("Cloud Noise", 2D) = "white" {}
		_WarpNoise ("Warp Noise", 2D) = "white" {}
		_ColorGradient ("Color Gradient", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		Blend One One
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float4 worldPos : TEXCOORD1;
			};

			sampler2D _CloudNoise, _ColorGradient, _WarpNoise;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.uv = v.uv;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{				
				float2 uv = i.uv;
				
				//warp the uvs by taking the derivative of the noise texture to get the normal direction of it as if it were a heightmapped surface
				float2 warpNoiseUV = 
					i.uv - 
					float2(
						tex2D( //grabbing a noise sample by time for x motion to be a little chaotic
							_CloudNoise, 
							float2(_Time.r * 2, _Time.r * 2.2)
						).r * 0.1 + _Time.r * 2.5,  //adding a good deal of constant time motion to fuzz out the noise a bit
						
						(_Time.a + tex2D(_CloudNoise, _Time.rr).r) * 0.25 //y noise is a combo of 
						+ _Time.g * 0.1 
					);

				float2 uvWarpNormal = 
					float2(
						ddx(tex2D(_CloudNoise, warpNoiseUV).r),
						ddy(tex2D(_CloudNoise, warpNoiseUV).r)
					);

				float2 warpedUV = i.uv + uvWarpNormal * 0; //this currently isnt working - the warping - so it's disabled
								
				//flameHeight is a horizontal slice (uv.x) of noise animating in the y direction
				float flameHeight = 
					tex2D(
						_CloudNoise, 
						float2(
							i.uv.x * 1, 
							_Time.g * 0.2 //+ i.worldPos.z * 0.25
						)
					).x;

				//add some lower buffer to flameheight so it's more consistent

				flameHeight = (sin(i.uv.x * 25 + _Time.a * 4) + 10) * 0.03 //soft rolling base
					+ pow(flameHeight, 2); //points

				//smoosh flameheight on a curve so it fades on the edges
				flameHeight *= sin(warpedUV.x * 3.1415);
				
				float flameOnOff = saturate(flameHeight - i.uv.y);
			
				//fade out at the bottom
				flameOnOff *= 1 - pow(1 - i.uv.y, 25);

				fixed4 returnColor = tex2D(_ColorGradient, float2(flameOnOff * 8, 0.25));
				
				UNITY_APPLY_FOG(i.fogCoord, returnColor);

				return returnColor;
			}
			ENDCG
		}
	}
}

					//return fixed4(i.uv.x, i.uv.y, 0, 0);  // UV DEBUG
