
using System.Numerics;

namespace Computer;
public class GPU
{
    public byte X_REG { get; set; } = 0;    // X coordinate
    public byte Y_REG { get; set; } = 0;    // Y coordinate  
    public byte R_REG { get; set; } = 0;    // Red
    public byte G_REG { get; set; } = 0;    // Green
    public byte B_REG { get; set; } = 0;    // Blue

    public const int XWidth = 64;
    public const int YWidth = 64;

    // VMEM: XWidthxYWidth pixels, 3 bytes per pixel (RGB)
    private byte[] videoMemory = new byte[XWidth * YWidth * 3];
    public bool VideoMemoryDirty { get; set; } = false;

    public (int , int, Vector3) DrawPixel()
    {
        if (X_REG < XWidth && Y_REG < YWidth)
        {
            int offset = (Y_REG * XWidth + X_REG) * 3;
            videoMemory[offset] = R_REG;     // Red
            videoMemory[offset + 1] = G_REG; // Green  
            videoMemory[offset + 2] = B_REG; // Blue
            VideoMemoryDirty = true;
            return (X_REG, Y_REG, new(R_REG, B_REG, G_REG));
        }
        return (0, 0, Vector3.Zero);
    }

    public Color GetPixelColor(int x, int y)
    {
        if (x >= XWidth || y >= YWidth) return Color.Black;

        int offset = (y * XWidth + x) * 3;
        byte r = videoMemory[offset];
        byte g = videoMemory[offset + 1];
        byte b = videoMemory[offset + 2];
        return Color.FromArgb(255, r, g, b);
    }

    public void ClearScreen()
    {
        Array.Clear(videoMemory, 0, videoMemory.Length);
        VideoMemoryDirty = true;
    }
}