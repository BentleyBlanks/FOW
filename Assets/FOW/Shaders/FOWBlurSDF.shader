Shader "Hidden/FOWBlurSDF"
{
	Properties {}

	SubShader 
	{
		Pass 
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
		    Texture2D _FOWTexture;
			SamplerState _FOW_Trilinear_Clamp_Sampler;

			float _BlurLevel;
			float _PlayerRadius;
			float2 _PlayerPos;
			float2 _TextureTexelSize;

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

			fragInput vert(vertInput input)
			{
			    fragInput output;
			        
			    output.positionCS = UnityObjectToClipPos(input.vertex);
			    output.texCoord   = input.texCoord;
                
			    return output;
			}

			fixed4 frag (fragInput input) : SV_Target
			{
				float2 position = input.texCoord;
				float distance = length(_PlayerPos - position) * _BlurLevel;

				float4 uv01 = input.texCoord.xyxy + distance * float4(0, 1, 0, -1) * _TextureTexelSize.xyxy;
				float4 uv10 = input.texCoord.xyxy + distance * float4(1, 0, -1, 0) * _TextureTexelSize.xyxy;
				float4 uv23 = input.texCoord.xyxy + distance * float4(0, 1, 0, -1) * _TextureTexelSize.xyxy * 2.0;
				float4 uv32 = input.texCoord.xyxy + distance * float4(1, 0, -1, 0) * _TextureTexelSize.xyxy * 2.0;
				float4 uv45 = input.texCoord.xyxy + distance * float4(0, 1, 0, -1) * _TextureTexelSize.xyxy * 3.0;
				float4 uv54 = input.texCoord.xyxy + distance * float4(1, 0, -1, 0) * _TextureTexelSize.xyxy * 3.0;

				fixed4 c = fixed4(0, 0, 0, 0);

				c += 0.4 * _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, input.texCoord);
				c += 0.075 * _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv01.xy);
				c += 0.075 * _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv01.zw);
				c += 0.075 * _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv10.xy);
				c += 0.075 * _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv10.zw);
				c += 0.05 * _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv23.xy);
				c += 0.05 * _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv23.zw);
				c += 0.05 * _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv32.xy);
				c += 0.05 * _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv32.zw);
				c += 0.025 * _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv45.xy);
				c += 0.025 * _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv45.zw);
				c += 0.025 * _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv54.xy);
				c += 0.025 * _FOWTexture.Sample(_FOW_Trilinear_Clamp_Sampler, uv54.zw);

				return c;
			}

			ENDCG
		}
	} 
	FallBack off
}
