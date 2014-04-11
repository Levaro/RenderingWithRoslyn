using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Levaro.Roslyn.Renderers
{
    /// <summary>
    /// Extension methods for C# <see cref="SyntaxToken"/> objects that are used to determine the use (semantics) of the token.
    /// </summary>
    public static class RendererExtensions
    {
        /// <summary>
        /// Determines whether the token is part of documentation trivia
        /// </summary>
        /// <param name="token">The <see cref="SyntaxToken"/> to check if it has an ancestor node of documentation trivia.</param>
        /// <returns><c>true</c> if the <paramref name="token"/> has a documentation trivia ancestor node; otherwise <c>false</c>.
        /// </returns>
        /// <seealso cref="IsInNode(SyntaxToken, Func{SyntaxNode, bool})"/>
        public static bool IsInDocumentationCommentTrivia(this SyntaxToken token)
        {
            return IsInNode(token, n => SyntaxFacts.IsDocumentationCommentTrivia(n.CSharpKind()));
        }

        /// <summary>
        /// Determines whether there is an ancestor node of the token having one of the specified <see cref="SyntaxKind"/>
        /// values.
        /// </summary>
        /// <param name="token">The syntax token whose ancestors' (parent, grand parent, etc.) syntax kind is checked.</param>
        /// <param name="syntaxKinds">The <c>SyntaxKind</c> values to check.</param>
        /// <returns><c>true</c> if the token has an ancestor node whose <c>SyntaxKind</c> is one of the specified values;
        /// <c>false</c> if no ancestor is found or <paramref name="syntaxKinds"/> is <c>null</c>.</returns>
        /// <seealso cref="IsInNode(SyntaxToken, Func{SyntaxNode, bool})"/>
        public static bool IsInNode(this SyntaxToken token, params SyntaxKind[] syntaxKinds)
        {
            return IsInNode(token, n => (syntaxKinds != null) ? syntaxKinds.Any(k => n.IsKind(k)) : false);
        }

        /// <summary>
        /// Determines whether there is an ancestor node of the token satisfying the specified predicate.
        /// values.
        /// </summary>
        /// <param name="token">
        /// The syntax token whose ancestors' (parent, grand parent, etc.) are evaluated by the predicate.
        /// </param>
        /// <param name="predicate">A predicate accepting a <see cref="SyntaxNode"/> instance that is evaluated for each ancestor
        /// of <paramref name="token"/> until it returns <c>true</c> or no other ancestors are found</param>
        /// <returns><c>true</c> if the token has an ancestor node for which the <paramref name="predicate"/> returns <c>true</c>;
        /// <c>false</c> if no ancestor is found or <paramref name="predicate"/> is <c>null</c>.</returns>
        public static bool IsInNode(this SyntaxToken token, Func<SyntaxNode, bool> predicate)
        {
            bool isTokenInNode = false;
            if (predicate != null)
            {
                SyntaxNode parent = token.Parent;
                while ((parent != null) && !isTokenInNode)
                {
                    isTokenInNode = predicate(parent);
                    parent = parent.Parent;
                }
            }

            return isTokenInNode;
        }

        /// <summary>
        /// Gets an <see cref="ISymbol"/> instance for the parent <see cref="SyntaxNode"/> of the specified syntax token.
        /// </summary>
        /// <remarks>
        /// If a symbol cannot be found for a syntax token of syntax kind <see cref="SyntaxKind.IdentifierToken"/>, then it is
        /// likely that the semantic model does not include a metadata reference for an assembly in which the identifier is
        /// defined.
        /// </remarks>
        /// <param name="token">The <see cref="SyntaxToken"/> whose parent's <see cref="ISymbol"/> is returned.</param>
        /// <param name="model">The <see cref="SemanticModel"/> used to find the symbol for the parent of the
        /// <paramref name="token"/>..</param>
        /// <returns>An <see cref="ISymbol"/> instance for the <c>token.Parent</c> syntax node or <c>null</c> if one cannot
        /// be found in the semantic model.</returns>
        public static ISymbol GetSymbol(this SyntaxToken token, SemanticModel model)
        {
            ISymbol symbol = null;
            if ((token != null) && (model != null))
            {
                SyntaxNode syntaxNode = token.Parent;
                if (syntaxNode is SimpleNameSyntax)
                {
                    SymbolInfo symbolInfo = model.GetSymbolInfo((ExpressionSyntax)syntaxNode);
                    symbol = symbolInfo.Symbol;
                    if ((symbol == null) && (symbolInfo.CandidateSymbols.Count() > 0))
                    {
                        symbol = symbolInfo.CandidateSymbols[0];
                    }
                }
                else
                {
                    symbol = model.GetDeclaredSymbol(syntaxNode);
                }

                if (symbol == null)
                {
                    symbol = model.LookupSymbols(syntaxNode.Span.Start, name: token.Text).SingleOrDefault();
                }
            }

            return symbol;
        }
    }
}
