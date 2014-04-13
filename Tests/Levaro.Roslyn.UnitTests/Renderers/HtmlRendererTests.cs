using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Levaro.Roslyn.Renderers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Levaro.Roslyn.UnitTests.Renderers
{
    /// <summary>
    /// Test fixture for the HTML renderer
    /// </summary>
    [TestClass]
    public class HtmlRendererTests
    {
        /// <summary>
        /// Checks that the ListStyles.css class is an embedded resource in the Levaro.Roslyn assembly.
        /// </summary>
        [TestMethod]
        [DeploymentItem(@"..\..\..\..\Source\Levaro.Roslyn\Renderers\ListStyles.css", ".")]
        public void ListStylesTest()
        {
            Assembly roslyn = typeof(CodeWalker).Assembly;
            string embeddedStyleSheet = string.Empty;
            using (StreamReader reader = new StreamReader(roslyn.GetManifestResourceStream("Levaro.Roslyn.Renderers.ListStyles.css")))
            {
                embeddedStyleSheet = reader.ReadToEnd();
            }

            string cssFile = File.ReadAllText("ListStyles.css");
            Assert.AreEqual<string>(cssFile, embeddedStyleSheet);
        }

        /// <summary>
        /// Renders the simplest of code and checks that the results are correct.
        /// </summary>
        [TestMethod]
        public void HtmlRendererTest()
        {
            string code = @"namespace SimpleCode
{
    internal sealed class Program
    {
        internal static Main()
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}";
            string expectedDefaultText = @"

<div class=""CodeContainer"">
    <ul>
        <li class=""Alternate""><span class=""Keyword"">namespace</span> SimpleCode</li>
        <li>{</li>
        <li class=""Alternate"">    <span class=""Keyword"">internal</span> <span class=""Keyword"">sealed</span> <span class=""Keyword"">class</span> <span class=""Identifier"">Program</span></li>
        <li>    {</li>
        <li class=""Alternate"">        <span class=""Keyword"">internal</span> <span class=""Keyword"">static</span> <span class=""Keyword""></span>Main()</li>
        <li>        {</li>
        <li class=""Alternate"">            <span class=""Identifier"">Console</span>.<span class=""Identifier"">WriteLine</span>(<span class=""StringLiteral"">&quot;Hello, World!&quot;</span>);</li>
        <li>        }</li>
        <li class=""Alternate"">    }</li>
        <li>}</li>
    </ul>
</div>";
            HtmlRenderer htmlRenderer = new HtmlRenderer();
            string renderedText = htmlRenderer.Render(code);
            renderedText = Regex.Replace(renderedText, "<!--.*?-->", string.Empty);

            Assert.AreEqual<string>(expectedDefaultText, renderedText);
        }
    }
}
