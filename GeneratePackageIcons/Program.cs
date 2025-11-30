using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "ClaudeLauncher.Package", "Images");
Directory.CreateDirectory(imagesPath);

// Square icons
GenerateIcon(Path.Combine(imagesPath, "SmallTile.png"), 71, 71);
GenerateIcon(Path.Combine(imagesPath, "Square44x44Logo.png"), 44, 44);
GenerateIcon(Path.Combine(imagesPath, "Square150x150Logo.png"), 150, 150);
GenerateIcon(Path.Combine(imagesPath, "Square310x310Logo.png"), 310, 310);
GenerateIcon(Path.Combine(imagesPath, "StoreLogo.png"), 50, 50);

// Wide tile
GenerateIcon(Path.Combine(imagesPath, "Wide310x150Logo.png"), 310, 150);

// Scaled versions for taskbar/start menu
GenerateIcon(Path.Combine(imagesPath, "Square44x44Logo.targetsize-256_altform-unplated.png"), 256, 256);
GenerateIcon(Path.Combine(imagesPath, "Square44x44Logo.targetsize-48_altform-unplated.png"), 48, 48);
GenerateIcon(Path.Combine(imagesPath, "Square44x44Logo.targetsize-32_altform-unplated.png"), 32, 32);
GenerateIcon(Path.Combine(imagesPath, "Square44x44Logo.targetsize-24_altform-unplated.png"), 24, 24);
GenerateIcon(Path.Combine(imagesPath, "Square44x44Logo.targetsize-16_altform-unplated.png"), 16, 16);

// Also generate the app icon for the main project
var appIconPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "ClaudeLauncher", "Resources");
Directory.CreateDirectory(appIconPath);
GenerateIco(Path.Combine(appIconPath, "app.ico"));

Console.WriteLine("\nAll icons generated successfully!");

void GenerateIcon(string path, int width, int height)
{
    using var bitmap = new Bitmap(width, height);
    using var graphics = Graphics.FromImage(bitmap);

    graphics.SmoothingMode = SmoothingMode.AntiAlias;
    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

    // Background - Claude's orange/terracotta color
    graphics.Clear(Color.FromArgb(255, 204, 102, 51));

    // Draw "C" for Claude
    var fontSize = Math.Min(width, height) * 0.55f;
    using var font = new Font("Segoe UI", fontSize, FontStyle.Bold);
    using var brush = new SolidBrush(Color.White);

    var format = new StringFormat
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };

    graphics.DrawString("C", font, brush, new RectangleF(0, 0, width, height), format);

    bitmap.Save(path, ImageFormat.Png);
    Console.WriteLine($"Created: {Path.GetFileName(path)} ({width}x{height})");
}

void GenerateIco(string path)
{
    // Create multiple sizes for the ICO file
    var sizes = new[] { 16, 32, 48, 256 };

    using var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms);

    // ICO header
    bw.Write((short)0);           // Reserved
    bw.Write((short)1);           // Type: 1 = ICO
    bw.Write((short)sizes.Length); // Number of images

    var imageDataList = new List<byte[]>();
    var offset = 6 + (sizes.Length * 16); // Header + directory entries

    // Directory entries
    foreach (var size in sizes)
    {
        using var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.Clear(Color.FromArgb(255, 204, 102, 51));

        var fontSize = size * 0.55f;
        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);

        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        graphics.DrawString("C", font, brush, new RectangleF(0, 0, size, size), format);

        using var pngStream = new MemoryStream();
        bitmap.Save(pngStream, ImageFormat.Png);
        var pngData = pngStream.ToArray();
        imageDataList.Add(pngData);

        bw.Write((byte)(size == 256 ? 0 : size)); // Width (0 = 256)
        bw.Write((byte)(size == 256 ? 0 : size)); // Height (0 = 256)
        bw.Write((byte)0);         // Color palette
        bw.Write((byte)0);         // Reserved
        bw.Write((short)1);        // Color planes
        bw.Write((short)32);       // Bits per pixel
        bw.Write(pngData.Length);  // Size of image data
        bw.Write(offset);          // Offset to image data

        offset += pngData.Length;
    }

    // Image data
    foreach (var imageData in imageDataList)
    {
        bw.Write(imageData);
    }

    File.WriteAllBytes(path, ms.ToArray());
    Console.WriteLine($"Created: {Path.GetFileName(path)} (multi-size ICO)");
}
