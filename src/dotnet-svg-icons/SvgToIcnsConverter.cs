using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SkiaSharp;
using Svg.Skia;

namespace IconTooling;

/// <summary>
/// Converts an SVG document into an Apple ICNS icon file containing every
/// resolution supported by modern macOS (16x16 through 1024x1024, including
/// the @2x retina variants).
/// </summary>
public sealed class SvgToIcnsConverter
{
    // Modern PNG-based ICNS entries. The type is an OSType (4 ASCII bytes);
    // the pixel size is the actual rasterized dimensions of the entry.
    // icp4..icp6 are the 16/32/64px legacy slots, ic07..ic10 are the standard
    // slots, and ic11..ic14 are the retina (2x) variants of the smaller sizes.
    private static readonly IconVariant[] Variants =
    {
        new("icp4", 16),   // 16x16
        new("icp5", 32),   // 32x32
        new("icp6", 64),   // 64x64
        new("ic07", 128),  // 128x128
        new("ic08", 256),  // 256x256
        new("ic09", 512),  // 512x512
        new("ic10", 1024), // 1024x1024
        new("ic11", 32),   // 16x16@2x
        new("ic12", 64),   // 32x32@2x
        new("ic13", 256),  // 128x128@2x
        new("ic14", 512),  // 256x256@2x
    };


    public byte[] Convert(string svgContent, byte[] darkContent, bool icnsHeader)
    {
        using var svg = new SKSvg();
        svg.Load(svgContent);
        var picture = svg.Picture;
        if (picture is null)
            throw new InvalidOperationException("The SVG could not be parsed into a drawable image.");

        var pngCache = new Dictionary<int, byte[]>();
        var entries = new List<IconEntry>(Variants.Length);

        foreach (var variant in Variants)
        {
            if (!pngCache.TryGetValue(variant.PixelSize, out var png))
            {
                png = Rasterize(picture, variant.PixelSize);
                pngCache[variant.PixelSize] = png;
            }

            entries.Add(new IconEntry(Encoding.ASCII.GetBytes(variant.Type), png));
        }

        if (darkContent.Length > 0)
            entries.Add(new IconEntry([0xFD, 0xD9, 0x2F, 0xA8], darkContent));

        return Pack(entries, icnsHeader);
    }

    public void ConvertFile(string svgPath, string? darkSvgPath, string icnsPath)
    {
        byte[] darkIcns = darkSvgPath != null ? Convert(darkSvgPath, [], false) : [];
        var icns = Convert(svgPath, darkIcns, true);
        File.WriteAllBytes(icnsPath, icns);
    }

    private static byte[] Rasterize(SKPicture picture, int size)
    {
        var bounds = picture.CullRect;

        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new InvalidOperationException(
                "The SVG has no intrinsic size; add a viewBox or an explicit width/height.");

        using var bitmap = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        // Fit the SVG into the square while preserving its aspect ratio,
        // letterboxing with transparency when the source is not square.
        var scale = Math.Min(size / bounds.Width, size / bounds.Height);
        var offsetX = (size - bounds.Width * scale) / 2f;
        var offsetY = (size - bounds.Height * scale) / 2f;

        canvas.Translate(offsetX, offsetY);
        canvas.Scale(scale, scale);
        canvas.Translate(-bounds.Left, -bounds.Top);
        canvas.DrawPicture(picture);
        canvas.Flush();

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static byte[] Pack(IReadOnlyList<IconEntry> entries, bool icnsHeader)
    {
        using var stream = new MemoryStream();

        if (icnsHeader)
        {
            WriteAscii(stream, "icns");
            WriteUInt32BigEndian(stream, 0); // total size placeholder
        }

        foreach (var entry in entries)
        {
            stream.Write(entry.Type, 0, entry.Type.Length);
            WriteUInt32BigEndian(stream, checked((uint)(entry.PngData.Length + 8)));
            stream.Write(entry.PngData, 0, entry.PngData.Length);
        }

        var totalSize = checked((uint)stream.Length);
        var bytes = stream.GetBuffer();
        if (icnsHeader)
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4, 4), totalSize);

        return bytes.AsSpan(0, (int)stream.Length).ToArray();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        Span<byte> buffer = stackalloc byte[4];
        Encoding.ASCII.GetBytes(value.AsSpan(), buffer);
        stream.Write(buffer);
    }

    private static void WriteUInt32BigEndian(Stream stream, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private readonly record struct IconVariant(string Type, int PixelSize);

    private readonly record struct IconEntry(byte[] Type, byte[] PngData);
}
