// single authority for the viewport size in code
public static class ViewportConfig
{
    public const int PixelsWide = 320;
    public const int PixelsHigh = 180;
    public const float PixelsPerUnit = 16f;

    public const float Width = PixelsWide / PixelsPerUnit;
    public const float Height = PixelsHigh / PixelsPerUnit;
    public const float HalfWidth = Width * 0.5f;
    public const float HalfHeight = Height * 0.5f;
}
