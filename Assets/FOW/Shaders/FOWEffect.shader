Shader "Hidden/FOWEffect"
{
	Properties {}
	
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
			
			#pragma shader_feature FOWSHADOWTYPE_SDF

			#include "UnityCG.cginc"

			struct vertInput
			{
				float4 vertex : POSITION;
				float2 texCoord : TEXCOORD0;
			};

			struct fragInput
			{
				float2 texCoord : TEXCOORD0;
				float4 positionCS : SV_POSITION;
			};

			sampler2D _CameraColorBuffer;
			sampler2D _CameraDepthTexture;
			sampler2D _FOWTexture;
			
			// Texture2D _CameraDepthTexture;
			// Texture2D _CameraColorBuffer;
			// Texture2D _FOWTexture;
			// SamplerState _FOW_Trilinear_Clamp_Sampler;

			float2 _InvSize;
			float4 _PositionWS;
			float4x4 _InvVP;
			float4x4 _FOWWorldToLocal;

			// shading
			float _LerpValue;
			float4 _FogColor;
			
			fragInput vert (vertInput input)
			{
			    fragInput output;
			        
			    output.positionCS = UnityObjectToClipPos(input.vertex);
                output.texCoord = input.texCoord;
                
			    return output;
			}

           float3 ComputeWorldSpacePosition(float2 positionNDC, float deviceDepth)
           {
               float4 positionCS = float4(float3(positionNDC, deviceDepth) * 2.0f - 1.0f, 1.0f);
               float4 positionVS = mul(_InvVP, positionCS);

               float3 positionWS = positionVS.xyz / positionVS.w;
               return positionWS;
           }

			float4 frag (fragInput input) : SV_Target
			{
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.texCoord);
			    
			#if defined(UNITY_REVERSED_Z)
				depth = 1.0f - depth;
			#endif

				// recreate world space position 
			    float3 positionWS = ComputeWorldSpacePosition(input.texCoord, depth);
			    float4 positionLS = mul(_FOWWorldToLocal, float4(positionWS, 1.0f));
			    positionLS /= positionLS.w;

			    // sample texture in mask's local space 
			    float3 fogValue = tex2D(_FOWTexture, positionLS.xz * float2(_InvSize));
			    float3 bgColor  = tex2D(_CameraColorBuffer, input.texCoord);

			    float3 color;
	    	#ifdef FOWSHADOWTYPE_SDF
		    	color = bgColor * max(fogValue.r, _FogColor.r);
		    #else
				color = lerp(_FogColor.rgb, float3(1, 1, 1), fogValue.r * _FogColor.a);

			    // Mixed between last and current frame fog texture
				float visual = lerp(fogValue.b, fogValue.g, _LerpValue);
				color = lerp(color, float3(1, 1, 1), visual);
				color *= bgColor;
		    #endif
			    return float4(color, 1.0f);
			}
			ENDCG
		}
	}
}
