using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Levaro.Roslyn
{
    /// <summary>
    /// Represents a visitor class that descends an entire <see cref="CSharpSyntaxNode"/> graph visiting each <c>SyntaxNode</c> 
    /// and its child Syntax Nodes and Syntax Tokens in depth-first order.
    /// </summary>
    /// <remarks>
    /// The class is a subclass of <see cref="CSharpSyntaxWalker"/> which itself subclasses the <c>CSharpSyntaxVisitor</c> by 
    /// primarily overriding the <see cref="CSharpSyntaxWalker.DefaultVisit"/> method. The 
    /// <see cref="CSharpSyntaxWalker.DefaultVisit"/> method then calls the visit method for child tokens and nodes. Ultimately 
    /// the <c>CSharpSyntaxWalker</c> visit methods return control to the <c>DefaultVisit</c> method to provide the depth-first 
    /// transversal of the syntax tree.
    /// <para>
    /// This is the visitor implementation that allows viewing the entire structure of a <see cref="CSharpSyntaxTree"/> instance.
    /// The renderers use this class to "walk the tree" and convert the node, token and trivia objects to a renderable format, for
    /// example text or HTML. See the <see cref="Levaro.Roslyn.Renderers.CoreRenderer.Render(System.IO.TextWriter, string)"/> 
    /// for an example.
    /// </para>
    /// </remarks>
    public class CodeWalker : CSharpSyntaxWalker
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeWalker"/> class and ensures that all nodes, tokens and trivia are
        /// visited by setting the <see cref="SyntaxWalkerDepth"/> of the base <see cref="SyntaxWalker"/> class.
        /// </summary>
        public CodeWalker() : base(SyntaxWalkerDepth.StructuredTrivia)
        {
        }

        /// <summary>
        /// Occurs when either a syntax node, token or trivia is visited during the syntax tree transversal.
        /// </summary>
        public event EventHandler<SyntaxVisitingEventArgs> SyntaxVisiting;

        /// <summary>
        /// Overrides the base <see cref="CSharpSyntaxWalker.DefaultVisit"/> class by simply raising events when the method 
        /// is entered and then left.
        /// </summary>
        /// <remarks>
        /// Because <c>DefaultVisit</c> is called recursively as the tree is transversed, the event raised when entering the method 
        /// and then when left can be separated by other raised events as child nodes and tokens are processed. The base method
        /// visits the child nodes and tokens.
        /// </remarks>
        /// <param name="node">The <see cref="SyntaxNode"/> instance currently visited.</param>
        public override void DefaultVisit(SyntaxNode node)
        {
            OnSyntaxVisiting(new SyntaxVisitingEventArgs(node, SyntaxVisitingState.EnteringNode), SyntaxVisiting);
            base.DefaultVisit(node);
            OnSyntaxVisiting(new SyntaxVisitingEventArgs(node, SyntaxVisitingState.LeavingNode), SyntaxVisiting);
        }

        /// <summary>
        /// The base <see cref="CSharpSyntaxWalker.VisitToken"/> is not called, so that an event can be raised between when the 
        /// leading and trailing trivia are visited.
        /// </summary>
        /// <remarks>
        /// The method simply visits each of the leading and trailing trivia for the token using the base methods that
        /// visit each of the trivia elements. The base <c>VisitToken</c> method is overridden so that the leading trivia is
        /// processed before the event is raised and then the trailing trivia.
        /// </remarks>
        /// <param name="token">The <see cref="SyntaxToken"/> object whose trivia are visited; first the leading and then the
        /// trailing <see cref="SyntaxTrivia"/> objects.</param>
        public override void VisitToken(SyntaxToken token)
        {
            VisitLeadingTrivia(token);
            OnSyntaxVisiting(new SyntaxVisitingEventArgs(token, SyntaxVisitingState.Token), SyntaxVisiting);
            VisitTrailingTrivia(token);
        }

        /// <summary>
        /// Visits the <see cref="SyntaxTrivia"/> object.
        /// </summary>
        /// <remarks>
        /// If the trivia object doesn't have structure, the <see cref="SyntaxVisiting"/> event is raised; otherwise the base
        /// <see cref="CSharpSyntaxWalker.VisitTrivia"/> method visits each of the <see cref="SyntaxNode"/> objects which comprise
        /// the structure for the trivia.
        /// </remarks>
        /// <param name="trivia">The visited <c>SyntaxTrivia</c> object.</param>
        public override void VisitTrivia(SyntaxTrivia trivia)
        {
            if (!trivia.HasStructure)
            {
                OnSyntaxVisiting(new SyntaxVisitingEventArgs(trivia, SyntaxVisitingState.Trivia), SyntaxVisiting);
            }
            else
            {
                base.VisitTrivia(trivia);
            }
        }

        /// <summary>
        /// Raises the <see cref="SyntaxVisiting" /> event.
        /// </summary>
        /// <param name="eventArgs">The <see cref="SyntaxVisitingEventArgs"/> instance containing the event data.</param>
        /// <param name="syntaxVisitingEvent">The syntax visiting event.</param>
        protected virtual void OnSyntaxVisiting(SyntaxVisitingEventArgs eventArgs, EventHandler<SyntaxVisitingEventArgs> syntaxVisitingEvent)
        {
            EventHandler<SyntaxVisitingEventArgs> eventHandler = syntaxVisitingEvent;
            if (eventHandler != null)
            {
                eventHandler(this, eventArgs);
            }
        }
    }
}
