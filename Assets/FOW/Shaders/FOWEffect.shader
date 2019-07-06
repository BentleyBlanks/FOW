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
				fixed4 vertex : POSITION;
				fixed2 texCoord : TEXCOORD0;
			};

			struct fragInput
			{
				fixed2 texCoord : TEXCOORD0;
				fixed4 positionCS : SV_POSITION;
			};

			// sampler2D _CameraColorBuffer;
			// sampler2D _CustomDepthTexture;
			// sampler2D _FOWTexture;
			
			Texture2D _CameraDepthTexture;
			Texture2D _CameraColorBuffer;
			Texture2D _FOWTexture;
			SamplerState _FOW_Trilinear_Clamp_Sampler;

			fixed _FogValue;
			fixed2 _InvSize;
			fixed4 _PositionWS;
			float4x4 _InvVP;
			float4x4 _FOWWorldToLocal;

			// shading
			half _LerpValue;
			half4 _FogColor;
			
			fragInput vert (vertInput input)
			{
			    fragInput output;
			        
			    output.positionCS = UnityObjectToClipPos(input.vertex);
                output.texCoord = input.texCoord;
                
			    return output;
			}

           fixed3 ComputeWorldSpacePosition(fixed2 positionNDC, fixed deviceDepth)
           {
               fixed4 positionCS = fixed4(fixed3(positionNDC, deviceDepth) * 2.0f - 1.0f, 1.0f);
               fixed4 positionVS = mul(_InvVP, positionCS);

               fixed3 positionWS = positionVS.xyz / positionVS.w;
               return positionWS;
           }

			fixed4 frag (fragInput input) : SV_Target
			{
				// fixed depth = SAMPLE_DEPTH_TEXTURE(_CustomDepthTexture, input.texCoord);
				fixed depth = _CameraDepthTexture.Sample(_FOW_Trilinear_Clamp_Sampler, input.texCoord);
			    
			#if defined(UNITY_REVERSED_Z)
				depth = 1.0f - depth;
			#endif
				// return fixed4(depth.xxx, 1.0f);

				// recreate world space position 
			    fixed3 positionWS = ComputeWorldSpacePosition(input.texCoord, depth);
			    fixed4 positionLS = mul(_FOWWorldToLocal, fixed4(positionWS, 1.0f));
			    positionLS /= positionLS.w;

			    // sample texture in mask's local space 
			    // fixed3 fogValue = tex2D(_FOWTexture, positionLS.xz * fixed2(_InvSize));
			    // fixed3 bgColor  = tex2D(_CameraColorBuffer, input.texCoord);
			    fixed3 fogValue = _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, positionLS.xz * fixed2(_InvSize));
			    fixed3 bgColor  = _CameraColorBuffer.Sample(_FOW_Trilinear_Clamp_Sampler, input.texCoord);

			    fixed3 color;
	    	#ifdef FOWSHADOWTYPE_SDF
		    	color = bgColor * max(fogValue.r, _FogValue);
		    #else
				color = lerp(_FogColor.rgb, fixed3(1, 1, 1), fogValue.r * _FogColor.a);

			    // Mixed between last and current frame fog texture
				fixed visual = lerp(fogValue.b, fogValue.g, _LerpValue);
				color = lerp(color, fixed3(1, 1, 1), visual);
				color *= bgColor;
		    #endif
			    return fixed4(color, 1.0f);
			}
			ENDCG
		}
	}
}
