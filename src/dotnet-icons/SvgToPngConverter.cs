using SkiaSharp;
using System.Collections.Generic;
using System.IO;

namespace LarinLive.DotnetTools.Icons;

public sealed class PngImageDef : PngImageDefBase 
{
	public static readonly PngImageDef[] IosAppIconPngSet =
		[
			new (){ SizeInPixels = 1024, ColorType = SKColorType.Rgb888x, AlphaType = SKAlphaType.Opaque },
			new (){ SizeInPixels = 180, ColorType = SKColorType.Rgb888x, AlphaType = SKAlphaType.Opaque },
			new (){ SizeInPixels = 120, ColorType = SKColorType.Rgb888x, AlphaType = SKAlphaType.Opaque },
			new (){ SizeInPixels = 167, ColorType = SKColorType.Rgb888x, AlphaType = SKAlphaType.Opaque },
			new (){ SizeInPixels = 152, ColorType = SKColorType.Rgb888x, AlphaType = SKAlphaType.Opaque },
			new (){ SizeInPixels = 120, ColorType = SKColorType.Rgb888x, AlphaType = SKAlphaType.Opaque },
			new (){ SizeInPixels = 80, ColorType = SKColorType.Rgb888x, AlphaType = SKAlphaType.Opaque },
			new (){ SizeInPixels = 87, ColorType = SKColorType.Rgb888x, AlphaType = SKAlphaType.Opaque },
			new (){ SizeInPixels = 58, ColorType = SKColorType.Rgb888x, AlphaType = SKAlphaType.Opaque }
		];
	public static readonly PngImageDef[] AndroidAppIconPngSet =
		[
			new (){ SizeInPixels = 512, ColorType = SKColorType.Rgba8888, AlphaType = SKAlphaType.Premul },
			new (){ SizeInPixels = 48, ColorType = SKColorType.Rgba8888, AlphaType = SKAlphaType.Premul },
			new (){ SizeInPixels = 72, ColorType = SKColorType.Rgba8888, AlphaType = SKAlphaType.Premul },
			new (){ SizeInPixels = 96, ColorType = SKColorType.Rgba8888, AlphaType = SKAlphaType.Premul },
			new (){ SizeInPixels = 144, ColorType = SKColorType.Rgba8888, AlphaType = SKAlphaType.Premul },
			new (){ SizeInPixels = 192, ColorType = SKColorType.Rgba8888, AlphaType = SKAlphaType.Premul }
		];
}

/// <summary>
/// Converts an SVG image into a PNG file.
/// </summary>
public sealed class SvgToPngConverter : SvgConverterBase<PngImageDef>
{
	public SvgToPngConverter(Stream source) : base(source) { }

	public void ConvertTo(Stream destination, PngImageDef imageDef)
	{
		Save([imageDef], [], destination);
	}

	protected override void DoSave(IReadOnlyList<Image<PngImageDef>> images, Stream destination)
    {
		destination.Write(images[0].Data);
    }

	protected override ImageEncodingRules GetImageEncodingRules() => new(SKEncodedImageFormat.Png, 100);
}
