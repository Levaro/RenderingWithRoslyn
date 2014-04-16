using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Levaro.CSharp.Display.Renderers
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
        /// Determines whether the grand parent of the token whose parent node is of <see cref="SyntaxKind.IdentifierName"/>
        /// has a <see cref="SyntaxKind"/> value specified.
        /// values.
        /// </summary>
        /// <remarks>
        /// This extension method is used by the <see cref="HtmlRenderer.IsIdentifier"/> method to determine if a token of 
        /// <c>SyntaxKind.IdentifierToken</c> should be decorated with the <see cref="HtmlClassName.Identifier"/> class when
        /// rendered as HTML.
        /// </remarks>
        /// <param name="token">
        /// The syntax token whose grand parent node is checked against the specified <c>SyntaxKind</c> values.
        /// </param>
        /// <param name="syntaxKinds">An optional array of <c>SyntaxKind</c> values to check</param>
        /// <returns><c>true</c> if the grand parent node is one of the <paramref name="syntaxKinds"/> values;
        /// <c>false</c> no grand parent node exists, or the parent node is not of <c>SyntaxKind.IdentifierName</c>, or
        /// the grand parent node does not have one of the <c>SyntaxKind</c> values.</returns>
        /// <seealso cref="IsNameInNode(SyntaxToken, int, SyntaxKind[])"/>
        public static bool IsNameInNode(this SyntaxToken token, params SyntaxKind[] syntaxKinds)
        {
            return IsNameInNode(token, 2, syntaxKinds);
        }

        /// <summary>
        /// Determines whether the ancestor node of the token whose parent node is of <see cref="SyntaxKind.IdentifierName"/>
        /// has a <see cref="SyntaxKind"/> value specified.
        /// values.
        /// </summary>
        /// <remarks>
        /// This extension method is used by the <see cref="HtmlRenderer.IsIdentifier"/> method to determine if a token of 
        /// <c>SyntaxKind.IdentifierToken</c> should be decorated with the <see cref="HtmlClassName.Identifier"/> class when
        /// rendered as HTML.
        /// </remarks>
        /// <param name="token">
        /// The syntax token whose ancestor at the specified level is checked against the specified <c>SyntaxKind</c> values.
        /// </param>
        /// <param name="ancestor">The level of the ancestor of the token. Level 1 is the parent, 2 is the parent.Parent (or
        /// grand parent) and so on.</param>
        /// <param name="syntaxKinds">An optional array of <c>SyntaxKind</c> values to check</param>
        /// <returns><c>true</c> if the specified ancestor node is one of the <paramref name="syntaxKinds"/> values;
        /// <c>false</c> if no ancestor is found, or the immediate parent is not of <c>SyntaxKind.IdentifierName</c>, or
        /// the specified ancestor does not have one of the <c>SyntaxKind</c> values.</returns>
        /// <seealso cref="IsNameInNode(SyntaxToken, int, Func{SyntaxNode, bool})"/>
        public static bool IsNameInNode(this SyntaxToken token, int ancestor, params SyntaxKind[] syntaxKinds)
        {
            return IsNameInNode(token, ancestor, n => (syntaxKinds != null) ? syntaxKinds.Any(k => n.IsKind(k)) : false);
        }

        /// <summary>
        /// Determines whether the ancestor node of the token whose parent node is of <see cref="SyntaxKind.IdentifierName"/>
        /// satisfies the <paramref name="predicate"/>.
        /// values.
        /// </summary>
        /// <remarks>
        /// This extension method is used by the <see cref="HtmlRenderer.IsIdentifier"/> method to determine if a token of 
        /// <c>SyntaxKind.IdentifierToken</c> should be decorated with the <see cref="HtmlClassName.Identifier"/> class when
        /// rendered as HTML.
        /// </remarks>
        /// <param name="token">
        /// The syntax token whose ancestor at the specified level is checked against the predicate
        /// </param>
        /// <param name="ancestor">The level of the ancestor of the token. Level 1 is the parent, 2 is the parent.Parent (or
        /// grand parent) and so on.</param>
        /// <param name="predicate">A predicate accepting a <see cref="SyntaxNode"/> instance that is evaluated for the ancestor
        /// of <paramref name="token"/> at the specified level.</param>
        /// <returns><c>true</c> if the specified ancestor node for which the <paramref name="predicate"/> returns <c>true</c>;
        /// <c>false</c> if no ancestor is found, or the immediate parent is not of <c>SyntaxKind.IdentifierName</c>,
        /// or <paramref name="predicate"/> is <c>null</c> or the predicate returns false for the specified ancestor.</returns>
        public static bool IsNameInNode(this SyntaxToken token, int ancestor,  Func<SyntaxNode, bool> predicate)
        {
            int ancestorLevel = Math.Max(ancestor, 1);
            bool isInNode = false;
            SyntaxNode parent = (token != null) ? token.Parent : null;
            if ((parent != null) && (parent.CSharpKind() == SyntaxKind.IdentifierName) && (predicate != null))
            {
                for (int i = 1; (i < ancestorLevel) && (parent != null); i++)
                {
                    parent = parent.Parent;
                }

                isInNode = predicate(parent);
            }

            return isInNode;
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
        /// <paramref name="token"/>.</param>
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
                    symbol = model.LookupSymbols(syntaxNode.Span.Start, name: token.Text).FirstOrDefault();
                }
            }

            return symbol;
        }
    }
}
