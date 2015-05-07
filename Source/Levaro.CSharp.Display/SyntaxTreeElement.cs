using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Levaro.CSharp.Display
{
    /// <summary>
    /// A convenience class that wraps <see cref="SyntaxNode"/>, <see cref="SyntaxToken"/> and <see cref="SyntaxTrivia"/> instances
    /// and exposes common properties of these syntax tree element types.
    /// </summary>
    public sealed class SyntaxTreeElement
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="SyntaxTreeElement"/> class from being created.
        /// </summary>
        /// <remarks>
        /// The static <see cref="Create"/> method calls this constructor to initialize the instance properties. Instances
        /// are only created using the <c>Create</c> method.
        /// </remarks>
        private SyntaxTreeElement()
        {
            Node = null;
            Token = new SyntaxToken();
            Trivia = new SyntaxTrivia();
            SyntaxKind = SyntaxKind.None;
            SyntaxElementCategory = SyntaxElementCategory.Unknown;
            Text = string.Empty;
            Span = new TextSpan();
        }

        /// <summary>
        /// Gets an instance whose properties are set to default values and does not represent a wrap a valid syntax tree element.
        /// </summary>
        /// <value>
        /// An empty <see cref="SyntaxTreeElement"/>.
        /// </value>
        /// <seealso cref="IsEmpty"/>
        public static SyntaxTreeElement Empty
        {
            get
            {
                return new SyntaxTreeElement();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <remarks>
        /// An instance is empty if the <see cref="SyntaxElementCategory"/> property is <c>Unknown</c> and the <see cref="Span"/>
        /// property is empty.
        /// </remarks>
        /// <value>
        /// <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty
        {
            get
            {
                return (SyntaxElementCategory == SyntaxElementCategory.Unknown) && Span.IsEmpty;
            }
        }
        
        /// <summary>
        /// Gets the syntax element category for this instance.
        /// </summary>
        /// <value>
        /// The syntax element category as a value of the <see cref="SyntaxElementCategory"/> enumeration.
        /// </value>
        public SyntaxElementCategory SyntaxElementCategory
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether this instance wraps a <see cref="SyntaxNode"/> object.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance wraps a syntax node; otherwise, <c>false</c>.
        /// </value>
        public bool IsNode
        {
            get
            {
                return Node != null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance wraps a <see cref="SyntaxToken"/> object.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance wraps a syntax token; otherwise, <c>false</c>.
        /// </value>
        public bool IsToken
        {
            get
            {
                return !(Token.IsKind(SyntaxKind.None) && (Token.SyntaxTree == null) && Token.FullSpan.IsEmpty);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance wraps a <see cref="SyntaxTrivia"/> object.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance wraps syntax trivia; otherwise, <c>false</c>.
        /// </value>
        public bool IsTrivia
        {
            get
            {
                return !(Trivia.IsKind(SyntaxKind.None) && (Trivia.SyntaxTree == null) && Trivia.FullSpan.IsEmpty);
            }
        }

        /// <summary>
        /// Gets the <see cref="SyntaxNode"/> object this instance wraps.
        /// </summary>
        /// <value>
        /// The syntax node object or <c>null</c> if <see cref="IsToken"/> is false.
        /// </value>
        public SyntaxNode Node
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the <see cref="SyntaxToken"/> this instance wraps
        /// </summary>
        /// <value>
        /// The syntax token. If <see cref="IsToken"/> is false, the token is empty and does not represent a valid syntax token.
        /// </value>
        public SyntaxToken Token
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the <see cref="SyntaxTrivia"/> this instance wraps
        /// </summary>
        /// <value>
        /// The syntax trivia. If <see cref="IsTrivia"/> is false, the trivia is empty and does not represent a valid syntax
        /// trivia item.
        /// </value>
        public SyntaxTrivia Trivia
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the kind of the wrapped syntax element.
        /// </summary>
        /// <value>
        /// The wrapped syntax element <c>Kind</c> property as a value of the <see cref="SyntaxKind"/> enumeration.
        /// </value>
        public SyntaxKind SyntaxKind
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the text representation of the wrapped syntax tree element.
        /// </summary>
        /// <value>
        /// The text of the wrapped element recovered from its <c>ToString</c> method.
        /// </value>
        public string Text
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the <see cref="TextSpan"/> object for the wrapped element.
        /// </summary>
        /// <value>
        /// The text span for the wrapped syntax tree element.
        /// </value>
        public TextSpan Span
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates an instance whose underlying syntax tree item is the specified object.
        /// </summary>
        /// <remarks>
        /// This method is the only way to create non-empty instances.
        /// </remarks>
        /// <param name="syntaxTreeItem">The syntax tree item to wrap.</param>
        /// <returns>A <see cref="SyntaxTreeElement"/> which wraps the specified <paramref name="syntaxTreeItem"/>
        /// syntax tree element. It is never <c>null</c>, but if <c>syntaxTreeItem</c> is either <c>null</c> or not a valid
        /// syntax node, token or trivia, the empty <c>SyntaxTreeElement</c> is returned.</returns>
        public static SyntaxTreeElement Create(object syntaxTreeItem)
        {
            SyntaxTreeElement element = new SyntaxTreeElement();

            if (syntaxTreeItem != null)
            {
                SyntaxNode node = syntaxTreeItem as SyntaxNode;
                if (node != null)
                {
                    element.Node = node;
                    element.SyntaxKind = element.Node.Kind();
                    element.Text = element.Node.ToString();
                    element.SyntaxElementCategory = SyntaxElementCategory.Node;
                    element.Span = element.Node.Span;
                }
                else if (syntaxTreeItem.GetType() == typeof(SyntaxToken))
                {
                    object token = syntaxTreeItem;
                    element.Token = (SyntaxToken)token;
                    element.SyntaxKind = element.Token.Kind();
                    element.Text = element.Token.ToString();
                    element.SyntaxElementCategory = SyntaxElementCategory.Token;
                    element.Span = element.Token.Span;
                }
                else if (syntaxTreeItem.GetType() == typeof(SyntaxTrivia))
                {
                    object trivia = syntaxTreeItem;
                    element.Trivia = (SyntaxTrivia)trivia;
                    element.SyntaxKind = element.Trivia.Kind();
                    element.Text = element.Trivia.ToString();
                    element.SyntaxElementCategory = SyntaxElementCategory.Trivia;
                    element.Span = element.Trivia.Span;
                }
            }

            return element;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that display the name, <see cref="SyntaxKind"/>, <see cref="Text"/>
        /// and <see cref="Span"/> properties. Within the text, carriage return/line feed and tab characters are escaped so
        /// these are not rendered as returns and tabs.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0} {1} |{2}| {3}", 
                                 SyntaxElementCategory,
                                 SyntaxKind, 
                                 Text.Replace("\r\n", "\\r\\n").Replace("\t", "\\t"),
                                 Span);
        }
    }
}