using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Levaro.CSharp.Display;
using Levaro.CSharp.Display.Renderers;
using Microsoft.CodeAnalysis;

namespace HtmlRendering
{
    /// <summary>
    /// Console application that generates HTML files for all C# code files in the Levaro.CSharp.Display source folder.
    /// </summary>
    internal class RenderFiles
    {
        /// <summary>
        /// The main entry point for this console application.
        /// </summary>
        /// <remarks>
        /// This simple application renders each of the C# source code files as HTML files. The default configuration for the
        /// <see cref="HtmlRenderer"/> is used except that line numbers are included. Metadata references are included
        /// for the source files in the access (Levaro.CSharp.Display) folder - they should be altered or removed if using a
        /// different source folder. The source and target folders are specified for the C# source code files - to execute
        /// this from Visual Studio, make HtmlRendering the StartUp Project and click Ctrl+F5 (Build | Start Without Debugging).
        /// </remarks>
        internal static void Main()
        {
            // TODO: Include a general command line parsing mechanism to allow full configuration from the command line.
            string sourceFolderPath = Path.GetFullPath(@"..\..\..\..\Source\Levaro.CSharp.Display");
            string targetFolderPath = Path.GetFullPath(@"..\..\Generated Html");
            List<string> sourceFiles = Directory.GetFiles(sourceFolderPath, "*.cs", SearchOption.AllDirectories)
                                                .Where(f => !(f.Contains("obj") || f.Contains("bin")))
                                                .ToList();

            Console.WriteLine("Reading {0} C# code files from \"{1}\" and subfolders\r\nCreating HTML files in \"{2}\"\r\nUsing the default CSS styles.",
                              sourceFiles.Count,
                              sourceFolderPath,
                              targetFolderPath);
            Console.WriteLine();

            string defaultCss = string.Empty;
            Assembly assembly = typeof(CodeWalker).Assembly;

            // If you want the style sheet to be readable in the generated files, use the non-minified version, ListStyles.css.
            string resourceName = "Levaro.CSharp.Display.Renderers.ListStyles.min.css";
            using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)))
            {
                defaultCss = reader.ReadToEnd();
            }

            // Uncomment the two code lines if you access the ListStyle.css resource in order to remove all the comment and blank
            // lines, of course this is not necessary when recovering the minified version.
            // The ListStyles.css style sheet has a lot of comments that are not necessary, so remove those and any blank lines just
            // so the generated HTML is not any bigger than it need be. First the CSS comments "/*...*/":
            // string noComments = Regex.Replace(defaultCss, @"(/\*.*?\*/)", string.Empty, RegexOptions.Singleline);
            // And now any blank lines (some may have been created when removing comments too!):
            // defaultCss = Regex.Replace(noComments, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);

            // The PageTemplate.html is a small HTML file having {{text}} that is replaced to create a stand-alone page for the
            // generated HTML code. The default style sheet (see ListStyle.css or ListStyle.min.css in 
            // Levaro.CSharp.Display.Renderers) is an embedded resource and is recovered and "inserted" in the pageTemplate.
            string pageTemplate = File.ReadAllText("PageTemplate.html").Replace("{{Styles}}", defaultCss);

            // The HTML renderer uses all the defaults except that line numbers are included. Metadata references should be
            // altered to reflect the code to render. This is currently set to render the Levaro.CSharp.Display code in this project.
            // You should change or remove appropriate for your needs.
            // TODO: Allow the assemblies to be specified from the command line.
            HtmlRenderer renderer = new HtmlRenderer();
            renderer.IncludeLineNumbers = true;
            Assembly csharpDisplay = typeof(CodeWalker).Assembly;
            renderer.MetadataReferences.Add(new MetadataFileReference(assembly.Location));
            csharpDisplay.GetReferencedAssemblies()
                         .ToList()
                         .ForEach(a => renderer.MetadataReferences.Add(new MetadataFileReference(Assembly.Load(a).Location)));

            foreach (string source in sourceFiles)
            {
                FileInfo fileInfo = new FileInfo(source);
                int extensionIndex = fileInfo.Name.IndexOf(".cs");
                string fileName = fileInfo.Name.Substring(0, extensionIndex);

                Console.Write("Reading {0} and rendering to HTML ... ", fileInfo.Name);

                string renderedContents = pageTemplate.Replace("{{FileName}}", fileName);
                string codeText = File.ReadAllText(source);
                string htmlCode = renderer.Render(codeText);
                renderedContents = renderedContents.Replace("{{Contents}}", htmlCode);
                string outputFilePath = string.Format("{0}\\{1}.html", targetFolderPath, fileName);
                File.WriteAllText(outputFilePath, renderedContents);

                Console.WriteLine("{0}.html created.", fileName);
            }
        }
    }
}
