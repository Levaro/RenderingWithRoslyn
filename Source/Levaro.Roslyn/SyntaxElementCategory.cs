namespace Levaro.Roslyn
{
    /// <summary>
    /// Describes the type of syntax element found in the C# syntax tree.
    /// </summary>
    public enum SyntaxElementCategory
    {
        /// <summary>
        /// The element category is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The element is a <c>SyntaxNode</c> instance.
        /// </summary>
        Node = 1,

        /// <summary>
        /// The element is a <c>SyntaxToken</c> object.
        /// </summary>
        Token = 2,

        /// <summary>
        /// The element is a <c>SyntaxTrivia</c> object.
        /// </summary>
        Trivia = 3
    }
}
