using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LarinLive.DotnetTools.Icons;

/// <summary>
/// A supported application icons format
/// </summary>
public record class ApplicationIconFormat(string Name)
{
	/// <summary>
	/// Windows ICO file
	/// </summary>
	public readonly static ApplicationIconFormat WindowsIco = new("win-ico");

	/// <summary>
	/// MacOS ICNS file
	/// </summary>
	public readonly static ApplicationIconFormat MacOsIcns = new("macos-icns");

	/// <summary>
	/// iOS PNG files set
	/// </summary>
	public readonly static ApplicationIconFormat IosPngSet = new("ios-png-set");


	/// <summary>
	/// iOS PNG files set
	/// </summary>
	public readonly static ApplicationIconFormat AndroidPngSet = new("android-png-set");
}


class Program
{
    private static Argument<string> _inFileArgument = default!;
   
    private static Argument<string> _outFileArgument = default!;
    
    private static Option<string> _outFormatOption = default!;

	private static ApplicationIconFormat[] _allowedOutFormats = 
	[
		ApplicationIconFormat.WindowsIco, 
		ApplicationIconFormat.MacOsIcns, 
		ApplicationIconFormat.IosPngSet, 
		ApplicationIconFormat.AndroidPngSet
	];

	private static string GetAllowedOutFormats(string quote, string delimiter) =>
		_allowedOutFormats.Aggregate(new StringBuilder(), (a, f) => a.Append(a.Length > 0 ? delimiter : string.Empty).Append(quote).Append(f.Name).Append(quote)).ToString();


	static async Task<int> Main(string[] args)
    {
		var productDescription = Assembly.GetEntryAssembly()?
			.GetCustomAttribute<AssemblyProductAttribute>()?
			.Product?
			.ToString();
		var rootCommand = new RootCommand(productDescription!);

		var convertCommand = new Command("convert", "Converts an input SVG image to an application icon with specified format.");

		_inFileArgument = new("in")
        {
            Description = "A source SVG file name.",
            Arity = ArgumentArity.ExactlyOne
		};

		_outFileArgument = new("out")
        {
            Description = "A destination file name. If not specified, the source file name is used with a appropriate extension depending on the output format.",
            Arity = ArgumentArity.ZeroOrOne
        };

       _outFormatOption = new("--out-format", "-f")
        {
            Description = $"An output application icon format. Must be one of: {GetAllowedOutFormats("'", ", ")}.",
            Arity = ArgumentArity.ExactlyOne
        };

        _outFormatOption.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0)
            {
                string value = result.Tokens.Single().Value.ToLowerInvariant();
                if (!_allowedOutFormats.Any(f => f.Name == value))
                    result.AddError($"The output format '{value}' is not recognized. Must be one of: {GetAllowedOutFormats("'", ", ")}.");
            }
        });

        convertCommand.Arguments.Add(_inFileArgument);
        convertCommand.Arguments.Add(_outFileArgument);
        convertCommand.Options.Add(_outFormatOption);
        convertCommand.SetAction(Convert);

		rootCommand.Subcommands.Add(convertCommand);

		if (args.Length == 0)
			args = ["--help"];

		return await rootCommand.Parse(args).InvokeAsync();
    }


    private static async Task<int> Convert(ParseResult parseResult, CancellationToken cancellationToken) 
    {
        var inFile = parseResult.GetValue(_inFileArgument)!;
		var outFormat = _allowedOutFormats.First(f => f.Name == parseResult.GetValue(_outFormatOption)!.ToLowerInvariant());

        if (outFormat == ApplicationIconFormat.MacOsIcns)
        {
			var outFile = parseResult.GetValue(_outFileArgument) ?? Path.ChangeExtension(inFile, ".icns"); 
			Console.WriteLine("Converting the SVG file to a MacOS ICNS file");
			using var source = File.OpenRead(inFile);
			using var converter = new SvgToIcnsConverter(source);
			using var destination = File.Create(outFile);
			Console.WriteLine($"{inFile} -> {outFile}");
			converter.ConvertTo(destination);
			Console.WriteLine("Conversion completed.");
			return 0;
        }
		else if (outFormat == ApplicationIconFormat.WindowsIco)
		{
			var outFile = parseResult.GetValue(_outFileArgument) ?? Path.ChangeExtension(inFile, ".ico");
			Console.WriteLine("Converting the SVG file to a Windows ICO file");
			using var source = File.OpenRead(inFile);
			using var converter = new SvgToIcoConverter(source);
			using var destination = File.Create(outFile);
			Console.WriteLine($"{inFile} -> {outFile}");
			converter.ConvertTo(destination);
			Console.WriteLine("Conversion completed.");
			return 0;
		}
		else if (outFormat == ApplicationIconFormat.IosPngSet)
		{
			var outFile = parseResult.GetValue(_outFileArgument) ?? Path.ChangeExtension(inFile, ".ios.png");
			Console.WriteLine("Converting the SVG file to an iOS PNG file set");
			using var source = File.OpenRead(inFile);
			using var converter = new SvgToPngConverter(source);
			var outFileNameWithoutExtesion = Path.GetDirectoryName(outFile) ?? string.Empty;
			outFileNameWithoutExtesion += (outFileNameWithoutExtesion.Length > 0 ? Path.DirectorySeparatorChar : string.Empty) + Path.GetFileNameWithoutExtension(outFile);
			var outFileExtension = Path.GetExtension(outFile) ?? string.Empty;
			foreach (var image in PngImageDef.IosAppIconPngSet)
			{
				var outFileName = $"{outFileNameWithoutExtesion}@{image.SizeInPixels}{outFileExtension}";
				using var destination = File.Create(outFileName);
				Console.WriteLine($"{inFile} -> {outFileName}");
				converter.ConvertTo(destination, image);
			}
			Console.WriteLine("Conversion completed.");
			return 0;
		}
		else if (outFormat == ApplicationIconFormat.AndroidPngSet)
		{
			var outFile = parseResult.GetValue(_outFileArgument) ?? Path.ChangeExtension(inFile, ".android.png");
			Console.WriteLine("Converting the SVG file to an Android PNG file set");
			using var source = File.OpenRead(inFile);
			using var converter = new SvgToPngConverter(source);
			var outFileNameWithoutExtesion = Path.GetDirectoryName(outFile) ?? string.Empty;
			outFileNameWithoutExtesion += (outFileNameWithoutExtesion.Length > 0 ? Path.DirectorySeparatorChar : string.Empty) + Path.GetFileNameWithoutExtension(outFile);
			var outFileExtension = Path.GetExtension(outFile) ?? string.Empty;
			foreach (var image in PngImageDef.AndroidAppIconPngSet)
			{
				var outFileName = $"{outFileNameWithoutExtesion}@{image.SizeInPixels}{outFileExtension}";
				using var destination = File.Create(outFileName);
				Console.WriteLine($"{inFile} -> {outFileName}");
				converter.ConvertTo(destination, image);
			}
			Console.WriteLine("Conversion completed.");
			return 0;
		}
		else
			return 1;
    }
}
