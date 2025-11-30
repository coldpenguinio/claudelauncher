using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

namespace ClaudeLauncher;

/// <summary>
/// Generates a simple icon programmatically when no icon file is present.
/// Run GenerateIcon() once to create the icon file, then it can be embedded as a resource.
/// </summary>
public static class IconGenerator
{
    public static Icon CreateIcon()
    {
        // Create a 32x32 bitmap for the icon
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        // Background - Claude's orange/terracotta color
        using var backgroundBrush = new SolidBrush(Color.FromArgb(255, 204, 102, 51));
        graphics.FillEllipse(backgroundBrush, 1, 1, 30, 30);

        // Draw "C" for Claude
        using var font = new Font("Segoe UI", 18, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);

        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        graphics.DrawString("C", font, textBrush, new RectangleF(0, 0, 32, 32), format);

        // Convert to icon
        return Icon.FromHandle(bitmap.GetHicon());
    }

    public static void SaveIconToFile(string path)
    {
        using var icon = CreateIcon();
        using var stream = new FileStream(path, FileMode.Create);

        // Write ICO header
        var writer = new BinaryWriter(stream);
        writer.Write((short)0);      // Reserved
        writer.Write((short)1);      // Type: 1 = ICO
        writer.Write((short)1);      // Number of images

        // Write ICO directory entry
        writer.Write((byte)32);      // Width
        writer.Write((byte)32);      // Height
        writer.Write((byte)0);       // Color palette
        writer.Write((byte)0);       // Reserved
        writer.Write((short)1);      // Color planes
        writer.Write((short)32);     // Bits per pixel

        // We need to write the bitmap data
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.Clear(Color.Transparent);

        using var backgroundBrush = new SolidBrush(Color.FromArgb(255, 204, 102, 51));
        graphics.FillEllipse(backgroundBrush, 1, 1, 30, 30);

        using var font = new Font("Segoe UI", 18, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);

        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        graphics.DrawString("C", font, textBrush, new RectangleF(0, 0, 32, 32), format);

        // Save as PNG to memory stream to get the data
        using var pngStream = new MemoryStream();
        bitmap.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
        var pngData = pngStream.ToArray();

        writer.Write(pngData.Length);  // Size of image data
        writer.Write(22);               // Offset to image data (6 + 16 = 22)

        writer.Write(pngData);
    }
}
