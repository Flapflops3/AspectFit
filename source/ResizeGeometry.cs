using System.Runtime.InteropServices;

namespace AspectWindow;

[StructLayout(LayoutKind.Sequential)]
public struct Rect(int left, int top, int right, int bottom)
{
    public int Left = left, Top = top, Right = right, Bottom = bottom;
    public readonly int Width => Right - Left;
    public readonly int Height => Bottom - Top;
}

public static class ResizeGeometry
{
    public static Rect Constrain(Rect proposed, Rect start, int edge, int frameWidth,
        int frameHeight, double ratio, int minWidth, int minHeight)
    {
        if (edge < 1 || edge > 8 || !double.IsFinite(ratio) || ratio <= 0)
            return proposed;
        double width = Math.Max(1, proposed.Width - frameWidth);
        double height = Math.Max(1, proposed.Height - frameHeight);
        bool vertical = edge is 3 or 6;
        if (edge is 4 or 5 or 7 or 8)
            vertical = Math.Abs(proposed.Height - start.Height) * ratio > Math.Abs(proposed.Width - start.Width);
        if (vertical) width = height * ratio;
        width = Math.Max(width, Math.Max(minWidth, minHeight * ratio));
        int w = (int)Math.Round(width) + frameWidth;
        int h = (int)Math.Round(width / ratio) + frameHeight;
        if (edge is 1 or 4 or 7) proposed.Left = proposed.Right - w;
        else if (edge is 2 or 5 or 8) proposed.Right = proposed.Left + w;
        else
        {
            proposed.Left = start.Left + (start.Width - w) / 2;
            proposed.Right = proposed.Left + w;
        }
        if (edge is 3 or 4 or 5) proposed.Top = proposed.Bottom - h;
        else if (edge is 6 or 7 or 8) proposed.Bottom = proposed.Top + h;
        else
        {
            proposed.Top = start.Top + (start.Height - h) / 2;
            proposed.Bottom = proposed.Top + h;
        }
        return proposed;
    }
}
