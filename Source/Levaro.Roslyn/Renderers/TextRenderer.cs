namespace Levaro.Roslyn.Renderers
{
    /// <summary>
    /// Renders code as just a list of <see cref="SyntaxTreeElement"/> objects recovered from the generated <c>SyntaxTree</c>.
    /// </summary>
    /// <remarks>
    /// Virtually all the work of this renderer is done by the <see cref="CoreRenderer"/> base class.
    /// </remarks>
    public sealed class TextRenderer : CoreRenderer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextRenderer"/> class.
        /// </summary>
        /// <remarks>
        /// This is a minimal concrete implementation of the abstract <see cref="CoreRenderer"/>. The <c>Callback</c> property is
        /// set to a delegate that just displays the <c>SyntaxTreeElement</c> for all visiting states.
        /// For example,
        /// <code>
        /// new TextRenderer().Render("Console.WriteLine(\"Hello\");")
        /// </code>
        /// produces the output
        /// <code>
        /// Node CompilationUnit || [27..27)
        /// Node SkippedTokensTrivia |Console| [0..7)
        /// Token IdentifierToken |Console| [0..7)
        /// Node SkippedTokensTrivia |Console| [0..7)
        /// Node SkippedTokensTrivia |.| [7..8)
        /// Token DotToken |.| [7..8)
        /// Node SkippedTokensTrivia |.| [7..8)
        /// Node SkippedTokensTrivia |WriteLine| [8..17)
        /// Token IdentifierToken |WriteLine| [8..17)
        /// Node SkippedTokensTrivia |WriteLine| [8..17)
        /// Node SkippedTokensTrivia |(| [17..18)
        /// Token OpenParenToken |(| [17..18)
        /// Node SkippedTokensTrivia |(| [17..18)
        /// Node SkippedTokensTrivia |"Hello"| [18..25)
        /// Token StringLiteralToken |"Hello"| [18..25)
        /// Node SkippedTokensTrivia |"Hello"| [18..25)
        /// Node SkippedTokensTrivia |)| [25..26)
        /// Token CloseParenToken |)| [25..26)
        /// Node SkippedTokensTrivia |)| [25..26)
        /// Node SkippedTokensTrivia |;| [26..27)
        /// Token SemicolonToken |;| [26..27)
        /// Node SkippedTokensTrivia |;| [26..27)
        /// Token EndOfFileToken || [27..27)
        /// Node CompilationUnit || [27..27)
        /// </code>
        /// </remarks>
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
}
