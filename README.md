# dotnet-tools

This repository contains some useful .NET tools.

## dotnet-icons

The **dotnet-icons** tool is designed for making application icons from SVG files. It supports creation of one of the following icon types:
- Windows [ICO file](https://en.wikipedia.org/wiki/ICO_(file_format));
- MacOS [ICNS file](https://en.wikipedia.org/wiki/Apple_Icon_Image_format).
- iOS PNG icon file set.
- Android PNG icon file set.

Binaries are hosted on [NuGet](https://www.nuget.org/packages/dotnet-icons).

Using of the tool is quite straitforward. 
1. Install it from NuGet with `dotnet install dotnet-icons`
2. Convert an SVG file with `dotnet-icons convert <in> [<out>] --out-format:<format>`
