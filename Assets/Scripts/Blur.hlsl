#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

void Blur_float(UnityTexture2D _MainTex, float2 uv, float blur, out float4 Out )
{
	float offset = blur * 0.0625f;

	float4 color = tex2D(_MainTex, float2(uv.x, uv.y)) * 0.147761f;
	
	//  Top left 
	color += tex2D(_MainTex, float2(uv.x - offset, uv.y - offset)) * 0.0947416f;
	//  On 
	color += tex2D(_MainTex, float2(uv.x, uv.y - offset)) * 0.118318f;
	//  The upper right 
	color += tex2D(_MainTex, float2(uv.x + offset, uv.y + offset)) * 0.0947416f;
	//  Left 
	color += tex2D(_MainTex, float2(uv.x - offset, uv.y)) * 0.118318f;
	//  Right 
	color += tex2D(_MainTex, float2(uv.x + offset, uv.y)) * 0.118318f;
	//  The lower left 
	color += tex2D(_MainTex, float2(uv.x - offset, uv.y + offset)) * 0.0947416f;
	//  Next 
	color += tex2D(_MainTex, float2(uv.x, uv.y + offset)) * 0.118318f;
	//  The lower right 
	color += tex2D(_MainTex, float2(uv.x + offset, uv.y - offset)) * 0.0947416f;

	Out = color;
}

#endif //MYHLSLINCLUDE_INCLUDED