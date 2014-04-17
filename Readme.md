#C# Code Rendering with Roslyn

## Release Notes ##

- 0.1.14107 First initial pre-release

----

The **C# Display** library contains classes to walk the C# syntax tree and general rendering support. The primary purpose was 
originally to create server-side capability to produce rendered HTML for C# code. It is much more because it allows you to 
display C# code in any format you wish.

## Background ##
There is nothing really new about using Roslyn to render C# code as HTML, and in fact, an inspiration was the 
[blog posting from Shiv Kumar](http://www.matlus.com/c-to-html-syntax-highlighter-using-roslyn/ "C# to Html Syntax Highlighter using Roslyn") 
in November 2011 using an early Community Technology Preview (**CTP**) of Roslyn. The idea of using the syntax visitor to inspect 
tokens and trivia as they are encountered and then using the semantic model and syntax tree to infer the role of identifiers so 
that they can be color coded properly are his.

The C# Display project is designed to be more general and can be easily included as part of your own applications. Within the C# 
Display code, the traversal through the syntax tree (the `CodeWalker` class) and rendering of the C# code is cleanly separated. 
Moreover, rendering is more general and does not restrict what the output can be.

The C# Display uses and requires the latest (April 2, 2014) .NET Compiler Platform (**Roslyn**) C# support 
via the "Microsoft.CodeAnalysis.CSharp" NuGet package, but does **not** require the 
[Roslyn SDK](http://roslyn.codeplex.com/ "The open source Roslyn compiler platform project") and in particular the "Roslyn End 
User Preview" Visual Studio extension.

## Quick Start ##
You can access C# code in many ways using the `CodeWalker` class, but for this quick start let's render the simplest C# code as 
HTML using the `HtmlRenderer` class. For each token, the renderer surrounds tokens (and trivia) text that should be formatted
with "span" tags having a class attribute For example, 

    string code = @"namespace SimpleCode
    {
        internal sealed class Program
        {
            internal static Main()
            {
                Console.WriteLine(""Hello, Roslyn!"");
            }
    }
    
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
            <li data-lineNumber="7">            <span class="InferredIdentifier">Console</span>.WriteLine(<span class="StringLiteral">&quot;Hello, Roslyn!&quot;</span>);</li>
            <li data-lineNumber="8" class="Alternate">    	}</li>
            <li data-lineNumber="9">    }</li>
            <li data-lineNumber="10" class="Alternate">}</li>
        </ol>
    </div>

and using the default style sheet that defines the emitted CSS classes, the HTML is rendered as

![Default rendering of simple code](https://raw.githubusercontent.com/wiki/Levaro/RenderingWithRoslyn/CodeDisplay.PNG)

To use the C# Display library, you need only reference the C# Display (`Levaro.CSharp.Display.dll`) library and install the 
"Microsoft.CodeAnalysis.CSharp" NuGet package. The Microsoft NuGet package does not change the C# compiler used by Visual Studio 
so your production environment is not altered.

## Architecture ##

Functionally the C# Display namespace is divided into two areas. The first traverses the a `CSharpSyntaxTree` and raises events 
so that you can easily gain control when tokens, trivia and nodes are encountered. The second implements a general rendering 
strategy using the event raised when the syntax tree is traversed.

### Traversing the Syntax Tree ###

The technique for inspecting the C# code is to use Roslyn to produce a syntax tree representing the code and when needed a 
semantic model to help determine how the syntax tree elements are used and what they mean. This is of course the heavy lifting 
and, Roslyn does it all for us.

####`CodeWalker` Class####

The `Levaro.CSharp.Display.CodeWalker` class subclasses the Roslyn `Microsoft.CodeAnalysis.CSharp.CSharpSyntaxWalker` class which 
is an implementation of a visitor that traverses the C# code syntax tree in depth-first order. `CodeWalker` raises a 
`SyntaxVisiting` event whenever a node, token or trivia element of the tree is encountered. The event data contains the event state 
(for example, whether a syntax node, token or trivia is visited) and a `SyntaxTreeElement` class that wraps all three kinds of 
syntax tree elements. It's just for convenience so that only one class type needs to be passed to event handlers.

The `CodeWalker` real purpose is to simply remove the traversing of the C# syntax tree from the code that either analyzes or 
displays the syntax tree elements.

### Renderers ###

The `Levaro.CSharp.Display.Renderers` namespace contains the classes that consume the events from the `CodeWalker` and render the 
syntax tree in a way that represents the C# source code. The `IRenderer` is quite simple: it contains contracts for `Render` 
methods that can accept C# source code as text or from a stream and can return the rendered code as a string or a text writer. 
Roslyn can create the syntax tree from the C# code alone, but the semantic model depends in part upon the assemblies that the code 
references. For that reason, the `IRender` contract requires a collection of `MetadataReference` objects so that if necessary 
a semantic model can be constructed.

Notice that the `IRender` interface makes no reference to syntax trees or semantic models directly. That is left up to the 
implementations.

#### The Core Renderer ####

Th `CoreRenderer` class implements the `IRender` interface by providing basic rendering methods and the necessary Roslyn access 
to recover the syntax tree, semantic model, and a `CodeWalker` instance. It is an abstract class. In summary the implementation of 
the `Render` method performs the following tasks:

1. Uses Roslyn to create a C# syntax tree from the C# source code passed to the render method.
2. Using the passed assembly information (`MetadataReference` instances), uses Roslyn to construct a semantic model. The assembly 
information for the .NET core library ("mscorlib") is always included even if no other references are supplied.
3. Creates an instance of the `CodeWalker` and sinks the `SyntaxVisiting` event. The event handler does nothing more than invoke a 
callback (`Action<SyntaxTreeElement, SyntaxVisitingState>`) for each event.
4. Writes text to a text writer (by default a `StringWriter`) recovered from a virtual `GetPrefixText` method.
5. Passes the root of the syntax tree to the `CodeWalker.Visit` method which in turn causes the delegate to be called whenever a 
syntax tree element is encountered.
6. Finally writes text recovered from a virtual `GetPostText` method.

`CoreRenderer` is abstract because the callback is not defined. Concrete renderers are much easier to create by subclassing the
core renderer.

#### Concrete Renderers ####

The simplest concrete implementation need only specify the callback delegate. For example, the following simply displays the 
`SyntaxTreeElement` object corresponding to each node, token or trivia element when found traversing the syntax tree.

    public sealed class TextRenderer : CoreRenderer
    {
        public TextRenderer()
        {
            Callback = (syntaxTreeElement, visitingState) =>
            {
                if (Writer != null)
                {
                    Writer.WriteLine(syntaxTreeElement.ToString());
                }
            };
        }
    }

Besides `TextRenderer`, there is also a very simple renderer (`SyntaxTreeRenderer`) that displays the syntax tree hierarchy with 
a bit more detail.

#### Rendering C# Code as Color-coded HTML ####

The `HtmlRenderer` is more complicated, but because of the division of labor in the C# Display project, the code in the renderer is 
just about output rather than processing the C# source. This summary does not contain a blow-by-blow description of the HTML 
renderer &mdash; for that you should inspect the code itself. Having an understanding of the core renderer provides a decent 
roadmap for inspecting and understanding the HTML renderer code. What follows are some (but not all) highlights.

Lists are used to render the lines of C# as seen earlier. An ordered list is used if the property `IncludeLineNumbers` is true and
otherwise an unordered list is used. Each list element (LI tag) begins with a data-dash property specifying the line number. This
is typically just informational but could be used by script. 

The HTML renderer callback delegate processes each token and trivia (comments, white space, etc.) &mdash; these are the elements 
that make up the actual C# source code. Syntax nodes are important as containers of tokens and trivia, but are not included in the 
rendering. Using the `Microsoft.CodeAnalysis.SyntaxKind` enumeration value of the token or trivia, we decide if the code element
should be "highlighted" or distinguished by placing it within a SPAN tag having a class attribute. Fundamentally, the process is to
associated each token or trivia element with a `HtmlClassName` enumeration value. If the class name is `HtmlClassName.None` then
the text of the element is displayed without alteration; otherwise it is wrapped in a SPAN tag using that class name.

The idea is straigtforward, but gets a little complicated especially with documentation and multi-line comments and most
importantly, identifiers.

Unlike keywords for example, which always have the class name `HtmlClassName.Keyword` value, identiers are not treated the same.
For example, types are typically displayed (color-coded) differently depending upon how they're used. And that's where the 
semantic model comes in. For identifiers, if it is not clear from the `SyntaxKind` what class name to assign, then the semantic
model is used to find a symbol for the token and the `Microsoft.CodeAnalysis.SymbolKind` enumeration value. For examaple, 
an identifier with the value `SymbolKind.NameType` is assigned `HtmlClassName.Identifier`, but for `SymbolKind.Field` 
it is assigned `HtmlClassName.None`.

Because the semantic model is used to find the symbol for the token, the semantic model must have references to assemblies where
types are declared. If the semantic model is not complete or an associated using statement is not present in the code, a symbol 
may not be found. If the symbol for an identifier token is null or doesn't have a `SymbolKind` that provides enough information, 
the class name for the identifier can often be inferred from the syntax tree. This is a fallback position that the HTML renderer 
uses if the `HtmlRenderer.InferIdentifier` property value is true (the default).

Once you have the HTML you can define the CSS classes and put the HTML in a file and display it all in a browser. You can define the
classes anyway you want, but primarily they simply set colors and define styles for the list elements and a container for the
list as a whole. The `Levaro.CSharp.Display.dll` assembly contains an embedded resource `ListStyle.css` which defines the CSS 
classes. For convenience, the minified version is also embedded. The style sheet and the `HtmlClassNames` enumeration are designed 
to create HTML that renders in the browser as close to Visual Studio 2013 rendering of C# code as possible.

The sample (console) program project `HtmlRendering` illustrates how the HTML renderer can be used and in particular how to
extract the embedded stylesheet. The program reads all the C# code files in the C# Display class library project and generates
corresponding HTML files. To execute the program, load the solution into Visual Studio 2013 make `HtmlRendering` the start up 
project and select "Debug | Start Without Debugging" (Ctrl+F5).

[Status]: #Status "Release Status Details"
## Status ##
This is the first pre-release (version 0.1.14107) for the C# Display solution and includes the core class library, unit 
tests and the sample console application project `HtmlRendering`. All projects target .NET 4.5.1 and enable both Code Analys and 
StyleCop. The Code Analysis rule set and custom dictionary files as well as the StyleCop settings file are included in the
solution. The 
[Rosyln C# NuGet (prerelease)](http://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/0.6.4033103-beta/ ".NET Compiler Platform -- Roslyn -- support for C#, Microsoft.CodeAnalysis.CSharp.dll.") 
package is referenced and can be automatically included when the solution file is opened in Visual Studio 2013.

Expect changes as Roslyn releases continue, and more unit test and sample programs are added. Finally, a NuGet package to include
the C# Display assembly and the dependent Roslyn assemblies will be available soon.