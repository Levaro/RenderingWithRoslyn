using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Levaro.CSharp.Display.Renderers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Levaro.CSharp.Display.UnitTests.Renderers
{
    /// <summary>
    /// Test fixture for the HTML renderer
    /// </summary>
    [TestClass]
    public class HtmlRendererTests
    {
        /// <summary>
        /// Checks that the ListStyles.css and ListStyles.min.css are an embedded resources in the Levaro.CSharp.Display assembly and the content data 
        /// are exactly that of the file.
        /// </summary>
        [TestMethod]
        [DeploymentItem(@"..\..\..\..\Source\Levaro.CSharp.Display\Renderers\ListStyles.css", ".")]
        [DeploymentItem(@"..\..\..\..\Source\Levaro.CSharp.Display\Renderers\ListStyles.min.css", ".")]
        public void ListStylesTest()
        {
            Assembly roslyn = typeof(CodeWalker).Assembly;
            string embeddedStyleSheet = string.Empty;
            using (StreamReader reader = new StreamReader(roslyn.GetManifestResourceStream("Levaro.CSharp.Display.Renderers.ListStyles.css")))
            {
                embeddedStyleSheet = reader.ReadToEnd();
            }

            string cssFile = File.ReadAllText("ListStyles.css");
            Assert.AreEqual<string>(cssFile, embeddedStyleSheet);

            using (StreamReader reader = new StreamReader(roslyn.GetManifestResourceStream("Levaro.CSharp.Display.Renderers.ListStyles.min.css")))
            {
                embeddedStyleSheet = reader.ReadToEnd();
            }

            cssFile = File.ReadAllText("ListStyles.css");
            Assert.AreEqual<string>(cssFile, embeddedStyleSheet);
        }

        /// <summary>
        /// Checks that tracing is active when a listener is present and no trace messages are written if an HtmlRenderer trace
        /// listener is not present.
        /// </summary>
        [TestMethod]
        public void HtmlRendererTraceTest()
        {
            using (StringWriter writer = new StringWriter())
            {
                TextWriterTraceListener listener = new TextWriterTraceListener(writer, "HtmlRenderer");
                {
                    Trace.Listeners.Add(listener);

                    // Force rendering so that tracing occurs.
                    (new HtmlRenderer()).Render("Console.WriteLine(\"Hello, TraceListener\");");
                    Assert.IsTrue(Trace.Listeners.OfType<TraceListener>().Any(l => l.Name == "HtmlRenderer"));

                    string traceContents = writer.GetStringBuilder().ToString();
                    Assert.IsTrue(traceContents.Length > 0);
                }

                Trace.Listeners.Remove("HtmlRenderer");
                writer.GetStringBuilder().Clear();

                listener = new TextWriterTraceListener(writer, "NotAnHtmlRenderer");
                {
                    Trace.Listeners.Add(listener);

                    // Force rendering so that tracing could occur if the right listener is added.
                    (new HtmlRenderer()).Render("Console.WriteLine(\"Hello, TraceListener\");");
                    Assert.IsFalse(Trace.Listeners.OfType<TraceListener>().Any(l => l.Name == "HtmlRenderer"));

                    string traceContents = writer.GetStringBuilder().ToString();
                    Assert.AreEqual<int>(0, traceContents.Length);
                }

                Trace.Listeners.Remove("NotAnHtmlRenderer");
            }
        }

        /// <summary>
        /// Renders the simplest of code and checks that the results are correct.
        /// </summary>
        [TestMethod]
        public void HtmlRendererTest()
        {
            // Notice that the code ends with an CR/LF -- the test checks that an empty line is displayed. That is, the
            // final post text closes the list tag properly. See the HtmlRenderer.GetPostText method for details.
            string code = @"namespace SimpleCode
{
    internal sealed class Program
    {
        internal static void Main()
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}
";
            string expectedDefaultText = @"

<div class=""CodeContainer"">
    <ul>
        <li data-lineNumber=""1""><span class=""Keyword"">namespace</span> SimpleCode</li>
        <li data-lineNumber=""2"" class=""Alternate"">{</li>
        <li data-lineNumber=""3"">    <span class=""Keyword"">internal</span> <span class=""Keyword"">sealed</span> <span class=""Keyword"">class</span> <span class=""Identifier"">Program</span></li>
        <li data-lineNumber=""4"" class=""Alternate"">    {</li>
        <li data-lineNumber=""5"">        <span class=""Keyword"">internal</span> <span class=""Keyword"">static</span> <span class=""Keyword"">void</span> Main()</li>
        <li data-lineNumber=""6"" class=""Alternate"">        {</li>
        <li data-lineNumber=""7"">            <span class=""InferredIdentifier"">Console</span>.WriteLine(<span class=""StringLiteral"">&quot;Hello, World!&quot;</span>);</li>
        <li data-lineNumber=""8"" class=""Alternate"">        }</li>
        <li data-lineNumber=""9"">    }</li>
        <li data-lineNumber=""10"" class=""Alternate"">}</li>
        <li data-lineNumber=""11"">&nbsp;</li>
    </ul>
</div>";
            HtmlRenderer htmlRenderer = new HtmlRenderer();
            htmlRenderer.IncludeDebugInfo = false;
            string renderedText = htmlRenderer.Render(code);
            renderedText = Regex.Replace(renderedText, "<!--.*?-->", string.Empty);

            Assert.AreEqual<string>(expectedDefaultText, renderedText);
        }
    }
}
