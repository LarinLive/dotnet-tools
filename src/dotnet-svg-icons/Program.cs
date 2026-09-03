using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IconTooling;

namespace scl;

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
    
    private static Option<string> _macOsDarkInFileOption = default!;

    static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = new("Platform-specific icon generator tool for .NET ecosystem.");

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

       _macOsDarkInFileOption = new("--macos-dark-infile")
        {
            Description = "A source SVG file for MacOS dark icons.",
            Arity = ArgumentArity.ZeroOrOne
        };
 
        rootCommand.Arguments.Add(_inFileArgument);
        rootCommand.Arguments.Add(_outFileArgument);
        rootCommand.Options.Add(_platformOption);
        rootCommand.Options.Add(_macOsDarkInFileOption);
        rootCommand.SetAction(Convert);

        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }


    static async Task<int> Convert(ParseResult parseResult, CancellationToken cancellationToken) 
    {
        var inFile = parseResult.GetValue(_inFileArgument)!;
        var outFile = parseResult.GetValue(_outFileArgument)!;

        var platformOptionValue = parseResult.GetValue(_platformOption)!.ToLowerInvariant();

        IconPlatform platform = platformOptionValue switch
        {
            "macos" => IconPlatform.MacOS,
            "windows" => IconPlatform.Windows,
            _ => throw new ArgumentOutOfRangeException(nameof(platformOptionValue), platformOptionValue, "Invalid platform value.")
        };

        if (platform == IconPlatform.MacOS)
        {
            var macOsDarkInFile = parseResult.GetValue(_macOsDarkInFileOption);
            new SvgToIcnsConverter().ConvertFile(inFile, macOsDarkInFile, outFile);
            return 0;
        }
        else if (platform == IconPlatform.Windows)
        {
            new SvgToIcoConverter().ConvertFile(inFile, outFile);
            return 0;
        }
        else
            return 1;
    }
}