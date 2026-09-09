using SkiaSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LarinLive.DotnetTools.SvgIcons;

public sealed class MacOsIconImageDef : ImageDefBase
{
	public MacOsIconImageDef(string type, int sizeInPixels)
	{
		Type = Encoding.ASCII.GetBytes(type);
		SizeInPixels = sizeInPixels;
	}

	public MacOsIconImageDef(byte[] type, int sizeInPixels)
	{
		Type = type;
		SizeInPixels = sizeInPixels;
	}

	/// <summary>
	/// The type is an OSType (4 ASCII bytes); the pixel size is the actual rasterized dimensions of the entry.
	/// icp4..icp6 are the 16/32/64px legacy slots, ic07..ic10 are the standard slots, and ic11..ic14 are the retina (2x) variants of the smaller sizes.
	/// FD D9 2F A8	- nested "dark" icns file. Allows automatic icon switching in Dark mode.
	/// </summary>
	public byte[] Type { get; }
}

/// <summary>
/// Converts an SVG iamge into an Apple ICNS icon file containing every resolution supported by modern macOS (16x16 through 1024x1024, including the @2x retina variants).
/// </summary>
public sealed class SvgToIcnsConverter : SvgConverterBase<MacOsIconImageDef>
{
	private readonly bool _writeIcnsHeader;

	private SvgToIcnsConverter(Stream source, bool writeHeader) : base(source) 
	{
		_writeIcnsHeader = writeHeader;
	}

	public SvgToIcnsConverter(Stream source) : this(source, true) { }

	private static readonly MacOsIconImageDef[] _defs =
	[
        new("icp4", 16), // 16x16
        new("icp5", 32), // 32x32
        new("icp6", 64), // 64x64
        new("ic07", 128), // 128x128
        new("ic08", 256), // 256x256
        new("ic09", 512), // 512x512
        new("ic10", 1024), // 1024x1024
        new("ic11", 32), // 16x16@2x
        new("ic12", 64), // 32x32@2x
        new("ic13", 256), // 128x128@2x
        new("ic14", 512) // 256x256@2x
    ];

	public void ConvertTo(Stream destination)
	{
		Save(_defs, [], destination);
	}

	protected override void DoSave(IReadOnlyList<Image<MacOsIconImageDef>> images, Stream destination)
    {
		var totalSize = sizeof(uint) + sizeof(uint) + images.Sum(i => i.ImageDef.Type.Length + sizeof(uint) + i.Data.Length);

		// ICNS file header
		if (_writeIcnsHeader)
		{
			WriteUInt32BigEndian(destination, 0x69636E73U); // icns magic field
			WriteUInt32BigEndian(destination, checked((uint)totalSize));
		}

		// ICNS file data
        foreach (var image in images)
        {
            destination.Write(image.ImageDef.Type);
            WriteUInt32BigEndian(destination, checked((uint)(image.ImageDef.Type.Length + sizeof(uint) + image.Data.Length)));
			destination.Write(image.Data);
        }
    }


	protected override ImageEncodingRules GetImageEncodingRules() => new(SKEncodedImageFormat.Png, 100);
	
    private static void WriteUInt32BigEndian(Stream destination, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        destination.Write(buffer);
    }
}
