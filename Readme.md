#C# Code Rendering with Roslyn

The **C# Code Rendering** library contains classes to walk the C# syntax tree and general rendering support.
The primary purpose was originally to create server-side capability to produce rendered HTML for C# code. 

## Background ##
There is nothing really new about this, and in fact, an inspiration was the 
[blog posting from Shiv Kumar](http://www.matlus.com/c-to-html-syntax-highlighter-using-roslyn/ "C# to Html Syntax Highlighter using Roslyn") 
in November 2011 in which he used an early CTP of Roslyn. The idea of using the syntax walker to inspect 
token and trivia as they are encountered is his as well as using the semantic model and syntax tree to infer the role of identifiers so that they can be color coded properly.

This project is designed to be more general and can be included as part of an application. Within the project,
the traversal through the syntax three (the <c>CodeWalker</c> class) and rendering of the C# code is cleanly
separated. Moreover, rendering is more general and lets you render the C# code in any way that you want 
rather than just producing HTML.

The Code Rendering  uses and requires the latest (April 2, 2014) Roslyn C# support
("Microsoft.CodeAnalysis.CSharp" NuGet package), but does **not** require the 
[Roslyn SDK](http://roslyn.codeplex.com/ "The open source Roslyn compiler platform project") and in particular
the "Roslyn End User Preview" Visual Studio extension.

## Quick Start ##
You can access C# code in many ways using the <code>CodeWalker</code> class, but as a quick start you can 
render C# code as HTML using the <code>HtmlRender</code> class. For each token, the renderer surrounds token
(and trivia) text with "span" tags having class attributes. For example, 

    string code = @"namespace SimpleCode
	{
    	internal sealed class Program
    	{
        	internal static Main()
        	{
            	Console.WriteLine(""Hello, World!"");
        	}
    }; 

    HtmlRenderer render = new HtmlRenderer();
    renderer.IncludeLineNumbers = true;
    string html = renderer.Render(code)

returns the HTML

    <div class="CodeContainer">
        <ol>
            <li data-lineNumber="1"><span class="Keyword">namespace</span> SimpleCode</li>
            <li data-lineNumber="2" class="Alternate">{</li>
            <li data-lineNumber="3">    <span class="Keyword">internal</span> <span class="Keyword">sealed</span> <span class="Keyword">class</span> <span class="Identifier">Program</span></li>
            <li data-lineNumber="4" class="Alternate">    {</li>
            <li data-lineNumber="5">    	<span class="Keyword">internal</span> <span class="Keyword">static</span> <span class="Keyword"></span>Main()</li>
            <li data-lineNumber="6" class="Alternate">    	{</li>
            <li data-lineNumber="7">        	<span class="InferredIdentifier">Console</span>.WriteLine(<span class="StringLiteral">&quot;Hello, Roslyn!&quot;</span>);</li>
            <li data-lineNumber="8" class="Alternate">    	}</li>
            <li data-lineNumber="9">    }</li>
            <li data-lineNumber="10" class="Alternate">}</li>
        </ol>
    </div>

and using the default style sheet that defines the emitted CSS classes, the HTML is rendered as

![Default rendering of simple code](https://raw.githubusercontent.com/wiki/Levaro/RenderingWithRoslyn/CodeDisplay.PNG)

To use the Code Rendering library, you need only reference the Code Rendering 
(`Levaro.Roslyn.CodeRendering.dll`) library and install the "Microsoft.CodeAnalysis.CSharp" NuGet package.
This package does not change the C# compiler is Visual Studio so none of your production work is affected.

## Architecture ##

- Traversing the Syntax Tree
    - Subclass of the CSharpSyntaxVisitor and CSharpSyntaxWalker
    - SyntaxVisiting event
- Rendering
    - IRenderer, CoreRenderer and extension points
        - Render method (recovery of syntax tree and semantic model) and Callback
        - GetPrefixText and GetPostfixText
    - TextRender and SyntaxTreeRender
    - HtmlRender and extension points (in addition to CoreRenderer)
        - ProcessTokenOrTrivia
        - GetText, GetLineStartText, GetLineEndText
        - GetHtmlClass (token and trivia), 
        - GetIdentifierTokenHtmlClass
    - 
## Status ##
This is an initial pre-release for project and includes the core class library which contains the 
syntax tree visitor and code renderers. The core class library (Levaro.Roslyn) targets .NET 4.5.1, and enables both Code Analysis
and StyleCop. The Code Analysis rule set and custom dictionary files as well as the StyleCop settings file are included in the
solution. The 
[Rosyln C# NuGet (prerelease)](http://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/0.6.4033103-beta/ ".NET Compiler Platform -- Roslyn -- support for C#, Microsoft.CodeAnalysis.CSharp.dll.") 
package is not, but is automatically included when the solution file is opened in Visual Studio 2013.

This release also contains a few unit tests and a sample console application ("HTMLRendering") that renders each of the C# files 
in the core class library as stand-alone HTML files.

