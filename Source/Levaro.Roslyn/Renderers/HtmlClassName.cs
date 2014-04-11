namespace Levaro.Roslyn.Renderers
{
    /// <summary>
    /// Specifies the HTML class name that can be affixed to an HTML tag enclosing syntax tokens or trivia when rendered in HTML.
    /// </summary>
    public enum HtmlClassName
    {
        /// <summary>
        /// Indicates that it is unknown if an HTML class name can be associated.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// No HTML class name should be associated.
        /// </summary>
        None,

        /// <summary>
        /// The token is a keyword.
        /// </summary>
        Keyword,

        /// <summary>
        /// The trivia is a string literal.
        /// </summary>
        StringLiteral,

        /// <summary>
        /// The trivia is a character literal.
        /// </summary>
        CharacterLiteral,

        /// <summary>
        /// The trivia is a numeric literal.
        /// </summary>
        NumericLiteral,

        /// <summary>
        /// The token is an identifier.
        /// </summary>
        Identifier,

        /// <summary>
        /// The token is an identifier, but its meaning is unknown (that is, no symbol in the semantic model)
        /// </summary>
        UnknownIdentifier,

        /// <summary>
        /// The trivia is a comment; inline (using //) or multi-line (using /* ... */)
        /// </summary>
        Comment,

        /// <summary>
        /// The trivia is a documentation comment (using ///).
        /// </summary>
        DocumentationComment,

        /// <summary>
        /// The trivia is a region (or end region) directive.
        /// </summary>
        Region,

        /// <summary>
        /// The trivia is disabled text.
        /// </summary>
        DisabledText
    }
}
