using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LarinLive.DotnetTools.Icons;

public enum IconPlatform
{
    MacOS,
    Windows
}

class Program
{
    private static Argument<string> _inFileArgument = default!;
   
    private static Argument<string> _outFileArgument = default!;
    
    private static Option<string> _platformOption = default!;
    
    static async Task<int> Main(string[] args)
    {
		var productDescription = Assembly.GetEntryAssembly()?
			.GetCustomAttribute<AssemblyProductAttribute>()?
			.Product?
			.ToString();
		var rootCommand = new RootCommand(productDescription!);

		var convertCommand = new Command("convert-svg", "Converts an SVG image to an application icon file for the specified platform.");

		_inFileArgument = new("inFile")
        {
            Description = "A source SVG file.",
            Arity = ArgumentArity.ExactlyOne
        };

        _outFileArgument = new("outFile")
        {
            Description = "A destination icons file.",
            Arity = ArgumentArity.ExactlyOne
        };

       _platformOption = new("--platform")
        {
            Description = "The target platform for the icon file. Must be one of: 'MacOS', 'Windows'.",
            Arity = ArgumentArity.ExactlyOne
        };

        _platformOption.Validators.Add(result =>
        {
            if (result.Tokens.Count > 0)
            {
                string value = result.Tokens.Single().Value.ToLowerInvariant();
                string[] validValues = [ "macos", "windows" ];

                if (!validValues.Contains(value))
                    result.AddError($"Platform '{value}' not recognized. Must be one of: 'MacOS', 'Windows'.");
            }
            else
                result.AddError($"Platform value must be one of: 'MacOS', 'Windows'.");
        });

        convertCommand.Arguments.Add(_inFileArgument);
        convertCommand.Arguments.Add(_outFileArgument);
        convertCommand.Options.Add(_platformOption);
        convertCommand.SetAction(Convert);

		rootCommand.Subcommands.Add(convertCommand);

		return await rootCommand.Parse(args).InvokeAsync();
    }


    private static async Task<int> Convert(ParseResult parseResult, CancellationToken cancellationToken) 
    {
        var inFile = parseResult.GetValue(_inFileArgument)!;
        var outFile = parseResult.GetValue(_outFileArgument)!;
		var platformOptionValue = parseResult.GetValue(_platformOption)!.ToLowerInvariant();

        var platform = platformOptionValue switch
        {
            "macos" => IconPlatform.MacOS,
            "windows" => IconPlatform.Windows,
			_ => throw new InvalidOperationException($"Invalid platform value '{platformOptionValue}'.")
        };

        if (platform == IconPlatform.MacOS)
        {
			Console.WriteLine("Converting an SVG file to a MacOS ICNS file");
			Console.WriteLine($"{inFile} -> {outFile}");
			using var source = File.OpenRead(inFile);
			using var converter = new SvgToIcnsConverter(source);
			using var destination = File.Create(outFile);
			converter.ConvertTo(destination);
			Console.WriteLine("Conversion completed.");
			return 0;
        }
        else if (platform == IconPlatform.Windows)
        {
			Console.WriteLine("Converting an SVG file to a Windows ICO file");
			Console.WriteLine($"{inFile} -> {outFile}");
			using var source = File.OpenRead(inFile);
			using var converter = new SvgToIcoConverter(source);
			using var destination = File.Create(outFile);
			converter.ConvertTo(destination);
			Console.WriteLine("Conversion completed.");
			return 0;
        }
        else
            return 1;
    }
}
