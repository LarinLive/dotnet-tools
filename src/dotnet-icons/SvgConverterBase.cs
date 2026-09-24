using SkiaSharp;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.IO;

namespace LarinLive.DotnetTools.Icons;

public abstract class PngImageDefBase
{
	public int SizeInPixels { get; init; }

	public SKColorType ColorType { get; init; } = SKColorType.Rgba8888;

	public SKAlphaType AlphaType { get; init; } = SKAlphaType.Premul;
}

public record struct Image<TImageDef>(TImageDef ImageDef, byte[] Data);

public record struct ImageEncodingRules(SKEncodedImageFormat Format, int Quality);

/// <summary>
/// Base class for conversion from an SVG image
/// </summary>
public abstract class SvgConverterBase<TImageDef> : IDisposable where TImageDef : PngImageDefBase
{
	private readonly SKSvg _source;
	private readonly SKPicture _sourcePicture;

	public SvgConverterBase(Stream source)
	{
		_source = new();
		_source.Load(source);
		_sourcePicture = _source.Picture ?? throw new InvalidOperationException("The SVG could not be parsed into a drawable image.");
	}

	public void Dispose()
	{
		_source.Dispose();
	}

	protected SKSvg Source => _source;

	protected SKPicture SourcePicture => _sourcePicture;

	protected void Save(IReadOnlyCollection<TImageDef> imageDefs, IReadOnlyCollection<Image<TImageDef>> images, Stream destination)
    {
        var innerImages = new List<Image<TImageDef>>(imageDefs.Count + images.Count);
        foreach (var imageDef in imageDefs)
			innerImages.Add(new() { ImageDef = imageDef, Data = Rasterize(imageDef.SizeInPixels, imageDef.ColorType, imageDef.AlphaType) });

		if (images.Count > 0)
			innerImages.AddRange(images);

        DoSave(innerImages, destination);
    }

	protected abstract ImageEncodingRules GetImageEncodingRules();

	private byte[] Rasterize(int size, SKColorType colorType, SKAlphaType alphaType)
    {
		var encodingRules = GetImageEncodingRules();

		var bounds = SourcePicture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new InvalidOperationException(
                "The SVG has no intrinsic size; add a viewBox or an explicit width/height.");

        using var bitmap = new SKBitmap(size, size, colorType, alphaType);
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
        canvas.DrawPicture(SourcePicture);
        canvas.Flush();

        using var data = bitmap.Encode(encodingRules.Format, encodingRules.Quality);
        return data.ToArray();
    }

	protected abstract void DoSave(IReadOnlyList<Image<TImageDef>> images, Stream destination);
}
