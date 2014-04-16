#C# Code Rendering with Roslyn

This code contains classes to walk the C# syntax tree and general rendering support. The primary purpose was originally to create 
server-side capability to produce rendered HTML for C# code. With the current April 2014 release of the .NET Compiler Platform 
("Roslyn")" project, the tools and the platform make the time right to produce something usefully more general while investigating 
the Roslyn platform.

## Background ##
There is nothing really new about this, and in fact, an inspiration was the 
[blog posting from Shiv Kumar](http://www.matlus.com/c-to-html-syntax-highlighter-using-roslyn/ "C# to Html Syntax Highlighter using Roslyn") 
in November 2011 in which he used an early CTP of Roslyn. The idea of using the syntax walker to inspect token and trivia as they 
are encountered is his.

This project is designed to be more general and can be included as part of an application. It uses and requires the latest 
(April 2, 2014) Roslyn code, but does **not** require the 
[Roslyn SDK](http://roslyn.codeplex.com/ "The open source Roslyn compiler platform project") and in particular the 
"Roslyn End User Preview" Visual Studio extension.

## Quick Start ##
You can view C# code in many ways using the <code>CodeWalker</code> class, a quick start is to use the <code>HtmlRender</code>
class to create HTML color-highlighted text that represents the code. For example, 

    string html = (new HtmlRenderer()).Render(code)

returns

## Status ##
This is an initial pre-release for project and includes the core class library which contains the 
syntax tree visitor and code renderers. The core class library (Levaro.Roslyn) targets .NET 4.5.1, and enables both Code Analysis
and StyleCop. The Code Analysis rule set and custom dictionary files as well as the StyleCop settings file are included in the
solution. The 
[Rosyln C# NuGet (prerelease)](http://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/0.6.4033103-beta/ ".NET Compiler Platform -- Roslyn -- support for C#, Microsoft.CodeAnalysis.CSharp.dll.") 
package is not, but is automatically included when the solution file is opened in Visual Studio 2013.

This release also contains a few unit tests and a sample console application ("HTMLRendering") that renders each of the C# files 
in the core class library as stand-alone HTML files.

