void Luminance(float3 Color, out float Result)
{
    Result = dot(Color, float3(0.2126, 0.7152, 0.0722));
}
