Shader "Hidden/FOWBlur"
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

			sampler2D _FOWTexture;
			float2 _TextureTexelSize;
			float _Offset;

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
				// return fixed4(tex2D(_FOWTexture, input.texCoord).rgb, 1.0f);
				
				float4 uv01 = input.texCoord.xyxy + _Offset * float4(0, 1, 0, -1) * _TextureTexelSize.xyxy;
				float4 uv10 = input.texCoord.xyxy + _Offset * float4(1, 0, -1, 0) * _TextureTexelSize.xyxy;
				float4 uv23 = input.texCoord.xyxy + _Offset * float4(0, 1, 0, -1) * _TextureTexelSize.xyxy * 2.0;
				float4 uv32 = input.texCoord.xyxy + _Offset * float4(1, 0, -1, 0) * _TextureTexelSize.xyxy * 2.0;
				float4 uv45 = input.texCoord.xyxy + _Offset * float4(0, 1, 0, -1) * _TextureTexelSize.xyxy * 3.0;
				float4 uv54 = input.texCoord.xyxy + _Offset * float4(1, 0, -1, 0) * _TextureTexelSize.xyxy * 3.0;

				fixed4 c = fixed4(0, 0, 0, 0);

				c += 0.4 * tex2D(_FOWTexture, input.texCoord);
				c += 0.075 * tex2D(_FOWTexture, uv01.xy);
				c += 0.075 * tex2D(_FOWTexture, uv01.zw);
				c += 0.075 * tex2D(_FOWTexture, uv10.xy);
				c += 0.075 * tex2D(_FOWTexture, uv10.zw);
				c += 0.05 * tex2D(_FOWTexture, uv23.xy);
				c += 0.05 * tex2D(_FOWTexture, uv23.zw);
				c += 0.05 * tex2D(_FOWTexture, uv32.xy);
				c += 0.05 * tex2D(_FOWTexture, uv32.zw);
				c += 0.025 * tex2D(_FOWTexture, uv45.xy);
				c += 0.025 * tex2D(_FOWTexture, uv45.zw);
				c += 0.025 * tex2D(_FOWTexture, uv54.xy);
				c += 0.025 * tex2D(_FOWTexture, uv54.zw);

				return c;
			}

			ENDCG
		}
	} 
	FallBack off
}
