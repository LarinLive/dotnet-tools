using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LarinLive.DotnetTools.Icons;

public sealed class WindowsIconImageDef : ImageDefBase { }

/// <summary>
/// Converts an SVG image into a Windows ICO file containing every standard icon resolution (16x16 through 256x256), using PNG-compressed entries.
/// </summary>
public sealed class SvgToIcoConverter : SvgConverterBase<WindowsIconImageDef>
{
    /// <summary>
	/// Standard Windows shell icon sizes; 256x256 is the largest size the ICO container supports (encoded as 0 in the directory width/height).
	/// </summary>
    private static readonly WindowsIconImageDef[] _defs = [.. new int[] { 16, 24, 32, 48, 64, 128, 256 }.Select(s => new WindowsIconImageDef() { SizeInPixels = s })];

	public SvgToIcoConverter(Stream source) : base(source) { }

	public void ConvertTo(Stream destination)
	{
		Save(_defs, [], destination);
	}

	protected override void DoSave(IReadOnlyList<Image<WindowsIconImageDef>> images, Stream destination)
    {
        const int directoryEntrySize = 16;
        var dataOffset = 6 + directoryEntrySize * images.Count;

        using var writer = new BinaryWriter(destination, Encoding.UTF8, true);

        writer.Write((ushort)0); // reserved
        writer.Write((ushort)1); // type: icon
        writer.Write((ushort)images.Count); // image count

        var offset = dataOffset;
        for (var i = 0; i < images.Count; i++)
        {
            var size = _defs[i].SizeInPixels;
            var width = (byte)(size >= 256 ? 0 : size);
            var height = (byte)(size >= 256 ? 0 : size);

            writer.Write(width); // bWidth (0 means 256)
            writer.Write(height); // bHeight (0 means 256)
            writer.Write((byte)0); // bColorCount
            writer.Write((byte)0); // bReserved
            writer.Write((ushort)1); // wPlanes
            writer.Write((ushort)32); // wBitCount
            writer.Write((uint)images[i].Data.Length); // dwBytesInRes
            writer.Write((uint)offset); // dwImageOffset

            offset += images[i].Data.Length;
	}

        foreach (var image in images)
            writer.Write(image.Data);
    }

	protected override ImageEncodingRules GetImageEncodingRules() => new(SKEncodedImageFormat.Png, 100);
}
