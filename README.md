# DrawOnShape for Rhino 8

[![Rhino 8](https://img.shields.io/badge/Rhino-8-blueviolet?style=for-the-badge)](https://www.rhino3d.com/)

A simple and intuitive plugin for Rhino 8 that allows you to draw smooth, interpolated curves directly onto any SubD, Mesh, or Polysurface.

![Demo GIF of DrawOnShape in action](https://github.com/nachomonereo/DrawOnShape/blob/main/drawonshape.gif?raw=true)

## Overview

**DrawOnShape** was created to provide the functionality of Rhino's native `InterpolateOnSrf` command, but for complex, multi-faced geometry. While the standard command is limited to single surfaces, **DrawOnShape** lets you draw a single, continuous curve across SubDs, meshes, and polysurfaces as if they were one seamless object.

This tool is perfect for sketching guide curves, trim lines, or design details directly on top of your models without being interrupted by surface boundaries.

## Key Features

-   **Works on Complex Geometry:** Draw seamlessly on SubDs, Meshes, and Polysurfaces.
-   **Interactive Drawing:** Get a real-time preview of your curve as you place points on the surface.
-   **Automatic Smooth Close:** Simply click near the curve's starting point to automatically create a perfectly closed curve with G2 continuity (curvature continuous). No manual adjustments are needed!
-   **Simple Workflow:** The process is clean and straightforward: run the command, select your geometry, and start drawing.

## Installation

You can install this plugin in two ways.

#### Method 1: Download from Food4Rhino (Recommended)

This plugin is available for direct download on Food4Rhino.

1.  Download the `DrawOnShape.rhp` file from the [Food4Rhino page](https://www.food4rhino.com/en/app/drawonshape). *(Note: Link will be active once the page is approved).*
2.  Simply **drag the `.rhp` file and drop it onto an open Rhino 8 window**.
3.  Rhino will handle the installation. You may need to restart Rhino.

#### Method 2: Manual Installation from GitHub

1.  Go to the [**Releases**](https://github.com/nachomonereo/DrawOnShape/releases) page of this repository.
2.  Download the `DrawOnShape.rhp` file from the latest release.
3.  Follow the installation steps from Method 1 (drag and drop the `.rhp` file onto Rhino).

## How to Use

1.  Run the `DrawOnShape` command in Rhino.
2.  You will be prompted to select the SubD, Mesh, or Polysurface you want to draw on.
3.  Click points directly on the surface to define the path of your curve.
4.  **To create a closed curve:** After placing at least two points, click near the starting point of the curve. The tool will automatically snap and generate a smooth, periodic curve.
5.  **To create an open curve:** Press `Enter` when you are finished placing points.

## Building from Source

If you want to build the project yourself, you will need:

-   Visual Studio 2022
-   .NET 7 SDK
-   Rhino 8 installed

The project is set up to reference `RhinoCommon` and other libraries via NuGet packages, so it should build directly after being opened in Visual Studio.

## Feedback & Contributing

This is a simple tool, but if you find a bug or have an idea for a new feature, please open an **[Issue](https://github.com/nachomonereo/DrawOnShape/issues)** on this repository. Contributions are welcome!

