using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Levaro.CSharp.Display.Renderers
{
    /// <summary>
    /// Encapsulates the .NET Compiler Platform (Roslyn) services needed to traverse the syntax tree generated from C# code and 
    /// render the syntax nodes, tokens and trivia as they are encountered.
    /// </summary>
    /// <remarks>
    /// To create a renderer, subclass this class and the constructor should call the base constructor. At the very minimum, the
    /// subclass should specify a <see cref="Callback"/> delegate which uses the passed parameter to generate text for the 
    /// syntax element items that are passed to it. <see cref="SyntaxTreeRenderer"/> and <see cref="HtmlRenderer"/> are 
    /// examples that illustrate this. This isolates the tree traversal mechanism from the rendering itself.
    /// </remarks>
    public abstract class CoreRenderer : IRenderer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoreRenderer"/> class.
        /// </summary>
        /// <remarks>
        /// The <see cref="MetadataReferences"/> collection is initialized to contain the single metadata reference for the 
        /// "mscorlib" assembly. Because you cannot initialize <c>MetadataReferences</c> in a subclass, if you don't want this
        /// reference (very rare), you must remove it. References are added by calling add on the collection.
        /// </remarks>
        protected CoreRenderer()
        {
            // Initialize the metadata reference list with the standard mscorlib DLL. Note that because this is not a virtual
            // property, there is no danger is doing this from the constructor.
            MetadataReferences = new List<MetadataReference>();
            MetadataReference mscorlib = MetadataReference.CreateFromAssembly(typeof(object).Assembly);
            MetadataReferences.Add(mscorlib);
        }

        /// <summary>
        /// Gets the <see cref="TextWriter"/> used to write the rendered text.
        /// </summary>
        /// <value>
        /// The writer is initialized from either a parameter passed to the <c>Render</c> method, or is initialized to a 
        /// <see cref="StringWriter"/>.
        /// </value>
        protected virtual TextWriter Writer
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the syntax tree constructed when a <c>Render</c> method is called.
        /// </summary>
        /// <value>
        /// The syntax tree as an instance of the <see cref="SyntaxTree"/> class.
        /// </value>
        protected virtual SyntaxTree SyntaxTree
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the semantic model constructed when one of the <c>Render</c> methods is called.
        /// </summary>
        /// <value>
        /// An instance of the <see cref="SemanticModel"/> which can be used to recover additional symbol information for tokens,
        /// nodes and trivia. The model is based upon both the <see cref="SyntaxTree"/> and the <see cref="MetadataReference"/>
        /// objects in the collection <see cref="MetadataReferences"/>.
        /// </value>
        protected virtual SemanticModel SemanticModel
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the callback delegate which is called from the <see cref="Render(TextWriter, string)"/> method
        /// when any node, token or trivia is visited in the <see cref="SyntaxTree"/>.
        /// </summary>
        /// <value>
        /// The callback delegate; it is initially null. It should be initialized by concrete subclasses (renderers).
        /// </value>
        protected virtual Action<SyntaxTreeElement, SyntaxVisitingState> Callback
        {
            get;
            set;
        }

        #region IRenderer implementation ...
        /// <summary>
        /// Gets the assembly metadata references used to construct the <c>SemanticModel</c> for the C# code.
        /// </summary>
        /// <value>
        /// The metadata references as a collection of 
        /// <see cref="MetadataReference" /> instances. References are added by recovering the property and adding a new reference. 
        /// The collection is initialized to include just the "mscorlib" assembly.
        /// </value>
        [SuppressMessage("StyleCode.CSharp.OrderingRules",
                         "SA1202:ElementsMustBeOrderedByAccess",
                         Justification = "Keep interface implementation together")]
        public ICollection<MetadataReference> MetadataReferences
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Renders the C# code text to the specified <see cref="TextWriter" />.
        /// </summary>
        /// <remarks>
        /// The version of the <c>Render</c> method does all of the work because the other versions ultimately call this one. The
        /// steps involved are:
        /// <list type="number">
        /// <item><description>
        ///     The code text is parsed to produce a <c>SyntaxTree</c>
        /// </description></item>
        /// <item><description>
        ///     A compilation unit is created from the <c>SyntaxTree</c> and <c>MetadataReferences</c> properties and from that
        ///     the <c>SemanticModel</c> is initialized. Note that renderers that are only interested in the syntax need not 
        ///     access the semantic mode, but the HTML renderer does need this information to determine if an identifier represents
        ///     a symbol that should be (color) formatted.
        /// </description></item>
        /// <item><description>
        ///     The <c>SyntaxVisiting</c> event is handled by invoking the <c>Callback</c> passing the <c>SyntaxTreeElement</c>
        ///     object and <c>VisitingState</c> enumeration value encapsulated in the <c>SyntaxVisitingEventArgs</c> object.
        ///     Concrete implementations due the actual rendering from the callback delegate.
        /// </description></item>
        /// <item><description>
        ///     Any prefix text is recovered using the <c>GetPrefixText</c> method and is written to the <c>Writer</c>.
        /// </description></item>
        /// <item><description>
        ///     The syntax tree root node is passed to the <c>SyntaxWalker.Visit</c> method (really the overridden 
        ///     <c>SyntaxVisitor.Visit</c> method. This causes the callback delegate to be called as the tree is traversed. From
        ///     the callback delegate, concrete implementations can write to the <c>Writer</c> to render the nodes, tokens and
        ///     trivia that make up the syntax tree.
        /// </description></item>
        /// <item><description>
        ///     Finally, any postfix text is recovered using the <c>GetPostfixText</c> method and is written to the <c>Writer</c>.
        /// </description></item>
        /// </list>
        /// </remarks>
        /// <param name="writer">The <c>TextWriter</c> which contains the rendered code. It is the responsibility of the 
        /// caller to dispose of the <paramref name="writer" /> when it is no longer needed. </param>
        /// <param name="codeText">The code text as a <see cref="string" /> instance.</param>
        /// <exception cref="ArgumentNullException">if <paramref name="writer"/> is null.</exception>
        public virtual void Render(TextWriter writer, string codeText)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer), "A non-null TextWriter is required.");
            }

            Writer = writer;
            SyntaxTree = CSharpSyntaxTree.ParseText(codeText ?? string.Empty);

            // TODO: Formatting no longer provides a extension method on nodes. Move the entire functionality to another namespace.
            // if (FormatCode)
            // {
            //     IFormattingResult formattingResult = SyntaxTree.GetRoot().Format(FormattingOptions.GetDefaultOptions());
            //     CompilationUnitSyntax formattedRoot = (CompilationUnitSyntax)formattingResult.GetFormattedRoot();
            //     SyntaxTree = SyntaxTree.Create(formattedRoot);
            // }
            Compilation compilation = CSharpCompilation.Create("CoreRenderer",
                                                               syntaxTrees: new List<SyntaxTree> { SyntaxTree },
                                                               references: MetadataReferences);
            SemanticModel = compilation.GetSemanticModel(SyntaxTree);

            CodeWalker walker = new CodeWalker();
            walker.SyntaxVisiting += (s, e) =>
            {
                if (Callback != null)
                {
                   Callback(e.SyntaxTreeElement, e.State);
                }
            };

            Writer.Write(GetPrefixText());
            walker.Visit(SyntaxTree.GetRoot());
            Writer.Write(GetPostfixText());
        }

        /// <summary>
        /// Renders the C# code recovered from the <see cref="Stream" /> to the specified <see cref="TextWriter" />.
        /// </summary>
        /// <remarks>
        /// This version simply recovers the code text from the stream and calls <see cref="Render(TextWriter, string)"/>.
        /// </remarks>
        /// <param name="writer">The 
        /// <c>TextWriter</c> which contains the rendered code. It is the responsibility of
        /// the caller to dispose of the 
        /// <paramref name="writer" /> when it is no longer needed.</param>
        /// <param name="codeStream">The code stream; it is disposed when the code text is recovered.</param>
        /// <seealso cref="Render(TextWriter, string)"/>
        public virtual void Render(TextWriter writer, Stream codeStream)
        {
            string codeText = string.Empty;
            using (StreamReader reader = new StreamReader(codeStream))
            {
                codeText = reader.ReadToEnd();
            }

            Render(writer, codeText);
        }

        /// <summary>
        /// Renders the C# code text and returns the result as a string.
        /// </summary>
        /// <remarks>
        /// This method creates a <see cref="StringWriter"/> and uses that to call <see cref="Render(TextWriter, string)"/>.
        /// The returned string is recovered from the string writer's underlying <c>StringBuilder</c> object and then the
        /// string writer is disposed.
        /// </remarks>
        /// <param name="codeText">The code text as a <see cref="string" /> instance.</param>
        /// <returns>
        /// A string of the rendered text; it can be empty but is never <c>null</c>.
        /// </returns>
        public virtual string Render(string codeText)
        {
            string renderedText = string.Empty;
            using (Writer = new StringWriter())
            {
                Render(Writer, codeText);
                renderedText = ((StringWriter)Writer).GetStringBuilder().ToString();
            }

            return renderedText;
        }

        /// <summary>
        /// Renders the C# code recovered from the <see cref="Stream" /> and returns the results as a string.
        /// </summary>
        /// <remarks>
        /// This version simply recovers the code text from the stream and calls <see cref="Render(string)"/>.
        /// </remarks>
        /// <param name="codeStream">The code stream; it is disposed when the code text is recovered.</param>
        /// <returns>
        /// A string of the rendered text; it could be empty but is never <c>null</c>.
        /// </returns>
        public virtual string Render(Stream codeStream)
        {
            string codeText = string.Empty;
            using (StreamReader reader = new StreamReader(codeStream))
            {
                codeText = reader.ReadToEnd();
            }

            return Render(codeText);
        }
        #endregion IRenderer implementation

        /// <summary>
        /// Gets the text that is written to <see cref="Writer"/> before the <see cref="SyntaxTree"/> elements are processed by
        /// the <see cref="Callback"/> delegate.
        /// </summary>
        /// <remarks>
        /// Override this method to display text before the code is rendered.
        /// </remarks>
        /// <returns>Text to render before any processing; this implementation returns the empty string.</returns>
        [SuppressMessage("Microsoft.Design", 
                         "CA1024:UsePropertiesWhereAppropriate",
                         Justification = "This method could contain complex code which is not appropriate for a property")]
        protected virtual string GetPrefixText()
        {
            return string.Empty;
        }

        /// <summary>
        /// Gets the text that is written to <see cref="Writer"/> after the <see cref="SyntaxTree"/> elements are processed by
        /// the <see cref="Callback"/> delegate.
        /// </summary>
        /// <remarks>
        /// Override this method to display text after the code is rendered.
        /// </remarks>
        /// <returns>Text to render after any processing; this implementation returns the empty string.</returns>
        [SuppressMessage("Microsoft.Design",
                         "CA1024:UsePropertiesWhereAppropriate",
                         Justification = "This method could contain complex code which is not appropriate for a property")]
        protected virtual string GetPostfixText()
        {
            return string.Empty;
        }
    }
}