using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Levaro.Roslyn.UnitTests
{
    /// <summary>
    /// Test fixture for the methods that test the <see cref="CodeWalker"/> class.
    /// </summary>
    [TestClass]
    public class CodeWalkerTests
    {
        /// <summary>
        /// Checks that no action is taken if the syntax tree root is <c>null</c>.
        /// </summary>
        [TestMethod]
        public void NullRootTest()
        {
            // If the root is null, then an exception is not thrown, but no action is taken either.
            CodeWalker codeWalker = new CodeWalker();
            codeWalker.SyntaxVisiting += (sender, eventArgs) =>
            {
                Assert.Fail("For a null root, no action should be taken.");
            };

            codeWalker.Visit(null);
        }

        /// <summary>
        /// Tests that if the code text is the empty string, then events are raised for just the "empty" root node and the
        /// end of file token.
        /// </summary>
        [TestMethod]
        public void EmptyRootTest()
        {
            string code = string.Empty;
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            SyntaxNode root = syntaxTree.GetRoot();

            // For the "empty" syntax tree, the root is a node of span length 0 having just one end of file token.
            CodeWalker codeWalker = new CodeWalker();
            codeWalker.SyntaxVisiting += (sender, eventArgs) =>
            {
                SyntaxTreeElement syntaxTreeElement = eventArgs.SyntaxTreeElement;
                if (syntaxTreeElement.SyntaxElementCategory == SyntaxElementCategory.Node)
                {
                    Assert.AreEqual<int>(0, syntaxTreeElement.Token.Span.Length);
                }
                else
                {
                    Assert.AreEqual<SyntaxKind>(SyntaxKind.EndOfFileToken, syntaxTreeElement.SyntaxKind);
                }
            };

            codeWalker.Visit(root);
        }

        /// <summary>
        /// Tests that all events are raised with the proper states in the correct order (for simple code).
        /// </summary>
        [TestMethod]
        public void EventHandlerTest()
        {
            string code = @"Write(""Hello Richard"") ;";

            // The visiting state, element type (node, token or trivia) and syntax kind for each raised event in this order
            // for the parsed code text.
            List<Tuple<SyntaxVisitingState, SyntaxElementCategory, SyntaxKind>> expectedResults = 
                new List<Tuple<SyntaxVisitingState, SyntaxElementCategory, SyntaxKind>>
            {
                Tuple.Create(SyntaxVisitingState.EnteringNode, SyntaxElementCategory.Node, SyntaxKind.CompilationUnit),
                Tuple.Create(SyntaxVisitingState.EnteringNode, SyntaxElementCategory.Node, SyntaxKind.MethodDeclaration),
                Tuple.Create(SyntaxVisitingState.EnteringNode, SyntaxElementCategory.Node, SyntaxKind.PredefinedType),
                Tuple.Create(SyntaxVisitingState.Token, SyntaxElementCategory.Token, SyntaxKind.VoidKeyword),
                Tuple.Create(SyntaxVisitingState.LeavingNode, SyntaxElementCategory.Node, SyntaxKind.PredefinedType),
                Tuple.Create(SyntaxVisitingState.Token, SyntaxElementCategory.Token, SyntaxKind.IdentifierToken),
                Tuple.Create(SyntaxVisitingState.EnteringNode, SyntaxElementCategory.Node, SyntaxKind.ParameterList),
                Tuple.Create(SyntaxVisitingState.Token, SyntaxElementCategory.Token, SyntaxKind.OpenParenToken),
                Tuple.Create(SyntaxVisitingState.EnteringNode, SyntaxElementCategory.Node, SyntaxKind.SkippedTokensTrivia),
                Tuple.Create(SyntaxVisitingState.Token, SyntaxElementCategory.Token, SyntaxKind.StringLiteralToken),
                Tuple.Create(SyntaxVisitingState.LeavingNode, SyntaxElementCategory.Node, SyntaxKind.SkippedTokensTrivia),
                Tuple.Create(SyntaxVisitingState.Token, SyntaxElementCategory.Token, SyntaxKind.CloseParenToken),
                Tuple.Create(SyntaxVisitingState.Trivia, SyntaxElementCategory.Trivia, SyntaxKind.WhitespaceTrivia),
                Tuple.Create(SyntaxVisitingState.LeavingNode, SyntaxElementCategory.Node, SyntaxKind.ParameterList),
                Tuple.Create(SyntaxVisitingState.Token, SyntaxElementCategory.Token, SyntaxKind.SemicolonToken),
                Tuple.Create(SyntaxVisitingState.LeavingNode, SyntaxElementCategory.Node, SyntaxKind.MethodDeclaration),
                Tuple.Create(SyntaxVisitingState.Token, SyntaxElementCategory.Token, SyntaxKind.EndOfFileToken),
                Tuple.Create(SyntaxVisitingState.LeavingNode, SyntaxElementCategory.Node, SyntaxKind.CompilationUnit),
            };
            
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            SyntaxNode root = syntaxTree.GetRoot();

            // Record the passed event arguments as each tree element is visited and then compare to the expected results.
            List<SyntaxVisitingEventArgs> results = new List<SyntaxVisitingEventArgs>();
            CodeWalker codeWalker = new CodeWalker();
            codeWalker.SyntaxVisiting += (sender, eventArgs) =>
                {
                    Assert.AreEqual<string>("CodeWalker", sender.GetType().Name);
                    results.Add(eventArgs);
                };

            codeWalker.Visit(root);

            for (int i = 0; i < results.Count; i++)
            {
                Assert.AreEqual<SyntaxVisitingState>(expectedResults[i].Item1, results[i].State);
                Assert.AreEqual<SyntaxElementCategory>(expectedResults[i].Item2, results[i].SyntaxTreeElement.SyntaxElementCategory);
                Assert.AreEqual<SyntaxKind>(expectedResults[i].Item3, results[i].SyntaxTreeElement.SyntaxKind);
            }
        }
    }
}
