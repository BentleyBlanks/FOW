Shader "Hidden/FOWJumpFloodSDF"
{
	Properties {}
	SubShader 
	{
		// SDF Initialization
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
		    Texture2D _MapDataTexture;
			SamplerState _SDFTexture_Trilinear_Clamp_Sampler;
	
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

			fragInput vert(vertInput input)
			{
			    fragInput output;
			        
			    output.positionCS = UnityObjectToClipPos(input.vertex);
			    output.texCoord   = input.texCoord;
                
			    return output;
			}

			float4 frag (fragInput input) : SV_Target
			{
				float2 uv = input.texCoord;
				float4 value = _MapDataTexture.Sample(_SDFTexture_Trilinear_Clamp_Sampler, uv);
				// Mapdata only contains Green and Red, see Red as seed for next computation pass
				float2 color = float2(0.0f, 1.0f);
				float2 coord = float2(0.0f, 0.0f);

				// Seed
				if(value.x >= 0.9f && value.y <= 0.1f)
				{
					color = float2(1.0f, 0.0f);
					coord = uv;
				}
				return float4(color, coord);
			}

			ENDCG
		}

		// SDF Generation
		Pass 
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
		    Texture2D _SDFTexture;
			SamplerState _SDFTexture_Trilinear_Clamp_Sampler;
	
			// Update with every ping-pong step			
			float2 _TexelSize;
			float _Power;
			float _Level;

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

			fragInput vert(vertInput input)
			{
			    fragInput output;
			        
			    output.positionCS = UnityObjectToClipPos(input.vertex);
			    output.texCoord   = input.texCoord;
                
			    return output;
			}

			float4 frag (fragInput input) : SV_Target
			{
				float minDistance = 99999.0;
				float2 minCoord = float2(0.0f, 0.0f);
				float2 minColor = float2(0.0f, 1.0f);
				float stepwidth = floor(exp2(_Power - _Level) + 0.5);
				// float stepwidth = _Power - _Level + 1;

			    for (int y = -1; y <= 1; ++y) 
			    {
        			for (int x = -1; x <= 1; ++x) 
        			{
						float2 uv = input.texCoord + float2(x, y) * _TexelSize * stepwidth;

						// Sample semi-finished sdf texture
						float4 value     = _SDFTexture.Sample(_SDFTexture_Trilinear_Clamp_Sampler, uv);
						float2 seedColor = value.xy;
						float2 seedCoord = value.zw;
						float distance = length(input.texCoord - seedCoord);
						// only need RG
						if ((seedCoord.x != 0.0 || seedCoord.y != 0.0) && distance < minDistance)
			            {
							minDistance = distance;
							minCoord    = seedCoord;
							minColor    = seedColor;
			            }
        			}
    			}

				return float4(minColor, minCoord);
			}

			ENDCG
		}

		// SDF Final Gathering
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
		    Texture2D _SDFFinalTexture;
			SamplerState _Texture_Trilinear_Clamp_Sampler;
	
			// Update with every ping-pong step			
			float2 _TexelSize;
			float _Power;
			float _Level;

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

			fragInput vert(vertInput input)
			{
			    fragInput output;
			        
			    output.positionCS = UnityObjectToClipPos(input.vertex);
			    output.texCoord   = input.texCoord;
                
			    return output;
			}

			float4 frag (fragInput input) : SV_Target
			{
				float2 uv = input.texCoord;
				float4 value = _SDFFinalTexture.Sample(_Texture_Trilinear_Clamp_Sampler, uv);
				float distance = length(uv - value.zw);
				return float4(distance.xxx, 1.0f);
			}

			ENDCG
		}
	} 
	FallBack off
}
