﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Web;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Levaro.CSharp.Display.Renderers
{
    /// <summary>
    /// Renders C# program code to text having HTML markup suitable for display in a web browser.
    /// </summary>
    /// <remarks>
    /// Using the syntax tree and semantic model provided by the .NET Compiler Platform ("Roslyn"), each token and trivia is 
    /// inspected for its syntax and meaning and wrapped in a "span" tag having a CSS class name. Each line of the formatted code 
    /// is represented in a list item ("li") tag. If line numbers are requested, an ordered list is used. Similarly, alternate 
    /// lines can be highlighted. The actual rendering in the browser is determined by the definitions of the CSS tags and classes 
    /// emitted by this renderer.
    /// <para>
    /// As a subclass of <see cref="CoreRenderer"/>, the real work is done by the callback delegate. The following examples
    /// illustrates the rendered HTML for a simple code fragment using the specified property settings:
    /// <code>
    /// htmlRenderer = new HtmlRenderer();
    /// htmlRenderer.AlternateLines = true;
    /// htmlRenderer.IncludeLineNumbers = true;
    /// htmlRenderer.IncludeDebugInfo = false;
    /// htmlRenderer.InferIdentifier = true;
    /// string codeText = "static void Main()\r\n{\r\nConsole.WriteLine(\"Hello, HTML Renderer\");\r\n}";
    /// htmlRenderer.Render(codeText)
    /// </code>
    /// produces the following rendered HTML
    /// <![CDATA[
    /// <!-- HTML was automatically generated by HtmlRenderer on Monday April 14, 2014 at 3:06 PM (Central Daylight Time) -->
    /// <div class="CodeContainer">
    ///     <ol>
    ///         <li data-lineNumber="1"><span class="Keyword">static</span> <span class="Keyword">void</span> Main()</li>
    ///         <li data-lineNumber="2" class="Alternate">{</li>
    ///         <li data-lineNumber="3"><span class="Identifier">Console</span>.<span class="Identifier">WriteLine</span>
    ///                                               (<span class="StringLiteral">&quot;Hello, HTML Renderer&quot;</span>);</li>
    ///         <li data-lineNumber="4" class="Alternate">}</li>
    ///     </ol>
    /// </div>
    /// ]]>
    /// </para>
    /// </remarks>
    public class HtmlRenderer : CoreRenderer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlRenderer"/> class.
        /// </summary>
        /// <remarks>
        /// The <see cref="CoreRenderer.MetadataReferences" /> collection is initialized to contain the single metadata reference 
        /// for the "mscorlib" assembly. The <see cref="IncludeLineNumbers"/> and <see cref="IncludeDebugInfo"/> properties are 
        /// set to <c>false</c>; the <see cref="AlternateLines"/> and <see cref="InferIdentifier"/> properties are set to 
        /// <c>true</c>. Finally the base <see cref="CoreRenderer.Callback"/> delegate is initialized.
        /// </remarks>
        /// <seealso cref="HtmlRenderer(bool, bool, bool, bool)"/>
        public HtmlRenderer()
            : this(false, true, false, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlRenderer"/> class.
        /// </summary>
        /// <remarks>
        /// Sets the <see cref="CoreRenderer.Callback"/> so that the method <see cref="ProcessTokenOrTrivia"/> is called 
        /// for syntax tokens and trivia encountered when transversing the syntax tree from the render methods.
        /// </remarks>
        /// <param name="includeLineNumbers">if set to <c>true</c>, line numbers are rendered.</param>
        /// <param name="alternateLines">if set to <c>true</c>, the class "Alternate" is included on alternate lines.</param>
        /// <param name="includeDebugInfo">if set to <c>true</c>, the attribute "data-syntaxKind" is set to the 
        /// <see cref="SyntaxKind"/> of the token or trivia wrapped in a "span" tag.</param>
        /// <param name="inferIdentifier">If set to <c>true</c>, and if the class name for an identifier token cannot be found
        /// simply by using the syntax kind of the token and associated symbol, the method <see cref="IsIdentifier"/> is
        /// used to determine if the token should be rendered as an identifier.</param>
        /// <seealso cref="ProcessTokenOrTrivia"/>
        public HtmlRenderer(bool includeLineNumbers, bool alternateLines, bool includeDebugInfo, bool inferIdentifier)
        {
            IncludeLineNumbers = includeLineNumbers;
            AlternateLines = alternateLines;
            IncludeDebugInfo = includeDebugInfo;
            InferIdentifier = inferIdentifier;

            // Find the HtmlRenderer listener if there is one; messages are only displayed to this renderer when it exists.
            Listener = Trace.Listeners.OfType<TraceListener>().SingleOrDefault(l => l.Name == "HtmlRenderer");
            TracingIndent = 0;
            IsTracingNewLine = true;
            TracingStopwatch = new Stopwatch();
            
            // The callback only processes the Token and Trivia states; the node states are used for tracing.
            Callback = (syntaxTreeElement, visitingState) =>
                {
                    switch (visitingState)
                    {
                        case SyntaxVisitingState.EnteringNode:
                            TraceWriteLine();
                            TracingIndent += 4;
                            string text = syntaxTreeElement.Text;
                            text = text.Substring(0, Math.Min(40, text.Length)).Replace("\r\n", "\\r\\n");
                            TraceWriteLine("Entering node {0} [{1}]", syntaxTreeElement.Node.CSharpKind(), text);
                            break;
                        case SyntaxVisitingState.LeavingNode:
                            TraceWriteLine("Leaving node ...");
                            TracingIndent -= 4;
                            break;
                        case SyntaxVisitingState.Token:
                        case SyntaxVisitingState.Trivia:
                            TraceWrite("Processing {0}; ", syntaxTreeElement);
                            ProcessTokenOrTrivia(syntaxTreeElement);
                            break;
                        default:
                            break;
                    }
                };
        }

        /// <summary>
        /// Gets or sets a value indicating whether line numbers are included in each line of the rendered text.
        /// </summary>
        /// <value>
        /// <c>true</c> to include line numbers; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeLineNumbers
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to include a "Alternate" class on every other rendered line.
        /// </summary>
        /// <value>
        /// <c>true</c> if wrapping span tags include class="Alternate" on alternate lines; otherwise, <c>false</c>.
        /// </value>
        public bool AlternateLines
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether an identifier syntax token having no symbol should have its HTML class
        /// name inferred from the syntax tree.
        /// </summary>
        /// <value>
        /// <c>true</c> if the HTML class name is inferred from the syntax tree; otherwise, <c>false</c>.
        /// </value>
        public bool InferIdentifier
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether an extra data attribute is included to aid in debugging.
        /// </summary>
        /// <remarks>
        /// The extra attribute information <c>data-syntaxKind = [[SyntaxKind]]</c> and is included in each "span" tag that 
        /// wraps a token or trivia element.
        /// </remarks>
        /// <value>
        /// <c>true</c> if extra attribute information is included; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeDebugInfo
        {
            get;
            set;
        }
       
        /// <summary>
        /// Gets the name of the list tag.
        /// </summary>
        /// <remarks>
        /// It is set to be an ordered list is including line numbers is requested (<see cref="IncludeLineNumbers"/> is <c>true</c>)
        /// and an unordered list otherwise.
        /// </remarks>
        /// <value>
        /// The name of the list tag; either "ol" or "ul".
        /// </value>
        protected virtual string ListTagName
        {
            get
            {
                return IncludeLineNumbers ? "ol" : "ul";
            }
        }

        /// <summary>
        /// Gets or sets the line number.
        /// </summary>
        /// <remarks>
        /// The number of lines is used to determine which lines are alternate and to display as debugging information if
        /// requested
        /// </remarks>
        /// <value>
        /// The line number for the currently processed rendered text line.
        /// </value>
        private int LineNumber
        {
            get;
            set;
        }

        #region Diagnostic support properties
        /// <summary>
        /// Gets or sets a value indicating whether currently processing line has any code text within.
        /// </summary>
        /// <remarks>
        /// If a line is empty, when the end of line text is written a non-breaking space is included to make sure a blank
        /// line is rendered.
        /// </remarks>
        /// <value>
        /// <c>true</c> if this instance is line empty; otherwise, <c>false</c>.
        /// </value>
        private bool IsLineEmpty
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="TraceListener"/> instance where messages are written.
        /// </summary>
        /// <value>
        /// The current <see cref="TraceListener"/> or <c>null</c> if tracing is not enabled.
        /// </value>
        private TraceListener Listener
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of spaces to indent tracing messages.
        /// </summary>s
        /// <value>
        /// The tracing indent value; it is used by the Trace method that write messages to the <see cref="TraceListener"/>.
        /// </value>
        private int TracingIndent
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the current trace information is placed on a new line.
        /// </summary>
        /// <remarks>
        /// When <c>true</c>, the number of <see cref="TracingIndent"/> spaces is written before the next content.
        /// </remarks>
        /// <value>
        /// <c>true</c> if the next tracing output is on a new line; otherwise, <c>false</c>.
        /// </value>
        private bool IsTracingNewLine
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the tracing stop watch; it is used to record the total time for rendering.
        /// </summary>
        /// <value>
        /// The tracing stop watch; it is never <c>null</c> even if there is no HtmlRenderer listener.
        /// </value>
        private Stopwatch TracingStopwatch
        {
            get;
            set;
        }
        #endregion

        /// <summary>
        /// Gets the text that is written to <see cref="CoreRenderer.Writer" /> before the <see cref="SyntaxTree" /> elements 
        /// are processed by the <see cref="CoreRenderer.Callback" /> delegate.
        /// </summary>
        /// <returns>
        /// An HTML comment that describes when and by what the HTML code was generated as well as the container DIV element for
        /// the generated HTML.
        /// </returns>
        protected override string GetPrefixText()
        {
            TracingStopwatch.Start();
            TraceWriteLine("Starting HTML rendering at {0:h:mm:ss.ff tt}", DateTime.Now);

            LineNumber = 1;
            IsLineEmpty = true;

            DateTime now = DateTime.Now;
            TimeZone timeZone = TimeZone.CurrentTimeZone;
            string timeZoneName = timeZone.IsDaylightSavingTime(now) ? timeZone.DaylightName : timeZone.StandardName;

            StringBuilder prefixText = new StringBuilder();
            prefixText.Append("<!-- HTML was automatically generated by HtmlRenderer on")
                      .AppendFormat(" {0:dddd MMMM d, yyyy a\\t h:mm tt} ({1}) -->",
                                    now,
                                    timeZoneName)
                      .AppendLine()
                      .AppendLine()
                      .AppendFormat("<div class=\"CodeContainer\">{0}{1,8}{0}{2}",
                                    Environment.NewLine,
                                    string.Format("<{0}>", ListTagName),
                                    GetLineStartText());

            return prefixText.ToString();
        }

        /// <summary>
        /// Gets the text that is written to <see cref="CoreRenderer.Writer" /> after the <see cref="SyntaxTree" /> elements 
        /// are processed by the <see cref="CoreRenderer.Callback" /> delegate.
        /// </summary>
        /// <returns>
        /// The final closing list element tag and closing DIV container tag. If no data has been written to the list element
        /// (that is, <see cref="IsLineEmpty"/> is <c>true</c>), then a non-breaking space is included in the list to make
        /// sure that the blank line is rendered.
        /// </returns>
        protected override string GetPostfixText()
        {
            string postfixText = string.Format("{0}{1}{2}{3,9}{2}{4}",
                                               IsLineEmpty ? "&nbsp;" : string.Empty,
                                               "</li>",
                                               Environment.NewLine,
                                               string.Format("</{0}>", ListTagName),
                                               "</div>");
            
            TraceWriteLine();
            TraceWriteLine("Rendering complete at {0:h:mm:ss.ff tt} taking {1:#,##0} milliseconds", 
                           DateTime.Now, 
                           TracingStopwatch.ElapsedMilliseconds);

            TracingStopwatch.Reset();
            return postfixText;
        }

        /// <summary>
        /// Processes the token or trivia by returning the text of the token possibly wrapped in a "span" tag with any 
        /// necessary class attribute.
        /// </summary>
        /// <remarks>
        /// This method is called from the <c>CoreRenderer.Callback</c> delegate which is set in the constructor. This method
        /// and its helper methods, primarily, <see cref="GetText"/> and <see cref="GetHtmlClass(SyntaxTreeElement)"/> 
        /// (and their helper methods) do the heavy lifting to generate the correct HTML code for the C# code text.
        /// </remarks>
        /// <param name="syntaxTreeElement">The syntax tree element containing the token or trivia.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="syntaxTreeElement"/> is <c>null</c>.</exception>
        protected virtual void ProcessTokenOrTrivia(SyntaxTreeElement syntaxTreeElement)
        {
            if (syntaxTreeElement == null)
            {
                throw new ArgumentNullException("syntaxTreeElement", "SyntaxTree element may not be null.");
            }

            HtmlClassName className = GetHtmlClass(syntaxTreeElement);
            string text = GetText(syntaxTreeElement);
            TraceWriteLine("Class Name: [{0}]; Text: [{1}]", className, text.Replace("\r\n", "\\r\\n"));

            StringBuilder outputText = new StringBuilder();
            if (syntaxTreeElement.SyntaxKind == SyntaxKind.MultiLineCommentTrivia)
            {
                string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    outputText.AppendFormat("<span class=\"{0}\"", HtmlClassName.Comment);
                    if (IncludeDebugInfo)
                    {
                        outputText.AppendFormat(" data-syntaxKind=\"{0}\"", syntaxTreeElement.SyntaxKind);
                    }

                    outputText.AppendFormat(">{0}</span>", line);

                    // Because GetLineEndText is called, IsLineEmpty is reset to true (actually GetLineStartText sets it to true
                    // which is called by GetLineEndText. Consequently, IsLineEmpty needs to be set to false for the lines of
                    // comment so that a trailing &nbsp; is not added to the end of the text. Usually this is done in GetText, 
                    // but that method is not called here, because we are creating the lines individually. All part of the 
                    // complexity of the "old-fashioned" multi-line comments (those marked with "/*" and "*/").
                    if (line.Length != 0)
                    {
                        IsLineEmpty = false;
                    }

                    if (i < lines.Length - 1)
                    {
                        outputText.Append(GetLineEndText());
                    }
                }
            }
            else if (className > HtmlClassName.None)
            {
                outputText.AppendFormat("<span class=\"{0}\"", className);
                if (IncludeDebugInfo)
                {
                    outputText.AppendFormat(" data-syntaxKind=\"{0}\"", syntaxTreeElement.SyntaxKind);
                }

                outputText.AppendFormat(">{0}</span>", text);
            }

            if (outputText.Length != 0)
            {
                text = outputText.ToString();
            }

            Writer.Write(text);
        }

        /// <summary>
        /// Gets the text associated with a syntax token or trivia.
        /// </summary>
        /// <param name="element">The <see cref="SyntaxTreeElement"/> containing the token or trivia.</param>
        /// <returns>The text that actually represents the token or trivia, which is typically the <c>Text</c>
        /// property of the <paramref name="element"/> parameter. However, the text is replaced or encoded for some tokens (end 
        /// of line or angle braces, for example) when that is required for HTML rendering.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="element"/> is <c>null</c>.</exception>
        protected virtual string GetText(SyntaxTreeElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element", "SyntaxTree element cannot be null.");
            }

            string text = element.Text;
            bool checkIsLineEmpty = true;
            switch (element.SyntaxKind)
            {
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                case SyntaxKind.StringLiteralToken:
                    text = HttpUtility.HtmlEncode(text);
                    break;
                case SyntaxKind.LessThanToken:
                case SyntaxKind.GreaterThanToken:
                case SyntaxKind.LessThanSlashToken:
                case SyntaxKind.SlashGreaterThanToken:
                case SyntaxKind.XmlTextLiteralToken:
                case SyntaxKind.XmlCDataEndToken:
                case SyntaxKind.XmlCDataStartToken:
                    if (element.Token.IsInDocumentationCommentTrivia())
                    {
                        text = HttpUtility.HtmlEncode(text);
                    }

                    break;
                case SyntaxKind.EndOfLineTrivia:
                case SyntaxKind.XmlTextLiteralNewLineToken:
                    text = GetLineEndText();

                    // The state of IsLineEmpty should not change if all that is returned is the end of line text.
                    checkIsLineEmpty = false;
                    break;
                default:
                    break;
            }

            if (checkIsLineEmpty)
            {
                IsLineEmpty = IsLineEmpty && (text.Length == 0);
            }

            return text;
        }

        /// <summary>
        /// Gets the text that starts a new line of rendered code.
        /// </summary>
        /// <remarks>
        /// This method is typically called from the <see cref="GetLineEndText"/> method when a new line is created in the
        /// rendered text.
        /// </remarks>
        /// <returns>The opening "li" tag including alternate and line number information.</returns>
        [SuppressMessage("Microsoft.Design",
                         "CA1024:UsePropertiesWhereAppropriate",
                         Justification = "This could be a complicated method not suitable as a property")]
        protected virtual string GetLineStartText()
        {
            StringBuilder startText = new StringBuilder(new string(' ', 8));
            startText.Append("<li");
            startText.AppendFormat(" data-lineNumber=\"{0}\"", LineNumber++);
            if (AlternateLines && (LineNumber % 2 != 0))
            {
                startText.Append(" class=\"Alternate\"");
            }

            startText.Append(">");
            IsLineEmpty = true;
            return startText.ToString();
        }

        /// <summary>
        /// Gets the text that is placed at the end of a rendered line.
        /// </summary>
        /// <returns>A string containing the closing "li" tag, a newline sequence and the text for the start of a new
        /// line.</returns>
        [SuppressMessage("Microsoft.Design",
                         "CA1024:UsePropertiesWhereAppropriate",
                         Justification = "This could be a complicated method not suitable as a property")]
        protected virtual string GetLineEndText()
        {
            // If the current line has no text other than the opening li tag, include a non-breaking space so that a blank
            // line is rendered.
            return string.Format("{0}{1}{2}{3}",
                                 IsLineEmpty ? "&nbsp;" : string.Empty,
                                 "</li>",
                                 Environment.NewLine,
                                 GetLineStartText());
        }

        /// <summary>
        /// Gets the HTML class name for the specified token or trivia wrapped by the <see cref="SyntaxTreeElement"/> instance.
        /// </summary>
        /// <remarks>
        /// The HTML class name is used to create a class attribute for a "span" tag that wraps the trivia or token if the
        /// value is not <c>HtmlClassName.None</c> or <c>HtmlClassName.Unknown</c>.
        /// </remarks>
        /// <param name="treeElement">The tree element.</param>
        /// <returns>A value of the <see cref="HtmlClassName"/> enumeration.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="treeElement"/> is <c>null</c>.</exception>
        protected virtual HtmlClassName GetHtmlClass(SyntaxTreeElement treeElement)
        {
            if (treeElement == null)
            {
                throw new ArgumentNullException("treeElement", "SyntaxTree element cannot be null.");
            }

            HtmlClassName className = HtmlClassName.Unknown;
            switch (treeElement.SyntaxElementCategory)
            {
                case SyntaxElementCategory.Token:
                    className = GetHtmlClass(treeElement.Token);
                    break;
                case SyntaxElementCategory.Trivia:
                    className = GetHtmlClass(treeElement.Trivia);
                    break;
                default:
                    break;
            }

            return className;
        }

        /// <summary>
        /// Gets the HTML class name for the specified syntax token.
        /// </summary>
        /// <param name="token">The <see cref="SyntaxToken"/> object for which a <c>HtmlClassName</c> enumeration
        /// value is returned.</param>
        /// <returns>A value of the <see cref="HtmlClassName"/> enumeration.</returns>
        protected virtual HtmlClassName GetHtmlClass(SyntaxToken token)
        {
            HtmlClassName className = HtmlClassName.Unknown;

            if (token.IsKeyword() || SyntaxFacts.IsPreprocessorKeyword(token.CSharpKind()))
            {
                className = token.IsInDocumentationCommentTrivia() ? HtmlClassName.DocumentationComment : HtmlClassName.Keyword;
            }
            else
            {
                switch (token.CSharpKind())
                {
                    case SyntaxKind.HashToken:
                        if (token.IsInNode(n => SyntaxFacts.IsPreprocessorDirective(n.CSharpKind())))
                        {
                            className = HtmlClassName.Keyword;
                        }

                        break;
                    case SyntaxKind.EqualsToken:
                    case SyntaxKind.DoubleQuoteToken:
                    case SyntaxKind.SingleQuoteToken:
                    case SyntaxKind.OpenParenToken:
                    case SyntaxKind.CloseParenToken:
                    case SyntaxKind.CommaToken:
                    case SyntaxKind.DotToken:
                    case SyntaxKind.LessThanToken:
                    case SyntaxKind.GreaterThanToken:
                    case SyntaxKind.LessThanSlashToken:
                    case SyntaxKind.SlashGreaterThanToken:
                        if (token.IsInDocumentationCommentTrivia())
                        {
                            className = HtmlClassName.DocumentationComment;
                        }

                        break;
                    case SyntaxKind.XmlCDataStartToken:
                    case SyntaxKind.XmlCDataEndToken:
                    case SyntaxKind.XmlTextLiteralToken:
                    case SyntaxKind.XmlEntityLiteralToken:  // &lt; &gt; &quot; &amp; &apos; or &name; or &#nnnn; or &#xhhhh;
                        if (token.IsInNode(SyntaxKind.XmlCDataSection))
                        {
                            className = HtmlClassName.DocumentationComment;
                        }
                        else if (token.IsInDocumentationCommentTrivia())
                        {
                            className = HtmlClassName.Comment;
                        }

                        break;
                    case SyntaxKind.StringLiteralToken:
                        className = HtmlClassName.StringLiteral;
                        break;
                    case SyntaxKind.CharacterLiteralToken:
                        className = HtmlClassName.CharacterLiteral;
                        break;
                    case SyntaxKind.NumericLiteralToken:
                        className = HtmlClassName.NumericLiteral;
                        break;
                    case SyntaxKind.IdentifierToken:

                        // If the identifier is part of XML documentation, it is just takes the DocumentComment class name.
                        if (token.IsInDocumentationCommentTrivia())
                        {
                            className = HtmlClassName.DocumentationComment;
                        }
                        else if (token.IsInNode(n => SyntaxFacts.IsPreprocessorDirective(n.CSharpKind())))
                        {
                            // If the identifier is part of the preprocessor directive (define, if, etc.), no class is used.
                            className = HtmlClassName.None;
                        }
                        else
                        {
                            className = GetIdentifierTokenHtmlClass(token);
                        }

                        break;
                    default:
                        break;
                }
            }

            return className;
        }

        /// <summary>
        /// Gets the HTML class name for the specified <see cref="SyntaxToken"/> whose <c>Kind</c> is the 
        /// <c>SyntaxKind.Identifier</c> enumeration value.
        /// </summary>
        /// <remarks>
        /// This method is called by <see cref="GetHtmlClass(SyntaxToken)"/> when the class name for a token cannot be determined 
        /// by the the syntax (<c>SyntaxKind</c> from the syntax tree) alone. This method uses the semantic model to recover
        /// an <see cref="ISymbol"/> instance and uses that to determine if the <see cref="HtmlClassName.Identifier"/>
        /// should be used for this token.
        /// </remarks>
        /// <param name="token">The identifier token whose class name is returned.</param>
        /// <returns>Returns the class name for the token. This is a value of the <see cref="HtmlClassName"/> enumeration and is
        /// used if the token is wrapped in a "span" tag. If <c>HtmlClassName.None</c> is returned, the identifier is not
        /// wrapped in a "span" tag. The only other values returned are <c>HtmlClassName.Identifier</c> or
        /// <c>HtmlClassName.UnknownIdentifier.</c></returns>
        /// <seealso cref="IsIdentifier"/>
        protected virtual HtmlClassName GetIdentifierTokenHtmlClass(SyntaxToken token)
        {
            HtmlClassName className = HtmlClassName.Unknown;

            ISymbol symbol = token.GetSymbol(SemanticModel);

            TraceWrite("For identifier token [{0}], symbol is [{1}] ", 
                       token.Text,
                       (symbol == null) ? "Null" : symbol.Kind.ToString());

            if (symbol != null)
            {
                switch (symbol.Kind)
                {
                    // Named types are highlighted
                    case SymbolKind.NamedType:
                        className = HtmlClassName.Identifier;
                        break;

                    // Methods are only highlighted when specified in attribute statement, for example 
                    // <c>[assembly: AssemblyTitle("This Title")]</c>
                    case SymbolKind.Method:
                        className = token.IsInNode(SyntaxKind.Attribute) ? HtmlClassName.Identifier : HtmlClassName.None;
                        break;

                    // The following known SymbolKind values for identifiers are never highlighted
                    case SymbolKind.Namespace:
                    case SymbolKind.Parameter:
                    case SymbolKind.Local:
                    case SymbolKind.Property:
                    case SymbolKind.Field:
                        className = HtmlClassName.None;
                        break;
                    default:
                        break;
                }
            }

            if (className == HtmlClassName.Unknown)
            {
                if (InferIdentifier)
                {
                    className = IsIdentifier(token) ? HtmlClassName.InferredIdentifier : HtmlClassName.None;
                }
                else
                {
                    className = HtmlClassName.UnknownIdentifier;
                }
            }

            return className;
        }

        /// <summary>
        /// Determines if the syntax node is an identifier by inspecting the ancestors nodes in the syntax tree.
        /// </summary>
        /// <remarks>
        /// This method is called by <see cref="GetIdentifierTokenHtmlClass(SyntaxToken)"/> when the class name for an identifier
        /// token cannot be determine by the syntax (<c>SyntaxKind</c> from the syntax tree) or is use (<c>SymbolKind</c> from its
        /// symbol in the semantic model). It is only called if the <see cref="InferIdentifier"/> property is <c>true</c>.
        /// <para>
        /// The process is to look how it fits in the syntax tree, particularly the <see cref="SyntaxKind"/> of its ancestor nodes, 
        /// to see how it is used. This method is virtual and can be overridden to provide alternate or additional rules.
        /// </para>
        /// </remarks>
        /// <param name="token">The identifier token whose class name is returned.</param>
        /// <returns>Returns <c>true</c> if the token should be viewed as an identifier when the HTML for the code is rendered;
        /// <c>false</c> otherwise.</returns>
        protected virtual bool IsIdentifier(SyntaxToken token)
        {
            bool isIdentifier = false;

            SyntaxKind[] identifierNameKnownKinds = new SyntaxKind[]
            {
                SyntaxKind.Attribute,
                SyntaxKind.BaseList,
                SyntaxKind.CastExpression,
                SyntaxKind.CatchDeclaration,
                SyntaxKind.MethodDeclaration,
                SyntaxKind.ObjectCreationExpression,
                SyntaxKind.Parameter,
                SyntaxKind.TypeArgumentList,
                SyntaxKind.TypeOfExpression,
                SyntaxKind.VariableDeclaration
            };

            if (token.IsNameInNode(identifierNameKnownKinds) || 
                (token.IsNameInNode(SyntaxKind.ForEachStatement) && (token.GetNextToken().CSharpKind() != SyntaxKind.CloseParenToken)) ||
                (token.IsNameInNode(SyntaxKind.SimpleMemberAccessExpression) && (token.GetPreviousToken().CSharpKind() != SyntaxKind.DotToken)) ||
                (token.IsNameInNode(3, SyntaxKind.CaseSwitchLabel) && (token.GetPreviousToken().CSharpKind() != SyntaxKind.DotToken)))
            {
                isIdentifier = true;
            }

            TraceWrite(" *** Inferred ... "); 
            return isIdentifier;
        }

        /// <summary>
        /// Gets the HTML class name for the specified syntax trivia.
        /// </summary>
        /// <param name="trivia">The <see cref="SyntaxTrivia"/> object for which a <c>HtmlClassName</c> enumeration
        /// value is returned.</param>
        /// <returns>A value of the <see cref="HtmlClassName"/> enumeration.</returns>
        protected virtual HtmlClassName GetHtmlClass(SyntaxTrivia trivia)
        {
            HtmlClassName className = HtmlClassName.Unknown;
            switch (trivia.CSharpKind())
            {
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                    className = HtmlClassName.Comment;
                    break;
                case SyntaxKind.DisabledTextTrivia:
                    className = HtmlClassName.DisabledText;
                    break;
                case SyntaxKind.PreprocessingMessageTrivia:
                    className = HtmlClassName.None;
                    break;
                default:
                    className = HtmlClassName.None;
                    break;
            }

            return className;
        }

        #region Tracing helper methods
        /// <summary>
        /// Writes an informational message to the HtmlRenderer trace listener using the specified array of objects and formatting 
        /// information if there is an HtmlRenderer trace listener in the collection of trace listeners.
        /// </summary>
        /// <param name="format">A format string that contains zero or more format items, which correspond to objects in the 
        /// <paramref name="args"/> array</param>
        /// <param name="args">An object array containing zero or more objects to format.</param>
        private void TraceWrite(string format = "", params object[] args)
        {
            if (Listener != null)
            {
                if (IsTracingNewLine)
                {
                    Listener.Write(new string(' ', TracingIndent));
                    IsTracingNewLine = false;
                }

                Listener.Write(string.Format(format, args));
                Listener.Flush();
            }
        }

        /// <summary>
        /// Writes an informational message to the HtmlRenderer trace listener using the specified array of objects and formatting 
        /// information and then emits a new line if there is an HtmlRenderer trace listener in the collection of trace listeners.
        /// </summary>
        /// <param name="format">A format string that contains zero or more format items, which correspond to objects in the 
        /// <paramref name="args"/> array</param>
        /// <param name="args">An object array containing zero or more objects to format.</param>
        private void TraceWriteLine(string format = "", params object[] args)
        {
            if (Listener != null)
            {
                if (IsTracingNewLine)
                {
                    Listener.Write(new string(' ', TracingIndent));
                }

                Listener.Write(string.Format(format, args));
                Listener.WriteLine(string.Empty);
                IsTracingNewLine = true;
                Listener.Flush();
            }
        }
        #endregion
    }
}