# dotnet-tools

This repository contains some useful .NET tools.

# dotnet-icons

The **dotnet-icons** tool is designed for making application icons from SVG files. It supports creation of one of the following icon types:
- Windows [ICO file](https://en.wikipedia.org/wiki/ICO_(file_format));
- MacOS [ICNS file](https://en.wikipedia.org/wiki/Apple_Icon_Image_format).

File in both formats are created using 32-bit PNG images of all supported sizes.

Using of the tool is quite straitforward. 
1. Install it from Nuget with `dotnet install dotnet-icons`
2. Convert an SVG file with `dotnet-icons convert-svg`
