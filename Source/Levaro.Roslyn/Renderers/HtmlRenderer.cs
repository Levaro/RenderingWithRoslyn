using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Levaro.Roslyn.Renderers
{
    /// <summary>
    /// Renders C# program code to text having HTML markup suitable for display in a web browser.
    /// </summary>
    /// <remarks>
    /// Using the syntax tree and semantic model provided by Roslyn, each token and trivia is inspected for its syntax and meaning
    /// and wrapped in a "span" tag having a CSS class name. Each line of the formatted code is represented in a list item ("li")
    /// tag. If line numbers are requested, an ordered list is used. Similarly, alternate lines can be highlighted. The actual
    /// rendering in the browser is determined by the definitions of the CSS tags and classes emitted by this renderer.
    /// <para>
    /// As a subclass of <see cref="CoreRenderer"/>, the real work is done by the callback delegate. For example,
    /// <code>
    /// htmlRenderer = new HtmlRenderer();
    /// htmlRenderer.FormatCode = true;
    /// htmlRenderer.AlternateLines = true;
    /// htmlRenderer.IncludeLineNumbers = true;
    /// string code = "static void Main()\r\n{\r\nConsole.WriteLine(\"Hello, HTML Renderer\");\r\n}";
    /// </code>
    /// produces the following rendered HTML
    /// <code>
    /// ◄!--
    ///     This file was automatically generated using the Levaro.Roslyn.Renderers.HtmlRenderer renderer.
    ///     The file was generated on Wednesday April 2, 2014 at 9:44 AM (Central Daylight Time)
    /// --►
    /// 
    /// ◄div class="CodeContainer"►
    ///     ◄ol►
    ///         ◄li class="Alternate"►◄span class="Keyword"►static◄/span► ◄span class="Keyword"►void◄/span► Main()◄/li►
    ///         ◄li►{◄/li►
    ///         ◄li class="Alternate"►    ◄span class="Identifier"►Console◄/span►.WriteLine(◄span class="StringLiteral"►"Hello, HTML Renderer"◄/span►);◄/li►
    ///         ◄li►}◄/li►
    ///     ◄/ol►
    /// ◄/div►
    /// </code>
    /// where ◄ represents left angle brace (&lt;) and ► represent right angle brace (&gt;)
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
        public HtmlRenderer() : this(false, true, false, true)
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
        /// <param name="includeDebugInfo">if set to <c>true</c>, the attribute "data-lineNumber" is set to the current line
        /// number for each line ("li" tag) and the attribute "data-syntaxKind" is set to the <see cref="SyntaxKind"/> of the 
        /// token or trivia wrapped in a "span" tag.</param>
        /// <param name="inferIdentifier">If set to <c>true</c>, if the class name for an identifier token cannot be found
        /// simply by using the syntax kind of the token and associated symbol, the method <see cref="IsIdentifier"/> is
        /// used to determine if the token should be rendered as an identifier.</param>
        /// <seealso cref="ProcessTokenOrTrivia"/>
        public HtmlRenderer(bool includeLineNumbers, bool alternateLines, bool includeDebugInfo, bool inferIdentifier)
        {
            IncludeLineNumbers = includeLineNumbers;
            AlternateLines = alternateLines;
            IncludeDebugInfo = includeDebugInfo;
            InferIdentifier = inferIdentifier;

            // The callback only processes the Token and Trivia states; the node states are used for tracing.
            Callback = (syntaxTreeElement, visitingState) =>
                {
                    switch (visitingState)
                    {
                        case SyntaxVisitingState.EnteringNode:
                            Trace.WriteLine(string.Empty);
                            Trace.Indent();
                            string text = syntaxTreeElement.Text;
                            text = text.Substring(0, Math.Min(25, text.Length)).Replace("\r\n", "\\r\\n");
                            Trace.WriteLine(string.Format("Entering node {0} [{1}]", syntaxTreeElement.Node.CSharpKind(), text));
                            break;
                        case SyntaxVisitingState.LeavingNode:
                            Trace.WriteLine("Leaving node ...");
                            Trace.Unindent();
                            break;
                        case SyntaxVisitingState.Token:
                        case SyntaxVisitingState.Trivia:
                            Trace.Write(string.Format("Processing {0}; ", syntaxTreeElement));
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
        /// Gets or sets a value indicating whether extra attributes are included to aid in debugging.
        /// </summary>
        /// <remarks>
        /// The extra attribute information is:
        /// <list type="bullet">
        /// <item><description>
        /// data-lineNumbers = [[line number]] is included in each "li" tag (even if <see cref="IncludeLineNumbers"/> 
        /// is <c>false</c>).
        /// </description></item>
        /// <item><description>
        /// data-syntaxKind = [[SyntaxKind]] is included in each "span" tag that wraps a token or trivia element.
        /// </description></item>
        /// </list>
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
        /// Gets the text that is written to <see cref="CoreRenderer.Writer" /> before the <see cref="SyntaxTree" /> elements 
        /// are processed by the <see cref="CoreRenderer.Callback" /> delegate.
        /// </summary>
        /// <returns>
        /// An HTML comment that describes when and by what the HTML code was generated as well as the container DIV element for
        /// the generated HTML.
        /// </returns>
        protected override string GetPrefixText()
        {
            LineNumber = 1;
            IsLineEmpty = true;

            DateTime now = DateTime.Now;
            TimeZone timeZone = TimeZone.CurrentTimeZone;
            string timeZoneName = timeZone.IsDaylightSavingTime(now) ? timeZone.DaylightName : timeZone.StandardName;

            StringBuilder prefixText = new StringBuilder();
            prefixText.Append("<!--");
            prefixText.AppendLine(" This file was automatically generated using the Levaro.RoslynRenderers.HtmlRenderer renderer.");
            prefixText.AppendFormat("     The file was generated on {0:dddd MMMM d, yyyy a\\t h:mm tt} ({1})",
                                    now,
                                    timeZoneName);
            prefixText.AppendLine("  -->").AppendLine();

            prefixText.AppendFormat("<div class=\"CodeContainer\">{0}{1,8}{0}{2}", 
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
        /// The final closing list element tag and closing DIV container tag.
        /// </returns>
         protected override string GetPostfixText()
        {
            return string.Format("{0}{1}{2,9}{1}{3}", 
                                 "</li>", 
                                 Environment.NewLine, 
                                 string.Format("</{0}>", ListTagName), 
                                 "</div>");
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
        private void ProcessTokenOrTrivia(SyntaxTreeElement syntaxTreeElement)
        {
            HtmlClassName className = GetHtmlClass(syntaxTreeElement);
            string text = GetText(syntaxTreeElement);
            Trace.WriteLine(string.Format("Class Name: [{0}]; Text: [{1}]", className, text.Replace("\r\n", "\\r\\n")));

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
                    // comment so that a trailing &nbsp is not added to the end of the text. Usually this is done in GetText, 
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
        private string GetText(SyntaxTreeElement element)
        {
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
        /// <returns>The opening "li" tag including alternate and line information if required.</returns>
        [SuppressMessage("Microsoft.Design", 
                         "CA1024:UsePropertiesWhereAppropriate",
                         Justification = "This could be a complicated method not suitable as a property")]
        protected virtual string GetLineStartText()
        {
            StringBuilder startText = new StringBuilder(new string(' ', 8));
            startText.Append("<li");
            if (AlternateLines && (LineNumber % 2 != 0))
            {
                startText.Append(" class=\"Alternate\"");
            }

            if (IncludeDebugInfo)
            {
                startText.AppendFormat(" data-lineNumber=\"{0}\"", LineNumber);
            }

            startText.Append(">");
            LineNumber++;
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
        private HtmlClassName GetHtmlClass(SyntaxTreeElement treeElement)
        {
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
        private HtmlClassName GetHtmlClass(SyntaxToken token)
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
                    case SyntaxKind.XmlTextLiteralToken:
                    case SyntaxKind.XmlEntityLiteralToken:  // &lt; &gt; &quot; &amp; &apos; or &name; or &#nnnn; or &#xhhhh;
                        if (token.IsInDocumentationCommentTrivia())
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
        private HtmlClassName GetIdentifierTokenHtmlClass(SyntaxToken token)
        {
            HtmlClassName className = HtmlClassName.Unknown;

            ISymbol symbol = token.GetSymbol(SemanticModel);

            Trace.WriteLine(string.Format("For identifier token [{0}], symbol is [{1}]",
                                          token.Text,
                                          (symbol == null) ? "Null" : symbol.Kind.ToString()));

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
                        // className = token.IsInNode(SyntaxKind.Attribute) ? HtmlClassName.Identifier : HtmlClassName.None;
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
                    className = IsIdentifier(token) ? HtmlClassName.Identifier : HtmlClassName.None;
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
        /// token cannot be determine by the syntax (<c>SyntaxKind</c> from the syntax tree) or is use 
        /// (<c>SymbolKind</c> from its symbol in the semantic model). 
        /// <para>
        /// The process is to look how it fits in the syntax tree, particularly the <see cref="SyntaxKind"/> of its ancestor nodes, 
        /// to see how it is used. This method is virtual and can be overridden to provide alternate or additional rules.
        /// </para>
        /// </remarks>
        /// <param name="token">The identifier token whose class name is returned.</param>
        /// <returns>Returns <c>true</c> if the token should be viewed as an identifier when the HTML for the code is rendered;
        /// <c>false</c> otherwise.</returns>
        [SuppressMessage("Microsoft.Maintainability",
                         "CA1502:AvoidExcessiveComplexity",
                         Justification = "The (ugly) conditional is required to check possibilities unhandled by GetHtmlClass.")]
        protected virtual bool IsIdentifier(SyntaxToken token)
        {
            bool isIdentifier = false;

            // Now if the class is still unknown, find it by checking its syntax kind of its ancestor nodes
            SyntaxKind parentKind = AncestorKind(token, 1);
            SyntaxKind grandParentKind = AncestorKind(token, 2);
            SyntaxKind greatGrandParentKind = AncestorKind(token, 3);

            SyntaxKind[] identifierNameKnownKinds = new SyntaxKind[]
            {
                SyntaxKind.Parameter,
                SyntaxKind.Attribute,
                SyntaxKind.CatchDeclaration,
                SyntaxKind.ObjectCreationExpression,
                SyntaxKind.MethodDeclaration,
                SyntaxKind.CastExpression,
                SyntaxKind.BaseList,
                SyntaxKind.TypeOfExpression,
                SyntaxKind.VariableDeclaration,
                SyntaxKind.TypeArgumentList
            };

            if ((parentKind == SyntaxKind.EnumDeclaration) || 
                ((parentKind == SyntaxKind.IdentifierName) && token.IsInNode(identifierNameKnownKinds)) ||
                ((parentKind == SyntaxKind.GenericName) && token.IsInNode(SyntaxKind.VariableDeclaration, SyntaxKind.ObjectCreationExpression)))
            {
                isIdentifier = true;
            }

            if (((parentKind == SyntaxKind.IdentifierName) &&
                                            (grandParentKind == SyntaxKind.ForEachStatement) &&
                                            !(token.GetNextToken().CSharpKind() == SyntaxKind.CloseParenToken)) ||
                 ((parentKind == SyntaxKind.IdentifierName) &&
                                            (greatGrandParentKind == SyntaxKind.CaseSwitchLabel) &&
                                            !(token.GetPreviousToken().CSharpKind() == SyntaxKind.DotToken)))
            {
                isIdentifier = true;
            }

            return isIdentifier;
        }

        /// <summary>
        /// Gets the HTML class name for the specified syntax trivia.
        /// </summary>
        /// <param name="trivia">The <see cref="SyntaxTrivia"/> object for which a <c>HtmlClassName</c> enumeration
        /// value is returned.</param>
        /// <returns>A value of the <see cref="HtmlClassName"/> enumeration.</returns>
        private static HtmlClassName GetHtmlClass(SyntaxTrivia trivia)
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
        
        /// <summary>
        /// Returns the <see cref="SyntaxKind"/> of ancestor syntax nodes of the specified syntax token.
        /// </summary>
        /// <param name="token">The token whose syntax kind of an ancestor node if found.</param>
        /// <param name="ancestorLevel">The ancestor level; for example, parent is 1, grandparent is 2 and great grandparent
        /// is 2.</param>
        /// <returns>The <c>SyntaxKind</c> enumeration value of the specified ancestor. If there is no ancestor at the
        /// requested level (<paramref name="ancestorLevel"/>) the <c>SyntaxKind.None</c> is returned.
        /// </returns>
        private static SyntaxKind AncestorKind(SyntaxToken token, int ancestorLevel)
        {
            int level = 1;
            SyntaxKind ancestorKind = SyntaxKind.None;
            for (SyntaxNode ancestor = token.Parent; ancestor != null; ancestor = ancestor.Parent)
            {
                if (level++ == ancestorLevel)
                {
                    ancestorKind = ancestor.CSharpKind();
                    break;
                }
            }

            return ancestorKind;
        }
    }
}
