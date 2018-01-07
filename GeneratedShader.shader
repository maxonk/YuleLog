Shader "PBR Master"
{
	Properties
	{
	}
	SubShader
	{
		Tags{ "RenderPipeline" = "LightweightPipeline"}
		Tags
		{
			"RenderType"="Opaque"
			"Queue"="Geometry"
		}
		
		Pass
{
	Tags{"LightMode" = "LightweightForward"}
	
			Blend One Zero
		
			Cull Back
		
			ZTest LEqual
		
			ZWrite On
		

	HLSLPROGRAM
    // Required to compile gles 2.0 with standard srp library
    #pragma prefer_hlslcc gles
	#pragma target 3.0

    // -------------------------------------
    // Lightweight Pipeline keywords
    // We have no good approach exposed to skip shader variants, e.g, ideally we would like to skip _CASCADE for all puctual lights
    // Lightweight combines light classification and shadows keywords to reduce shader variants.
    // Lightweight shader library declares defines based on these keywords to avoid having to check them in the shaders
    // Core.hlsl defines _MAIN_LIGHT_DIRECTIONAL and _MAIN_LIGHT_SPOT (point lights can't be main light)
    // Shadow.hlsl defines _SHADOWS_ENABLED, _SHADOWS_SOFT, _SHADOWS_CASCADE, _SHADOWS_PERSPECTIVE
    #pragma multi_compile _ _MAIN_LIGHT_DIRECTIONAL_SHADOW _MAIN_LIGHT_DIRECTIONAL_SHADOW_CASCADE _MAIN_LIGHT_DIRECTIONAL_SHADOW_SOFT _MAIN_LIGHT_DIRECTIONAL_SHADOW_CASCADE_SOFT _MAIN_LIGHT_SPOT_SHADOW _MAIN_LIGHT_SPOT_SHADOW_SOFT
    #pragma multi_compile _ _MAIN_LIGHT_COOKIE
    #pragma multi_compile _ _ADDITIONAL_LIGHTS
    #pragma multi_compile _ _VERTEX_LIGHTS
    #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

    // -------------------------------------
    // Unity defined keywords
    #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
    #pragma multi_compile _ LIGHTMAP_ON
    #pragma multi_compile _ DIRLIGHTMAP_COMBINED
    #pragma multi_compile_fog

    #pragma vertex vert
	#pragma fragment frag

	

	#include "LWRP/Shaders/LightweightShaderLibrary/Core.hlsl"
	#include "LWRP/Shaders/LightweightShaderLibrary/Lighting.hlsl"
	#include "CoreRP/ShaderLibrary/Color.hlsl"
	#include "ShaderGraphLibrary/Functions.hlsl"

						struct GraphVertexInput
							{
								float4 vertex : POSITION;
								float3 normal : NORMAL;
								float4 tangent : TANGENT;
								float4 texcoord1 : TEXCOORD1;
							};
					
							struct SurfaceInputs{
							};
					
							struct SurfaceDescription{
								float3 Albedo;
								float3 Normal;
								float3 Emission;
								float Metallic;
								float Smoothness;
								float Occlusion;
								float Alpha;
								float AlphaClipThreshold;
							};
					
							float4 _PBRMaster_C49AAC85_Albedo;
							float4 _PBRMaster_C49AAC85_Normal;
							float4 _PBRMaster_C49AAC85_Emission;
							float _PBRMaster_C49AAC85_Metallic;
							float _PBRMaster_C49AAC85_Smoothness;
							float _PBRMaster_C49AAC85_Occlusion;
							float _PBRMaster_C49AAC85_Alpha;
							float _PBRMaster_C49AAC85_AlphaClipThreshold;
					
							GraphVertexInput PopulateVertexData(GraphVertexInput v){
								return v;
							}
					
							SurfaceDescription PopulateSurfaceData(SurfaceInputs IN) {
								SurfaceDescription surface = (SurfaceDescription)0;
								surface.Albedo = _PBRMaster_C49AAC85_Albedo;
								surface.Normal = _PBRMaster_C49AAC85_Normal;
								surface.Emission = _PBRMaster_C49AAC85_Emission;
								surface.Metallic = _PBRMaster_C49AAC85_Metallic;
								surface.Smoothness = _PBRMaster_C49AAC85_Smoothness;
								surface.Occlusion = _PBRMaster_C49AAC85_Occlusion;
								surface.Alpha = _PBRMaster_C49AAC85_Alpha;
								surface.AlphaClipThreshold = _PBRMaster_C49AAC85_AlphaClipThreshold;
								return surface;
							}
					
		

	struct GraphVertexOutput
    {
        float4 clipPos : SV_POSITION;
        float4 lightmapUVOrVertexSH : TEXCOORD0;
		half4 fogFactorAndVertexLight : TEXCOORD1; // x: fogFactor, yzw: vertex light
        			float3 WorldSpaceNormal : TEXCOORD3;
					float3 WorldSpaceTangent : TEXCOORD4;
					float3 WorldSpaceBiTangent : TEXCOORD5;
					float3 WorldSpaceViewDirection : TEXCOORD6;
					float3 WorldSpacePosition : TEXCOORD7;
					half4 uv1 : TEXCOORD8;
		
    };

    GraphVertexOutput vert (GraphVertexInput v)
	{
	    v = PopulateVertexData(v);

        GraphVertexOutput o = (GraphVertexOutput)0;

        			o.WorldSpaceNormal = mul(v.normal,(float3x3)UNITY_MATRIX_I_M);
					o.WorldSpaceTangent = mul((float3x3)UNITY_MATRIX_M,v.tangent);
					o.WorldSpaceBiTangent = normalize(cross(o.WorldSpaceNormal, o.WorldSpaceTangent.xyz) * v.tangent.w);
					o.WorldSpaceViewDirection = SafeNormalize(_WorldSpaceCameraPos.xyz - mul(GetObjectToWorldMatrix(), float4(v.vertex.xyz, 1.0)).xyz);
					o.WorldSpacePosition = mul(UNITY_MATRIX_M,v.vertex);
					o.uv1 = v.texcoord1;
		

		float3 lwWNormal = TransformObjectToWorldNormal(v.normal);
		float3 lwWorldPos = TransformObjectToWorld(v.vertex.xyz);
		float4 clipPos = TransformWorldToHClip(lwWorldPos);

 		// We either sample GI from lightmap or SH. lightmap UV and vertex SH coefficients
	    // are packed in lightmapUVOrVertexSH to save interpolator.
	    // The following funcions initialize
	    OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUVOrVertexSH);
	    OUTPUT_SH(lwWNormal, o.lightmapUVOrVertexSH);

	    half3 vertexLight = VertexLighting(lwWorldPos, lwWNormal);
	    half fogFactor = ComputeFogFactor(clipPos.z);
	    o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
	    o.clipPos = clipPos;

		return o;
	}

	half4 frag (GraphVertexOutput IN) : SV_Target
    {
    				float3 WorldSpaceNormal = normalize(IN.WorldSpaceNormal);
					float3 WorldSpaceTangent = IN.WorldSpaceTangent;
					float3 WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
					float3 WorldSpaceViewDirection = normalize(IN.WorldSpaceViewDirection);
					float3 WorldSpacePosition = IN.WorldSpacePosition;
					float4 uv1  = IN.uv1;
		

        SurfaceInputs surfaceInput = (SurfaceInputs)0;
        

        SurfaceDescription surf = PopulateSurfaceData(surfaceInput);

		float3 Albedo = float3(0.5, 0.5, 0.5);
		float3 Specular = float3(0, 0, 0);
		float Metallic = 0;
		float3 Normal = float3(0, 0, 1);
		float3 Emission = 0;
		float Smoothness = 0.5;
		float Occlusion = 1;
		float Alpha = 1;
		float AlphaClipThreshold = 0;

        			Albedo = surf.Albedo;
					Normal = surf.Normal;
					Emission = surf.Emission;
					Metallic = surf.Metallic;
					Smoothness = surf.Smoothness;
					Occlusion = surf.Occlusion;
					Alpha = surf.Alpha;
					AlphaClipThreshold = surf.AlphaClipThreshold;
		

#if _NORMALMAP
    half3 normalWS = TangentToWorldNormal(Normal, WorldSpaceTangent, WorldSpaceBiTangent, WorldSpaceNormal);
#else
    half3 normalWS = normalize(WorldSpaceNormal);
#endif

	half3 indirectDiffuse = SampleGI(IN.lightmapUVOrVertexSH, normalWS);

	half4 color = LightweightFragmentPBR(
			WorldSpacePosition,
			normalWS,
			WorldSpaceViewDirection,
			indirectDiffuse,
			IN.fogFactorAndVertexLight.yzw,
			Albedo,
			Metallic,
			Specular,
			Smoothness,
			Occlusion,
			Emission,
			Alpha);

	// Computes fog factor per-vertex
    ApplyFog(color.rgb, IN.fogFactorAndVertexLight.x);

#if _AlphaOut
		color.a = Alpha;
#else
		color.a = 1;
#endif

#if _AlphaClip
		clip(Alpha - AlphaClipThreshold);
#endif
		return color;
    }

	ENDHLSL
}

		        Pass
        {
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On ZTest LEqual

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "LWRP/Shaders/LightweightPassShadow.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "LWRP/Shaders/LightweightShaderLibrary/Core.hlsl"

            float4 vert(float4 pos : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(pos.xyz);
            }

            half4 frag() : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Tags{"LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles

            #pragma vertex LightweightVertexMeta
            #pragma fragment LightweightFragmentMeta

            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICSPECGLOSSMAP
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature EDITOR_VISUALIZATION

            #pragma shader_feature _SPECGLOSSMAP

            #include "LWRP/Shaders/LightweightPassMeta.hlsl"
            ENDHLSL
        }

	}
	
}
