using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SkiaSharp;
using Svg.Skia;

namespace IconTooling;

/// <summary>
/// Converts an SVG document into a Windows ICO file containing every standard
/// icon resolution (16x16 through 256x256), using PNG-compressed entries.
/// </summary>
public sealed class SvgToIcoConverter
{
    // Standard Windows shell icon sizes; 256x256 is the largest size the
    // ICO container supports (encoded as 0 in the directory width/height).
    private static readonly int[] Sizes = [16, 24, 32, 48, 64, 128, 256];

    public byte[] Convert(string svgContent)
    {
        using var svg = new SKSvg();
        svg.Load(svgContent);
        return Convert(svg);
    }

    public byte[] Convert(Stream svgStream)
    {
        ArgumentNullException.ThrowIfNull(svgStream);

        using var svg = new SKSvg();
        svg.Load(svgStream);
        return Convert(svg);
    }

    public void Convert(string svgContent, Stream output)
    {
        var ico = Convert(svgContent);
        output.Write(ico, 0, ico.Length);
    }

    public void ConvertFile(string svgPath, string icoPath)
    {
        var ico = Convert(svgPath);
        File.WriteAllBytes(icoPath, ico);
    }

    private byte[] Convert(SKSvg svg)
    {
        var picture = svg.Picture;
        if (picture is null)
            throw new InvalidOperationException("The SVG could not be parsed into a drawable image.");

        var pngs = new byte[Sizes.Length][];

        for (var i = 0; i < Sizes.Length; i++)
            pngs[i] = Rasterize(picture, Sizes[i]);

        return Pack(pngs);
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

    private static byte[] Pack(IReadOnlyList<byte[]> pngs)
    {
        const int directoryEntrySize = 16;
        var dataOffset = 6 + directoryEntrySize * pngs.Count;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write((ushort)0);            // reserved
        writer.Write((ushort)1);            // type: icon
        writer.Write((ushort)pngs.Count);   // image count

        var offset = dataOffset;
        for (var i = 0; i < pngs.Count; i++)
        {
            var size = Sizes[i];
            var width = (byte)(size >= 256 ? 0 : size);
            var height = (byte)(size >= 256 ? 0 : size);

            writer.Write(width);                // bWidth (0 means 256)
            writer.Write(height);               // bHeight (0 means 256)
            writer.Write((byte)0);              // bColorCount
            writer.Write((byte)0);              // bReserved
            writer.Write((ushort)1);            // wPlanes
            writer.Write((ushort)32);           // wBitCount
            writer.Write((uint)pngs[i].Length); // dwBytesInRes
            writer.Write((uint)offset);         // dwImageOffset

            offset += pngs[i].Length;
        }

        foreach (var png in pngs)
            writer.Write(png);

        writer.Flush();
        return stream.ToArray();
    }
}
