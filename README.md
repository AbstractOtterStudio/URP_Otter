# URP_Otter

## Set up VSCODE for unity
1. Install [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download) if you haven't already
2. Check your PATH environment variable to see if the .NET SDK is included
   - If not, add `C:\Program Files\dotnet` to your PATH
3. Install the following C# extensions in VSCODE:
   - C# (v.2.0.328 or later)
   - C# Dev Kit (v0.3.21 or later)
   - Unity (v0.9.0 or later)
4. Clean up the old environment (Skip if this is a clean clone)
   - Delete the `.vscode` folder
   - Delete the `Library` folder
   - Delete the `Temp` folder
   - Run the following commands in VSCode command palette (Ctrl+Shift+P):
     - `> .NET Clean`
     - `> .NET Open Solution`
     - `> .NET Restart Language Server`
