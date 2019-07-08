Shader "Hidden/FOWShadowSDF"
{
    Properties {}

	SubShader
	{
		Pass
		{
			CGPROGRAM

			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag

			// Scene dependent parameter
			float _CutOff;
			float _Luminance;
			float _StepScale;
			float _StepMinValue;
			
			float _PlayerRadius;
		    float2 _PlayerPos;
		    float2 _TextureSizeScale;

		    Texture2D _SDFTexture;
			SamplerState _FOW_Trilinear_Clamp_Sampler;

			struct vertInput
			{
				float4 vertex 	: POSITION;
				fixed2 texCoord : TEXCOORD0;
			};

			struct fragInput
			{
				float4 position : SV_POSITION;
				float2 texCoord : TEXCOORD0;
			};

			fragInput vert(vertInput input)
			{
				fragInput output;

                output.texCoord = input.texCoord;
				output.position = UnityObjectToClipPos(input.vertex);

				return output;
			}

			float fillMask(float dist)
			{
				return clamp(-dist, 0.0, 1.0);
			}

			float circleDist(float2 p, float radius)
			{
				return length(p) - radius;
			}

			float luminance(float4 col)
			{
				return 0.2126 * col.r + 0.7152 * col.g + 0.0722 * col.b;
			}

			void setLuminance(inout float4 col, float lum)
			{
				lum /= luminance(col);
				col *= lum;
			}

            float drawShadertoyShadow(float2 p, float2 pos, float radius)
			{
				float2 dir = normalize(pos - p);
				float dl = length(p - pos);

				// fraction of light visible, starts at one radius (second half added in the end);
				float lf = radius * dl;
				
				// distance traveled
				float dt = 0.01;
			    
				for (int i = 0; i < 64; ++i)
				{
					// distance to scene at current position
			    	// float sd = tex2D(_SDFTexture, p);
			    	float sd = _SDFTexture.Sample(_FOW_Trilinear_Clamp_Sampler, p).r;
			        
			        // early out when this ray is guaranteed to be full shadow
			        if (sd <= 0.0) 
			            return 0.0;
			        
					// width of cone-overlap at light
					// 0 in center, so 50% overlap: add one radius outside of loop to get total coverage
					// should be '(sd / dt) * dl', but '*dl' outside of loop
					lf = min(lf, sd / dt);
					
					// move ahead
			        dt += max(1.0, abs(sd)); 
			        
					if (dt > dl) break;
				}

				// multiply by dl to get the real projected overlap (moved out of loop)
				// add one radius, before between -radius and + radius
				// normalize to 1 ( / 2*radius)
				lf = clamp((lf * dl + radius) / (2.0 * radius), 0.0, 1.0);
				lf = smoothstep(0.0, 1.0, lf);
				return lf;
			}

            float drawShadow(float2 uv, float2 lightPos)
			{
			    float2 direction = normalize(lightPos - uv);
				float2 p = uv;
			 	float distanceToLight = length(uv - lightPos);
			    float distance = 0.0f;
			    
			    for(int i = 0; i < 32; i++)
			    {
			    	float s = _SDFTexture.Sample(_FOW_Trilinear_Clamp_Sampler, p).r;
			        
			        if(s <= 0.00001) return 0.0;
			        
			        if(distance > distanceToLight)
			            return 1.0;
			        
			        distance += max(s * _StepScale, _StepMinValue);
			        p = uv + direction * distance;
			    }
			    
			    return 0.0;
			}

			float4 drawLight(float2 uv, float2 lightPos, float4 lightColor, float lightRadius)
			{
				// distance to light
				float distanceToLight = length((uv - lightPos) * _TextureSizeScale);
				
				// out of range
				if (distanceToLight > lightRadius) 
					return float4(0.0f, 0.0f, 0.0f, 0.0f);
				
				// shadow and falloff
				// float shadow = drawShadertoyShadow(p, lightPos, 0.001);
			    float shadow = drawShadow(uv, lightPos);
			    
				float fall = (lightRadius - distanceToLight) / lightRadius;
				fall *= fall;
				return (shadow * fall) * lightColor;
			}

			float4 frag(fragInput input) : SV_TARGET
			{
                float2 uv = input.texCoord;
                // float minLightDistance = tex2D(_SDFTexture, uv);
			    float minLightDistance = _SDFTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv).r;
			    // float4 value = _SDFTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv);
			    // return float4(length(uv - value.zw).xxx, 1.0f);
			    // return float4(minLightDistance.xxx, 1.0f);

                float4 lightColor = float4(1.0f, 1.0f, 1.0f, 1.0f);
				setLuminance(lightColor, _Luminance);

                float4 color = float4(0.0f, 0.0f, 0.0f, 1.0f);
				color += drawLight(uv, _PlayerPos, lightColor, _PlayerRadius);
				color = lerp(color, float4(1.0, 0.4, 0.0, 1.0), fillMask(minLightDistance));

                return color;
			}

			ENDCG
		}
	}
}