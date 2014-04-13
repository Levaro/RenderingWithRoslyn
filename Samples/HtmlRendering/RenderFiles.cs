using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Levaro.Roslyn;
using Levaro.Roslyn.Renderers;

using Microsoft.CodeAnalysis;

namespace HtmlRendering
{
    /// <summary>
    /// Console application that generates HTML files for all C# code files in the Levaro.Roslyn source folder.
    /// </summary>
    internal class RenderFiles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderFiles"/> class.
        /// </summary>
        /// <param name="sourceFolderPath">The source folder path where the source code resides.</param>
        /// <param name="targetFolderPath">The target folder path where the generated HTML files reside.</param>
        private RenderFiles(string sourceFolderPath, string targetFolderPath)
        {
            SourceFolderPath = sourceFolderPath;
            TargetFolderPath = targetFolderPath;
        }

        /// <summary>
        /// Gets or sets the full path of the folder where the C# source files reside.
        /// </summary>
        /// <remarks>
        /// All sub-folders except "obj" and "bin" are checked for source files in addition to the specified folder. The folder
        /// must exist.
        /// </remarks>
        /// <value>
        /// The source folder path.
        /// </value>
        private string SourceFolderPath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full path of the folder where the generated HTML files are placed.
        /// </summary>
        /// <value>
        /// The target folder path.
        /// </value>
        private string TargetFolderPath
        {
            get;
            set;
        }

        /// <summary>
        /// The main entry point for this console application.
        /// </summary>
        /// <remarks>
        /// This simple application renders each of the C# source code files as HTML files. The default configuration for the
        /// <see cref="HtmlRenderer"/> is used except that line numbers are included. Metadata references are included
        /// for the source files in the Roslyn access (Levaro.Roslyn) folder - they should be altered or removed if using a
        /// different source folder. The source and target folders are specified for the Roslyn access source files - to execute
        /// this from Visual Studio, make HtmlRendering the StartUp Project and click Ctrl+F5 (Build | Start Without Debugging).
        /// </remarks>
        internal static void Main()
        {
            // TODO: Include a general command line parsing mechanism to allow full configuration from the command line.
            RenderFiles renderFiles = new RenderFiles(Path.GetFullPath(@"..\..\..\..\Source\Levaro.Roslyn"),
                                                      Path.GetFullPath(@"..\..\Generated Html"));
            List<string> sourceFiles = Directory.GetFiles(renderFiles.SourceFolderPath, "*.cs", SearchOption.AllDirectories)
                                                .Where(f => !(f.Contains("obj") || f.Contains("bin")))
                                                .ToList();
            Console.WriteLine("Reading {0} C# code files from \"{1}\" and subfolders\r\nCreating HTML files in \"{2}\"\r\nUsing the default CSS styles.",
                              sourceFiles.Count,
                              renderFiles.SourceFolderPath,
                              renderFiles.TargetFolderPath);
            Console.WriteLine();

            string defaultCss = string.Empty;
            Assembly assembly = typeof(CodeWalker).Assembly;
            using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream("Levaro.Roslyn.Renderers.ListStyles.css")))
            {
                defaultCss = reader.ReadToEnd();
            }

            // The PageTemplate.html is a small HTML file having {{text}} that is replaced to create a stand-alone page for the
            // generated HTML code. The default style sheet (see ListStyle.css in Levaro.Roslyn.Renderers) is an embedded
            // resource and is recovered and "inserted" in the pageTemplate.
            string pageTemplate = File.ReadAllText("PageTemplate.html").Replace("{{Styles}}", defaultCss);

            // The HTML renderer uses all the defaults except that line numbers are included. Metadata references should be
            // altered to reflect that code to render. This is currently set to render the Levaro.Roslyn code in this project.
            // You should change or remove appropriate for your needs.
            // TODO: All the assemblies to be specified from the command line.
            HtmlRenderer renderer = new HtmlRenderer();
            renderer.IncludeLineNumbers = true;
            Assembly roslyn = typeof(CodeWalker).Assembly;
            renderer.MetadataReferences.Add(new MetadataFileReference(assembly.Location));
            roslyn.GetReferencedAssemblies()
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
                string outputFilePath = string.Format("{0}\\{1}.html", renderFiles.TargetFolderPath, fileName);
                File.WriteAllText(outputFilePath, renderedContents);

                Console.WriteLine("{0}.html created.", fileName);
            }
        }
    }
}
