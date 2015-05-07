using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Levaro.CSharp.Display.Renderers
{
    /// <summary>
    /// Renders the C# code as a tree of syntax nodes, tokens and trivia.
    /// </summary>
    public sealed class SyntaxTreeRenderer : CoreRenderer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxTreeRenderer"/> class.
        /// </summary>
        public SyntaxTreeRenderer()
        {
            Indent = string.Empty;
            NestingLevel = 0;

            Callback = (syntaxTreeElement, visitingState) =>
                {
                    switch (visitingState)
                    {
                        case SyntaxVisitingState.EnteringNode:
                        case SyntaxVisitingState.Token:
                        case SyntaxVisitingState.Trivia:
                            RenderElementCallback(syntaxTreeElement);
                            break;
                        case SyntaxVisitingState.LeavingNode:
                            Indent = new string(' ', 4 * (--NestingLevel));
                            Writer.WriteLine(string.Format("{0}{2}{1}", Indent, syntaxTreeElement, '\u25c4'.ToString()));
                            break;
                        default:
                            break;
                    }
                };
        }

        /// <summary>
        /// Gets or sets the number of spaces to indent the current element.
        /// </summary>
        /// <value>
        /// The indent as a string of spaces equal to 4 times the <see cref="NestingLevel"/>
        /// </value>
        private string Indent
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the nesting level of the current element in the rendered tree.
        /// </summary>
        /// <value>
        /// The nesting level.
        /// </value>
        private int NestingLevel
        {
            get;
            set;
        }

        /// <summary>
        /// The delegate that receives control when nodes, tokens and trivia are visited.
        /// </summary>
        /// <remarks>
        /// This method performs most of the rendering work and for the most part simply displays the 
        /// <paramref name="treeElement"/> using its <c>ToString()</c> method with the text indented to reflect the position of 
        /// the child nodes and tokens. For example, to code
        /// <code>
        /// Console.WriteLine(new SyntaxTreeRenderer().Render("static void Main() {Console.WriteLine(\"Hello\");}"));
        /// </code>
        /// is rendered as
        /// <code>
        /// <![CDATA[
        /// >Node CompilationUnit |static void Main() {Console.WriteLine("Hello");}| [0..46)
        ///     >Node MethodDeclaration |static void Main() {Console.WriteLine("Hello");}| [0..46)
        ///         Token StaticKeyword |static| [0..6) 1 trailing trivia  Parent.Kind=MethodDeclaration
        ///         Trivia WhitespaceTrivia | | [6..7)  for Token StaticKeyword
        ///         >Node PredefinedType |void| [7..11)
        ///             Token VoidKeyword |void| [7..11) 1 trailing trivia  Parent.Kind=PredefinedType
        ///             Trivia WhitespaceTrivia | | [11..12)  for Token VoidKeyword
        ///         <Node PredefinedType |void| [7..11)
        ///         Token IdentifierToken |Main| [12..16)  Parent.Kind=MethodDeclaration
        ///         >Node ParameterList |()| [16..18)
        ///             Token OpenParenToken |(| [16..17)  Parent.Kind=ParameterList
        ///             Token CloseParenToken |)| [17..18) 1 trailing trivia  Parent.Kind=ParameterList
        ///             Trivia WhitespaceTrivia | | [18..19)  for Token CloseParenToken
        ///         <Node ParameterList |()| [16..18)
        ///         >Node Block |{Console.WriteLine("Hello");}| [19..46)
        ///             Token OpenBraceToken |{| [19..20)  Parent.Kind=Block
        ///             >Node ExpressionStatement |Console.WriteLine("Hello");| [20..45)
        ///                 >Node InvocationExpression |Console.WriteLine("Hello")| [20..44)
        ///                     >Node MemberAccessExpression |Console.WriteLine| [20..35)
        ///                         >Node IdentifierName |Console| [20..27)
        ///                             Token IdentifierToken |Console| [20..27)  Parent.Kind=IdentifierName
        ///                         <Node IdentifierName |Console| [20..27)
        ///                         Token DotToken |.| [27..28)  Parent.Kind=MemberAccessExpression
        ///                         >Node IdentifierName |WriteLine| [28..35)
        ///                             Token IdentifierToken |WriteLine| [28..35)  Parent.Kind=IdentifierName
        ///                         <Node IdentifierName |WriteLine| [28..35)
        ///                     <Node MemberAccessExpression |Console.WriteLine| [20..35)
        ///                     >Node ArgumentList |("Hello")| [35..44)
        ///                         Token OpenParenToken |(| [35..36)  Parent.Kind=ArgumentList
        ///                         >Node Argument |"Hello"| [36..43)
        ///                             >Node StringLiteralExpression |"Hello"| [36..43)
        ///                                 Token StringLiteralToken |"Hello"| [36..43)  Parent.Kind=StringLiteralExpression
        ///                             <Node StringLiteralExpression |"Hello"| [36..43)
        ///                         <Node Argument |"Hello"| [36..43)
        ///                         Token CloseParenToken |)| [43..44)  Parent.Kind=ArgumentList
        ///                     <Node ArgumentList |("Hello")| [35..44)
        ///                 <Node InvocationExpression |Console.WriteLine("Hello")| [20..44)
        ///                 Token SemicolonToken |;| [44..45)  Parent.Kind=ExpressionStatement
        ///             <Node ExpressionStatement |Console.WriteLine("Hello");| [20..45)
        ///             Token CloseBraceToken |}| [45..46)  Parent.Kind=Block
        ///         <Node Block |{Console.WriteLine("Hello");}| [19..46)
        ///     <Node MethodDeclaration |static void Main() {Console.WriteLine("Hello");}| [0..46)
        ///     Token EndOfFileToken || [46..46)  Parent.Kind=CompilationUnit
        /// <Node CompilationUnit |static void Main() {Console.WriteLine("Hello");}| [0..46)
        /// ]]>
        /// </code>
        /// </remarks>
        /// <param name="treeElement">A <see cref="SyntaxTreeElement"/> instance that contains the wrapped syntax tree element
        /// currently processed.</param>
        private void RenderElementCallback(SyntaxTreeElement treeElement)
        {
            Writer.Write("{0}{2}{1}", 
                         Indent, 
                         treeElement,
                         treeElement.IsNode ? '\u25BA'.ToString() : string.Empty);
            if (treeElement.IsNode)
            {
                Indent = new string(' ', 4 * (++NestingLevel));
            }
            else if (treeElement.IsToken)
            {
                SyntaxToken token = treeElement.Token;
                StringBuilder triviaInfo = new StringBuilder();
                
                if (token.HasLeadingTrivia)
                {
                    SyntaxTriviaList triviaList = token.LeadingTrivia;
                    triviaInfo.AppendFormat(" {0} leading trivia", triviaList.Count());
                    if (triviaList.Any(t => t.HasStructure))
                    {
                        triviaInfo.AppendFormat(" with {0} structured and the first is \"{1}\"", 
                                                triviaList.Count(t => t.HasStructure),
                                                triviaList.First(t => t.HasStructure).ToString());
                    }
                }

                if (token.HasTrailingTrivia)
                {
                    SyntaxTriviaList triviaList = token.TrailingTrivia;
                    triviaInfo.AppendFormat(" {0} trailing trivia", triviaList.Count());
                    if (triviaList.Any(t => t.HasStructure))
                    {
                        triviaInfo.AppendFormat(" with {0} structured and the first is \"{1}\"",
                                                triviaList.Count(t => t.HasStructure),
                                                triviaList.First(t => t.HasStructure).ToString());
                    }
                }

                Writer.Write(triviaInfo.ToString());

                if (token.Parent != null)
                {
                    Writer.Write(string.Format("  Parent.Kind={0}", token.Parent.Kind()));
                }
            }
            else if (treeElement.IsTrivia)
            {
                Writer.Write(string.Format("  for Token {0}", treeElement.Trivia.Token.Kind()));
            }

            Writer.WriteLine();
        }
    }
}
