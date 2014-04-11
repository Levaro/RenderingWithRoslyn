namespace Levaro.Roslyn
{
    /// <summary>
    /// Represents the state of the <c>CodeWalker</c> syntax tree visitor.
    /// </summary>
    public enum SyntaxVisitingState
    {
        /// <summary>
        /// The state is unknown
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The visitor is entering the <c>DefaultVisitor</c> method.
        /// </summary>
        EnteringNode,

        /// <summary>
        /// The visitor is leaving the <c>DefaultVisitor</c> method.
        /// </summary>
        LeavingNode,

        /// <summary>
        /// The visitor is in the <c>VisitToken</c> method.
        /// </summary>
        Token,

        /// <summary>
        /// The visitor is in the <c>VisitTrivia</c> method.
        /// </summary>
        Trivia
    }
}
