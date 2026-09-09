using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LarinLive.DotnetTools.SvgIcons;

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
		var rootCommand = new RootCommand("Platform-specific icon tool for .NET ecosystem.");

		var convertCommand = new Command("convert", "Converts an SVG image to an application icon file for the specified platform.");

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
			Console.WriteLine($"Converting '{inFile}' to '{outFile}' for Windows");
			using var source = File.OpenRead(inFile);
			using var converter = new SvgToIcnsConverter(source);
			using var destination = File.Create(outFile);
			converter.ConvertTo(destination);
			return 0;
        }
        else if (platform == IconPlatform.Windows)
        {
			Console.WriteLine($"Converting '{inFile}' to '{outFile}' for MacOS");
			using var source = File.OpenRead(inFile);
			using var converter = new SvgToIcoConverter(source);
			using var destination = File.Create(outFile);
			converter.ConvertTo(destination);
            return 0;
        }
        else
            return 1;
    }
}
