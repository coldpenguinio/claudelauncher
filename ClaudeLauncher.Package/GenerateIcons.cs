// Run this as a C# script or standalone app to generate placeholder icons
// dotnet run GenerateIcons.cs

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

class IconGenerator
{
    static void Main()
    {
        var sizes = new Dictionary<string, int>
        {
            { "SmallTile", 71 },
            { "Square44x44Logo", 44 },
            { "Square150x150Logo", 150 },
            { "Wide310x150Logo.width", 310 },
            { "Wide310x150Logo.height", 150 },
            { "Square310x310Logo", 310 },
            { "StoreLogo", 50 },
            { "LargeTile", 310 },
        };

        var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
        Directory.CreateDirectory(imagesPath);

        // Square icons
        GenerateIcon(Path.Combine(imagesPath, "SmallTile.png"), 71, 71);
        GenerateIcon(Path.Combine(imagesPath, "Square44x44Logo.png"), 44, 44);
        GenerateIcon(Path.Combine(imagesPath, "Square150x150Logo.png"), 150, 150);
        GenerateIcon(Path.Combine(imagesPath, "Square310x310Logo.png"), 310, 310);
        GenerateIcon(Path.Combine(imagesPath, "StoreLogo.png"), 50, 50);

        // Wide tile
        GenerateIcon(Path.Combine(imagesPath, "Wide310x150Logo.png"), 310, 150);

        // Also generate scaled versions
        GenerateIcon(Path.Combine(imagesPath, "Square44x44Logo.targetsize-44.png"), 44, 44);
        GenerateIcon(Path.Combine(imagesPath, "Square44x44Logo.targetsize-32.png"), 32, 32);
        GenerateIcon(Path.Combine(imagesPath, "Square44x44Logo.targetsize-24.png"), 24, 24);
        GenerateIcon(Path.Combine(imagesPath, "Square44x44Logo.targetsize-16.png"), 16, 16);

        Console.WriteLine("Icons generated successfully!");
    }

    static void GenerateIcon(string path, int width, int height)
    {
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        // Background - Claude's orange/terracotta color
        graphics.Clear(Color.FromArgb(255, 204, 102, 51));

        // Draw "C" for Claude
        var fontSize = Math.Min(width, height) * 0.6f;
        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);

        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        graphics.DrawString("C", font, brush, new RectangleF(0, 0, width, height), format);

        bitmap.Save(path, ImageFormat.Png);
        Console.WriteLine($"Created: {path}");
    }
}
