void SelectColor_float(float4 TextureColor, float4 VertexColor, float2 uv, out float4 OutputColor)
{
    OutputColor = any(uv.x < -1) ? VertexColor : TextureColor;
}