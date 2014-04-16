using System.Collections.Generic;
using System.IO;

using Microsoft.CodeAnalysis;

namespace Levaro.CSharp.Display.Renderers
{
    /// <summary>
    /// Defines the properties and methods that are shared by all classes that can render C# code using the associated (Roslyn) C#
    /// <c>SyntaxTree</c> and <c>SemanticModel</c> instances. Implementations typically construct the <c>SyntaxTree</c> from 
    /// the C# and construct the <c>SemanticModel</c> if needed.
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// Gets the assembly metadata references used to construct the <c>SemanticModel</c> for the C# code.
        /// </summary>
        /// <value>
        /// The metadata references as a collection of <see cref="MetadataReference"/> instances. References are added by
        /// recovering the property and adding a new reference. Concrete implementations should initialize the collection, 
        /// for example by including the "mscorlib" assembly or as just the empty collection.
        /// </value>
        ICollection<MetadataReference> MetadataReferences
        {
            get;
        }

        /// <summary>
        /// Renders the C# code recovered from the <see cref="Stream"/> to the specified <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <c>TextWriter</c> which contains the rendered code. It is the responsibility of
        /// the caller to dispose of the <paramref name="writer"/> when it is no longer needed.</param>
        /// <param name="codeStream">The code stream; it is disposed when the code text is recovered.</param>
        void Render(TextWriter writer, Stream codeStream);

        /// <summary>
        /// Renders the C# code text to the specified <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <c>TextWriter</c> which contains the rendered code. It is the responsibility of
        /// the caller to dispose of the <paramref name="writer"/> when it is no longer needed.</param>
        /// <param name="codeText">The code text as a <see cref="string"/> instance.</param>
        void Render(TextWriter writer, string codeText);

        /// <summary>
        /// Renders the C# code recovered from the <see cref="Stream"/> and returns the results as a string.
        /// </summary>
        /// <param name="codeStream">The code stream; it is disposed when the code text is recovered.</param>
        /// <returns>
        /// A string of the rendered text; it could be empty but is never <c>null</c>.
        /// </returns>
        string Render(Stream codeStream);

        /// <summary>
        /// Renders the C# code text and returns the result as a string.
        /// </summary>
        /// <param name="codeText">The code text as a <see cref="string"/> instance.</param>
        /// <returns>
        /// A string of the rendered text; it can be empty but is never <c>null</c>.
        /// </returns>
        string Render(string codeText);
    }
}